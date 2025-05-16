using Microsoft.AspNetCore.Identity;
using Roovia.Interfaces;
using Roovia.Models.UserCompanyModels;
using Roovia.Security;
using Roovia.Services;
using Roovia.Services.General;

namespace Roovia
{
    public static class ServiceExtensions
    {
        public static void RegisterApplicationServices(this IServiceCollection services)
        {
            // Register domain services
            services.AddScoped<IUser, UserService>();
            services.AddScoped<IPermissionService, PermissionService>();

            // Register business services
            services.AddScoped<IProperty, PropertyService>();
            services.AddScoped<IPropertyOwner, PropertyOwnerService>();
            services.AddScoped<ITenant, TenantService>();
            services.AddScoped<IBeneficiary, BeneficiaryService>();
            services.AddScoped<IVendor, VendorService>();
            services.AddScoped<IInspection, InspectionService>();
            services.AddScoped<IMaintenance, MaintenanceService>();
            services.AddScoped<IPayment, PaymentService>();
            services.AddScoped<ICdnService, CdnService>();

            // Register Email Services
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IEmailSender<ApplicationUser>, IdentityEmailSender>();

            // Register helper services
            services.AddScoped<INoteService, NoteService>();
            services.AddScoped<IReminderService, ReminderService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<ICommunicationService, CommunicationService>();
            services.AddScoped<IDocumentService, DocumentService>();
            services.AddScoped<IReportingService, ReportingService>();
            services.AddScoped<IAuditService, AuditService>();

            // Register utility services
            services.AddScoped<ToastService>();

            // Register seed data service
            services.AddScoped<ISeedDataService, SeedDataService>();
        }
    }
}