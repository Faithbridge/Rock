﻿// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Data;
using Rock.Model;
using Rock.SystemKey;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

using Rock.Utility.Settings.DataAutomation;

namespace RockWeb.Blocks.Administration
{
    /// <summary>
    /// Data Automation Settings
    /// </summary>
    [DisplayName( "Data Automation Settings" )]
    [Category( "Administration" )]
    [Description( "Block used to set values specific to data automation (NCOA, Updating Person Status, Family Campus, Etc)." )]
    public partial class DataAutomationSettings : Rock.Web.UI.RockBlock
    {
        #region private variables

        private List<IgnoreCampusChangeRow> _ignoreCampusChangeRows { get; set; }
        private RockContext _rockContext = new RockContext();
        private ReactivatePeople _reactivateSettings = new ReactivatePeople();
        private InactivatePeople _inactivateSettings = new InactivatePeople();
        private UpdateFamilyCampus _campusSettings = new UpdateFamilyCampus();
        private MoveAdultChildren _adultChildrenSettings = new MoveAdultChildren();
        private List<InteractionItem> _interactionChannelTypes = new List<InteractionItem>();
        private List<CampusCache> _campuses = new List<CampusCache>();

        #endregion

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );

            _campuses = CampusCache.All();
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                BindControls();
                GetSettings();
            }
            else
            {
                GetRepeaterData();
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the BlockUpdated event of the SystemConfiguration control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
        }

        /// <summary>
        /// Handles saving all the data set by the user to the web.config.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void bbtnSaveConfig_Click( object sender, EventArgs e )
        {
            if ( !Page.IsValid )
            {
                return;
            }

            SaveSettings();
        }

        /// <summary>
        /// Handles the Click event of the lbAdd control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbAdd_Click( object sender, EventArgs e )
        {
            int newId = _ignoreCampusChangeRows.Max( a => a.Id ) + 1;
            _ignoreCampusChangeRows.Add( new IgnoreCampusChangeRow { Id = newId } );

            rIgnoreCampusChanges.DataSource = _ignoreCampusChangeRows;
            rIgnoreCampusChanges.DataBind();
        }

        /// <summary>
        /// Handles the ItemCommand event of the rIgnoreCampusChanges control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RepeaterItemEventArgs"/> instance containing the event data.</param>
        protected void rIgnoreCampusChanges_ItemCommand( object Sender, RepeaterCommandEventArgs e )
        {
            if ( e.CommandName == "delete" )
            {
                int rowId = e.CommandArgument.ToString().AsInteger();
                var repeaterRow = _ignoreCampusChangeRows.SingleOrDefault( a => a.Id == rowId );
                if ( repeaterRow != null )
                {
                    _ignoreCampusChangeRows.Remove( repeaterRow );
                }

                rIgnoreCampusChanges.DataSource = _ignoreCampusChangeRows;
                rIgnoreCampusChanges.DataBind();
            }
        }

        /// <summary>
        /// Handles the ItemDataBound event of the rIgnoreCampusChanges control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RepeaterItemEventArgs"/> instance containing the event data.</param>
        protected void rIgnoreCampusChanges_ItemDataBound( object sender, RepeaterItemEventArgs e )
        {
            var ignoreCampusChangeRow = e.Item.DataItem as IgnoreCampusChangeRow;
            CampusPicker fromCampus = e.Item.FindControl( "cpFromCampus" ) as CampusPicker;
            CampusPicker toCampus = e.Item.FindControl( "cpToCampus" ) as CampusPicker;
            RockDropDownList ddlCampusAttendanceOrGiving = e.Item.FindControl( "ddlAttendanceOrGiving" ) as RockDropDownList;

            if ( ignoreCampusChangeRow != null && fromCampus != null && toCampus != null && ddlCampusAttendanceOrGiving != null )
            {
                fromCampus.Campuses = _campuses;
                fromCampus.SelectedCampusId = ignoreCampusChangeRow.FromCampusId;

                toCampus.Campuses = _campuses;
                toCampus.SelectedCampusId = ignoreCampusChangeRow.ToCampusId;

                if ( ignoreCampusChangeRow.CampusCriteria.HasValue )
                {
                    ddlCampusAttendanceOrGiving.SetValue( ignoreCampusChangeRow.CampusCriteria.ConvertToInt() );
                }
            }
        }

        /// <summary>
        /// Handles the CheckedChanged event of the cbReactivatePeople control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void cbReactivatePeople_CheckedChanged( object sender, EventArgs e )
        {
            pnlReactivatePeople.Enabled = cbReactivatePeople.Checked;
        }
        /// <summary>
        /// Handles the CheckedChanged event of the cbInactivatePeople control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void cbInactivatePeople_CheckedChanged( object sender, EventArgs e )
        {
            pnlInactivatePeople.Enabled = cbInactivatePeople.Checked;
        }
        /// <summary>
        /// Handles the CheckedChanged event of the cbCampusUpdate control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void cbCampusUpdate_CheckedChanged( object sender, EventArgs e )
        {
            pnlCampusUpdate.Enabled = cbCampusUpdate.Checked;
        }
        /// <summary>
        /// Handles the CheckedChanged event of the cbAdultChildren control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void cbAdultChildren_CheckedChanged( object sender, EventArgs e )
        {
            pnlAdultChildren.Enabled = cbAdultChildren.Checked;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Binds the controls.
        /// </summary>
        private void BindControls()
        {
            var groupTypes = new GroupTypeService( _rockContext )
                .Queryable().AsNoTracking()
                .Where( t => t.TakesAttendance == true)
                .OrderBy( t => t.Order )
                .ThenBy( t => t.Name )
                .Select( t => new { value = t.Id, text = t.Name } )
                .ToList();

            rlbAttendanceInGroupType.DataSource = groupTypes;
            rlbAttendanceInGroupType.DataBind();


            rlbNoAttendanceInGroupType.DataSource = groupTypes;
            rlbNoAttendanceInGroupType.DataBind();

            var personAttributes = new AttributeService( _rockContext )
                .GetByEntityTypeId( new Person().TypeId )
                .OrderBy( t => t.Order )
                .ThenBy( t => t.Name )
                .Select( t => new { value = t.Id, text = t.Name } )
                .ToList();

            rlbPersonAttributes.DataSource = personAttributes;
            rlbPersonAttributes.DataBind();

            rlbNoPersonAttributes.DataSource = personAttributes;
            rlbNoPersonAttributes.DataBind();

            ddlAttendanceOrGiving.BindToEnum<CampusCriteria>();

            var knownRelGroupType = GroupTypeCache.Read( Rock.SystemGuid.GroupType.GROUPTYPE_KNOWN_RELATIONSHIPS.AsGuid() );
            if ( knownRelGroupType != null )
            {
                rpParentRelationship.GroupTypeId = knownRelGroupType.Id;
                rpSiblingRelationship.GroupTypeId = knownRelGroupType.Id;
            }
        }

        /// <summary>
        /// Gets the settings.
        /// </summary>
        private void GetSettings()
        {
            // Get Data Automation Settings
            _reactivateSettings = Rock.Web.SystemSettings.GetValue( SystemSetting.DATA_AUTOMATION_REACTIVATE_PEOPLE ).FromJsonOrNull<ReactivatePeople>() ?? new ReactivatePeople();
            _inactivateSettings = Rock.Web.SystemSettings.GetValue( SystemSetting.DATA_AUTOMATION_INACTIVATE_PEOPLE ).FromJsonOrNull<InactivatePeople>() ?? new InactivatePeople();
            _campusSettings = Rock.Web.SystemSettings.GetValue( SystemSetting.DATA_AUTOMATION_CAMPUS_UPDATE ).FromJsonOrNull<UpdateFamilyCampus>() ?? new UpdateFamilyCampus();
            _adultChildrenSettings = Rock.Web.SystemSettings.GetValue( SystemSetting.DATA_AUTOMATION_ADULT_CHILDREN ).FromJsonOrNull<MoveAdultChildren>() ?? new MoveAdultChildren();

            // Set Data Automation Controls

            // Reactivate
            cbReactivatePeople.Checked = _reactivateSettings.IsEnabled;
            pnlReactivatePeople.Enabled = _reactivateSettings.IsEnabled;
            cbLastContribution.Checked = _reactivateSettings.IsLastContributionEnabled;
            nbLastContribution.Text = _reactivateSettings.LastContributionPeriod.ToStringSafe();
            cbAttendanceInServiceGroup.Checked = _reactivateSettings.IsAttendanceInServiceGroupEnabled;
            nbAttendanceInServiceGroup.Text = _reactivateSettings.AttendanceInServiceGroupPeriod.ToStringSafe();
            cbAttendanceInGroupType.Checked = _reactivateSettings.IsAttendanceInGroupTypeEnabled;
            nbAttendanceInGroupType.Text = _reactivateSettings.AttendanceInGroupTypeDays.ToStringSafe();
            rlbAttendanceInGroupType.SetValues( _reactivateSettings.AttendanceInGroupType ?? new List<int>() );
            cbPrayerRequest.Checked = _reactivateSettings.IsPrayerRequestEnabled;
            nbPrayerRequest.Text = _reactivateSettings.PrayerRequestPeriod.ToStringSafe();
            cbPersonAttributes.Checked = _reactivateSettings.IsPersonAttributesEnabled;
            nbPersonAttributes.Text = _reactivateSettings.PersonAttributesDays.ToStringSafe();
            rlbPersonAttributes.SetValues( _reactivateSettings.PersonAttributes ?? new List<int>() );
            cbIncludeDataView.Checked = _reactivateSettings.IsIncludeDataViewEnabled;
            dvIncludeDataView.EntityTypeId = EntityTypeCache.Read( typeof( Rock.Model.Person ) ).Id;
            dvIncludeDataView.SetValue( _reactivateSettings.IncludeDataView );
            cbExcludeDataView.Checked = _reactivateSettings.IsExcludeDataViewEnabled;
            dvExcludeDataView.EntityTypeId = EntityTypeCache.Read( typeof( Rock.Model.Person ) ).Id;
            dvExcludeDataView.SetValue( _reactivateSettings.ExcludeDataView );
            cbInteractions.Checked = _reactivateSettings.IsInteractionsEnabled;

            var interactionChannels = new InteractionChannelService( _rockContext )
                .Queryable().AsNoTracking()
                .Select( a => new { a.Guid, a.Name } )
                .ToList();

            var reactivateChannelTypes = interactionChannels.Select( c => new InteractionItem( c.Guid, c.Name ) ).ToList();
            if ( _reactivateSettings.Interactions != null )
            {
                foreach ( var settingInteractionItem in _reactivateSettings.Interactions )
                {
                    var interactionChannelType = reactivateChannelTypes.SingleOrDefault( a => a.Guid == settingInteractionItem.Guid );
                    if ( interactionChannelType != null )
                    {
                        interactionChannelType.IsInteractionTypeEnabled = settingInteractionItem.IsInteractionTypeEnabled;
                        interactionChannelType.LastInteractionDays = settingInteractionItem.LastInteractionDays;
                    }
                }
            }
            rInteractions.DataSource = reactivateChannelTypes;
            rInteractions.DataBind();

            // Inactivate
            cbInactivatePeople.Checked = _inactivateSettings.IsEnabled;
            pnlInactivatePeople.Enabled = _inactivateSettings.IsEnabled;
            cbNoLastContribution.Checked = _inactivateSettings.IsNoLastContributionEnabled;
            nbNoLastContribution.Text = _inactivateSettings.NoLastContributionPeriod.ToStringSafe();
            cbNoAttendanceInServiceGroup.Checked = _inactivateSettings.IsNoAttendanceInServiceGroupEnabled;
            nbNoAttendanceInServiceGroup.Text = _inactivateSettings.NoAttendanceInServiceGroupPeriod.ToStringSafe();
            cbNoAttendanceInGroupType.Checked = _inactivateSettings.IsNoAttendanceInGroupTypeEnabled;
            nbNoAttendanceInGroupType.Text = _inactivateSettings.NoAttendanceInGroupTypeDays.ToStringSafe();
            rlbNoAttendanceInGroupType.SetValues( _inactivateSettings.AttendanceInGroupType ?? new List<int>() );
            cbNoPrayerRequest.Checked = _inactivateSettings.IsNoPrayerRequestEnabled;
            nbNoPrayerRequest.Text = _inactivateSettings.NoPrayerRequestPeriod.ToStringSafe();
            cbNoPersonAttributes.Checked = _inactivateSettings.IsNoPersonAttributesEnabled;
            nbNoPersonAttributes.Text = _inactivateSettings.NoPersonAttributesDays.ToStringSafe();
            rlbNoPersonAttributes.SetValues( _inactivateSettings.PersonAttributes ?? new List<int>() );
            cbNotInDataView.Checked = _inactivateSettings.IsNotInDataviewEnabled;
            dvNotInDataView.EntityTypeId = EntityTypeCache.Read( typeof( Rock.Model.Person ) ).Id;
            dvNotInDataView.SetValue( _inactivateSettings.NotInDataview );
            cbNoInteractions.Checked = _inactivateSettings.IsNoInteractionsEnabled;

            var inactivateChannelTypes = interactionChannels.Select( c => new InteractionItem( c.Guid, c.Name ) ).ToList();
            if ( _inactivateSettings.NoInteractions != null )
            {
                foreach ( var settingInteractionItem in _inactivateSettings.NoInteractions )
                {
                    var interactionChannelType = inactivateChannelTypes.SingleOrDefault( a => a.Guid == settingInteractionItem.Guid );
                    if ( interactionChannelType != null )
                    {
                        interactionChannelType.IsInteractionTypeEnabled = settingInteractionItem.IsInteractionTypeEnabled;
                        interactionChannelType.LastInteractionDays = settingInteractionItem.LastInteractionDays;
                    }
                }
            }
            rNoInteractions.DataSource = inactivateChannelTypes;
            rNoInteractions.DataBind();

            // campus Update
            cbCampusUpdate.Checked = _campusSettings.IsEnabled;
            pnlCampusUpdate.Enabled = _campusSettings.IsEnabled;
            cbMostFamilyAttendance.Checked = _campusSettings.IsMostFamilyAttendanceEnabled;
            nbMostFamilyAttendance.Text = _campusSettings.MostFamilyAttendancePeriod.ToStringSafe();
            cbMostFamilyGiving.Checked = _campusSettings.IsMostFamilyGivingEnabled;
            nbMostFamilyGiving.Text = _campusSettings.MostFamilyGivingPeriod.ToStringSafe();
            ddlAttendanceOrGiving.SetValue( _campusSettings.MostAttendanceOrGiving.ConvertToInt() );
            cbIgnoreIfManualUpdate.Checked = _campusSettings.IsIgnoreIfManualUpdateEnabled;
            nbIgnoreIfManualUpdate.Text = _campusSettings.IgnoreIfManualUpdatePeriod.ToStringSafe();

            cbIgnoreCampusChanges.Checked = _campusSettings.IsIgnoreCampusChangesEnabled;
            if ( _campusSettings.IgnoreCampusChanges != null && _campusSettings.IgnoreCampusChanges.Any() )
            {
                int i = 1;
                _ignoreCampusChangeRows = _campusSettings.IgnoreCampusChanges
                    .Select( a => new IgnoreCampusChangeRow()
                    {
                        Id = i++,
                        CampusCriteria = a.BasedOn,
                        FromCampusId = a.FromCampus,
                        ToCampusId = a.ToCampus
                    } ).ToList();
            }
            else
            {
                _ignoreCampusChangeRows = new List<IgnoreCampusChangeRow>() { new IgnoreCampusChangeRow() { Id = 1 } };
            }
            rIgnoreCampusChanges.DataSource = _ignoreCampusChangeRows;
            rIgnoreCampusChanges.DataBind();

            // Adult Children
            cbAdultChildren.Checked = _adultChildrenSettings.IsEnabled;
            pnlAdultChildren.Enabled = _adultChildrenSettings.IsEnabled;
            nbAdultAge.Text = _adultChildrenSettings.AdultAge.ToString();
            rpParentRelationship.GroupRoleId = _adultChildrenSettings.ParentRelationshipId;
            rpSiblingRelationship.GroupRoleId = _adultChildrenSettings.SiblingRelationshipId;
            cbSameAddress.Checked = _adultChildrenSettings.UseSameHomeAddress;
            cbSamePhone.Checked = _adultChildrenSettings.UseSameHomePhone;
            wfWorkflows.SetValues( _adultChildrenSettings.WorkflowTypeIds );
            nbMaxRecords.Text = _adultChildrenSettings.MaximumRecords.ToString();
        }

        private void SaveSettings()
        {
            // Save Data Automation
            _reactivateSettings = new ReactivatePeople();
            _inactivateSettings = new InactivatePeople();
            _campusSettings = new UpdateFamilyCampus();

            //Reactivate 
            _reactivateSettings.IsEnabled = cbReactivatePeople.Checked;

            _reactivateSettings.IsLastContributionEnabled = cbLastContribution.Checked;
            _reactivateSettings.LastContributionPeriod = nbLastContribution.Text.AsInteger();

            _reactivateSettings.IsAttendanceInServiceGroupEnabled = cbAttendanceInServiceGroup.Checked;
            _reactivateSettings.AttendanceInServiceGroupPeriod = nbAttendanceInServiceGroup.Text.AsInteger();

            _reactivateSettings.IsAttendanceInGroupTypeEnabled = cbAttendanceInGroupType.Checked;
            _reactivateSettings.AttendanceInGroupType = rlbAttendanceInGroupType.SelectedValues.AsIntegerList();
            _reactivateSettings.AttendanceInGroupTypeDays = nbAttendanceInGroupType.Text.AsInteger();

            _reactivateSettings.IsPrayerRequestEnabled = cbPrayerRequest.Checked;
            _reactivateSettings.PrayerRequestPeriod = nbPrayerRequest.Text.AsInteger();

            _reactivateSettings.IsPersonAttributesEnabled = cbPersonAttributes.Checked;
            _reactivateSettings.PersonAttributes = rlbPersonAttributes.SelectedValues.AsIntegerList();
            _reactivateSettings.PersonAttributesDays = nbPersonAttributes.Text.AsInteger();

            _reactivateSettings.IsIncludeDataViewEnabled = cbIncludeDataView.Checked;
            _reactivateSettings.IncludeDataView = dvIncludeDataView.SelectedValueAsInt();

            _reactivateSettings.IsExcludeDataViewEnabled = cbExcludeDataView.Checked;
            _reactivateSettings.ExcludeDataView = dvExcludeDataView.SelectedValueAsInt();

            _reactivateSettings.IsInteractionsEnabled = cbInteractions.Checked;
            foreach ( RepeaterItem rItem in rInteractions.Items )
            {
                RockCheckBox isInterationTypeEnabled = rItem.FindControl( "cbInterationType" ) as RockCheckBox;
                if ( isInterationTypeEnabled.Checked )
                {
                    _reactivateSettings.Interactions = _reactivateSettings.Interactions ?? new List<InteractionItem>();
                    HiddenField interactionTypeId = rItem.FindControl( "hfInteractionTypeId" ) as HiddenField;
                    NumberBox lastInteractionDays = rItem.FindControl( "nbInteractionDays" ) as NumberBox;
                    var item = new InteractionItem( interactionTypeId.Value.AsGuid(), string.Empty )
                    {
                        IsInteractionTypeEnabled = true,
                        LastInteractionDays = lastInteractionDays.Text.AsInteger()
                    };
                    _reactivateSettings.Interactions.Add( item );
                }

            }

            //Inactivate
            _inactivateSettings.IsEnabled = cbInactivatePeople.Checked;

            _inactivateSettings.IsNoLastContributionEnabled = cbNoLastContribution.Checked;
            _inactivateSettings.NoLastContributionPeriod = nbNoLastContribution.Text.AsInteger();

            _inactivateSettings.IsNoAttendanceInServiceGroupEnabled = cbNoAttendanceInServiceGroup.Checked;
            _inactivateSettings.NoAttendanceInServiceGroupPeriod = nbNoAttendanceInServiceGroup.Text.AsInteger();

            _inactivateSettings.IsNoAttendanceInGroupTypeEnabled = cbNoAttendanceInGroupType.Checked;
            _inactivateSettings.AttendanceInGroupType = rlbNoAttendanceInGroupType.SelectedValues.AsIntegerList();
            _inactivateSettings.NoAttendanceInGroupTypeDays = nbNoAttendanceInGroupType.Text.AsInteger();

            _inactivateSettings.IsNoPrayerRequestEnabled = cbNoPrayerRequest.Checked;
            _inactivateSettings.NoPrayerRequestPeriod = nbNoPrayerRequest.Text.AsInteger();

            _inactivateSettings.IsNoPersonAttributesEnabled = cbNoPersonAttributes.Checked;
            _inactivateSettings.PersonAttributes = rlbNoPersonAttributes.SelectedValues.AsIntegerList();
            _inactivateSettings.NoPersonAttributesDays = nbNoPersonAttributes.Text.AsInteger();

            _inactivateSettings.IsNotInDataviewEnabled = cbNotInDataView.Checked;
            _inactivateSettings.NotInDataview = dvNotInDataView.SelectedValueAsInt();

            _inactivateSettings.IsNoInteractionsEnabled = cbNoInteractions.Checked;
            foreach ( RepeaterItem rItem in rNoInteractions.Items )
            {
                RockCheckBox isInterationTypeEnabled = rItem.FindControl( "cbInterationType" ) as RockCheckBox;
                if ( isInterationTypeEnabled.Checked )
                {
                    _inactivateSettings.NoInteractions = _inactivateSettings.NoInteractions ?? new List<InteractionItem>();
                    HiddenField interactionTypeId = rItem.FindControl( "hfInteractionTypeId" ) as HiddenField;
                    NumberBox lastInteractionDays = rItem.FindControl( "nbNoInteractionDays" ) as NumberBox;
                    var item = new InteractionItem( interactionTypeId.Value.AsGuid(), string.Empty )
                    {
                        IsInteractionTypeEnabled = true,
                        LastInteractionDays = lastInteractionDays.Text.AsInteger()
                    };
                    _inactivateSettings.NoInteractions.Add( item );
                }
            }

            //Campus Update
            _campusSettings.IsEnabled = cbCampusUpdate.Checked;

            _campusSettings.IsMostFamilyAttendanceEnabled = cbMostFamilyAttendance.Checked;
            _campusSettings.MostFamilyAttendancePeriod = nbMostFamilyAttendance.Text.AsInteger();

            _campusSettings.IsMostFamilyGivingEnabled = cbMostFamilyGiving.Checked;
            _campusSettings.MostFamilyGivingPeriod = nbMostFamilyGiving.Text.AsInteger();

            _campusSettings.MostAttendanceOrGiving = ddlAttendanceOrGiving.SelectedValueAsEnum<CampusCriteria>();

            _campusSettings.IsIgnoreIfManualUpdateEnabled = cbIgnoreIfManualUpdate.Checked;
            _campusSettings.IgnoreIfManualUpdatePeriod = nbIgnoreIfManualUpdate.Text.AsInteger();

            _campusSettings.IsIgnoreCampusChangesEnabled = cbIgnoreCampusChanges.Checked;
            _campusSettings.IgnoreCampusChanges =
                _ignoreCampusChangeRows
                    .Where( a => a.CampusCriteria.HasValue && a.FromCampusId.HasValue && a.ToCampusId.HasValue )
                    .Select( a => new IgnoreCampusChangeItem
                    {
                        FromCampus = a.FromCampusId.Value,
                        ToCampus = a.ToCampusId.Value,
                        BasedOn = a.CampusCriteria.Value
                    } )
                    .ToList();

            // Adult Children
            _adultChildrenSettings.IsEnabled = cbAdultChildren.Checked;
            _adultChildrenSettings.AdultAge = nbAdultAge.Text.AsIntegerOrNull() ?? 18;
            _adultChildrenSettings.ParentRelationshipId = rpParentRelationship.GroupRoleId;
            _adultChildrenSettings.SiblingRelationshipId = rpSiblingRelationship.GroupRoleId;
            _adultChildrenSettings.UseSameHomeAddress = cbSameAddress.Checked;
            _adultChildrenSettings.UseSameHomePhone = cbSamePhone.Checked;
            _adultChildrenSettings.WorkflowTypeIds = wfWorkflows.SelectedValuesAsInt().ToList();
            _adultChildrenSettings.MaximumRecords = nbMaxRecords.Text.AsIntegerOrNull() ?? 200;

            Rock.Web.SystemSettings.SetValue( SystemSetting.DATA_AUTOMATION_REACTIVATE_PEOPLE, _reactivateSettings.ToJson() );
            Rock.Web.SystemSettings.SetValue( SystemSetting.DATA_AUTOMATION_INACTIVATE_PEOPLE, _inactivateSettings.ToJson() );
            Rock.Web.SystemSettings.SetValue( SystemSetting.DATA_AUTOMATION_CAMPUS_UPDATE, _campusSettings.ToJson() );
            Rock.Web.SystemSettings.SetValue( SystemSetting.DATA_AUTOMATION_ADULT_CHILDREN, _adultChildrenSettings.ToJson() );
        }

        private void GetRepeaterData()
        {
            _ignoreCampusChangeRows = new List<IgnoreCampusChangeRow>();

            foreach ( RepeaterItem item in rIgnoreCampusChanges.Items )
            {
                CampusPicker fromCampus = item.FindControl( "cpFromCampus" ) as CampusPicker;
                CampusPicker toCampus = item.FindControl( "cpToCampus" ) as CampusPicker;
                HiddenField hiddenField = item.FindControl( "hfRowId" ) as HiddenField;
                RockDropDownList ddlCampusCriteria = item.FindControl( "ddlAttendanceOrGiving" ) as RockDropDownList;

                _ignoreCampusChangeRows.Add( new IgnoreCampusChangeRow
                {
                    Id = hiddenField.ValueAsInt(),
                    ToCampusId = toCampus.SelectedCampusId,
                    FromCampusId = fromCampus.SelectedCampusId,
                    CampusCriteria = ddlCampusCriteria.SelectedValueAsEnumOrNull<CampusCriteria>()
                } );
            }
        }

        public class IgnoreCampusChangeRow
        {
            public int Id { get; set; }

            public int? FromCampusId { get; set; }

            public int? ToCampusId { get; set; }

            public CampusCriteria? CampusCriteria { get; set; }
        }

        #endregion

    }
}