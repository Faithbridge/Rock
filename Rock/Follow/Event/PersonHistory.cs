﻿using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text.RegularExpressions;

using Rock;
using Rock.Data;
using Rock.Follow;
using Rock.Attribute;
using Rock.Model;
using Rock.Web.Cache;
using System.Collections.Generic;

namespace Rock.Follow.Event
{
    [Description( "Person History" )]
    [Export( typeof( EventComponent ) )]
    [ExportMetadata( "ComponentName", "PersonHistory" )]

    [TextField( "Fields", "Field name(s) to monitor in history data. Seperate multiple items by a comma. If you look at a person's history data it would be in the format of 'Modified FIELD value from OLD to NEW'.", true, order: 0 )]
    [IntegerField( "Max Days Back", "Maximum number of days back to look at a person's history.", true, 30, "", order: 1 )]

    [BooleanField( "Match Both", "Require a match on both the Old Value and the New Value. This equates to an AND comparison, otherwise it equates to an OR comparison on the values.", true, category: "Values", order: 0 )]
    [TextField( "Old Value", "Value to be matched as the old value or leave blank to match any old value.", false, category: "Values", order: 1 )]
    [TextField( "New Value", "Value to be matched as the new value or leave blank to match any new value.", false, category: "Values", order: 2 )]

    [BooleanField( "Negate Person", "Changes the Person match to a NOT Person match. If you want to trigger events only when it is NOT the specified person making the change then turn this option on.", category: "Changed By", order: 0 )]
    [PersonField( "Person", "Filter by the person who changed the value. This is always an AND condition with the two value changes. If the Negate Changed By option is also set then this becomes and AND NOT condition.", false, category: "Changed By", order: 1 )]
    public class PersonHistory : EventComponent
    {
        static readonly string AddedRegex = "Added.*<span class=['\"]field-name['\"]>(.*)<\\/span>.*<span class=['\"]field-value['\"]>(.*)<\\/span>";
        static readonly string ModifiedRegex = "Modified.*<span class=['\"]field-name['\"]>(.*)<\\/span>.*<span class=['\"]field-value['\"]>(.*)<\\/span>.*<span class=['\"]field-value['\"]>(.*)<\\/span>";
        static readonly string DeletedRegex = "Deleted.*<span class=['\"]field-name['\"]>(.*)<\\/span>.*<span class=['\"]field-value['\"]>(.*)<\\/span>";

        public override Type FollowedType
        {
            get
            {
                return typeof( PersonAlias );
            }
        }

