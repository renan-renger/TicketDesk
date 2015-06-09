// TicketDesk - Attribution notice
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
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Linq;
using System.Threading.Tasks;
using TicketDesk.Domain.Annotations;
using TicketDesk.Domain.Localization;
using TicketDesk.Domain.Model;
using System.Data.Entity;


namespace TicketDesk.Domain
{
    public sealed class TdDomainContext : DbContext
    {

        /// <summary>
        /// Occurs when tickets have been changed and persisted to the database. 
        /// </summary>
        /// <remarks>
        /// WARNING: Do not to register events from instance objects unless you 
        /// are sure they will de-register when they go out of scope. The 
        /// registered delegate will keep a reference to the isntance that that 
        /// registered. You can cause memory leaks if you aren't careful with 
        /// static events.
        ///</remarks>
        public static event EventHandler<IEnumerable<Ticket>> TicketsChanged;

        private static void RaiseTicketsChanged(TdDomainContext sender, IEnumerable<Ticket> tickets)
        {
            //TODO: Static events have their (rare) uses, but this should use a service bus or formal pub/sub mechanism eventually
            if (TicketsChanged != null)
            {
                TicketsChanged(sender, tickets);
            }
        }

        public static event EventHandler<IEnumerable<TicketEventNotification>>  NotificationsCreated;
        private static void RaiseNotificationsCreated(TdDomainContext sender, IEnumerable<TicketEventNotification> notifications)
        {
            //TODO: Static events have their (rare) uses, but this should use a service bus or formal pub/sub mechanism eventually
            if (NotificationsCreated != null)
            {
                NotificationsCreated(sender, notifications);
            }
        }


        public TdDomainSecurityProviderBase SecurityProvider { get; private set; }
        public TicketActionManager TicketActions { get; private set; }


        /// <summary>
        /// Initializes a new instance of the <see cref="TdDomainContext"/> class.
        /// </summary>
        /// <remarks>
        /// The securityProvider parameter can be left null; however, this should 
        /// be reserved only for back-end and automated functionality that runs
        /// outside of a user's context (e.g. migrations)
        /// </remarks>
        /// <param name="securityProvider">The security provider.</param>
        public TdDomainContext(TdDomainSecurityProviderBase securityProvider)
            : this()
        {
            SecurityProvider = securityProvider;
            TicketActions = TicketActionManager.GetInstance(SecurityProvider);
        }


