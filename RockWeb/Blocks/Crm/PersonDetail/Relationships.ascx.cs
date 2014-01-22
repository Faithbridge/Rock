﻿// <copyright>
// Copyright 2013 by the Spark Development Network
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;

namespace RockWeb.Blocks.Crm.PersonDetail
{
    /// <summary>
    /// Renders the related members of a group (typically used for the Relationships group and the Peer Network group)
    /// </summary>
    [DisplayName( "Relationships" )]
    [Category( "CRM > Person Detail" )]
    [Description( "Allows you to view relationships of a particular person." )]

    [GroupRoleField( "", "Group Type/Role Filter", "The Group Type and role to display other members from.", false, "" )]
    [BooleanField("Show Role", "Should the member's role be displayed with their name")]
    [BooleanField("Create Group", "Should group be created if a group/role cannot be found for the current person.", true)]
    public partial class Relationships : Rock.Web.UI.PersonBlock
    {
        protected bool ShowRole = false;
        protected Guid ownerRoleGuid = Guid.Empty;
        protected bool IsKnownRelationships = false;

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            if ( Guid.TryParse( GetAttributeValue( "GroupType/RoleFilter" ), out ownerRoleGuid ) )
            {
                IsKnownRelationships = ownerRoleGuid.Equals(new Guid(Rock.SystemGuid.GroupRole.GROUPROLE_KNOWN_RELATIONSHIPS_OWNER));
            }

            rGroupMembers.ItemCommand += rGroupMembers_ItemCommand;

            acPerson.Url = "api/People/Search/%QUERY/false";
            acPerson.NameProperty = "Name";
            acPerson.IdProperty = "Id";
            acPerson.Template = @"
<div class='picker-select-item'>
    <label>{{Name}}</label>
    <div class='picker-select-item-details clearfix'>
        {{ImageHtmlTag}}
        <div class='contents'>
            {% if Age >= 0 %}<em>({{ Age }} yrs old)</em>{% endif %}
        </div>
    </div>
</div>
";
            acPerson.OnClientSelected = string.Format( "updateRelationshipSelection('{0}', selectedItem);", pnlSelectedPerson.ClientID );
            modalAddPerson.SaveClick += modalAddPerson_SaveClick;

            string script = @"
    $('a.remove-relationship').click(function(){
        return confirm('Are you sure you want to remove this relationship?');
    });

    function updateRelationshipSelection(clientId, person)
    {
        $('#' + clientId).html(person.Name);
    }
";
            ScriptManager.RegisterStartupScript( rGroupMembers, rGroupMembers.GetType(), "ConfirmRemoveRelationship", script, true );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            bool.TryParse( GetAttributeValue( "ShowRole" ), out ShowRole );

            BindData();
        }

        protected void lbAdd_Click( object sender, EventArgs e )
        {
            ShowModal( null, null );
        }

        void rGroupMembers_ItemCommand( object source, System.Web.UI.WebControls.RepeaterCommandEventArgs e )
        {
            int groupMemberId = int.MinValue;
            if ( int.TryParse( e.CommandArgument.ToString(), out groupMemberId ) )
            {
                var service = new GroupMemberService();
                var groupMember = service.Get( groupMemberId );
                if ( groupMember != null )
                {
                    if ( e.CommandName == "EditRole" )
                    {
                        ShowModal(groupMember.Person, groupMember.GroupRoleId);
                    }

                    else if ( e.CommandName == "RemoveRole" )
                    {
                        if ( IsKnownRelationships )
                        {
                            var inverseGroupMember = service.GetInverseRelationship( groupMember, false, CurrentPersonId );
                            if ( inverseGroupMember != null )
                            {
                                service.Delete( inverseGroupMember, CurrentPersonId );
                            }
                        }

                        service.Delete( groupMember, CurrentPersonId );
                        service.Save( groupMember, CurrentPersonId );

                        BindData();
                    }
                }
            }
        }

