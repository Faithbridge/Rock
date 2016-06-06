//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by the Rock.CodeGeneration project
//     Changes to this file will be lost when the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
// <copyright>
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
using System.Linq;

using Rock.Data;

namespace Rock.Model
{
    /// <summary>
    /// GroupType Service class
    /// </summary>
    public partial class GroupTypeService : Service<GroupType>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GroupTypeService"/> class
        /// </summary>
        /// <param name="context">The context.</param>
        public GroupTypeService(RockContext context) : base(context)
        {
        }

        /// <summary>
        /// Determines whether this instance can delete the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>
        ///   <c>true</c> if this instance can delete the specified item; otherwise, <c>false</c>.
        /// </returns>
        public bool CanDelete( GroupType item, out string errorMessage )
        {
            errorMessage = string.Empty;
 
            if ( new Service<ConnectionOpportunity>( Context ).Queryable().Any( a => a.GroupTypeId == item.Id ) )
            {
                errorMessage = string.Format( "This {0} is assigned to a {1}.", GroupType.FriendlyTypeName, ConnectionOpportunity.FriendlyTypeName );
                return false;
            }  
 
            if ( new Service<Group>( Context ).Queryable().Any( a => a.GroupTypeId == item.Id ) )
            {
                errorMessage = string.Format( "This {0} is assigned to a {1}.", GroupType.FriendlyTypeName, Group.FriendlyTypeName );
                return false;
            }  
 
            if ( new Service<GroupType>( Context ).Queryable().Any( a => a.InheritedGroupTypeId == item.Id ) )
            {
                errorMessage = string.Format( "This {0} is assigned to a {1}.", GroupType.FriendlyTypeName, GroupType.FriendlyTypeName );
                return false;
            }  
            
            // ignoring GroupTypeAssociation,ChildGroupTypeId 
            
            // ignoring GroupTypeAssociation,GroupTypeId 
 
            if ( new Service<RegistrationTemplate>( Context ).Queryable().Any( a => a.GroupTypeId == item.Id ) )
            {
                errorMessage = string.Format( "This {0} is assigned to a {1}.", GroupType.FriendlyTypeName, RegistrationTemplate.FriendlyTypeName );
                return false;
            }  
            return true;
        }
    }

    /// <summary>
    /// Generated Extension Methods
    /// </summary>
    public static partial class GroupTypeExtensionMethods
    {
        /// <summary>
        /// Clones this GroupType object to a new GroupType object
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="deepCopy">if set to <c>true</c> a deep copy is made. If false, only the basic entity properties are copied.</param>
        /// <returns></returns>
        public static GroupType Clone( this GroupType source, bool deepCopy )
        {
            if (deepCopy)
            {
                return source.Clone() as GroupType;
            }
            else
            {
                var target = new GroupType();
                target.CopyPropertiesFrom( source );
                return target;
            }
        }

        /// <summary>
        /// Copies the properties from another GroupType object to this GroupType object
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="source">The source.</param>
        public static void CopyPropertiesFrom( this GroupType target, GroupType source )
        {
            target.Id = source.Id;
            target.AllowedScheduleTypes = source.AllowedScheduleTypes;
            target.AllowMultipleLocations = source.AllowMultipleLocations;
            target.AttendanceCountsAsWeekendService = source.AttendanceCountsAsWeekendService;
            target.AttendancePrintTo = source.AttendancePrintTo;
            target.AttendanceRule = source.AttendanceRule;
            target.DefaultGroupRoleId = source.DefaultGroupRoleId;
            target.Description = source.Description;
            target.EnableLocationSchedules = source.EnableLocationSchedules;
            target.ForeignGuid = source.ForeignGuid;
            target.ForeignKey = source.ForeignKey;
            target.GroupCapacityRule = source.GroupCapacityRule;
            target.GroupMemberTerm = source.GroupMemberTerm;
            target.GroupTerm = source.GroupTerm;
            target.GroupTypePurposeValueId = source.GroupTypePurposeValueId;
            target.IconCssClass = source.IconCssClass;
            target.IgnorePersonInactivated = source.IgnorePersonInactivated;
            target.InheritedGroupTypeId = source.InheritedGroupTypeId;
            target.IsSystem = source.IsSystem;
            target.LocationSelectionMode = source.LocationSelectionMode;
            target.Name = source.Name;
            target.Order = source.Order;
            target.SendAttendanceReminder = source.SendAttendanceReminder;
            target.ShowConnectionStatus = source.ShowConnectionStatus;
            target.ShowInGroupList = source.ShowInGroupList;
            target.ShowInNavigation = source.ShowInNavigation;
            target.TakesAttendance = source.TakesAttendance;
            target.CreatedDateTime = source.CreatedDateTime;
            target.ModifiedDateTime = source.ModifiedDateTime;
            target.CreatedByPersonAliasId = source.CreatedByPersonAliasId;
            target.ModifiedByPersonAliasId = source.ModifiedByPersonAliasId;
            target.Guid = source.Guid;
            target.ForeignId = source.ForeignId;

        }
    }
}
