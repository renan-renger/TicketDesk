﻿// TicketDesk - Attribution notice
// Contributor(s):
//
//      Stephen Redd (stephen@reddnet.net, http://www.reddnet.net)
//
// This file is distributed under the terms of the Microsoft Public 
// License (Ms-PL). See http://opensource.org/licenses/MS-PL
// for the complete terms of use. 
//
// For any distribution that contains code from this file, this notice of 
// attribution must remain intact, and a copy of the license must be 
// provided to the recipient.

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace TicketDesk.Domain.Model
{
    
    public class UserSetting
    {

        [Key]
        [StringLength(256)]
        public string UserId { get; set; }

        public virtual UserTicketListSettingsCollection ListSettings { get; internal set; }
       

        public UserTicketListSetting GetUserListSettingByName(string listName)
        {
            return ListSettings.FirstOrDefault(us => us.ListName.Equals(listName, StringComparison.InvariantCultureIgnoreCase));
        }

        public static UserSetting GetDefaultSettingsForUser(string userId, bool isHelpDeskUser)
        {
            var collection = new UserTicketListSettingsCollection
            {
                UserTicketListSetting.GetDefaultListSettings(userId, isHelpDeskUser)
            };

            return new UserSetting { UserId = userId, ListSettings = collection };
        }
    }


}
