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

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicketDesk.PushNotifications.Model
{
    [Table("PushNotificationDestinations", Schema = "notifications")]
    public class PushNotificationDestination
    {
        [Key]
        public int DestinationId { get; set; }

        [Index("IX_SubscriberDestination", 0, IsUnique = true)]
        [StringLength(100)]
        public string SubscriberName { get; set; }

        [Index("IX_SubscriberDestination", 1, IsUnique = true)]
        [StringLength(256)]
        public string DestinationAddress { get; set; }

        [Index("IX_SubscriberDestination", 2, IsUnique = true)]
        [StringLength(50)]
        public string DestinationType { get; set; }

        [StringLength(256)]
        public string SubscriberId { get; set; }


        public virtual SubscriberNotificationSetting SubscriberNotificationSettings { get; set; }
    }
}
