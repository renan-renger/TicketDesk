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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Runtime.Serialization.Formatters.Binary;
using Postal;
using S22.Mail;
using TicketDesk.PushNotifications.Model;
using TicketDesk.Web.Client.Controllers;

namespace TicketDesk.Domain.Model
{
    public static class TicketEventNotificationExtensions
    {
        public static IEnumerable<TicketPushNotificationEventInfo> ToNotificationEventInfoCollection(
            this IEnumerable<TicketEventNotification> eventNotifications, bool subscriberExclude)
        {

            return eventNotifications.Select(note => new TicketPushNotificationEventInfo()
            {
                TicketId = note.TicketId,
                SubscriberId = note.SubscriberId,
                EventId = note.EventId,
                CancelNotification = subscriberExclude && note.IsRead,
                MessageContent = GetEmailForNote(note)
            });

        }

        private static string GetEmailForNote(TicketEventNotification note)
        {
            var email = new TicketEmail { Ticket = note.TicketEvent.Ticket };
            var mailService = new EmailService();
            SerializableMailMessage message = mailService.CreateMailMessage(email);
            using (var ms = new MemoryStream())
            {
                new BinaryFormatter().Serialize(ms, message);
                return Convert.ToBase64String(ms.ToArray());
            }
        }

    }
}