        public override bool HasEventHappened( FollowingEventType followingEvent, IEntity entity, DateTime? lastNotified )
        {
            if ( followingEvent != null && entity != null )
            {
                var personAlias = entity as PersonAlias;

                if ( personAlias != null && personAlias.Person != null )
                {
                    //
                    // Get all the attributes/settings we need.
                    //
                    int daysBack = GetAttributeValue( followingEvent, "MaxDaysBack" ).AsInteger();
                    string targetOldValue = GetAttributeValue( followingEvent, "OldValue" ) ?? string.Empty;
                    string targetNewValue = GetAttributeValue( followingEvent, "NewValue" ) ?? string.Empty;
                    string targetPersonGuid = GetAttributeValue( followingEvent, "Person" );
                    bool negateChangedBy = GetAttributeValue( followingEvent, "NegatePerson" ).AsBoolean();
                    bool matchBothValues = GetAttributeValue( followingEvent, "MatchBoth" ).AsBoolean();
                    var attributes = GetAttributeValue( followingEvent, "Fields" ).Split( ',' ).Select( a => a.Trim() );

                    //
                    // Populate all the other random variables we need for processing.
                    //
                    PersonAlias targetPersonAlias = new PersonAliasService( new RockContext() ).Get( targetPersonGuid.AsGuid() );
                    DateTime daysBackDate = RockDateTime.Now.AddDays( -daysBack );
                    var person = personAlias.Person;
                    int personEntityTypeId = EntityTypeCache.Read( typeof( Person ) ).Id;
                    int categoryId = CategoryCache.Read( Rock.SystemGuid.Category.HISTORY_PERSON_DEMOGRAPHIC_CHANGES.AsGuid() ).Id;

                    //
                    // Start building the basic query. We want all History items that are for
                    // people objects and use the Demographic Changes category.
                    //
                    var qry = new HistoryService( new RockContext() ).Queryable()
                        .Where( h => h.EntityTypeId == personEntityTypeId && h.EntityId == person.Id && h.CategoryId == categoryId );

                    //
                    // Put in our limiting dates. We always limit by our days back date,
                    // and conditionally limit based on the last time we notified the
                    // stalker - I mean the follower.
                    //
                    if ( lastNotified.HasValue )
                    {
                        qry = qry.Where( h => h.CreatedDateTime >= lastNotified.Value );
                    }
                    qry = qry.Where( h => h.CreatedDateTime >= daysBackDate );

                    //
                    // Walk each history item found that matches our filter.
                    //
                    Dictionary<string, List<HistoryChange>> changes = new Dictionary<string, List<HistoryChange>>();
                    foreach ( var history in qry.ToList() )
                    {
                        Match modified = Regex.Match( history.Summary, ModifiedRegex );
                        Match added = Regex.Match( history.Summary, AddedRegex );
                        Match deleted = Regex.Match( history.Summary, DeletedRegex );

                        //
                        // Walk each attribute entered by the user to match against.
                        //
                        foreach ( var attribute in attributes )
                        {
                            HistoryChange change = new HistoryChange();
                            string title = null;

                            //
                            // Check what kind of change this was.
                            //
                            if ( modified.Success )
                            {
                                title = modified.Groups[1].Value;
                                change.Old = modified.Groups[2].Value;
                                change.New = modified.Groups[3].Value;
                            }
                            else if ( added.Success )
                            {
                                title = added.Groups[1].Value;
                                change.Old = string.Empty;
                                change.New = added.Groups[2].Value;
                            }
                            else if ( deleted.Success )
                            {
                                title = deleted.Groups[1].Value;
                                change.Old = deleted.Groups[2].Value;
                                change.New = string.Empty;
                            }

                            //
                            // Check if this is one of the attributes we are following.
                            //
                            if ( title != null && title.Trim() == attribute )
                            {
                                //
                                // Get the ValuePair object to work with.
                                //
                                if ( !changes.ContainsKey( attribute ) )
                                {
                                    changes.Add( attribute, new List<HistoryChange>() );
                                }

                                change.PersonId = history.CreatedByPersonId;
                                changes[attribute].Add( change );

                                //
                                // If the value has been changed back to what it was then ignore the change.
                                //
                                if ( changes[attribute].Count >= 2 )
                                {
                                    var changesList = changes[attribute].ToList();

                                    if ( changesList[changesList.Count - 2].Old == changesList[changesList.Count - 1].New )
                                    {
                                        changes.Remove( title );
                                    }
                                }
                            }
                        }
                    }

                    //
                    // Walk the list of final changes and see if we need to notify.
                    //
                    foreach ( var items in changes.Values )
                    {
                        foreach ( HistoryChange change in items )
                        {
                            //
                            // Check for a match on the person who made the change.
                            //
                            if ( targetPersonAlias == null
                                 || targetPersonAlias.Id == 0
                                 || ( !negateChangedBy && targetPersonAlias.PersonId == change.PersonId )
                                 || ( negateChangedBy && targetPersonAlias.PersonId != change.PersonId ) )
                            {
                                bool oldMatch = ( string.IsNullOrEmpty( targetOldValue ) || targetOldValue == change.Old );
                                bool newMatch = ( string.IsNullOrEmpty( targetNewValue ) || targetNewValue == change.New );

                                //
                                // If the old value and the new value match then trigger the event.
                                //
                                if ( ( matchBothValues && oldMatch && newMatch )
                                     || ( !matchBothValues && ( oldMatch || newMatch ) ) )
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Contains a list of changes for an attribute. This allows us to compile
        /// a list of changes and remove ones that were undone (e.g. in changes A, B, and C.
        /// Change C changes the value back to what it was before change A happened, therefore
        /// it becomes a non-op).
        /// </summary>
        class HistoryChange
        {
            public string Old = string.Empty;

            public string New = string.Empty;

            public int? PersonId = 0;
        }
    }
}