        void modalAddPerson_SaveClick( object sender, EventArgs e )
        {
            if (!string.IsNullOrWhiteSpace(acPerson.Value))
            {
                int personId = int.MinValue;
                if ( int.TryParse( acPerson.Value, out personId ) )
                {
                    int? roleId = grpRole.GroupRoleId;
                    if ( roleId.HasValue )
                    {
                        using ( new UnitOfWorkScope() )
                        {
                            var memberService = new GroupMemberService();
                            var group = memberService.Queryable()
                                .Where( m =>
                                    m.PersonId == Person.Id &&
                                    m.GroupRole.Guid == ownerRoleGuid
                                )
                                .Select( m => m.Group )
                                .FirstOrDefault();

                            if ( group != null )
                            {
                                var groupMember = memberService.Queryable()
                                    .Where( m =>
                                        m.GroupId == group.Id &&
                                        m.PersonId == personId &&
                                        m.GroupRoleId == roleId.Value )
                                    .FirstOrDefault();

                                if ( groupMember == null )
                                {
                                    groupMember = new GroupMember();
                                    groupMember.GroupId = group.Id;
                                    groupMember.PersonId = personId;
                                    groupMember.GroupRoleId = roleId.Value;
                                    memberService.Add( groupMember, CurrentPersonId );
                                }

                                memberService.Save( groupMember, CurrentPersonId );
                                if ( IsKnownRelationships )
                                {
                                    var inverseGroupMember = memberService.GetInverseRelationship(
                                        groupMember, bool.Parse( GetAttributeValue( "CreateGroup" ) ), CurrentPersonId );
                                    if ( inverseGroupMember != null )
                                    {
                                        memberService.Save( inverseGroupMember, CurrentPersonId );
                                    }
                                }
                            }
                        }
                    }
                }
            }

            BindData();
        }

        /// <summary>
        /// Binds the data.
        /// </summary>
        private void BindData()
        {
            if ( Person != null && Person.Id > 0 )
            {
                if ( ownerRoleGuid != Guid.Empty )
                {
                    using ( new UnitOfWorkScope() )
                    {
                        var memberService = new GroupMemberService();
                        var group = memberService.Queryable()
                            .Where( m =>
                                m.PersonId == Person.Id &&
                                m.GroupRole.Guid == ownerRoleGuid
                            )
                            .Select( m => m.Group )
                            .FirstOrDefault();

                        if ( group == null && bool.Parse( GetAttributeValue( "CreateGroup" ) ) )
                        {
                            var role = new GroupTypeRoleService().Get( ownerRoleGuid );
                            if ( role != null && role.GroupTypeId.HasValue )
                            {
                                var groupMember = new GroupMember();
                                groupMember.PersonId = Person.Id;
                                groupMember.GroupRoleId = role.Id;

                                group = new Group();
                                group.Name = role.GroupType.Name;
                                group.GroupTypeId = role.GroupTypeId.Value;
                                group.Members.Add( groupMember );

                                var groupService = new GroupService();
                                groupService.Add( group, CurrentPersonId );
                                groupService.Save( group, CurrentPersonId );

                                group = groupService.Get( group.Id );
                            }
                        }

                        if ( group != null )
                        {
                            if ( group.IsAuthorized( "View", CurrentPerson ) )
                            {
                                phGroupTypeIcon.Controls.Clear();
                                if ( !string.IsNullOrWhiteSpace( group.GroupType.IconCssClass ) )
                                {
                                    phGroupTypeIcon.Controls.Add(
                                        new LiteralControl(
                                            string.Format( "<i class='{0}'></i>", group.GroupType.IconCssClass ) ) );
                                }

                                lGroupName.Text = group.Name;

                                phEditActions.Visible = group.IsAuthorized( "Edit", CurrentPerson );

                                // TODO: How many implied relationships should be displayed

                                rGroupMembers.DataSource = new GroupMemberService().GetByGroupId( group.Id )
                                    .Where( m => m.PersonId != Person.Id )
                                    .OrderBy( m => m.Person.LastName )
                                    .ThenBy( m => m.Person.FirstName )
                                    .Take( 50 )
                                    .ToList();
                                rGroupMembers.DataBind();
                            }
                        }
                    }
                }
            }
        }

        private void ShowModal( Person person, int? roleId )
        {
            Guid roleGuid = Guid.Empty;
            if ( Guid.TryParse( GetAttributeValue( "GroupType/RoleFilter" ), out roleGuid ) )
            {
                grpRole.GroupTypeId = new GroupTypeRoleService().Queryable()
                    .Where( r => r.Guid == roleGuid )
                    .Select( r => r.GroupTypeId )
                    .FirstOrDefault();

            }
            grpRole.GroupRoleId = roleId;

            if ( person != null )
            {
                acPerson.Value = person.Id.ToString();
                acPerson.Text = person.FullName;
            }
            else
            {
                acPerson.Value = string.Empty;
                acPerson.Text = string.Empty;
            }

            modalAddPerson.Show();

        }
    }
}