        public TdDomainContext()
            : base("name=TicketDesk")
        {
            // some dbsets are internal, manually initialize for use by EF
            ApplicationSettings = Set<ApplicationSetting>();
            UserSettings = Set<UserSetting>();

            //TODO: This is only public because it is used by migrations and related functions
            //  This ctor can be removed or made internal by creating a class that implements IDbContextFactory<TicketDeskContext>
            //      As I understand it, if the factory exists EF will use it instead of looking for a public ctor with no params

        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {

            modelBuilder.Entity<TicketEventNotification>()
                .HasRequired(c => c.TicketSubscriber)
                .WithMany()
                .WillCascadeOnDelete(false);

            //json flattening: see this for more info: http://www.reddnet.net/entity-framework-json-column/
            modelBuilder.ComplexType<UserTicketListSettingsCollection>()
                .Property(p => p.Serialized)
                .HasColumnName("ListSettingsJson");

         
            modelBuilder.ComplexType<ApplicationSelectListSetting>()
                .Property(p => p.Serialized)
                .HasColumnName("SelectListSettingsJson");

            modelBuilder.ComplexType<ApplicationPermissionsSetting>()
                .Property(p => p.Serialized)
                .HasColumnName("PermissionsSettingsJson");

         

        }

        public DbSet<TicketEvent> TicketEvents { get; [UsedImplicitly]set; }
        public DbSet<Ticket> Tickets { get; [UsedImplicitly] set; }
        public DbSet<TicketTag> TicketTags { get; [UsedImplicitly] set; }
        public DbSet<TicketSubscriber> TicketSubscribers { get; set; }
        public DbSet<TicketEventNotification> TicketEventNotifications { get; set; }


        //These DbSets contain json serialized content. Callers cannot use standard LINQ to Entities 
        //  expressions with these safely. Marking internal to prevent callers having direct access
        //  We'll provide a thin layer of abstraction for safely handling external interactions instead. 
        internal DbSet<ApplicationSetting> ApplicationSettings { get; [UsedImplicitly]set; }
        internal DbSet<UserSetting> UserSettings { get; [UsedImplicitly] set; }

        private UserSettingsManager _userSettingsManager;
        public UserSettingsManager UserSettingsManager
        {
            get
            {
                return _userSettingsManager ??
                       (_userSettingsManager = new UserSettingsManager(this));
            }
        }


        /// <summary>
        /// Gets or sets the application settings specific to ticketdesk.
        /// </summary>
        /// <value>The application settings.</value>
        public ApplicationSetting TicketDeskSettings
        {
            //TODO: these change infrequently, cache these
            get
            {
                var aps = ApplicationSettings.GetTicketDeskSettings();
                //this should only ever happen once, but if no settings are in DB, make default set
                if (aps == null)
                {
                    aps = new ApplicationSetting();
                    //do this on another instance, because we don't want to commit other things that may be tracking on this instance
                    using (var tempContext = new TdDomainContext()) //this feels like cheating :)
                    {
                        ApplicationSettings.Add(aps);
                        tempContext.SaveChanges();
                    }
                }
                return aps;
            }
            set
            {
                var oldSettings = ApplicationSettings.GetTicketDeskSettings();
                if (oldSettings != null)
                {
                    ApplicationSettings.Remove(oldSettings);
                }
                ApplicationSettings.Add(value);
            }
        }

        /// <summary>
        /// Gets an object query for the specified type
        /// </summary>
        /// <remarks>
        /// This is used for applying filter and sort using ESQL. This may present some challenges 
        /// when EF 7 is finalized, as it will no longer support ESQL nor be backed by ObjectContext
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity">The entity for which an object query is needed.</param>
        /// <returns>ObjectQuery&lt;T&gt;.</returns>
        public ObjectQuery<T> GetObjectQueryFor<T>(IDbSet<T> entity) where T : class
        {
            var oc = ((IObjectContextAdapter)this).ObjectContext;
            return oc.CreateObjectSet<T>();
        }

        protected override DbEntityValidationResult ValidateEntity(DbEntityEntry entityEntry, IDictionary<object, object> items)
        {
            var result = new DbEntityValidationResult(entityEntry, new List<DbValidationError>());

            //skip the custom validation if a security provider wasn't supplied
            if (SecurityProvider != null && entityEntry.Entity is Ticket && entityEntry.State == EntityState.Added)
            {
                var ticket = entityEntry.Entity as Ticket;
                if (!TicketActions.IsTicketActivityValid(ticket, TicketActivity.Create))
                {
                    result.ValidationErrors.Add(new
                        DbValidationError("authorization",
                        TicketDeskDomainText.ExceptionSecurityUserCannotCreateNewTicket));
                }
            }
            return result.ValidationErrors.Count > 0 ? result : base.ValidateEntity(entityEntry, items);
        }

        public override async Task<int> SaveChangesAsync()
        {
            var pendingEntityChanges = OnSaving();
            var result = await base.SaveChangesAsync();
            if (result > 0)
            {
                RaiseEntityChangeEvents(pendingEntityChanges);
            }
            return result;
        }

      
        public override int SaveChanges()
        {
            var pendingEntityChanges = OnSaving();
            var result = base.SaveChanges();
            if (result > 0)
            {
                RaiseEntityChangeEvents(pendingEntityChanges);
            }
            return result;
        }

        private void RaiseEntityChangeEvents(PendingEventEntities pendingEntityChanges)
        {
            RaiseTicketsChanged(this, pendingEntityChanges.PendingTicketChanges);
            RaiseNotificationsCreated(this, pendingEntityChanges.PendingEventNotificationChanges);
        }

        private PendingEventEntities OnSaving()
        {
            var pending = new PendingEventEntities();
            pending.PendingTicketChanges = GetTicketChanges().ToArray();
           
            if (SecurityProvider != null)
            {
                PreProcessNewTickets();
                PreProcessModifiedTickets(pending.PendingTicketChanges);
                //IMPORTANT! ticket event changes may not exist until after preprocess methods above 
                //  are called. Order dependent operations!
                CreateEventNotifications();
                pending.PendingEventNotificationChanges = GetTicketEventNotificationChanges().ToArray();
            }
            return pending;
        }

        private void CreateEventNotifications()
        {
            var pendingEventChanges = GetTicketEventChanges().ToArray();
            foreach (var change in pendingEventChanges)
            {
                change.CreateSubscriberEventNotifications();
            }
        }

        private void PreProcessModifiedTickets(IEnumerable<Ticket> ticketChanges)
        {
            foreach (var change in ticketChanges)
            {
                PrePopulateModifiedTicket(change);
            }
        }

        private void PreProcessNewTickets()
        {
            var ticketChanges = ChangeTracker.Entries<Ticket>().Where(t => t.State == EntityState.Added).Select(t => t.Entity);

            foreach (var change in ticketChanges)
            {
                PrePopulateNewTicket(change);
            }
        }

        private void PrePopulateModifiedTicket(Ticket modifiedTicket)
        {
            var o = ChangeTracker.Entries<Ticket>().Single(e => e.Entity.TicketId == modifiedTicket.TicketId);
            var now = DateTime.Now;

            modifiedTicket.LastUpdateBy = SecurityProvider.CurrentUserId;
            modifiedTicket.LastUpdateDate = now;

            if (o.State != EntityState.Added)//can't access orig values for new entities
            {
                var origTicket = (Ticket)o.OriginalValues.ToObject();
                if (modifiedTicket.TicketStatus != origTicket.TicketStatus)
                //if status change, force update to status by/date
                {
                    modifiedTicket.CurrentStatusDate = now;
                    modifiedTicket.CurrentStatusSetBy = SecurityProvider.CurrentUserId;
                }
            }
            modifiedTicket.EnsureSubscribers();
        }

        private void PrePopulateNewTicket(Ticket newTicket)
        {
            //TODO: Move this somewhere else?

            //TODO: double check owner if populated, make sure submitter can set this field if it isn't their id already
            //TODO: double check assigned if populated, make sure submitter can set this field.

            var now = DateTime.Now;
            newTicket.Owner = newTicket.Owner ?? SecurityProvider.CurrentUserId;
            newTicket.CreatedBy = SecurityProvider.CurrentUserId;
            newTicket.CreatedDate = now;
            newTicket.TicketStatus = TicketStatus.Active;
            newTicket.CurrentStatusDate = now;
            newTicket.CurrentStatusSetBy = SecurityProvider.CurrentUserId;

            //last update info will be set by PrePopulateModifiedTicket method, no need to set it here too

            if (newTicket.TagList != null && newTicket.TagList.Any())
            {
                newTicket.TicketTags.AddRange(newTicket.TagList.Split(',').Select(tag =>
                    new TicketTag
                    {
                        TagName = tag.Trim()
                    }));
            }

            var act = (newTicket.Owner != SecurityProvider.CurrentUserId)
                ? TicketActivity.CreateOnBehalfOf
                : TicketActivity.Create;

            newTicket.TicketEvents.AddActivityEvent(
                SecurityProvider.CurrentUserId,
                act,
                null,
                null,
                SecurityProvider.GetUserDisplayName(newTicket.Owner));
            newTicket.EnsureSubscribers();
        }

        private IEnumerable<Ticket> GetTicketChanges()
        {
            var pendingEventChanges = GetTicketEventChanges();

            var pendingTicketChanges = ChangeTracker.Entries<Ticket>()
                .Where(t => t.State != EntityState.Unchanged || pendingEventChanges.Select(c => c.TicketId).Contains(t.Entity.TicketId))
                .Select(t => t.Entity)
                .ToArray(); //execute now, because after save changes this query will return no results
            return pendingTicketChanges;
        }

        private IEnumerable<TicketEvent> GetTicketEventChanges()
        {
            var pendingEventChanges =
                ChangeTracker.Entries<TicketEvent>().Where(t => t.State != EntityState.Unchanged)
                .Select(t => t.Entity);

            return pendingEventChanges;
        }

        private IEnumerable<TicketEventNotification> GetTicketEventNotificationChanges()
        {
            var pendingEventNotificationChanges =
                ChangeTracker.Entries<TicketEventNotification>().Where(t => t.State != EntityState.Unchanged)
                .Select(t => t.Entity);

            return pendingEventNotificationChanges;
        }


        private class PendingEventEntities
        {
            public IEnumerable<Ticket> PendingTicketChanges { get; set; }
            public IEnumerable<TicketEventNotification> PendingEventNotificationChanges { get; set; }
        }
    }
}
