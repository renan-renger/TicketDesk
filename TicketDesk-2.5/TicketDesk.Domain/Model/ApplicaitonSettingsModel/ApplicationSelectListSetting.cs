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

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using TicketDesk.Domain.Annotations;

namespace TicketDesk.Domain.Model
{
    public class ApplicationSelectListSetting
    {
        public ApplicationSelectListSetting()
        {
            //To ensure that missing database values for settings do not completely brick the
            //  entire instance, make sure all default settings are initialized in the ctor
            CategoryList = new List<string>(new[] { "Software", "Hardware", "Network" });
            PriorityList = new List<string>(new[] { "High", "Medium", "Low" });
            TicketTypesList = new List<string>(new[] { "Problem", "Question", "Request" });
        }

        [JsonIgnore]
        [Display( AutoGenerateField = false)]
        [ScaffoldColumn(false)]
        public string Serialized
        {
            get { return JsonConvert.SerializeObject(this); }
            [UsedImplicitly]
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    return;
                }
                var jsettings = new JsonSerializerSettings {ObjectCreationHandling = ObjectCreationHandling.Replace};
                var jData = JsonConvert.DeserializeObject<ApplicationSelectListSetting>(value, jsettings);
                CategoryList = jData.CategoryList;
                PriorityList = jData.PriorityList;
                TicketTypesList = jData.TicketTypesList;
            }
        }

        [NotMapped]
        [Display(Name = "Categories")]
        public ICollection<string> CategoryList { get; set; }

        [NotMapped]
        [Display(Name="Priorities")]
        public ICollection<string> PriorityList { get; set; }

        [NotMapped]
        [Display(Name = "Ticket Types")]
        public ICollection<string> TicketTypesList { get; set; }
    }

}
