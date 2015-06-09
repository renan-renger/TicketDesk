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

using System.Threading.Tasks;
using System.Web.Mvc;
using TicketDesk.Domain;
using TicketDesk.Domain.Model;
using TicketDesk.Search.Common;

namespace TicketDesk.Web.Client.Controllers
{
    [RoutePrefix("search")]
    [Route("{action=index")]
    [Authorize(Roles = "TdInternalUsers,TdHelpDeskUsers,TdAdministrators")]
    public class SearchController : Controller
    {
        private TdDomainContext Context { get; set; }
        public SearchController(TdDomainContext context)
        {
            Context = context;
        }

        [Route("")]
        [Route("{term}")]
        [HttpGet]
        public async Task<ActionResult> Index(string term)
        {
            if (!string.IsNullOrEmpty(term))
            {
                var model = await TdSearchContext.Current.SearchAsync(Context.Tickets, term);
                return View(model);
            }
            return View(new Ticket[0]);
        }
    }
}