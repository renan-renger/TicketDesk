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
using System.Data.Entity.Validation;
using System.Globalization;
using System.Threading.Tasks;
using System.Web.Mvc;
using TicketDesk.Domain;
using TicketDesk.Domain.Model;
using TicketDesk.IO;

namespace TicketDesk.Web.Client.Controllers
{
    /// <summary>
    /// Class TicketController.
    /// </summary>
    [RoutePrefix("ticket")]
    [Route("{action=index}")]
    [Authorize(Roles = "TdInternalUsers,TdHelpDeskUsers,TdAdministrators")]
    public class TicketController : Controller
    {
        private TdDomainContext Context { get; set; }
        public TicketController(TdDomainContext context)
        {
            Context = context;
        }

        public RedirectToRouteResult Index()
        {
            return RedirectToAction("Index", "TicketCenter");
        }

        [Route("{id:int}")]
        public async Task<ActionResult> Index(int id)
        {
            var model = await Context.Tickets.FindAsync(id);
            if (model == null)
            {
                return RedirectToAction("Index", "TicketCenter");
            }

            return View(model);
        }

       
        [Route("new")]
        public ActionResult New()
        {
            var model = new Ticket { Owner = Context.SecurityProvider.CurrentUserId };
            ViewBag.TempId = Guid.NewGuid();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateOnlyIncomingValues]
        [Route("new")]
        public async Task<ActionResult> New(Ticket ticket, Guid tempId)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (await CreateTicketAsync(ticket, tempId))
                    {
                        return RedirectToAction("Index", new { id = ticket.TicketId });
                    }
                }
                // ReSharper disable once EmptyGeneralCatchClause
                catch (DbEntityValidationException)
                {
                    
                    //TODO: catch rule exceptions? or can annotations handle this fully now?
                }

            }
            ViewBag.TempId = tempId;
            return View(ticket);
        }

        [Route("ticket-files")]
        public ActionResult TicketFiles(int ticketId)
        {
            var attachments = TicketDeskFileStore.ListAttachmentInfo(ticketId.ToString(CultureInfo.InvariantCulture), false);
            ViewBag.TicketId = ticketId;
            return PartialView("_TicketFiles", attachments);
        }

        [Route("ticket-events")]
        public async Task<ActionResult> TicketEvents(int ticketId)
        {
            var ticket = await Context.Tickets.FindAsync(ticketId);
            return PartialView("_TicketEvents", ticket.TicketEvents);
        }

        [Route("ticket-details")]
        public async Task<ActionResult> TicketDetails(int ticketId)
        {
            var ticket = await Context.Tickets.FindAsync(ticketId);


            return PartialView("_TicketDetails", ticket);
        }

        private async Task<bool> CreateTicketAsync(Ticket ticket, Guid tempId)
        {
            Context.Tickets.Add(ticket);
            await Context.SaveChangesAsync();
            ticket.CommitPendingAttachments(tempId);

            return ticket.TicketId != default(int);
        }

    }
}