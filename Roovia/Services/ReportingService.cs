using Microsoft.EntityFrameworkCore;
using Roovia.Data;
using Roovia.Interfaces;
using Roovia.Models.BusinessHelperModels;
using Roovia.Models.ReportingModels;
using System.Text;

namespace Roovia.Services.General
{
    public class ReportingService : IReportingService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<ReportingService> _logger;
        private readonly ICdnService _cdnService;
        private readonly IEmailService _emailService;

        public ReportingService(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            ILogger<ReportingService> logger,
            ICdnService cdnService,
            IEmailService emailService)
        {
            _contextFactory = contextFactory;
            _logger = logger;
            _cdnService = cdnService;
            _emailService = emailService;
        }
        #region Dashboard Reports

        public async Task<ResponseModel> GetDashboardSummary(int companyId, int? branchId = null)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Get property count
                var propertyQuery = context.Properties.Where(p => p.CompanyId == companyId && !p.IsRemoved);
                if (branchId.HasValue)
                    propertyQuery = propertyQuery.Where(p => p.BranchId == branchId.Value);
                var propertyCount = await propertyQuery.CountAsync();

                // Get occupied property count
                var occupiedCount = await propertyQuery.Where(p => p.HasTenant).CountAsync();

                // Get tenant count
                var tenantQuery = context.PropertyTenants.Where(t => t.CompanyId == companyId && !t.IsRemoved);
                if (branchId.HasValue)
                    tenantQuery = tenantQuery.Where(t => t.Property.BranchId == branchId.Value);
                var tenantCount = await tenantQuery.CountAsync();

                // Get owner count
                var ownerQuery = context.PropertyOwners.Where(o => o.CompanyId == companyId && !o.IsRemoved);
                if (branchId.HasValue)
                    ownerQuery = ownerQuery.Where(o => o.Properties.Any(p => p.BranchId == branchId.Value));
                var ownerCount = await ownerQuery.CountAsync();

                // Get open maintenance tickets count
                var maintenanceQuery = context.MaintenanceTickets.Where(m => m.CompanyId == companyId);
                if (branchId.HasValue)
                    maintenanceQuery = maintenanceQuery.Where(m => m.Property.BranchId == branchId.Value);
                var openTicketsCount = await maintenanceQuery.Where(m => m.StatusId != 4).CountAsync(); // Assuming 4 is Completed

                // Get recent payments (last 30 days)
                var recentPaymentsQuery = context.PropertyPayments
                    .Where(p => p.CompanyId == companyId && p.PaymentDate >= DateTime.Now.AddDays(-30));
                if (branchId.HasValue)
                    recentPaymentsQuery = recentPaymentsQuery.Where(p => p.Property.BranchId == branchId.Value);
                var recentPaymentsAmount = await recentPaymentsQuery.SumAsync(p => p.Amount);

                // Get lease expiry count (next 30 days)
                var leaseExpiryQuery = context.PropertyTenants
                    .Where(t => t.CompanyId == companyId &&
                            !t.IsRemoved &&
                            t.LeaseEndDate >= DateTime.Now &&
                            t.LeaseEndDate <= DateTime.Now.AddDays(30));
                if (branchId.HasValue)
                    leaseExpiryQuery = leaseExpiryQuery.Where(t => t.Property.BranchId == branchId.Value);
                var leaseExpiryCount = await leaseExpiryQuery.CountAsync();

                response.Response = new
                {
                    PropertyCount = propertyCount,
                    OccupiedCount = occupiedCount,
                    VacancyRate = propertyCount > 0 ? (double)(propertyCount - occupiedCount) / propertyCount * 100 : 0,
                    TenantCount = tenantCount,
                    OwnerCount = ownerCount,
                    OpenTicketsCount = openTicketsCount,
                    RecentPaymentsAmount = recentPaymentsAmount,
                    LeaseExpiryCount = leaseExpiryCount
                };

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Dashboard summary retrieved successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dashboard summary: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving dashboard summary: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> GetUserDashboard(string userId)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Get user info
                var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null || !user.CompanyId.HasValue)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "User not found or not associated with a company";
                    return response;
                }

                int companyId = user.CompanyId.Value;

                // Get assigned maintenance tickets
                var assignedTicketsCount = await context.MaintenanceTickets
                    .Where(m => m.CompanyId == companyId &&
                            m.AssignedToUserId == userId &&
                            m.StatusId != 4) // Assuming 4 is Completed
                    .CountAsync();

                // Get overdue reminders
                var overdueRemindersCount = await context.Reminders
                    .Where(r => r.AssignedToUserId == userId &&
                            r.DueDate < DateTime.Now &&
                            r.ReminderStatusId == 1) // Assuming 1 is Pending
                    .CountAsync();

                // Get upcoming inspections (next 7 days)
                var upcomingInspectionsCount = await context.PropertyInspections
                    .Where(i => i.CompanyId == companyId &&
                            i.InspectorUserId == userId &&
                            i.ScheduledDate >= DateTime.Now &&
                            i.ScheduledDate <= DateTime.Now.AddDays(7) &&
                            i.StatusId == 1) // Assuming 1 is Scheduled
                    .CountAsync();

                // Get unread notifications
                var unreadNotificationsCount = await context.Notifications
                    .Where(n => n.RecipientUserId == userId && !n.IsRead)
                    .CountAsync();

                response.Response = new
                {
                    UserFullName = user.FullName,
                    AssignedTicketsCount = assignedTicketsCount,
                    OverdueRemindersCount = overdueRemindersCount,
                    UpcomingInspectionsCount = upcomingInspectionsCount,
                    UnreadNotificationsCount = unreadNotificationsCount
                };

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "User dashboard retrieved successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user dashboard: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving user dashboard: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> GetFinancialDashboard(int companyId, int? branchId = null)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Current month range
                var now = DateTime.Now;
                var startOfMonth = new DateTime(now.Year, now.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

                // Previous month range
                var startOfPrevMonth = startOfMonth.AddMonths(-1);
                var endOfPrevMonth = startOfMonth.AddDays(-1);

                // Build query base
                var paymentsQuery = context.PropertyPayments.Where(p => p.CompanyId == companyId);
                if (branchId.HasValue)
                    paymentsQuery = paymentsQuery.Where(p => p.Property.BranchId == branchId.Value);

                // Current month payments
                var currentMonthPayments = await paymentsQuery
                    .Where(p => p.PaymentDate >= startOfMonth && p.PaymentDate <= endOfMonth)
                    .SumAsync(p => p.Amount);

                // Previous month payments
                var prevMonthPayments = await paymentsQuery
                    .Where(p => p.PaymentDate >= startOfPrevMonth && p.PaymentDate <= endOfPrevMonth)
                    .SumAsync(p => p.Amount);

                // Expected monthly income (based on property rent amounts)
                var propertiesQuery = context.Properties.Where(p => p.CompanyId == companyId && !p.IsRemoved);
                if (branchId.HasValue)
                    propertiesQuery = propertiesQuery.Where(p => p.BranchId == branchId.Value);
                var expectedMonthlyIncome = await propertiesQuery.SumAsync(p => p.RentalAmount);

                // Get arrears (overdue payments)
                var tenantsQuery = context.PropertyTenants.Where(t => t.CompanyId == companyId && !t.IsRemoved);
                if (branchId.HasValue)
                    tenantsQuery = tenantsQuery.Where(t => t.Property.BranchId == branchId.Value);
                var totalArrears = await tenantsQuery.Where(t => t.Balance < 0).SumAsync(t => -t.Balance);

                // Get recent maintenance expenses
                var maintenanceQuery = context.MaintenanceExpenses.Where(m => m.MaintenanceTicket.CompanyId == companyId);
                if (branchId.HasValue)
                    maintenanceQuery = maintenanceQuery.Where(m => m.MaintenanceTicket.Property.BranchId == branchId.Value);
                var maintenanceExpenses = await maintenanceQuery
                    .Where(m => m.InvoiceDate >= startOfMonth && m.InvoiceDate <= endOfMonth)
                    .SumAsync(m => m.Amount);

                response.Response = new
                {
                    CurrentMonthPayments = currentMonthPayments,
                    PreviousMonthPayments = prevMonthPayments,
                    MonthlyChangePercentage = prevMonthPayments > 0 ?
                        ((currentMonthPayments - prevMonthPayments) / prevMonthPayments * 100) : 0,
                    ExpectedMonthlyIncome = expectedMonthlyIncome,
                    CollectionRate = expectedMonthlyIncome > 0 ?
                        (currentMonthPayments / expectedMonthlyIncome * 100) : 0,
                    TotalArrears = totalArrears,
                    MaintenanceExpenses = maintenanceExpenses
                };

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Financial dashboard retrieved successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving financial dashboard: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving financial dashboard: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> GetMaintenanceDashboard(int companyId, int? branchId = null)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Base query
                var ticketsQuery = context.MaintenanceTickets.Where(m => m.CompanyId == companyId);
                if (branchId.HasValue)
                    ticketsQuery = ticketsQuery.Where(m => m.Property.BranchId == branchId.Value);

                // Get tickets by status count
                var ticketsByStatus = await ticketsQuery
                    .GroupBy(m => m.StatusId)
                    .Select(g => new
                    {
                        StatusId = g.Key,
                        StatusName = g.First().Status.Name,
                        Count = g.Count()
                    })
                    .ToListAsync();

                // Get tickets by priority
                var ticketsByPriority = await ticketsQuery
                    .GroupBy(m => m.PriorityId)
                    .Select(g => new
                    {
                        PriorityId = g.Key,
                        PriorityName = g.First().Priority.Name,
                        Count = g.Count()
                    })
                    .ToListAsync();

                // Get tickets by category
                var ticketsByCategory = await ticketsQuery
                    .GroupBy(m => m.CategoryId)
                    .Select(g => new
                    {
                        CategoryId = g.Key,
                        CategoryName = g.First().Category.Name,
                        Count = g.Count()
                    })
                    .ToListAsync();

                // Get average resolution time
                var resolvedTickets = await ticketsQuery
                    .Where(m => m.CompletedDate.HasValue)
                    .Select(m => new
                    {
                        ResolutionTime = (m.CompletedDate.Value - m.CreatedOn).TotalHours
                    })
                    .ToListAsync();

                double avgResolutionTime = resolvedTickets.Count > 0 ?
                    resolvedTickets.Average(t => t.ResolutionTime) : 0;

                // Get top vendors by ticket count
                var topVendors = await ticketsQuery
                    .Where(m => m.VendorId.HasValue)
                    .GroupBy(m => m.VendorId.Value)
                    .Select(g => new
                    {
                        VendorId = g.Key,
                        VendorName = g.First().Vendor.Name,
                        Count = g.Count()
                    })
                    .OrderByDescending(v => v.Count)
                    .Take(5)
                    .ToListAsync();

                response.Response = new
                {
                    TicketsByStatus = ticketsByStatus,
                    TicketsByPriority = ticketsByPriority,
                    TicketsByCategory = ticketsByCategory,
                    AverageResolutionTimeHours = avgResolutionTime,
                    TopVendors = topVendors
                };

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Maintenance dashboard retrieved successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving maintenance dashboard: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving maintenance dashboard: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> GetOccupancyDashboard(int companyId, int? branchId = null)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Base query
                var propertiesQuery = context.Properties.Where(p => p.CompanyId == companyId && !p.IsRemoved);
                if (branchId.HasValue)
                    propertiesQuery = propertiesQuery.Where(p => p.BranchId == branchId.Value);

                // Get total property count
                var totalCount = await propertiesQuery.CountAsync();

                // Get occupied property count
                var occupiedCount = await propertiesQuery.Where(p => p.HasTenant).CountAsync();

                // Get vacant property count
                var vacantCount = totalCount - occupiedCount;

                // Calculate vacancy rate
                double vacancyRate = totalCount > 0 ? (double)vacantCount / totalCount * 100 : 0;

                // Get properties by type
                var propertiesByType = await propertiesQuery
                    .GroupBy(p => p.PropertyTypeId)
                    .Select(g => new
                    {
                        TypeId = g.Key,
                        TypeName = g.First().PropertyType.Name,
                        Count = g.Count(),
                        OccupiedCount = g.Count(p => p.HasTenant),
                        VacantCount = g.Count(p => !p.HasTenant)
                    })
                    .ToListAsync();

                // Get upcoming lease expirations (next 90 days)
                var leaseExpirations = await context.PropertyTenants
                    .Where(t => t.CompanyId == companyId &&
                            !t.IsRemoved &&
                            t.LeaseEndDate >= DateTime.Now &&
                            t.LeaseEndDate <= DateTime.Now.AddDays(90))
                    .GroupBy(t => new
                    {
                        Year = t.LeaseEndDate.Year,
                        Month = t.LeaseEndDate.Month
                    })
                    .Select(g => new
                    {
                        Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                        Count = g.Count()
                    })
                    .OrderBy(r => r.Period)
                    .ToListAsync();

                // Get average lease duration
                var averageLeaseDuration = await context.PropertyTenants
                    .Where(t => t.CompanyId == companyId && !t.IsRemoved)
                    .Select(t => (t.LeaseEndDate - t.LeaseStartDate).TotalDays / 30) // Convert to months
                    .DefaultIfEmpty(0)
                    .AverageAsync();

                response.Response = new
                {
                    TotalProperties = totalCount,
                    OccupiedProperties = occupiedCount,
                    VacantProperties = vacantCount,
                    VacancyRate = vacancyRate,
                    PropertiesByType = propertiesByType,
                    LeaseExpirations = leaseExpirations,
                    AverageLeaseDurationMonths = averageLeaseDuration
                };

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Occupancy dashboard retrieved successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving occupancy dashboard: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving occupancy dashboard: {ex.Message}";
            }

            return response;
        }

        #endregion Dashboard Reports

        #region Property Reports

        public async Task<ResponseModel> GetPropertyPerformanceReport(int companyId, int? branchId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Set default date range if not provided
                startDate ??= DateTime.Now.AddMonths(-12);
                endDate ??= DateTime.Now;

                // Base query
                var propertiesQuery = context.Properties.Where(p => p.CompanyId == companyId && !p.IsRemoved);
                if (branchId.HasValue)
                    propertiesQuery = propertiesQuery.Where(p => p.BranchId == branchId.Value);

                // Get property performance data
                var propertyPerformance = await propertiesQuery
                    .Select(p => new
                    {
                        p.Id,
                        p.PropertyName,
                        p.PropertyCode,
                        OwnerName = p.Owner.DisplayName,
                        RentalAmount = p.RentalAmount,
                        CurrentTenantName = p.CurrentTenantId.HasValue ?
                            p.Tenants.FirstOrDefault(t => t.Id == p.CurrentTenantId).DisplayName : null,
                        HasTenant = p.HasTenant,
                        Payments = p.Payments
                            .Where(pmt => pmt.PaymentDate >= startDate && pmt.PaymentDate <= endDate)
                            .Sum(pmt => pmt.Amount),
                        MaintenanceCosts = p.MaintenanceTickets
                            .SelectMany(m => m.Expenses)
                            .Where(e => e.InvoiceDate >= startDate && e.InvoiceDate <= endDate)
                            .Sum(e => e.Amount),
                        MaintenanceTicketCount = p.MaintenanceTickets
                            .Count(m => m.CreatedOn >= startDate && m.CreatedOn <= endDate),
                        DaysVacant = p.HasTenant ? 0 : (int?)(DateTime.Now - (p.Tenants
                            .Where(t => t.MoveOutDate.HasValue)
                            .OrderByDescending(t => t.MoveOutDate)
                            .FirstOrDefault().MoveOutDate ?? DateTime.Now)).TotalDays
                    })
                    .ToListAsync();

                // Calculate performance metrics
                var performanceResults = propertyPerformance.Select(p => new
                {
                    p.Id,
                    p.PropertyName,
                    p.PropertyCode,
                    p.OwnerName,
                    p.RentalAmount,
                    p.CurrentTenantName,
                    p.HasTenant,
                    TotalRevenue = p.Payments,
                    TotalExpenses = p.MaintenanceCosts,
                    NetIncome = p.Payments - p.MaintenanceCosts,
                    ROI = p.RentalAmount > 0 ?
                        ((p.Payments - p.MaintenanceCosts) / (p.RentalAmount * 12)) * 100 : 0,
                    MaintenanceCostPercentage = p.Payments > 0 ?
                        (p.MaintenanceCosts / p.Payments) * 100 : 0,
                    MaintenanceTicketCount = p.MaintenanceTicketCount,
                    DaysVacant = p.DaysVacant,
                    VacancyRate = p.DaysVacant > 0 ?
                        (double)p.DaysVacant / (endDate.Value - startDate.Value).TotalDays * 100 : 0
                }).OrderByDescending(p => p.ROI).ToList();

                response.Response = new
                {
                    DateRange = new { Start = startDate, End = endDate },
                    Properties = performanceResults,
                    Summary = new
                    {
                        TotalProperties = performanceResults.Count,
                        TotalRevenue = performanceResults.Sum(p => p.TotalRevenue),
                        TotalExpenses = performanceResults.Sum(p => p.TotalExpenses),
                        TotalNetIncome = performanceResults.Sum(p => p.NetIncome),
                        AverageROI = performanceResults.Count > 0 ? performanceResults.Average(p => p.ROI) : 0,
                        AverageMaintenanceCost = performanceResults.Count > 0 ? performanceResults.Average(p => p.MaintenanceCostPercentage) : 0,
                        OccupancyRate = performanceResults.Count > 0 ?
                            (double)performanceResults.Count(p => p.HasTenant) / performanceResults.Count * 100 : 0
                    }
                };

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Property performance report generated successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating property performance report: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error generating property performance report: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> GetVacancyReport(int companyId, int? branchId = null)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Base query
                var propertiesQuery = context.Properties.Where(p => p.CompanyId == companyId && !p.IsRemoved);
                if (branchId.HasValue)
                    propertiesQuery = propertiesQuery.Where(p => p.BranchId == branchId.Value);

                // Get vacant properties
                var vacantProperties = await propertiesQuery
                    .Where(p => !p.HasTenant)
                    .Select(p => new
                    {
                        p.Id,
                        p.PropertyName,
                        p.PropertyCode,
                        p.PropertyTypeId,
                        PropertyTypeName = p.PropertyType.Name,
                        p.RentalAmount,
                        OwnerName = p.Owner.DisplayName,
                        Address = new
                        {
                            p.Address.Street,
                            p.Address.City,
                            p.Address.Province,
                            p.Address.PostalCode
                        },
                        DaysVacant = (int?)(DateTime.Now - (p.Tenants
                            .Where(t => t.MoveOutDate.HasValue)
                            .OrderByDescending(t => t.MoveOutDate)
                            .FirstOrDefault().MoveOutDate ?? p.CreatedOn)).TotalDays,
                        LastTenantName = p.Tenants
                            .Where(t => t.MoveOutDate.HasValue)
                            .OrderByDescending(t => t.MoveOutDate)
                            .FirstOrDefault().DisplayName,
                        LastMoveOutDate = p.Tenants
                            .Where(t => t.MoveOutDate.HasValue)
                            .OrderByDescending(t => t.MoveOutDate)
                            .FirstOrDefault().MoveOutDate
                    })
                    .OrderByDescending(p => p.DaysVacant)
                    .ToListAsync();

                // Get vacancy statistics by property type
                var vacancyByType = await propertiesQuery
                    .GroupBy(p => new { p.PropertyTypeId, TypeName = p.PropertyType.Name })
                    .Select(g => new
                    {
                        g.Key.PropertyTypeId,
                        g.Key.TypeName,
                        TotalCount = g.Count(),
                        VacantCount = g.Count(p => !p.HasTenant),
                        OccupiedCount = g.Count(p => p.HasTenant),
                        VacancyRate = g.Count() > 0 ?
                            (double)g.Count(p => !p.HasTenant) / g.Count() * 100 : 0
                    })
                    .ToListAsync();

                // Get vacancy statistics by location
                var vacancyByLocation = await propertiesQuery
                    .GroupBy(p => p.Address.City)
                    .Select(g => new
                    {
                        City = g.Key,
                        TotalCount = g.Count(),
                        VacantCount = g.Count(p => !p.HasTenant),
                        OccupiedCount = g.Count(p => p.HasTenant),
                        VacancyRate = g.Count() > 0 ?
                            (double)g.Count(p => !p.HasTenant) / g.Count() * 100 : 0
                    })
                    .ToListAsync();

                // Calculate total vacancy rate
                var totalCount = await propertiesQuery.CountAsync();
                var vacantCount = await propertiesQuery.CountAsync(p => !p.HasTenant);
                double totalVacancyRate = totalCount > 0 ? (double)vacantCount / totalCount * 100 : 0;

                // Calculate potential lost revenue
                decimal potentialLostRevenue = vacantProperties.Sum(p => p.RentalAmount);

                response.Response = new
                {
                    TotalProperties = totalCount,
                    VacantProperties = vacantCount,
                    VacancyRate = totalVacancyRate,
                    PotentialMonthlyLostRevenue = potentialLostRevenue,
                    VacantPropertiesList = vacantProperties,
                    VacancyByPropertyType = vacancyByType,
                    VacancyByLocation = vacancyByLocation
                };

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Vacancy report generated successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating vacancy report: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error generating vacancy report: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> GetPropertyValuationReport(int companyId, int? branchId = null)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Base query for properties
                var propertiesQuery = context.Properties.Where(p => p.CompanyId == companyId && !p.IsRemoved);
                if (branchId.HasValue)
                    propertiesQuery = propertiesQuery.Where(p => p.BranchId == branchId.Value);

                // Calculate annual rental yield
                var propertyValuations = await propertiesQuery
                    .Select(p => new
                    {
                        p.Id,
                        p.PropertyName,
                        p.PropertyCode,
                        PropertyType = p.PropertyType.Name,
                        OwnerName = p.Owner.DisplayName,
                        p.RentalAmount,
                        AnnualRentalIncome = p.RentalAmount * 12,
                        HasTenant = p.HasTenant,
                        Address = new
                        {
                            p.Address.Street,
                            p.Address.City,
                            p.Address.Province,
                            p.Address.PostalCode
                        },
                        // You would need to add a purchase price or estimated value field to your Property model
                        // For now, we'll use a calculated value based on rental amount as placeholder
                        EstimatedValue = p.RentalAmount * 120, // Rough estimate: 10 years worth of rent
                        LastYearMaintenanceCosts = p.MaintenanceTickets
                            .SelectMany(m => m.Expenses)
                            .Where(e => e.InvoiceDate >= DateTime.Now.AddYears(-1))
                            .Sum(e => e.Amount)
                    })
                    .ToListAsync();

                // Calculate derived metrics
                var valuationResults = propertyValuations.Select(p => new
                {
                    p.Id,
                    p.PropertyName,
                    p.PropertyCode,
                    p.PropertyType,
                    p.OwnerName,
                    p.RentalAmount,
                    p.AnnualRentalIncome,
                    p.EstimatedValue,
                    GrossYield = p.EstimatedValue > 0 ?
                        (p.AnnualRentalIncome / p.EstimatedValue) * 100 : 0,
                    NetYield = p.EstimatedValue > 0 ?
                        ((p.AnnualRentalIncome - p.LastYearMaintenanceCosts) / p.EstimatedValue) * 100 : 0,
                    p.Address,
                    p.HasTenant,
                    p.LastYearMaintenanceCosts
                }).OrderByDescending(p => p.NetYield).ToList();

                // Group valuations by type
                var valuationsByType = valuationResults
                    .GroupBy(p => p.PropertyType)
                    .Select(g => new
                    {
                        PropertyType = g.Key,
                        Count = g.Count(),
                        AverageValue = g.Average(p => p.EstimatedValue),
                        AverageRent = g.Average(p => p.RentalAmount),
                        AverageGrossYield = g.Average(p => p.GrossYield),
                        AverageNetYield = g.Average(p => p.NetYield),
                        TotalValue = g.Sum(p => p.EstimatedValue)
                    })
                    .OrderByDescending(g => g.TotalValue)
                    .ToList();

                // Group valuations by location
                var valuationsByLocation = valuationResults
                    .GroupBy(p => p.Address.City)
                    .Select(g => new
                    {
                        City = g.Key,
                        Count = g.Count(),
                        AverageValue = g.Average(p => p.EstimatedValue),
                        AverageRent = g.Average(p => p.RentalAmount),
                        AverageGrossYield = g.Average(p => p.GrossYield),
                        AverageNetYield = g.Average(p => p.NetYield),
                        TotalValue = g.Sum(p => p.EstimatedValue)
                    })
                    .OrderByDescending(g => g.TotalValue)
                    .ToList();

                response.Response = new
                {
                    TotalProperties = valuationResults.Count,
                    TotalPortfolioValue = valuationResults.Sum(p => p.EstimatedValue),
                    AveragePropertyValue = valuationResults.Count > 0 ?
                        valuationResults.Average(p => p.EstimatedValue) : 0,
                    AverageGrossYield = valuationResults.Count > 0 ?
                        valuationResults.Average(p => p.GrossYield) : 0,
                    AverageNetYield = valuationResults.Count > 0 ?
                        valuationResults.Average(p => p.NetYield) : 0,
                    Properties = valuationResults,
                    ValuationsByPropertyType = valuationsByType,
                    ValuationsByLocation = valuationsByLocation
                };

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Property valuation report generated successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating property valuation report: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error generating property valuation report: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> GetPropertyMaintenanceReport(int companyId, int? branchId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Set default date range if not provided
                startDate ??= DateTime.Now.AddMonths(-12);
                endDate ??= DateTime.Now;

                // Base query
                var maintenanceQuery = context.MaintenanceTickets
                    .Where(m => m.CompanyId == companyId && m.CreatedOn >= startDate && m.CreatedOn <= endDate);
                if (branchId.HasValue)
                    maintenanceQuery = maintenanceQuery.Where(m => m.Property.BranchId == branchId.Value);

                // Get maintenance tickets grouped by property
                var propertyMaintenanceData = await maintenanceQuery
                    .GroupBy(m => new
                    {
                        PropertyId = m.PropertyId,
                        PropertyName = m.Property.PropertyName,
                        PropertyCode = m.Property.PropertyCode
                    })
                    .Select(g => new
                    {
                        g.Key.PropertyId,
                        g.Key.PropertyName,
                        g.Key.PropertyCode,
                        TicketCount = g.Count(),
                        OpenTickets = g.Count(m => m.StatusId != 4), // Assuming 4 is Completed
                        TotalCost = g.SelectMany(m => m.Expenses).Sum(e => e.Amount),
                        AvgResolutionTime = g.Where(m => m.CompletedDate.HasValue)
                            .Select(m => (m.CompletedDate.Value - m.CreatedOn).TotalHours)
                            .DefaultIfEmpty(0)
                            .Average(),
                        TicketsByCategory = g.GroupBy(m => new { m.CategoryId, CategoryName = m.Category.Name })
                            .Select(c => new
                            {
                                c.Key.CategoryId,
                                c.Key.CategoryName,
                                Count = c.Count()
                            })
                            .OrderByDescending(c => c.Count)
                            .ToList()
                    })
                    .OrderByDescending(p => p.TotalCost)
                    .ToListAsync();

                // Get average cost per property
                decimal totalMaintenanceCost = propertyMaintenanceData.Sum(p => p.TotalCost);
                int totalTickets = propertyMaintenanceData.Sum(p => p.TicketCount);
                decimal avgCostPerProperty = propertyMaintenanceData.Count > 0 ?
                    totalMaintenanceCost / propertyMaintenanceData.Count : 0;
                decimal avgCostPerTicket = totalTickets > 0 ?
                    totalMaintenanceCost / totalTickets : 0;

                // Get maintenance data by category
                var maintenanceByCategory = await maintenanceQuery
                    .GroupBy(m => new { m.CategoryId, CategoryName = m.Category.Name })
                    .Select(g => new
                    {
                        g.Key.CategoryId,
                        g.Key.CategoryName,
                        TicketCount = g.Count(),
                        TotalCost = g.SelectMany(m => m.Expenses).Sum(e => e.Amount),
                        AvgCostPerTicket = g.Count() > 0 ?
                            g.SelectMany(m => m.Expenses).Sum(e => e.Amount) / g.Count() : 0
                    })
                    .OrderByDescending(c => c.TotalCost)
                    .ToListAsync();

                // Get maintenance data by month
                var maintenanceByMonth = await maintenanceQuery
                    .GroupBy(m => new { Year = m.CreatedOn.Year, Month = m.CreatedOn.Month })
                    .Select(g => new
                    {
                        Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                        TicketCount = g.Count(),
                        TotalCost = g.SelectMany(m => m.Expenses).Sum(e => e.Amount)
                    })
                    .OrderBy(m => m.Period)
                    .ToListAsync();

                response.Response = new
                {
                    DateRange = new { Start = startDate, End = endDate },
                    TotalProperties = propertyMaintenanceData.Count,
                    TotalMaintenanceCost = totalMaintenanceCost,
                    TotalTickets = totalTickets,
                    AverageCostPerProperty = avgCostPerProperty,
                    AverageCostPerTicket = avgCostPerTicket,
                    PropertyMaintenanceData = propertyMaintenanceData,
                    MaintenanceByCategory = maintenanceByCategory,
                    MaintenanceByMonth = maintenanceByMonth
                };

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Property maintenance report generated successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating property maintenance report: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error generating property maintenance report: {ex.Message}";
            }

            return response;
        }

        #endregion Property Reports

        #region Financial Reports

        public async Task<ResponseModel> GetIncomeReport(int companyId, int? branchId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Set default date range if not provided
                startDate ??= DateTime.Now.AddMonths(-12);
                endDate ??= DateTime.Now;

                // Base query for payments
                var paymentsQuery = context.PropertyPayments
                    .Where(p => p.CompanyId == companyId &&
                           p.PaymentDate >= startDate &&
                           p.PaymentDate <= endDate);
                if (branchId.HasValue)
                    paymentsQuery = paymentsQuery.Where(p => p.Property.BranchId == branchId.Value);

                // Get payments by month
                var paymentsByMonth = await paymentsQuery
                    .GroupBy(p => new { Year = p.PaymentDate.Value.Year, Month = p.PaymentDate.Value.Month })
                    .Select(g => new
                    {
                        Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                        Count = g.Count(),
                        Total = g.Sum(p => p.Amount)
                    })
                    .OrderBy(p => p.Period)
                    .ToListAsync();

                // Get payments by payment type
                var paymentsByType = await paymentsQuery
                    .GroupBy(p => new { p.PaymentTypeId, TypeName = p.PaymentType.Name })
                    .Select(g => new
                    {
                        g.Key.PaymentTypeId,
                        g.Key.TypeName,
                        Count = g.Count(),
                        Total = g.Sum(p => p.Amount)
                    })
                    .OrderByDescending(p => p.Total)
                    .ToListAsync();

                // Get payments by property
                var paymentsByProperty = await paymentsQuery
                    .GroupBy(p => new
                    {
                        PropertyId = p.PropertyId,
                        PropertyName = p.Property.PropertyName,
                        PropertyCode = p.Property.PropertyCode
                    })
                    .Select(g => new
                    {
                        g.Key.PropertyId,
                        g.Key.PropertyName,
                        g.Key.PropertyCode,
                        Count = g.Count(),
                        Total = g.Sum(p => p.Amount)
                    })
                    .OrderByDescending(p => p.Total)
                    .ToListAsync();

                // Get payments by tenant
                var paymentsByTenant = await paymentsQuery
                    .Where(p => p.TenantId.HasValue)
                    .GroupBy(p => new
                    {
                        TenantId = p.TenantId.Value,
                        TenantName = p.Tenant.DisplayName
                    })
                    .Select(g => new
                    {
                        g.Key.TenantId,
                        g.Key.TenantName,
                        Count = g.Count(),
                        Total = g.Sum(p => p.Amount)
                    })
                    .OrderByDescending(p => p.Total)
                    .ToListAsync();

                // Get total and average values
                var totalPayments = await paymentsQuery.SumAsync(p => p.Amount);
                var paymentCount = await paymentsQuery.CountAsync();
                var avgPaymentAmount = paymentCount > 0 ? totalPayments / paymentCount : 0;

                // Calculate monthly averages and growth
                var monthlyAverages = new List<object>();
                decimal? previousMonthTotal = null;

                foreach (var month in paymentsByMonth)
                {
                    decimal growth = 0;
                    if (previousMonthTotal.HasValue && previousMonthTotal.Value > 0)
                    {
                        growth = ((month.Total - previousMonthTotal.Value) / previousMonthTotal.Value) * 100;
                    }

                    monthlyAverages.Add(new
                    {
                        month.Period,
                        month.Total,
                        month.Count,
                        AveragePayment = month.Count > 0 ? month.Total / month.Count : 0,
                        GrowthPercentage = growth
                    });

                    previousMonthTotal = month.Total;
                }

                response.Response = new
                {
                    DateRange = new { Start = startDate, End = endDate },
                    TotalPayments = totalPayments,
                    PaymentCount = paymentCount,
                    AveragePaymentAmount = avgPaymentAmount,
                    PaymentsByMonth = paymentsByMonth,
                    MonthlyAnalysis = monthlyAverages,
                    PaymentsByType = paymentsByType,
                    PaymentsByProperty = paymentsByProperty,
                    PaymentsByTenant = paymentsByTenant
                };

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Income report generated successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating income report: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error generating income report: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> GetExpenseReport(int companyId, int? branchId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Set default date range if not provided
                startDate ??= DateTime.Now.AddMonths(-12);
                endDate ??= DateTime.Now;

                // Base query for maintenance expenses
                var expensesQuery = context.MaintenanceExpenses
                    .Where(e => e.MaintenanceTicket.CompanyId == companyId &&
                           e.InvoiceDate >= startDate &&
                           e.InvoiceDate <= endDate);
                if (branchId.HasValue)
                    expensesQuery = expensesQuery.Where(e => e.MaintenanceTicket.Property.BranchId == branchId.Value);

                // Get expenses by month
                var expensesByMonth = await expensesQuery
                    .GroupBy(e => new { Year = e.InvoiceDate.Value.Year, Month = e.InvoiceDate.Value.Month })
                    .Select(g => new
                    {
                        Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                        Count = g.Count(),
                        Total = g.Sum(e => e.Amount)
                    })
                    .OrderBy(e => e.Period)
                    .ToListAsync();

                // Get expenses by category
                var expensesByCategory = await expensesQuery
                    .GroupBy(e => new { e.CategoryId, CategoryName = e.Category.Name })
                    .Select(g => new
                    {
                        g.Key.CategoryId,
                        g.Key.CategoryName,
                        Count = g.Count(),
                        Total = g.Sum(e => e.Amount)
                    })
                    .OrderByDescending(e => e.Total)
                    .ToListAsync();

                // Get expenses by property
                var expensesByProperty = await expensesQuery
                    .GroupBy(e => new
                    {
                        PropertyId = e.MaintenanceTicket.PropertyId,
                        PropertyName = e.MaintenanceTicket.Property.PropertyName,
                        PropertyCode = e.MaintenanceTicket.Property.PropertyCode
                    })
                    .Select(g => new
                    {
                        g.Key.PropertyId,
                        g.Key.PropertyName,
                        g.Key.PropertyCode,
                        Count = g.Count(),
                        Total = g.Sum(e => e.Amount)
                    })
                    .OrderByDescending(e => e.Total)
                    .ToListAsync();

                // Get expenses by vendor
                var expensesByVendor = await expensesQuery
                    .Where(e => e.VendorId.HasValue)
                    .GroupBy(e => new
                    {
                        VendorId = e.VendorId.Value,
                        VendorName = e.Vendor.Name
                    })
                    .Select(g => new
                    {
                        g.Key.VendorId,
                        g.Key.VendorName,
                        Count = g.Count(),
                        Total = g.Sum(e => e.Amount)
                    })
                    .OrderByDescending(e => e.Total)
                    .ToListAsync();

                // Get total and average values
                var totalExpenses = await expensesQuery.SumAsync(e => e.Amount);
                var expenseCount = await expensesQuery.CountAsync();
                var avgExpenseAmount = expenseCount > 0 ? totalExpenses / expenseCount : 0;

                // Calculate monthly averages and growth
                var monthlyAverages = new List<object>();
                decimal? previousMonthTotal = null;

                foreach (var month in expensesByMonth)
                {
                    decimal growth = 0;
                    if (previousMonthTotal.HasValue && previousMonthTotal.Value > 0)
                    {
                        growth = ((month.Total - previousMonthTotal.Value) / previousMonthTotal.Value) * 100;
                    }

                    monthlyAverages.Add(new
                    {
                        month.Period,
                        month.Total,
                        month.Count,
                        AverageExpense = month.Count > 0 ? month.Total / month.Count : 0,
                        GrowthPercentage = growth
                    });

                    previousMonthTotal = month.Total;
                }

                response.Response = new
                {
                    DateRange = new { Start = startDate, End = endDate },
                    TotalExpenses = totalExpenses,
                    ExpenseCount = expenseCount,
                    AverageExpenseAmount = avgExpenseAmount,
                    ExpensesByMonth = expensesByMonth,
                    MonthlyAnalysis = monthlyAverages,
                    ExpensesByCategory = expensesByCategory,
                    ExpensesByProperty = expensesByProperty,
                    ExpensesByVendor = expensesByVendor
                };

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Expense report generated successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating expense report: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error generating expense report: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> GetCashFlowReport(int companyId, int? branchId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Set default date range if not provided
                startDate ??= DateTime.Now.AddMonths(-12);
                endDate ??= DateTime.Now;

                // Get payments by month
                var paymentsQuery = context.PropertyPayments
                    .Where(p => p.CompanyId == companyId &&
                           p.PaymentDate >= startDate &&
                           p.PaymentDate <= endDate);
                if (branchId.HasValue)
                    paymentsQuery = paymentsQuery.Where(p => p.Property.BranchId == branchId.Value);

                var paymentsByMonth = await paymentsQuery
                    .GroupBy(p => new { Year = p.PaymentDate.Value.Year, Month = p.PaymentDate.Value.Month })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                        Income = g.Sum(p => p.Amount)
                    })
                    .OrderBy(p => p.Year).ThenBy(p => p.Month)
                    .ToListAsync();

                // Get expenses by month
                var expensesQuery = context.MaintenanceExpenses
                    .Where(e => e.MaintenanceTicket.CompanyId == companyId &&
                           e.InvoiceDate >= startDate &&
                           e.InvoiceDate <= endDate);
                if (branchId.HasValue)
                    expensesQuery = expensesQuery.Where(e => e.MaintenanceTicket.Property.BranchId == branchId.Value);

                var expensesByMonth = await expensesQuery
                    .GroupBy(e => new { Year = e.InvoiceDate.Value.Year, Month = e.InvoiceDate.Value.Month })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                        Expenses = g.Sum(e => e.Amount)
                    })
                    .OrderBy(e => e.Year).ThenBy(e => e.Month)
                    .ToListAsync();

                // Combine payments and expenses into monthly cash flow
                var allMonths = paymentsByMonth.Select(p => new { p.Year, p.Month })
                    .Union(expensesByMonth.Select(e => new { e.Year, e.Month }))
                    .OrderBy(m => m.Year).ThenBy(m => m.Month)
                    .ToList();

                var cashFlow = new List<object>();
                decimal cumulativeNetIncome = 0;

                foreach (var month in allMonths)
                {
                    var payment = paymentsByMonth.FirstOrDefault(p => p.Year == month.Year && p.Month == month.Month);
                    var expense = expensesByMonth.FirstOrDefault(e => e.Year == month.Year && e.Month == month.Month);

                    decimal income = payment?.Income ?? 0;
                    decimal expenses = expense?.Expenses ?? 0;
                    decimal netIncome = income - expenses;
                    cumulativeNetIncome += netIncome;

                    cashFlow.Add(new
                    {
                        Period = $"{month.Year}-{month.Month:D2}",
                        Income = income,
                        Expenses = expenses,
                        NetIncome = netIncome,
                        CumulativeNetIncome = cumulativeNetIncome
                    });
                }

                // Get total values
                var totalIncome = paymentsByMonth.Sum(p => p.Income);
                var totalExpenses = expensesByMonth.Sum(e => e.Expenses);
                var totalNetIncome = totalIncome - totalExpenses;

                // Get property-specific cash flow
                var propertiesQuery = context.Properties.Where(p => p.CompanyId == companyId && !p.IsRemoved);
                if (branchId.HasValue)
                    propertiesQuery = propertiesQuery.Where(p => p.BranchId == branchId.Value);

                var propertyCashFlow = await propertiesQuery
                    .Select(p => new
                    {
                        p.Id,
                        p.PropertyName,
                        p.PropertyCode,
                        Income = p.Payments
                            .Where(pmt => pmt.PaymentDate >= startDate && pmt.PaymentDate <= endDate)
                            .Sum(pmt => pmt.Amount),
                        Expenses = p.MaintenanceTickets
                            .SelectMany(m => m.Expenses)
                            .Where(e => e.InvoiceDate >= startDate && e.InvoiceDate <= endDate)
                            .Sum(e => e.Amount)
                    })
                    .ToListAsync();

                var propertyCashFlowResults = propertyCashFlow
                    .Select(p => new
                    {
                        p.Id,
                        p.PropertyName,
                        p.PropertyCode,
                        p.Income,
                        p.Expenses,
                        NetIncome = p.Income - p.Expenses,
                        ProfitMargin = p.Income > 0 ?
                            ((p.Income - p.Expenses) / p.Income) * 100 : 0
                    })
                    .OrderByDescending(p => p.NetIncome)
                    .ToList();

                response.Response = new
                {
                    DateRange = new { Start = startDate, End = endDate },
                    TotalIncome = totalIncome,
                    TotalExpenses = totalExpenses,
                    TotalNetIncome = totalNetIncome,
                    MonthlyCashFlow = cashFlow,
                    PropertyCashFlow = propertyCashFlowResults
                };

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Cash flow report generated successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating cash flow report: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error generating cash flow report: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> GetArrearsReport(int companyId, int? branchId = null)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Base query
                var tenantsQuery = context.PropertyTenants.Where(t => t.CompanyId == companyId && !t.IsRemoved);
                if (branchId.HasValue)
                    tenantsQuery = tenantsQuery.Where(t => t.Property.BranchId == branchId.Value);

                // Get tenants in arrears
                var tenantsInArrears = await tenantsQuery
                    .Where(t => t.Balance < 0)
                    .Select(t => new
                    {
                        t.Id,
                        t.DisplayName,
                        PropertyId = t.PropertyId,
                        PropertyName = t.Property.PropertyName,
                        PropertyCode = t.Property.PropertyCode,
                        t.RentAmount,
                        Balance = t.Balance,
                        ArrearsAmount = -t.Balance, // Convert negative balance to positive arrears amount
                        LastPaymentDate = t.LastPaymentDate,
                        LastPaymentAmount = t.Payments
                            .OrderByDescending(p => p.PaymentDate)
                            .FirstOrDefault().Amount,
                        DaysInArrears = t.LastPaymentDate.HasValue ?
                            (int)(DateTime.Now - t.LastPaymentDate.Value).TotalDays : 0,
                        ContactEmail = t.EmailAddresses
                            .FirstOrDefault(e => e.IsPrimary)
                            .EmailAddress,
                        ContactPhone = t.ContactNumbers
                            .FirstOrDefault(c => c.IsPrimary)
                            .Number
                    })
                    .OrderByDescending(t => t.ArrearsAmount)
                    .ToListAsync();

                // Calculate arrears statistics
                decimal totalArrearsAmount = tenantsInArrears.Sum(t => t.ArrearsAmount);

                // Group arrears by age
                var arrearsAging = tenantsInArrears
                    .GroupBy(t => t.DaysInArrears switch
                    {
                        var d when d <= 30 => "1-30 Days",
                        var d when d <= 60 => "31-60 Days",
                        var d when d <= 90 => "61-90 Days",
                        _ => "90+ Days"
                    })
                    .Select(g => new
                    {
                        AgeGroup = g.Key,
                        Count = g.Count(),
                        TotalAmount = g.Sum(t => t.ArrearsAmount)
                    })
                    .OrderBy(g => g.AgeGroup)
                    .ToList();

                response.Response = new
                {
                    TotalTenantsInArrears = tenantsInArrears.Count,
                    TotalArrearsAmount = totalArrearsAmount,
                    ArrearsRate = tenantsQuery.Count() > 0 ?
                        (double)tenantsInArrears.Count / tenantsQuery.Count() * 100 : 0,
                    TenantsInArrears = tenantsInArrears,
                    ArrearsAging = arrearsAging
                };

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Arrears report generated successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating arrears report: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error generating arrears report: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> GetBeneficiaryPaymentsReport(int companyId, int? branchId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Set default date range if not provided
                startDate ??= DateTime.Now.AddMonths(-12);
                endDate ??= DateTime.Now;

                // Base query for beneficiary payments
                var beneficiaryPaymentsQuery = context.BeneficiaryPayments
                    .Where(bp => bp.Beneficiary.CompanyId == companyId &&
                           bp.PaymentDate >= startDate &&
                           bp.PaymentDate <= endDate);
                if (branchId.HasValue)
                    beneficiaryPaymentsQuery = beneficiaryPaymentsQuery.Where(bp => bp.Beneficiary.Property.BranchId == branchId.Value);

                // Get payments by month
                var paymentsByMonth = await beneficiaryPaymentsQuery
                    .GroupBy(bp => new { Year = bp.PaymentDate.Value.Year, Month = bp.PaymentDate.Value.Month })
                    .Select(g => new
                    {
                        Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                        Count = g.Count(),
                        Total = g.Sum(bp => bp.Amount)
                    })
                    .OrderBy(p => p.Period)
                    .ToListAsync();

                // Get payments by beneficiary
                var paymentsByBeneficiary = await beneficiaryPaymentsQuery
                    .GroupBy(bp => new
                    {
                        BeneficiaryId = bp.BeneficiaryId,
                        BeneficiaryName = bp.Beneficiary.Name
                    })
                    .Select(g => new
                    {
                        g.Key.BeneficiaryId,
                        g.Key.BeneficiaryName,
                        Count = g.Count(),
                        Total = g.Sum(bp => bp.Amount)
                    })
                    .OrderByDescending(p => p.Total)
                    .ToListAsync();

                // Get payments by property
                var paymentsByProperty = await beneficiaryPaymentsQuery
                    .GroupBy(bp => new
                    {
                        PropertyId = bp.Beneficiary.PropertyId,
                        PropertyName = bp.Beneficiary.Property.PropertyName,
                        PropertyCode = bp.Beneficiary.Property.PropertyCode
                    })
                    .Select(g => new
                    {
                        g.Key.PropertyId,
                        g.Key.PropertyName,
                        g.Key.PropertyCode,
                        Count = g.Count(),
                        Total = g.Sum(bp => bp.Amount)
                    })
                    .OrderByDescending(p => p.Total)
                    .ToListAsync();

                // Get payments by status
                var paymentsByStatus = await beneficiaryPaymentsQuery
                    .GroupBy(bp => new { bp.StatusId, StatusName = bp.Status.Name })
                    .Select(g => new
                    {
                        g.Key.StatusId,
                        g.Key.StatusName,
                        Count = g.Count(),
                        Total = g.Sum(bp => bp.Amount)
                    })
                    .OrderByDescending(p => p.Count)
                    .ToListAsync();

                // Get total and average values
                var totalPayments = await beneficiaryPaymentsQuery.SumAsync(bp => bp.Amount);
                var paymentCount = await beneficiaryPaymentsQuery.CountAsync();
                var avgPaymentAmount = paymentCount > 0 ? totalPayments / paymentCount : 0;

                response.Response = new
                {
                    DateRange = new { Start = startDate, End = endDate },
                    TotalPayments = totalPayments,
                    PaymentCount = paymentCount,
                    AveragePaymentAmount = avgPaymentAmount,
                    PaymentsByMonth = paymentsByMonth,
                    PaymentsByBeneficiary = paymentsByBeneficiary,
                    PaymentsByProperty = paymentsByProperty,
                    PaymentsByStatus = paymentsByStatus
                };

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Beneficiary payments report generated successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating beneficiary payments report: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error generating beneficiary payments report: {ex.Message}";
            }

            return response;
        }

        #endregion Financial Reports

        #region Tenant Reports

        public async Task<ResponseModel> GetTenantTurnoverReport(int companyId, int? branchId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Set default date range if not provided
                startDate ??= DateTime.Now.AddYears(-2);
                endDate ??= DateTime.Now;

                // Base query for tenants
                var tenantsQuery = context.PropertyTenants
                    .Where(t => t.CompanyId == companyId);
                if (branchId.HasValue)
                    tenantsQuery = tenantsQuery.Where(t => t.Property.BranchId == branchId.Value);

                // Get tenant move-outs during the period
                var tenantMoveOuts = await tenantsQuery
                    .Where(t => t.MoveOutDate >= startDate && t.MoveOutDate <= endDate)
                    .Select(t => new
                    {
                        t.Id,
                        t.DisplayName,
                        PropertyId = t.PropertyId,
                        PropertyName = t.Property.PropertyName,
                        PropertyCode = t.Property.PropertyCode,
                        t.LeaseStartDate,
                        t.LeaseEndDate,
                        t.MoveInDate,
                        t.MoveOutDate,
                        TenancyDuration = t.MoveOutDate.HasValue && t.MoveInDate.HasValue ?
                            (t.MoveOutDate.Value - t.MoveInDate.Value).TotalDays : 0,
                        LeaseTerm = (t.LeaseEndDate - t.LeaseStartDate).TotalDays,
                        CompletedLease = t.MoveOutDate.HasValue && t.LeaseEndDate <= t.MoveOutDate
                    })
                    .OrderByDescending(t => t.MoveOutDate)
                    .ToListAsync();

                // Get tenant move-ins during the period
                var tenantMoveIns = await tenantsQuery
                    .Where(t => t.MoveInDate >= startDate && t.MoveInDate <= endDate)
                    .Select(t => new
                    {
                        t.Id,
                        t.DisplayName,
                        PropertyId = t.PropertyId,
                        PropertyName = t.Property.PropertyName,
                        PropertyCode = t.Property.PropertyCode,
                        t.LeaseStartDate,
                        t.LeaseEndDate,
                        t.MoveInDate
                    })
                    .OrderByDescending(t => t.MoveInDate)
                    .ToListAsync();

                // Calculate monthly turnover
                var monthlyTurnover = new List<object>();
                var allMonths = tenantMoveOuts.Select(t => new { Year = t.MoveOutDate.Value.Year, Month = t.MoveOutDate.Value.Month })
                    .Union(tenantMoveIns.Select(t => new { Year = t.MoveInDate.Value.Year, Month = t.MoveInDate.Value.Month }))
                    .OrderBy(m => m.Year).ThenBy(m => m.Month)
                    .Distinct()
                    .ToList();

                // Get total active properties for each month for calculating turnover rate
                foreach (var month in allMonths)
                {
                    var monthStart = new DateTime(month.Year, month.Month, 1);
                    var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                    var moveOutsInMonth = tenantMoveOuts.Count(t =>
                        t.MoveOutDate.Value.Year == month.Year && t.MoveOutDate.Value.Month == month.Month);

                    var moveInsInMonth = tenantMoveIns.Count(t =>
                        t.MoveInDate.Value.Year == month.Year && t.MoveInDate.Value.Month == month.Month);

                    // Count active properties at the end of the month
                    var activeProperties = await context.Properties
                        .Where(p => p.CompanyId == companyId && !p.IsRemoved)
                        .CountAsync(p => p.HasTenant);

                    if (branchId.HasValue)
                        activeProperties = await context.Properties
                            .Where(p => p.CompanyId == companyId && !p.IsRemoved && p.BranchId == branchId.Value)
                            .CountAsync(p => p.HasTenant);

                    // Calculate turnover rate
                    double turnoverRate = activeProperties > 0 ?
                        (double)moveOutsInMonth / activeProperties * 100 : 0;

                    monthlyTurnover.Add(new
                    {
                        Period = $"{month.Year}-{month.Month:D2}",
                        MoveOuts = moveOutsInMonth,
                        MoveIns = moveInsInMonth,
                        NetChange = moveInsInMonth - moveOutsInMonth,
                        TurnoverRate = turnoverRate
                    });
                }

                // Calculate average tenancy duration
                double avgTenancyDuration = tenantMoveOuts.Count > 0 ?
                    tenantMoveOuts.Average(t => t.TenancyDuration) : 0;

                // Calculate percentage of completed leases vs early terminations
                int completedLeases = tenantMoveOuts.Count(t => t.CompletedLease);
                int earlyTerminations = tenantMoveOuts.Count - completedLeases;
                double completedLeasePercentage = tenantMoveOuts.Count > 0 ?
                    (double)completedLeases / tenantMoveOuts.Count * 100 : 0;

                response.Response = new
                {
                    DateRange = new { Start = startDate, End = endDate },
                    TotalMoveOuts = tenantMoveOuts.Count,
                    TotalMoveIns = tenantMoveIns.Count,
                    NetChange = tenantMoveIns.Count - tenantMoveOuts.Count,
                    AverageTenancyDurationDays = avgTenancyDuration,
                    AverageTenancyDurationMonths = avgTenancyDuration / 30,
                    CompletedLeases = completedLeases,
                    EarlyTerminations = earlyTerminations,
                    CompletedLeasePercentage = completedLeasePercentage,
                    MonthlyTurnover = monthlyTurnover,
                    TenantMoveOuts = tenantMoveOuts,
                    TenantMoveIns = tenantMoveIns
                };

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Tenant turnover report generated successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating tenant turnover report: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error generating tenant turnover report: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> GetLeaseExpiryReport(int companyId, int? branchId = null, int monthsAhead = 3)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Calculate date range
                var startDate = DateTime.Now;
                var endDate = startDate.AddMonths(monthsAhead);

                // Base query
                var tenantsQuery = context.PropertyTenants
                    .Where(t => t.CompanyId == companyId &&
                           !t.IsRemoved &&
                           t.LeaseEndDate >= startDate &&
                           t.LeaseEndDate <= endDate);
                if (branchId.HasValue)
                    tenantsQuery = tenantsQuery.Where(t => t.Property.BranchId == branchId.Value);

                // Get tenants with expiring leases
                var expiringLeases = await tenantsQuery
                    .Select(t => new
                    {
                        t.Id,
                        t.DisplayName,
                        PropertyId = t.PropertyId,
                        PropertyName = t.Property.PropertyName,
                        PropertyCode = t.Property.PropertyCode,
                        t.LeaseStartDate,
                        t.LeaseEndDate,
                        DaysUntilExpiry = (t.LeaseEndDate - DateTime.Now).TotalDays,
                        t.RentAmount,
                        TenantEmail = t.EmailAddresses
                            .FirstOrDefault(e => e.IsPrimary)
                            .EmailAddress,
                        TenantPhone = t.ContactNumbers
                            .FirstOrDefault(c => c.IsPrimary)
                            .Number
                    })
                    .OrderBy(t => t.LeaseEndDate)
                    .ToListAsync();

                // Group expiring leases by month
                var expiriesByMonth = expiringLeases
                    .GroupBy(t => new { Year = t.LeaseEndDate.Year, Month = t.LeaseEndDate.Month })
                    .Select(g => new
                    {
                        Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                        Count = g.Count(),
                        TotalRentAmount = g.Sum(t => t.RentAmount)
                    })
                    .OrderBy(g => g.Period)
                    .ToList();

                // Group expiring leases by days until expiry
                var expiriesByTimeframe = expiringLeases
                    .GroupBy(t => t.DaysUntilExpiry switch
                    {
                        var d when d <= 30 => "0-30 Days",
                        var d when d <= 60 => "31-60 Days",
                        _ => "61-90 Days"
                    })
                    .Select(g => new
                    {
                        Timeframe = g.Key,
                        Count = g.Count(),
                        TotalRentAmount = g.Sum(t => t.RentAmount)
                    })
                    .OrderBy(g => g.Timeframe)
                    .ToList();

                response.Response = new
                {
                    DateRange = new { Start = startDate, End = endDate },
                    TotalExpiringLeases = expiringLeases.Count,
                    TotalRentAmount = expiringLeases.Sum(t => t.RentAmount),
                    ExpiringLeasesByMonth = expiriesByMonth,
                    ExpiringLeasesByTimeframe = expiriesByTimeframe,
                    ExpiringLeases = expiringLeases
                };

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Lease expiry report generated successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating lease expiry report: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error generating lease expiry report: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> GetTenantPaymentHistoryReport(int companyId, int? branchId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Set default date range if not provided
                startDate ??= DateTime.Now.AddMonths(-12);
                endDate ??= DateTime.Now;

                // Base query for payments
                var paymentsQuery = context.PropertyPayments
                    .Where(p => p.CompanyId == companyId &&
                           p.TenantId.HasValue &&
                           p.PaymentDate >= startDate &&
                           p.PaymentDate <= endDate);
                if (branchId.HasValue)
                    paymentsQuery = paymentsQuery.Where(p => p.Property.BranchId == branchId.Value);

                // Get payment history by tenant
                var paymentsByTenant = await paymentsQuery
                    .GroupBy(p => new
                    {
                        TenantId = p.TenantId.Value,
                        TenantName = p.Tenant.DisplayName,
                        PropertyId = p.PropertyId,
                        PropertyName = p.Property.PropertyName,
                        PropertyCode = p.Property.PropertyCode,
                        RentAmount = p.Tenant.RentAmount
                    })
                    .Select(g => new
                    {
                        g.Key.TenantId,
                        g.Key.TenantName,
                        g.Key.PropertyId,
                        g.Key.PropertyName,
                        g.Key.PropertyCode,
                        g.Key.RentAmount,
                        PaymentCount = g.Count(),
                        TotalPaid = g.Sum(p => p.Amount),
                        ExpectedPayments = g.Key.RentAmount * 12, // Simplified: assuming monthly rent for a year
                        AveragePayment = g.Average(p => p.Amount),
                        OnTimePayments = g.Count(p => p.DueDate >= p.PaymentDate),
                        LatePayments = g.Count(p => p.DueDate < p.PaymentDate),
                        Payments = g.OrderByDescending(p => p.PaymentDate)
                            .Select(p => new
                            {
                                p.Id,
                                p.PaymentReference,
                                PaymentType = p.PaymentType.Name,
                                p.Amount,
                                p.PaymentDate,
                                p.DueDate,
                                IsLate = p.DueDate < p.PaymentDate,
                                DaysLate = p.DaysLate
                            })
                            .ToList()
                    })
                    .ToListAsync();

                // Calculate payment reliability metrics
                var tenantPaymentHistory = paymentsByTenant.Select(t => new
                {
                    t.TenantId,
                    t.TenantName,
                    t.PropertyId,
                    t.PropertyName,
                    t.PropertyCode,
                    t.RentAmount,
                    t.PaymentCount,
                    t.TotalPaid,
                    t.AveragePayment,
                    t.OnTimePayments,
                    t.LatePayments,
                    OnTimePercentage = t.PaymentCount > 0 ?
                        (double)t.OnTimePayments / t.PaymentCount * 100 : 0,
                    PaymentReliabilityScore = CalculatePaymentReliabilityScore(
                        t.PaymentCount, t.OnTimePayments, t.LatePayments),
                    t.Payments
                }).OrderByDescending(t => t.PaymentReliabilityScore).ToList();

                // Calculate overall payment statistics
                int totalPayments = paymentsByTenant.Sum(t => t.PaymentCount);
                int totalOnTimePayments = paymentsByTenant.Sum(t => t.OnTimePayments);
                int totalLatePayments = paymentsByTenant.Sum(t => t.LatePayments);
                double overallOnTimePercentage = totalPayments > 0 ?
                    (double)totalOnTimePayments / totalPayments * 100 : 0;

                response.Response = new
                {
                    DateRange = new { Start = startDate, End = endDate },
                    TotalTenants = tenantPaymentHistory.Count,
                    TotalPayments = totalPayments,
                    TotalOnTimePayments = totalOnTimePayments,
                    TotalLatePayments = totalLatePayments,
                    OverallOnTimePercentage = overallOnTimePercentage,
                    TenantPaymentHistory = tenantPaymentHistory
                };

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Tenant payment history report generated successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating tenant payment history report: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error generating tenant payment history report: {ex.Message}";
            }

            return response;
        }

        // Helper method to calculate payment reliability score (0-100)
        private int CalculatePaymentReliabilityScore(int paymentCount, int onTimePayments, int latePayments)
        {
            if (paymentCount == 0)
                return 0;

            // Base score is the percentage of on-time payments
            double baseScore = (double)onTimePayments / paymentCount * 100;

            // Adjust score based on total payment history (more payments = more reliable score)
            double historyFactor = Math.Min(1.0, paymentCount / 12.0); // Max factor at 12+ payments

            // Calculate final score (0-100)
            int finalScore = (int)Math.Round(baseScore * historyFactor);

            return Math.Max(0, Math.Min(100, finalScore));
        }

        #endregion Tenant Reports

        #region Owner Reports

        public async Task<ResponseModel> GetOwnerPortfolioReport(int ownerId)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Get owner info
                var owner = await context.PropertyOwners
                    .FirstOrDefaultAsync(o => o.Id == ownerId && !o.IsRemoved);

                if (owner == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Owner with ID {ownerId} not found";
                    return response;
                }

                // Get owner properties
                var properties = await context.Properties
                    .Where(p => p.OwnerId == ownerId && !p.IsRemoved)
                    .Select(p => new
                    {
                        p.Id,
                        p.PropertyName,
                        p.PropertyCode,
                        PropertyType = p.PropertyType.Name,
                        Address = new
                        {
                            p.Address.Street,
                            p.Address.City,
                            p.Address.Province,
                            p.Address.PostalCode
                        },
                        p.RentalAmount,
                        HasTenant = p.HasTenant,
                        CurrentTenant = p.CurrentTenantId.HasValue ?
                            p.Tenants.FirstOrDefault(t => t.Id == p.CurrentTenantId).DisplayName : null,
                        LeaseEndDate = p.CurrentTenantId.HasValue ?
                            p.Tenants.FirstOrDefault(t => t.Id == p.CurrentTenantId).LeaseEndDate : (DateTime?)null,
                        LastInspectionDate = p.Inspections
                            .OrderByDescending(i => i.ActualDate)
                            .FirstOrDefault().ActualDate,
                        OpenMaintenanceTickets = p.MaintenanceTickets
                            .Count(m => m.StatusId != 4) // Assuming 4 is Completed
                    })
                    .ToListAsync();

                // Calculate portfolio statistics
                int totalProperties = properties.Count;
                int occupiedProperties = properties.Count(p => p.HasTenant);
                int vacantProperties = totalProperties - occupiedProperties;
                decimal totalMonthlyRent = properties.Sum(p => p.RentalAmount);
                decimal occupiedRent = properties.Where(p => p.HasTenant).Sum(p => p.RentalAmount);
                decimal vacantRent = totalMonthlyRent - occupiedRent;
                double occupancyRate = totalProperties > 0 ?
                    (double)occupiedProperties / totalProperties * 100 : 0;

                // Group properties by type
                var propertiesByType = properties
                    .GroupBy(p => p.PropertyType)
                    .Select(g => new
                    {
                        PropertyType = g.Key,
                        Count = g.Count(),
                        OccupiedCount = g.Count(p => p.HasTenant),
                        VacantCount = g.Count(p => !p.HasTenant),
                        TotalRent = g.Sum(p => p.RentalAmount)
                    })
                    .OrderByDescending(g => g.Count)
                    .ToList();

                // Group properties by location
                var propertiesByLocation = properties
                    .GroupBy(p => p.Address.City)
                    .Select(g => new
                    {
                        City = g.Key,
                        Count = g.Count(),
                        OccupiedCount = g.Count(p => p.HasTenant),
                        VacantCount = g.Count(p => !p.HasTenant),
                        TotalRent = g.Sum(p => p.RentalAmount)
                    })
                    .OrderByDescending(g => g.Count)
                    .ToList();

                // Get owner contact info
                var ownerContacts = new
                {
                    PrimaryEmail = owner.EmailAddresses.FirstOrDefault(e => e.IsPrimary)?.EmailAddress,
                    PrimaryPhone = owner.ContactNumbers.FirstOrDefault(c => c.IsPrimary)?.Number,
                    Address = new
                    {
                        owner.Address.Street,
                        owner.Address.City,
                        owner.Address.Province,
                        owner.Address.PostalCode
                    }
                };

                response.Response = new
                {
                    OwnerInfo = new
                    {
                        owner.Id,
                        owner.DisplayName,
                        owner.PropertyOwnerTypeId,
                        ownerContacts
                    },
                    PortfolioSummary = new
                    {
                        TotalProperties = totalProperties,
                        OccupiedProperties = occupiedProperties,
                        VacantProperties = vacantProperties,
                        OccupancyRate = occupancyRate,
                        TotalMonthlyRent = totalMonthlyRent,
                        OccupiedRent = occupiedRent,
                        VacantRent = vacantRent,
                        AnnualRentalIncome = totalMonthlyRent * 12
                    },
                    PropertiesByType = propertiesByType,
                    PropertiesByLocation = propertiesByLocation,
                    Properties = properties
                };

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Owner portfolio report generated successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating owner portfolio report: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error generating owner portfolio report: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> GetOwnerFinancialReport(int ownerId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Set default date range if not provided
                startDate ??= DateTime.Now.AddMonths(-12);
                endDate ??= DateTime.Now;

                // Get owner info
                var owner = await context.PropertyOwners
                    .FirstOrDefaultAsync(o => o.Id == ownerId && !o.IsRemoved);

                if (owner == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Owner with ID {ownerId} not found";
                    return response;
                }

                // Get owner properties
                var properties = await context.Properties
                    .Where(p => p.OwnerId == ownerId && !p.IsRemoved)
                    .Select(p => new
                    {
                        p.Id,
                        p.PropertyName,
                        p.PropertyCode,
                        p.RentalAmount,
                        // Income
                        Payments = p.Payments
                            .Where(pmt => pmt.PaymentDate >= startDate && pmt.PaymentDate <= endDate)
                            .Sum(pmt => pmt.Amount),
                        // Expenses
                        MaintenanceCosts = p.MaintenanceTickets
                            .SelectMany(m => m.Expenses)
                            .Where(e => e.InvoiceDate >= startDate && e.InvoiceDate <= endDate)
                            .Sum(e => e.Amount),
                        // Beneficiary payments
                        BeneficiaryPayments = p.Beneficiaries
                            .SelectMany(b => b.Payments)
                            .Where(bp => bp.PaymentDate >= startDate && bp.PaymentDate <= endDate)
                            .Sum(bp => bp.Amount)
                    })
                    .ToListAsync();

                // Calculate financial metrics for each property
                var propertyFinancials = properties.Select(p => new
                {
                    p.Id,
                    p.PropertyName,
                    p.PropertyCode,
                    p.RentalAmount,
                    AnnualRentalIncome = p.RentalAmount * 12,
                    ActualIncome = p.Payments,
                    Expenses = p.MaintenanceCosts,
                    BeneficiaryPayments = p.BeneficiaryPayments,
                    NetIncome = p.Payments - p.MaintenanceCosts - p.BeneficiaryPayments,
                    ExpenseRatio = p.Payments > 0 ?
                        (p.MaintenanceCosts / p.Payments) * 100 : 0,
                    OperatingCostRatio = p.Payments > 0 ?
                        ((p.MaintenanceCosts + p.BeneficiaryPayments) / p.Payments) * 100 : 0
                }).OrderByDescending(p => p.NetIncome).ToList();

                // Get income by month for all properties
                var incomeByMonth = await context.PropertyPayments
                    .Where(p => p.Property.OwnerId == ownerId &&
                           p.PaymentDate >= startDate &&
                           p.PaymentDate <= endDate)
                    .GroupBy(p => new { Year = p.PaymentDate.Value.Year, Month = p.PaymentDate.Value.Month })
                    .Select(g => new
                    {
                        Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                        Income = g.Sum(p => p.Amount)
                    })
                    .OrderBy(p => p.Period)
                    .ToListAsync();

                // Get expenses by month for all properties
                var expensesByMonth = await context.MaintenanceExpenses
                    .Where(e => e.MaintenanceTicket.Property.OwnerId == ownerId &&
                           e.InvoiceDate >= startDate &&
                           e.InvoiceDate <= endDate)
                    .GroupBy(e => new { Year = e.InvoiceDate.Value.Year, Month = e.InvoiceDate.Value.Month })
                    .Select(g => new
                    {
                        Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                        Expenses = g.Sum(e => e.Amount)
                    })
                    .OrderBy(e => e.Period)
                    .ToListAsync();

                // Get beneficiary payments by month
                var beneficiaryPaymentsByMonth = await context.BeneficiaryPayments
                    .Where(bp => bp.Beneficiary.Property.OwnerId == ownerId &&
                           bp.PaymentDate >= startDate &&
                           bp.PaymentDate <= endDate)
                    .GroupBy(bp => new { Year = bp.PaymentDate.Value.Year, Month = bp.PaymentDate.Value.Month })
                    .Select(g => new
                    {
                        Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                        Payments = g.Sum(bp => bp.Amount)
                    })
                    .OrderBy(bp => bp.Period)
                    .ToListAsync();

                // Combine monthly data for cash flow
                var allMonths = incomeByMonth.Select(m => m.Period)
                    .Union(expensesByMonth.Select(m => m.Period))
                    .Union(beneficiaryPaymentsByMonth.Select(m => m.Period))
                    .OrderBy(m => m)
                    .ToList();

                var cashFlowByMonth = new List<object>();

                foreach (var month in allMonths)
                {
                    var incomeMonth = incomeByMonth.FirstOrDefault(m => m.Period == month);
                    var expenseMonth = expensesByMonth.FirstOrDefault(m => m.Period == month);
                    var beneficiaryMonth = beneficiaryPaymentsByMonth.FirstOrDefault(m => m.Period == month);

                    decimal income = incomeMonth?.Income ?? 0;
                    decimal expenses = expenseMonth?.Expenses ?? 0;
                    decimal beneficiaryPayments = beneficiaryMonth?.Payments ?? 0;
                    decimal netIncome = income - expenses - beneficiaryPayments;

                    cashFlowByMonth.Add(new
                    {
                        Period = month,
                        Income = income,
                        Expenses = expenses,
                        BeneficiaryPayments = beneficiaryPayments,
                        NetIncome = netIncome
                    });
                }

                // Summary totals
                decimal totalIncome = propertyFinancials.Sum(p => p.ActualIncome);
                decimal totalExpenses = propertyFinancials.Sum(p => p.Expenses);
                decimal totalBeneficiaryPayments = propertyFinancials.Sum(p => p.BeneficiaryPayments);
                decimal totalNetIncome = propertyFinancials.Sum(p => p.NetIncome);

                response.Response = new
                {
                    DateRange = new { Start = startDate, End = endDate },
                    OwnerInfo = new
                    {
                        owner.Id,
                        owner.DisplayName
                    },
                    FinancialSummary = new
                    {
                        TotalProperties = properties.Count,
                        TotalIncome = totalIncome,
                        TotalExpenses = totalExpenses,
                        TotalBeneficiaryPayments = totalBeneficiaryPayments,
                        TotalNetIncome = totalNetIncome,
                        OperatingCostRatio = totalIncome > 0 ?
                            ((totalExpenses + totalBeneficiaryPayments) / totalIncome) * 100 : 0
                    },
                    CashFlowByMonth = cashFlowByMonth,
                    PropertyFinancials = propertyFinancials
                };

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Owner financial report generated successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating owner financial report: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error generating owner financial report: {ex.Message}";
            }

            return response;
        }

        #endregion Owner Reports

        #region Inspection & Maintenance Reports

        public async Task<ResponseModel> GetInspectionComplianceReport(int companyId, int? branchId = null)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Base query for properties
                var propertiesQuery = context.Properties.Where(p => p.CompanyId == companyId && !p.IsRemoved);
                if (branchId.HasValue)
                    propertiesQuery = propertiesQuery.Where(p => p.BranchId == branchId.Value);

                // Get inspection status for each property
                var propertyInspections = await propertiesQuery
                    .Select(p => new
                    {
                        p.Id,
                        p.PropertyName,
                        p.PropertyCode,
                        p.HasTenant,
                        LastInspection = p.Inspections
                            .OrderByDescending(i => i.ActualDate)
                            .FirstOrDefault(),
                        InspectionCount = p.Inspections.Count,
                        DaysSinceLastInspection = p.Inspections.Any() ?
                            (int)(DateTime.Now - p.Inspections
                                .OrderByDescending(i => i.ActualDate)
                                .FirstOrDefault().ActualDate).GetValueOrDefault().TotalDays :
                            int.MaxValue // For properties never inspected
                    })
                    .ToListAsync();

                // Determine inspection compliance status
                var complianceReport = propertyInspections.Select(p => new
                {
                    p.Id,
                    p.PropertyName,
                    p.PropertyCode,
                    p.HasTenant,
                    LastInspectionDate = p.LastInspection?.ActualDate,
                    LastInspectionType = p.LastInspection != null ?
                        p.LastInspection.InspectionType.Name : null,
                    p.InspectionCount,
                    p.DaysSinceLastInspection,
                    ComplianceStatus = p.LastInspection == null ?
                        "Never Inspected" :
                        (p.DaysSinceLastInspection <= 90 ? "Compliant" :
                         p.DaysSinceLastInspection <= 180 ? "Due Soon" : "Overdue"),
                    NextInspectionDue = p.LastInspection?.NextInspectionDue,
                    DaysUntilNextInspection = p.LastInspection?.NextInspectionDue.HasValue ?? false ?
                        (int?)(p.LastInspection.NextInspectionDue.Value - DateTime.Now).TotalDays : null
                }).OrderBy(p => p.ComplianceStatus)
                   .ThenBy(p => p.DaysUntilNextInspection)
                   .ToList();

                // Group by compliance status
                var complianceSummary = complianceReport
                    .GroupBy(p => p.ComplianceStatus)
                    .Select(g => new
                    {
                        Status = g.Key,
                        Count = g.Count(),
                        Percentage = propertyInspections.Count > 0 ?
                            (double)g.Count() / propertyInspections.Count * 100 : 0
                    })
                    .OrderBy(g => g.Status)
                    .ToList();

                // Properties due for inspection in next 30 days
                var dueForInspection = complianceReport
                    .Where(p => p.NextInspectionDue.HasValue &&
                           p.NextInspectionDue.Value >= DateTime.Now &&
                           p.NextInspectionDue.Value <= DateTime.Now.AddDays(30))
                    .OrderBy(p => p.NextInspectionDue)
                    .ToList();

                // Get recent inspections (last 90 days)
                var recentInspections = await context.PropertyInspections
                    .Where(i => i.CompanyId == companyId &&
                           i.ActualDate >= DateTime.Now.AddDays(-90) &&
                           (branchId == null || i.Property.BranchId == branchId.Value))
                    .Select(i => new
                    {
                        i.Id,
                        PropertyId = i.PropertyId,
                        PropertyName = i.Property.PropertyName,
                        PropertyCode = i.Property.PropertyCode,
                        InspectionType = i.InspectionType.Name,
                        i.ActualDate,
                        i.InspectorName,
                        i.OverallRating,
                        MaintenanceItemsFound = i.InspectionItems.Count(item => item.RequiresMaintenance),
                        i.NextInspectionDue
                    })
                    .OrderByDescending(i => i.ActualDate)
                    .ToListAsync();

                response.Response = new
                {
                    TotalProperties = propertyInspections.Count,
                    InspectedProperties = propertyInspections.Count(p => p.InspectionCount > 0),
                    NeverInspected = propertyInspections.Count(p => p.InspectionCount == 0),
                    ComplianceSummary = complianceSummary,
                    PropertiesDueForInspection = dueForInspection.Count,
                    DueForInspectionList = dueForInspection,
                    RecentInspections = recentInspections,
                    PropertyInspectionReport = complianceReport
                };

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Inspection compliance report generated successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating inspection compliance report: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error generating inspection compliance report: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> GetMaintenanceStatusReport(int companyId, int? branchId = null)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Base query for maintenance tickets
                var ticketsQuery = context.MaintenanceTickets.Where(m => m.CompanyId == companyId);
                if (branchId.HasValue)
                    ticketsQuery = ticketsQuery.Where(m => m.Property.BranchId == branchId.Value);

                // Get tickets by status
                var ticketsByStatus = await ticketsQuery
                    .GroupBy(m => new { m.StatusId, StatusName = m.Status.Name })
                    .Select(g => new
                    {
                        g.Key.StatusId,
                        g.Key.StatusName,
                        Count = g.Count(),
                        AverageAge = g.Average(m => (DateTime.Now - m.CreatedOn).TotalDays)
                    })
                    .OrderBy(t => t.StatusId)
                    .ToListAsync();

                // Get tickets by priority
                var ticketsByPriority = await ticketsQuery
                    .GroupBy(m => new { m.PriorityId, PriorityName = m.Priority.Name })
                    .Select(g => new
                    {
                        g.Key.PriorityId,
                        g.Key.PriorityName,
                        Count = g.Count(),
                        OpenCount = g.Count(m => m.StatusId != 4), // Assuming 4 is Completed
                        AverageResolutionTime = g.Where(m => m.CompletedDate.HasValue)
                            .Average(m => (m.CompletedDate.Value - m.CreatedOn).TotalHours)
                    })
                    .OrderBy(t => t.PriorityId)
                    .ToListAsync();

                // Get tickets by category
                var ticketsByCategory = await ticketsQuery
                    .GroupBy(m => new { m.CategoryId, CategoryName = m.Category.Name })
                    .Select(g => new
                    {
                        g.Key.CategoryId,
                        g.Key.CategoryName,
                        Count = g.Count(),
                        OpenCount = g.Count(m => m.StatusId != 4), // Assuming 4 is Completed
                        AverageCost = g.SelectMany(m => m.Expenses).Average(e => e.Amount)
                    })
                    .OrderByDescending(t => t.Count)
                    .ToListAsync();

                // Get open tickets with details
                var openTickets = await ticketsQuery
                    .Where(m => m.StatusId != 4) // Assuming 4 is Completed
                    .Select(m => new
                    {
                        m.Id,
                        m.TicketNumber,
                        m.Title,
                        PropertyId = m.PropertyId,
                        PropertyName = m.Property.PropertyName,
                        PropertyCode = m.Property.PropertyCode,
                        CategoryName = m.Category.Name,
                        PriorityName = m.Priority.Name,
                        StatusName = m.Status.Name,
                        m.AssignedToName,
                        m.VendorId,
                        VendorName = m.Vendor.Name,
                        m.CreatedOn,
                        m.ScheduledDate,
                        AgeInDays = (int)(DateTime.Now - m.CreatedOn).TotalDays,
                        EstimatedCost = m.EstimatedCost,
                        ActualCost = m.Expenses.Sum(e => e.Amount)
                    })
                    .OrderByDescending(m => m.AgeInDays)
                    .ToListAsync();

                // Calculate aging buckets for open tickets
                var ticketAging = openTickets
                    .GroupBy(t => t.AgeInDays switch
                    {
                        var d when d <= 7 => "0-7 Days",
                        var d when d <= 14 => "8-14 Days",
                        var d when d <= 30 => "15-30 Days",
                        var d when d <= 60 => "31-60 Days",
                        _ => "60+ Days"
                    })
                    .Select(g => new
                    {
                        AgeBucket = g.Key,
                        Count = g.Count()
                    })
                    .OrderBy(a => a.AgeBucket)
                    .ToList();

                // Calculate resolution time (for completed tickets)
                var resolutionTimes = await ticketsQuery
                    .Where(m => m.CompletedDate.HasValue)
                    .Select(m => new
                    {
                        ResolutionHours = (m.CompletedDate.Value - m.CreatedOn).TotalHours,
                        Priority = m.Priority.Name,
                        Category = m.Category.Name
                    })
                    .ToListAsync();

                var avgResolutionTime = resolutionTimes.Count > 0 ?
                    resolutionTimes.Average(r => r.ResolutionHours) : 0;

                response.Response = new
                {
                    TotalTickets = await ticketsQuery.CountAsync(),
                    OpenTickets = openTickets.Count,
                    ClosedTickets = await ticketsQuery.CountAsync(m => m.StatusId == 4), // Assuming 4 is Completed
                    AverageResolutionTimeHours = avgResolutionTime,
                    TicketsByStatus = ticketsByStatus,
                    TicketsByPriority = ticketsByPriority,
                    TicketsByCategory = ticketsByCategory,
                    TicketAging = ticketAging,
                    OpenTicketsList = openTickets
                };

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Maintenance status report generated successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating maintenance status report: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error generating maintenance status report: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> GetVendorPerformanceReport(int companyId, int? branchId = null)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Base query for maintenance tickets
                var ticketsQuery = context.MaintenanceTickets
                    .Where(m => m.CompanyId == companyId && m.VendorId.HasValue);
                if (branchId.HasValue)
                    ticketsQuery = ticketsQuery.Where(m => m.Property.BranchId == branchId.Value);

                // Get vendor performance metrics
                var vendorPerformance = await ticketsQuery
                    .GroupBy(m => new
                    {
                        VendorId = m.VendorId.Value,
                        VendorName = m.Vendor.Name
                    })
                    .Select(g => new
                    {
                        g.Key.VendorId,
                        g.Key.VendorName,
                        TotalTickets = g.Count(),
                        CompletedTickets = g.Count(m => m.StatusId == 4), // Assuming 4 is Completed
                        OpenTickets = g.Count(m => m.StatusId != 4),
                        AverageResolutionTime = g.Where(m => m.CompletedDate.HasValue)
                            .Average(m => (m.CompletedDate.Value - m.CreatedOn).TotalHours),
                        TotalCost = g.SelectMany(m => m.Expenses).Sum(e => e.Amount),
                        AverageCost = g.Count() > 0 ?
                            g.SelectMany(m => m.Expenses).Sum(e => e.Amount) / g.Count() : 0,
                        OnTimeCompletion = g.Count(m => m.CompletedDate.HasValue && m.ScheduledDate.HasValue &&
                            m.CompletedDate.Value <= m.ScheduledDate.Value)
                    })
                    .ToListAsync();

                // Calculate additional performance metrics
                var vendorPerformanceResults = vendorPerformance.Select(v => new
                {
                    v.VendorId,
                    v.VendorName,
                    v.TotalTickets,
                    v.CompletedTickets,
                    v.OpenTickets,
                    CompletionRate = v.TotalTickets > 0 ?
                        (double)v.CompletedTickets / v.TotalTickets * 100 : 0,
                    AverageResolutionTimeHours = v.AverageResolutionTime,
                    AverageResolutionTimeDays = v.AverageResolutionTime / 24,
                    v.TotalCost,
                    v.AverageCost,
                    OnTimeCompletionRate = v.CompletedTickets > 0 ?
                        (double)v.OnTimeCompletion / v.CompletedTickets * 100 : 0,
                    PerformanceScore = CalculateVendorPerformanceScore(
                        v.TotalTickets, v.CompletedTickets, v.OnTimeCompletion, v.AverageResolutionTime)
                }).OrderByDescending(v => v.PerformanceScore).ToList();

                // Get tickets by category for each vendor
                var vendorCategorySpecialization = await ticketsQuery
                    .GroupBy(m => new
                    {
                        VendorId = m.VendorId.Value,
                        VendorName = m.Vendor.Name,
                        CategoryId = m.CategoryId,
                        CategoryName = m.Category.Name
                    })
                    .Select(g => new
                    {
                        g.Key.VendorId,
                        g.Key.VendorName,
                        g.Key.CategoryId,
                        g.Key.CategoryName,
                        Count = g.Count(),
                        SuccessRate = g.Count(m => m.StatusId == 4) / (double)g.Count() * 100,
                        AverageCost = g.SelectMany(m => m.Expenses).Average(e => e.Amount)
                    })
                    .OrderBy(v => v.VendorId)
                    .ThenByDescending(v => v.Count)
                    .ToListAsync();

                // Group vendor performance by category specialization
                var vendorsByCategory = vendorCategorySpecialization
                    .GroupBy(v => new { v.CategoryId, v.CategoryName })
                    .Select(g => new
                    {
                        g.Key.CategoryId,
                        g.Key.CategoryName,
                        VendorCount = g.Select(v => v.VendorId).Distinct().Count(),
                        BestVendor = g.OrderByDescending(v => v.SuccessRate).ThenBy(v => v.AverageCost).First().VendorName,
                        AverageSuccessRate = g.Average(v => v.SuccessRate),
                        AverageCost = g.Average(v => v.AverageCost)
                    })
                    .OrderByDescending(c => c.VendorCount)
                    .ToList();

                response.Response = new
                {
                    TotalVendors = vendorPerformanceResults.Count,
                    AverageCompletionRate = vendorPerformanceResults.Count > 0 ?
                        vendorPerformanceResults.Average(v => v.CompletionRate) : 0,
                    AverageResolutionTime = vendorPerformanceResults.Count > 0 ?
                        vendorPerformanceResults.Average(v => v.AverageResolutionTimeHours) : 0,
                    TopPerformers = vendorPerformanceResults.OrderByDescending(v => v.PerformanceScore).Take(3).ToList(),
                    VendorPerformance = vendorPerformanceResults,
                    VendorSpecializations = vendorCategorySpecialization,
                    CategoryOverview = vendorsByCategory
                };

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Vendor performance report generated successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating vendor performance report: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error generating vendor performance report: {ex.Message}";
            }

            return response;
        }

        // Helper method to calculate vendor performance score (0-100)
        private int CalculateVendorPerformanceScore(int totalTickets, int completedTickets, int onTimeCompletions, double avgResolutionTime)
        {
            if (totalTickets == 0)
                return 0;

            // Weight factors
            double completionWeight = 0.4;
            double timelinessWeight = 0.3;
            double speedWeight = 0.3;

            // Completion rate (0-100)
            double completionRate = (double)completedTickets / totalTickets * 100;

            // On-time rate (0-100)
            double onTimeRate = completedTickets > 0 ?
                (double)onTimeCompletions / completedTickets * 100 : 0;

            // Resolution speed score (0-100)
            // Assuming 24 hours is excellent (100), 72 hours is average (50), 168 hours (1 week) is poor (0)
            double speedScore = 100 - Math.Min(100, Math.Max(0, avgResolutionTime - 24) / (168 - 24) * 100);

            // Calculate final score (0-100)
            int finalScore = (int)Math.Round(
                completionRate * completionWeight +
                onTimeRate * timelinessWeight +
                speedScore * speedWeight
            );

            return Math.Max(0, Math.Min(100, finalScore));
        }

        #endregion Inspection & Maintenance Reports

        #region Export and Custom Reports

        public async Task<ResponseModel> ExportReport(string reportType, Dictionary<string, object> parameters, string exportFormat)
        {
            var response = new ResponseModel();

            try
            {
                // Generate appropriate report based on reportType
                ResponseModel reportData = reportType.ToLower() switch
                {
                    "propertyperformance" => await GetPropertyPerformanceReport(
                        Convert.ToInt32(parameters["companyId"]),
                        parameters.ContainsKey("branchId") ? Convert.ToInt32(parameters["branchId"]) : null,
                        parameters.ContainsKey("startDate") ? Convert.ToDateTime(parameters["startDate"]) : null,
                        parameters.ContainsKey("endDate") ? Convert.ToDateTime(parameters["endDate"]) : null
                    ),

                    "vacancy" => await GetVacancyReport(
                        Convert.ToInt32(parameters["companyId"]),
                        parameters.ContainsKey("branchId") ? Convert.ToInt32(parameters["branchId"]) : null
                    ),

                    "arrears" => await GetArrearsReport(
                        Convert.ToInt32(parameters["companyId"]),
                        parameters.ContainsKey("branchId") ? Convert.ToInt32(parameters["branchId"]) : null
                    ),

                    "cashflow" => await GetCashFlowReport(
                        Convert.ToInt32(parameters["companyId"]),
                        parameters.ContainsKey("branchId") ? Convert.ToInt32(parameters["branchId"]) : null,
                        parameters.ContainsKey("startDate") ? Convert.ToDateTime(parameters["startDate"]) : null,
                        parameters.ContainsKey("endDate") ? Convert.ToDateTime(parameters["endDate"]) : null
                    ),

                    "tenantpaymenthistory" => await GetTenantPaymentHistoryReport(
                        Convert.ToInt32(parameters["companyId"]),
                        parameters.ContainsKey("branchId") ? Convert.ToInt32(parameters["branchId"]) : null,
                        parameters.ContainsKey("startDate") ? Convert.ToDateTime(parameters["startDate"]) : null,
                        parameters.ContainsKey("endDate") ? Convert.ToDateTime(parameters["endDate"]) : null
                    ),

                    "leaseexpiry" => await GetLeaseExpiryReport(
                        Convert.ToInt32(parameters["companyId"]),
                        parameters.ContainsKey("branchId") ? Convert.ToInt32(parameters["branchId"]) : null,
                        parameters.ContainsKey("monthsAhead") ? Convert.ToInt32(parameters["monthsAhead"]) : 3
                    ),

                    "ownerportfolio" => await GetOwnerPortfolioReport(
                        Convert.ToInt32(parameters["ownerId"])
                    ),

                    "ownerfinancial" => await GetOwnerFinancialReport(
                        Convert.ToInt32(parameters["ownerId"]),
                        parameters.ContainsKey("startDate") ? Convert.ToDateTime(parameters["startDate"]) : null,
                        parameters.ContainsKey("endDate") ? Convert.ToDateTime(parameters["endDate"]) : null
                    ),

                    "maintenancestatus" => await GetMaintenanceStatusReport(
                        Convert.ToInt32(parameters["companyId"]),
                        parameters.ContainsKey("branchId") ? Convert.ToInt32(parameters["branchId"]) : null
                    ),

                    "vendorperformance" => await GetVendorPerformanceReport(
                        Convert.ToInt32(parameters["companyId"]),
                        parameters.ContainsKey("branchId") ? Convert.ToInt32(parameters["branchId"]) : null
                    ),

                    // Add other report types as needed

                    _ => new ResponseModel
                    {
                        ResponseInfo = new ResponseInfo
                        {
                            Success = false,
                            Message = $"Unsupported report type: {reportType}"
                        }
                    }
                };

                if (!reportData.ResponseInfo.Success)
                {
                    return reportData;
                }

                // Format report data according to requested format
                string fileContent = string.Empty;
                string fileName = $"{reportType}_{DateTime.Now:yyyyMMdd}.{exportFormat}";

                if (exportFormat.ToLower() == "csv")
                {
                    // Generate CSV format - implementation would depend on the specific report structure
                    var stringBuilder = new StringBuilder();

                    // Example for a simple property list from vacancy report
                    if (reportType.ToLower() == "vacancy")
                    {
                        var vacantProperties = ((dynamic)reportData.Response).VacantPropertiesList;

                        // Add headers
                        stringBuilder.AppendLine("PropertyID,PropertyName,PropertyCode,PropertyTypeName,RentalAmount,OwnerName,Street,City,Province,PostalCode,DaysVacant");

                        // Add data rows
                        foreach (var property in vacantProperties)
                        {
                            stringBuilder.AppendLine($"{property.Id},{property.PropertyName},{property.PropertyCode},{property.PropertyTypeName},{property.RentalAmount},{property.OwnerName},{property.Address.Street},{property.Address.City},{property.Address.Province},{property.Address.PostalCode},{property.DaysVacant}");
                        }
                    }

                    fileContent = stringBuilder.ToString();
                }
                else if (exportFormat.ToLower() == "json")
                {
                    // Generate JSON format
                    fileContent = System.Text.Json.JsonSerializer.Serialize(reportData.Response,
                        new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                }
                else if (exportFormat.ToLower() == "xlsx")
                {
                    // Generate Excel format - would require a library like EPPlus or ClosedXML
                    // For simplicity, we'll return a placeholder for now
                    fileContent = "XLSX export not implemented in demo code";
                }
                else
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Unsupported export format: {exportFormat}";
                    return response;
                }

                // For this implementation, we'll return the file content
                // In a real implementation, you might want to save to a CDN or stream to the client
                var filePath = Path.Combine(Path.GetTempPath(), fileName);
                await File.WriteAllTextAsync(filePath, fileContent);

                response.Response = new
                {
                    FileName = fileName,
                    FilePath = filePath,
                    FileType = exportFormat,
                    FileSize = fileContent.Length
                };

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = $"Report exported to {exportFormat} format successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting report: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error exporting report: {ex.Message}";
            }

            return response;
        }

        #endregion Export and Custom Reports

        // Add these methods to the ReportingService.cs file

        #region Custom Reports

        public async Task<ResponseModel> GetCustomReport(string reportName, Dictionary<string, object> parameters)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Find the saved custom report by name
                var customReport = await context.Set<CustomReport>()
                    .FirstOrDefaultAsync(r => r.Name == reportName);

                if (customReport == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Custom report '{reportName}' not found";
                    return response;
                }

                // Validate that all required parameters are provided
                if (customReport.Parameters != null)
                {
                    var requiredParams = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(customReport.Parameters);
                    foreach (var param in requiredParams)
                    {
                        if (!parameters.ContainsKey(param.Key))
                        {
                            response.ResponseInfo.Success = false;
                            response.ResponseInfo.Message = $"Required parameter '{param.Key}' not provided";
                            return response;
                        }
                    }
                }

                // Execute the custom report query with parameters
                var results = new List<dynamic>();

                // Execute the raw query
                // Note: In production, ensure the query is properly sanitized to prevent SQL injection
                // Use parameterized queries instead of string concatenation
                string formattedQuery = customReport.Query;

                // Replace parameter placeholders with values
                foreach (var param in parameters)
                {
                    string paramValue;

                    if (param.Value is DateTime dateValue)
                    {
                        paramValue = $"'{dateValue:yyyy-MM-dd HH:mm:ss}'";
                    }
                    else if (param.Value is string stringValue)
                    {
                        paramValue = $"'{stringValue.Replace("'", "''")}'";
                    }
                    else
                    {
                        paramValue = param.Value?.ToString() ?? "NULL";
                    }

                    formattedQuery = formattedQuery.Replace($"@{param.Key}", paramValue);
                }

                // Execute the query
                using var command = context.Database.GetDbConnection().CreateCommand();
                command.CommandText = formattedQuery;
                command.CommandType = System.Data.CommandType.Text;

                if (context.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                {
                    await context.Database.GetDbConnection().OpenAsync();
                }

                using var reader = await command.ExecuteReaderAsync();

                // Get column names
                var columnNames = new List<string>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    columnNames.Add(reader.GetName(i));
                }

                // Read results
                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row[columnNames[i]] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    }
                    results.Add(row);
                }

                response.Response = new
                {
                    ReportName = customReport.Name,
                    Description = customReport.Description,
                    ExecutedAt = DateTime.Now,
                    Parameters = parameters,
                    Results = results,
                    RowCount = results.Count
                };

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = $"Custom report '{reportName}' executed successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing custom report: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error executing custom report: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> SaveCustomReport(string reportName, string reportQuery, string description, Dictionary<string, object> parameters, string userId)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Check if a report with this name already exists
                var existingReport = await context.Set<CustomReport>()
                    .FirstOrDefaultAsync(r => r.Name == reportName);

                if (existingReport != null)
                {
                    // Update existing report
                    existingReport.Query = reportQuery;
                    existingReport.Description = description;
                    existingReport.Parameters = System.Text.Json.JsonSerializer.Serialize(parameters);
                    existingReport.UpdatedBy = userId;
                    existingReport.UpdatedDate = DateTime.Now;

                    context.Update(existingReport);
                    await context.SaveChangesAsync();

                    response.Response = existingReport;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = $"Custom report '{reportName}' updated successfully";
                }
                else
                {
                    // Create new report
                    var newReport = new CustomReport
                    {
                        Name = reportName,
                        Query = reportQuery,
                        Description = description,
                        Parameters = System.Text.Json.JsonSerializer.Serialize(parameters),
                        CreatedBy = userId,
                        CreatedOn = DateTime.Now,
                        IsActive = true
                    };

                    context.Add(newReport);
                    await context.SaveChangesAsync();

                    response.Response = newReport;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = $"Custom report '{reportName}' created successfully";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving custom report: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error saving custom report: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> GetSavedReports(int companyId)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Get all saved reports for the company
                var reports = await context.Set<CustomReport>()
                    .Where(r => r.CompanyId == companyId && r.IsActive)
                    .Select(r => new
                    {
                        r.Id,
                        r.Name,
                        r.Description,
                        ParameterDefinitions = r.Parameters,
                        r.CreatedBy,
                        r.CreatedOn,
                        r.UpdatedBy,
                        r.UpdatedDate,
                        CreatedByName = r.CreatedByUser.FullName,
                        UpdatedByName = r.UpdatedByUser.FullName
                    })
                    .OrderBy(r => r.Name)
                    .ToListAsync();

                response.Response = reports;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = $"Retrieved {reports.Count} custom reports";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving saved reports: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving saved reports: {ex.Message}";
            }

            return response;
        }

        #endregion Custom Reports

        #region Report Scheduling

        /// <summary>
        /// Creates a new report schedule
        /// </summary>
        public async Task<ResponseModel> CreateReportSchedule(ReportSchedule schedule)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Validate input
                if (schedule.CustomReportId == null && string.IsNullOrEmpty(schedule.StandardReportType))
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Either CustomReportId or StandardReportType must be specified";
                    return response;
                }

                // Validate frequency type
                var frequencyType = await context.Set<ReportFrequencyType>()
                    .FirstOrDefaultAsync(f => f.Id == schedule.FrequencyTypeId);

                if (frequencyType == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Invalid frequency type specified";
                    return response;
                }

                // Set the next run date based on frequency
                schedule.NextRunDate = CalculateNextRunDate(schedule.FrequencyTypeId, schedule.DayOfWeek,
                    schedule.DayOfMonth, schedule.ExecutionTime);

                // Save the schedule
                context.Add(schedule);
                await context.SaveChangesAsync();

                response.Response = schedule;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Report schedule created successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating report schedule: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error creating report schedule: {ex.Message}";
            }

            return response;
        }

        /// <summary>
        /// Updates an existing report schedule
        /// </summary>
        public async Task<ResponseModel> UpdateReportSchedule(int scheduleId, ReportSchedule updatedSchedule)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Find the existing schedule
                var existingSchedule = await context.Set<ReportSchedule>()
                    .FirstOrDefaultAsync(r => r.Id == scheduleId);

                if (existingSchedule == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Report schedule with ID {scheduleId} not found";
                    return response;
                }

                // Update properties
                existingSchedule.Name = updatedSchedule.Name;
                existingSchedule.CustomReportId = updatedSchedule.CustomReportId;
                existingSchedule.StandardReportType = updatedSchedule.StandardReportType;
                existingSchedule.Parameters = updatedSchedule.Parameters;
                existingSchedule.FrequencyTypeId = updatedSchedule.FrequencyTypeId;
                existingSchedule.DayOfWeek = updatedSchedule.DayOfWeek;
                existingSchedule.DayOfMonth = updatedSchedule.DayOfMonth;
                existingSchedule.ExecutionTime = updatedSchedule.ExecutionTime;
                existingSchedule.RecipientEmails = updatedSchedule.RecipientEmails;
                existingSchedule.ExportFormat = updatedSchedule.ExportFormat;
                existingSchedule.IsActive = updatedSchedule.IsActive;
                existingSchedule.UpdatedBy = updatedSchedule.UpdatedBy;
                existingSchedule.UpdatedDate = DateTime.Now;

                // Recalculate the next run date
                existingSchedule.NextRunDate = CalculateNextRunDate(existingSchedule.FrequencyTypeId,
                    existingSchedule.DayOfWeek, existingSchedule.DayOfMonth, existingSchedule.ExecutionTime);

                context.Update(existingSchedule);
                await context.SaveChangesAsync();

                response.Response = existingSchedule;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Report schedule updated successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating report schedule: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error updating report schedule: {ex.Message}";
            }

            return response;
        }

        /// <summary>
        /// Deletes a report schedule
        /// </summary>
        public async Task<ResponseModel> DeleteReportSchedule(int scheduleId)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Find the schedule
                var schedule = await context.Set<ReportSchedule>()
                    .FirstOrDefaultAsync(r => r.Id == scheduleId);

                if (schedule == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Report schedule with ID {scheduleId} not found";
                    return response;
                }

                // Remove the schedule
                context.Remove(schedule);
                await context.SaveChangesAsync();

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Report schedule deleted successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting report schedule: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error deleting report schedule: {ex.Message}";
            }

            return response;
        }

        /// <summary>
        /// Gets all report schedules for a company
        /// </summary>
        public async Task<ResponseModel> GetReportSchedules(int companyId)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Get schedules for the company
                var schedules = await context.Set<ReportSchedule>()
                    .Where(s => s.CompanyId == companyId)
                    .Include(s => s.FrequencyType)
                    .Include(s => s.CustomReport)
                    .Select(s => new
                    {
                        s.Id,
                        s.Name,
                        s.CustomReportId,
                        CustomReportName = s.CustomReport != null ? s.CustomReport.Name : null,
                        s.StandardReportType,
                        s.Parameters,
                        s.FrequencyTypeId,
                        FrequencyName = s.FrequencyType.Name,
                        s.DayOfWeek,
                        s.DayOfMonth,
                        s.ExecutionTime,
                        s.RecipientEmails,
                        s.ExportFormat,
                        s.LastRunDate,
                        s.NextRunDate,
                        s.IsActive,
                        s.CreatedOn,
                        s.CreatedBy,
                        CreatedByName = s.CreatedByUser.FullName
                    })
                    .OrderBy(s => s.NextRunDate)
                    .ToListAsync();

                response.Response = schedules;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = $"Retrieved {schedules.Count} report schedules";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving report schedules: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving report schedules: {ex.Message}";
            }

            return response;
        }

        /// <summary>
        /// Gets a specific report schedule by ID
        /// </summary>
        public async Task<ResponseModel> GetReportSchedule(int scheduleId)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Get the schedule
                var schedule = await context.Set<ReportSchedule>()
                    .Where(s => s.Id == scheduleId)
                    .Include(s => s.FrequencyType)
                    .Include(s => s.CustomReport)
                    .FirstOrDefaultAsync();

                if (schedule == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Report schedule with ID {scheduleId} not found";
                    return response;
                }

                response.Response = schedule;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Report schedule retrieved successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving report schedule: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving report schedule: {ex.Message}";
            }

            return response;
        }

        /// <summary>
        /// Executes a scheduled report
        /// </summary>
        public async Task<ResponseModel> ExecuteScheduledReport(int scheduleId, string userId)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Get the schedule
                var schedule = await context.Set<ReportSchedule>()
                    .Include(s => s.CustomReport)
                    .FirstOrDefaultAsync(r => r.Id == scheduleId);

                if (schedule == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Report schedule with ID {scheduleId} not found";
                    return response;
                }

                // Start the execution log
                var executionLog = new ReportExecutionLog
                {
                    ReportScheduleId = scheduleId,
                    ReportType = schedule.CustomReportId.HasValue ? "Custom" : "Standard",
                    Parameters = schedule.Parameters,
                    ExecutionStartTime = DateTime.Now,
                    CompanyId = schedule.CompanyId,
                    ExecutedBy = userId
                };

                context.Add(executionLog);
                await context.SaveChangesAsync();

                try
                {
                    // Parse parameters
                    var parameters = string.IsNullOrEmpty(schedule.Parameters) ?
                        new Dictionary<string, object>() :
                        System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(schedule.Parameters);

                    // Execute the appropriate report
                    ResponseModel reportResult;

                    if (schedule.CustomReportId.HasValue && schedule.CustomReport != null)
                    {
                        // Execute custom report
                        reportResult = await GetCustomReport(schedule.CustomReport.Name, parameters);
                    }
                    else if (!string.IsNullOrEmpty(schedule.StandardReportType))
                    {
                        // Execute standard report
                        reportResult = await ExportReport(schedule.StandardReportType, parameters, schedule.ExportFormat);
                    }
                    else
                    {
                        throw new InvalidOperationException("Neither CustomReportId nor StandardReportType is valid");
                    }

                    if (!reportResult.ResponseInfo.Success)
                    {
                        throw new Exception(reportResult.ResponseInfo.Message);
                    }

                    // Save the report to CDN if applicable
                    string outputFilePath = null;
                    int? cdnFileId = null;

                    if (reportResult.Response != null)
                    {
                        try
                        {
                            dynamic resultObj = reportResult.Response;
                            if (resultObj.GetType().GetProperty("FilePath") != null)
                            {
                                var filePath = (string)resultObj.FilePath;
                                var fileName = (string)resultObj.FileName;

                                if (File.Exists(filePath))
                                {
                                    // Upload to CDN for permanent storage
                                    using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                                    var cdnUrl = await _cdnService.UploadFileAsync(
                                        fileStream,
                                        fileName,
                                        GetContentType(schedule.ExportFormat),
                                        "reports",
                                        $"{DateTime.Now.Year}/{DateTime.Now.Month}");

                                    // Get metadata for the uploaded file
                                    var fileMetadata = await _cdnService.GetFileMetadataAsync(cdnUrl);
                                    if (fileMetadata != null)
                                    {
                                        cdnFileId = fileMetadata.Id;
                                        outputFilePath = cdnUrl;
                                    }

                                    // Delete temporary file
                                    try { File.Delete(filePath); } catch { /* Ignore */ }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning("Error processing report file path: {Message}", ex.Message);
                        }
                    }

                    // Send email if recipients specified
                    bool emailSent = false;
                    if (!string.IsNullOrEmpty(schedule.RecipientEmails) && !string.IsNullOrEmpty(outputFilePath))
                    {
                        var recipients = schedule.RecipientEmails.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(e => e.Trim())
                            .ToList();

                        if (recipients.Any())
                        {
                            var subject = $"Scheduled Report: {schedule.Name}";
                            var body = $"<p>Your scheduled report '{schedule.Name}' is attached.</p>" +
                                       $"<p>Report generated on: {DateTime.Now:yyyy-MM-dd HH:mm}</p>";

                            if (cdnFileId.HasValue)
                            {
                                // Email with link
                                body += $"<p>You can access your report <a href='{outputFilePath}'>here</a>.</p>";
                                await _emailService.SendEmailAsync(recipients, subject, body);
                                emailSent = true;
                            }
                        }
                    }

                    // Update execution log
                    executionLog.ExecutionEndTime = DateTime.Now;
                    executionLog.IsSuccess = true;

                    // Handle row count
                    executionLog.RowCount = null; // Default value
                    if (reportResult.Response != null)
                    {
                        try
                        {
                            dynamic dynamicResult = reportResult.Response;
                            if (dynamicResult.GetType().GetProperty("RowCount") != null)
                            {
                                executionLog.RowCount = (int)dynamicResult.RowCount;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning("Error extracting row count: {Message}", ex.Message);
                        }
                    }

                    executionLog.OutputFilePath = outputFilePath;
                    executionLog.CdnFileMetadataId = cdnFileId;
                    executionLog.EmailSent = emailSent;
                    executionLog.RecipientEmails = schedule.RecipientEmails;

                    // Update schedule
                    schedule.LastRunDate = DateTime.Now;
                    schedule.NextRunDate = CalculateNextRunDate(schedule.FrequencyTypeId,
                        schedule.DayOfWeek, schedule.DayOfMonth, schedule.ExecutionTime);

                    context.Update(executionLog);
                    context.Update(schedule);
                    await context.SaveChangesAsync();

                    response.Response = new
                    {
                        ExecutionId = executionLog.Id,
                        ReportScheduleId = scheduleId,
                        ReportName = schedule.Name,
                        ExecutionTime = executionLog.ExecutionStartTime,
                        Duration = (executionLog.ExecutionEndTime - executionLog.ExecutionStartTime).Value.TotalSeconds,
                        Success = true,
                        OutputFilePath = outputFilePath,
                        EmailSent = emailSent
                    };

                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Report executed successfully";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during report execution: {Message}", ex.Message);

                    // Update execution log with error
                    executionLog.ExecutionEndTime = DateTime.Now;
                    executionLog.IsSuccess = false;
                    executionLog.ErrorMessage = ex.Message;

                    context.Update(executionLog);
                    await context.SaveChangesAsync();

                    throw; // Re-throw to be caught by outer catch block
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing scheduled report: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error executing scheduled report: {ex.Message}";
            }

            return response;
        }

        /// <summary>
        /// Process all reports that are due for execution
        /// </summary>
        public async Task<ResponseModel> ProcessDueReports(string systemUserId)
        {
            var response = new ResponseModel();
            var executedReports = new List<object>();
            var failedReports = new List<object>();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Find all active schedules that are due
                var now = DateTime.Now;
                var dueSchedules = await context.Set<ReportSchedule>()
                    .Where(s => s.IsActive && s.NextRunDate <= now)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} reports due for execution", dueSchedules.Count);

                // Execute each due report
                foreach (var schedule in dueSchedules)
                {
                    try
                    {
                        var result = await ExecuteScheduledReport(schedule.Id, systemUserId);

                        if (result.ResponseInfo.Success)
                        {
                            executedReports.Add(new
                            {
                                ScheduleId = schedule.Id,
                                ScheduleName = schedule.Name,
                                Result = result.Response
                            });
                        }
                        else
                        {
                            failedReports.Add(new
                            {
                                ScheduleId = schedule.Id,
                                ScheduleName = schedule.Name,
                                Error = result.ResponseInfo.Message
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing schedule {ScheduleId}: {Message}",
                            schedule.Id, ex.Message);

                        failedReports.Add(new
                        {
                            ScheduleId = schedule.Id,
                            ScheduleName = schedule.Name,
                            Error = ex.Message
                        });
                    }
                }

                response.Response = new
                {
                    TotalSchedulesProcessed = dueSchedules.Count,
                    SuccessfulReports = executedReports.Count,
                    FailedReports = failedReports.Count,
                    ExecutedReports = executedReports,
                    FailedReportDetails = failedReports
                };

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = $"Processed {dueSchedules.Count} scheduled reports";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing due reports: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error processing due reports: {ex.Message}";
            }

            return response;
        }

        /// <summary>
        /// Get report execution logs
        /// </summary>
        public async Task<ResponseModel> GetReportExecutionLogs(int companyId, int? scheduleId = null,
            DateTime? startDate = null, DateTime? endDate = null, bool? isSuccess = null, int page = 1, int pageSize = 20)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Build query
                var query = context.Set<ReportExecutionLog>()
                    .Where(l => l.CompanyId == companyId);

                if (scheduleId.HasValue)
                    query = query.Where(l => l.ReportScheduleId == scheduleId);

                if (startDate.HasValue)
                    query = query.Where(l => l.ExecutionStartTime >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(l => l.ExecutionStartTime <= endDate.Value);

                if (isSuccess.HasValue)
                    query = query.Where(l => l.IsSuccess == isSuccess.Value);

                // Get total count
                var totalCount = await query.CountAsync();

                // Get paginated results
                var logs = await query
                    .OrderByDescending(l => l.ExecutionStartTime)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(l => new
                    {
                        l.Id,
                        l.ReportScheduleId,
                        ScheduleName = l.ReportSchedule.Name,
                        l.ReportType,
                        l.ExecutionStartTime,
                        l.ExecutionEndTime,
                        Duration = l.ExecutionEndTime.HasValue ?
                            (l.ExecutionEndTime.Value - l.ExecutionStartTime).TotalSeconds : (double?)null,
                        l.IsSuccess,
                        l.ErrorMessage,
                        l.RowCount,
                        l.OutputFilePath,
                        l.EmailSent,
                        l.ExecutedBy,
                        ExecutedByName = l.ExecutedByUser.FullName
                    })
                    .ToListAsync();

                response.Response = new
                {
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    Logs = logs
                };

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = $"Retrieved {logs.Count} execution logs";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving report execution logs: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving report execution logs: {ex.Message}";
            }

            return response;
        }

        #endregion

        #region Dashboards

        /// <summary>
        /// Creates a new dashboard
        /// </summary>
        public async Task<ResponseModel> CreateDashboard(ReportDashboard dashboard)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Check if default dashboard needs to be reset
                if (dashboard.IsDefault)
                {
                    // Find existing default dashboards
                    var existingDefaults = await context.Set<ReportDashboard>()
                        .Where(d => d.CompanyId == dashboard.CompanyId &&
                                   (dashboard.UserId == null ? d.UserId == null : d.UserId == dashboard.UserId) &&
                                   d.IsDefault)
                        .ToListAsync();

                    // Reset default flag on other dashboards
                    foreach (var existing in existingDefaults)
                    {
                        existing.IsDefault = false;
                        context.Update(existing);
                    }
                }

                // Save the dashboard
                context.Add(dashboard);
                await context.SaveChangesAsync();

                response.Response = dashboard;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Dashboard created successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating dashboard: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error creating dashboard: {ex.Message}";
            }

            return response;
        }

        /// <summary>
        /// Updates an existing dashboard
        /// </summary>
        public async Task<ResponseModel> UpdateDashboard(int dashboardId, ReportDashboard updatedDashboard)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Find the existing dashboard
                var existingDashboard = await context.Set<ReportDashboard>()
                    .FirstOrDefaultAsync(d => d.Id == dashboardId);

                if (existingDashboard == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Dashboard with ID {dashboardId} not found";
                    return response;
                }

                // Check if default dashboard needs to be reset
                if (updatedDashboard.IsDefault && !existingDashboard.IsDefault)
                {
                    // Find existing default dashboards
                    var existingDefaults = await context.Set<ReportDashboard>()
                        .Where(d => d.CompanyId == existingDashboard.CompanyId &&
                               (existingDashboard.UserId == null ? d.UserId == null : d.UserId == existingDashboard.UserId) &&
                               d.IsDefault && d.Id != dashboardId)
                        .ToListAsync();

                    // Reset default flag on other dashboards
                    foreach (var existing in existingDefaults)
                    {
                        existing.IsDefault = false;
                        context.Update(existing);
                    }
                }

                // Update properties
                existingDashboard.Name = updatedDashboard.Name;
                existingDashboard.Description = updatedDashboard.Description;
                existingDashboard.IsDefault = updatedDashboard.IsDefault;
                existingDashboard.LayoutColumns = updatedDashboard.LayoutColumns;
                existingDashboard.Configuration = updatedDashboard.Configuration;
                existingDashboard.IsActive = updatedDashboard.IsActive;
                existingDashboard.UpdatedBy = updatedDashboard.UpdatedBy;
                existingDashboard.UpdatedDate = DateTime.Now;

                context.Update(existingDashboard);
                await context.SaveChangesAsync();

                response.Response = existingDashboard;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Dashboard updated successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating dashboard: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error updating dashboard: {ex.Message}";
            }

            return response;
        }

        /// <summary>
        /// Deletes a dashboard
        /// </summary>
        public async Task<ResponseModel> DeleteDashboard(int dashboardId)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Find the dashboard
                var dashboard = await context.Set<ReportDashboard>()
                    .Include(d => d.Widgets)
                    .FirstOrDefaultAsync(d => d.Id == dashboardId);

                if (dashboard == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Dashboard with ID {dashboardId} not found";
                    return response;
                }

                // Remove all widgets first
                context.RemoveRange(dashboard.Widgets);

                // Remove the dashboard
                context.Remove(dashboard);
                await context.SaveChangesAsync();

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Dashboard deleted successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting dashboard: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error deleting dashboard: {ex.Message}";
            }

            return response;
        }

        /// <summary>
        /// Gets all dashboards for a company or user
        /// </summary>
        public async Task<ResponseModel> GetDashboards(int companyId, string userId = null)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Get dashboards for the company
                var query = context.Set<ReportDashboard>()
                    .Where(d => d.CompanyId == companyId && d.IsActive);

                if (userId != null)
                {
                    // Get both company-wide dashboards and user-specific dashboards
                    query = query.Where(d => d.UserId == null || d.UserId == userId);
                }

                var dashboards = await query
                    .Select(d => new
                    {
                        d.Id,
                        d.Name,
                        d.Description,
                        d.IsDefault,
                        d.LayoutColumns,
                        d.UserId,
                        IsUserSpecific = d.UserId != null,
                        UserName = d.User.FullName,
                        d.CreatedOn,
                        d.CreatedBy,
                        CreatedByName = d.CreatedByUser.FullName,
                        WidgetCount = d.Widgets.Count
                    })
                    .OrderByDescending(d => d.IsDefault)
                    .ThenBy(d => d.Name)
                    .ToListAsync();

                response.Response = dashboards;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = $"Retrieved {dashboards.Count} dashboards";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dashboards: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving dashboards: {ex.Message}";
            }

            return response;
        }

        /// <summary>
        /// Gets a specific dashboard with its widgets
        /// </summary>
        public async Task<ResponseModel> GetDashboard(int dashboardId)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Get the dashboard with widgets
                var dashboard = await context.Set<ReportDashboard>()
                    .Include(d => d.Widgets)
                    .FirstOrDefaultAsync(d => d.Id == dashboardId);

                if (dashboard == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Dashboard with ID {dashboardId} not found";
                    return response;
                }

                // Get additional data for each widget
                var widgets = await context.Set<ReportDashboardWidget>()
                    .Where(w => w.DashboardId == dashboardId)
                    .Select(w => new
                    {
                        w.Id,
                        w.Title,
                        w.WidgetType,
                        w.DataSource,
                        w.CustomReportId,
                        CustomReportName = w.CustomReport != null ? w.CustomReport.Name : null,
                        w.StandardReportType,
                        w.Parameters,
                        w.VisualizationType,
                        w.GridColumn,
                        w.GridRow,
                        w.GridWidth,
                        w.GridHeight,
                        w.Configuration,
                        w.AutoRefresh,
                        w.RefreshInterval,
                        w.LastRefreshed,
                        w.IsActive,
                        w.CreatedOn,
                        w.CreatedBy
                    })
                    .OrderBy(w => w.GridRow)
                    .ThenBy(w => w.GridColumn)
                    .ToListAsync();

                var result = new
                {
                    Id = dashboard.Id,
                    Name = dashboard.Name,
                    Description = dashboard.Description,
                    IsDefault = dashboard.IsDefault,
                    LayoutColumns = dashboard.LayoutColumns,
                    Configuration = dashboard.Configuration,
                    CompanyId = dashboard.CompanyId,
                    UserId = dashboard.UserId,
                    IsUserSpecific = dashboard.UserId != null,
                    CreatedOn = dashboard.CreatedOn,
                    CreatedBy = dashboard.CreatedBy,
                    UpdatedDate = dashboard.UpdatedDate,
                    UpdatedBy = dashboard.UpdatedBy,
                    Widgets = widgets
                };

                response.Response = result;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Dashboard retrieved successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dashboard: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving dashboard: {ex.Message}";
            }

            return response;
        }

        /// <summary>
        /// Gets the default dashboard for a user
        /// </summary>
        public async Task<ResponseModel> GetDefaultDashboard(int companyId, string userId = null)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Try to find user-specific default first
                ReportDashboard dashboard = null;

                if (userId != null)
                {
                    dashboard = await context.Set<ReportDashboard>()
                        .Where(d => d.CompanyId == companyId && d.UserId == userId && d.IsDefault && d.IsActive)
                        .FirstOrDefaultAsync();
                }

                // If no user-specific default, try to find company-wide default
                if (dashboard == null)
                {
                    dashboard = await context.Set<ReportDashboard>()
                        .Where(d => d.CompanyId == companyId && d.UserId == null && d.IsDefault && d.IsActive)
                        .FirstOrDefaultAsync();
                }

                // If no default found, just pick any active dashboard
                if (dashboard == null)
                {
                    dashboard = await context.Set<ReportDashboard>()
                        .Where(d => d.CompanyId == companyId && d.IsActive &&
                               (userId == null || d.UserId == null || d.UserId == userId))
                        .FirstOrDefaultAsync();
                }

                if (dashboard == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "No default dashboard found";
                    return response;
                }

                // Get the full dashboard details
                return await GetDashboard(dashboard.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving default dashboard: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving default dashboard: {ex.Message}";
            }

            return response;
        }

        #endregion

        #region Dashboard Widgets

        /// <summary>
        /// Adds a widget to a dashboard
        /// </summary>
        public async Task<ResponseModel> AddWidgetToDashboard(ReportDashboardWidget widget)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Validate the dashboard
                var dashboard = await context.Set<ReportDashboard>()
                    .FirstOrDefaultAsync(d => d.Id == widget.DashboardId);

                if (dashboard == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Dashboard with ID {widget.DashboardId} not found";
                    return response;
                }

                // Save the widget
                context.Add(widget);
                await context.SaveChangesAsync();

                response.Response = widget;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Widget added to dashboard successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding widget to dashboard: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error adding widget to dashboard: {ex.Message}";
            }

            return response;
        }

        /// <summary>
        /// Updates a dashboard widget
        /// </summary>
        public async Task<ResponseModel> UpdateWidget(int widgetId, ReportDashboardWidget updatedWidget)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Find the existing widget
                var existingWidget = await context.Set<ReportDashboardWidget>()
                    .FirstOrDefaultAsync(w => w.Id == widgetId);

                if (existingWidget == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Widget with ID {widgetId} not found";
                    return response;
                }

                // Update properties
                existingWidget.Title = updatedWidget.Title;
                existingWidget.WidgetType = updatedWidget.WidgetType;
                existingWidget.DataSource = updatedWidget.DataSource;
                existingWidget.CustomReportId = updatedWidget.CustomReportId;
                existingWidget.StandardReportType = updatedWidget.StandardReportType;
                existingWidget.Parameters = updatedWidget.Parameters;
                existingWidget.VisualizationType = updatedWidget.VisualizationType;
                existingWidget.GridColumn = updatedWidget.GridColumn;
                existingWidget.GridRow = updatedWidget.GridRow;
                existingWidget.GridWidth = updatedWidget.GridWidth;
                existingWidget.GridHeight = updatedWidget.GridHeight;
                existingWidget.Configuration = updatedWidget.Configuration;
                existingWidget.AutoRefresh = updatedWidget.AutoRefresh;
                existingWidget.RefreshInterval = updatedWidget.RefreshInterval;
                existingWidget.IsActive = updatedWidget.IsActive;

                context.Update(existingWidget);
                await context.SaveChangesAsync();

                response.Response = existingWidget;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Widget updated successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating widget: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error updating widget: {ex.Message}";
            }

            return response;
        }

        /// <summary>
        /// Deletes a dashboard widget
        /// </summary>
        public async Task<ResponseModel> DeleteWidget(int widgetId)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Find the widget
                var widget = await context.Set<ReportDashboardWidget>()
                    .FirstOrDefaultAsync(w => w.Id == widgetId);

                if (widget == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Widget with ID {widgetId} not found";
                    return response;
                }

                // Remove the widget
                context.Remove(widget);
                await context.SaveChangesAsync();

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Widget deleted successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting widget: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error deleting widget: {ex.Message}";
            }

            return response;
        }

        /// <summary>
        /// Gets dashboard widget data
        /// </summary>
        public async Task<ResponseModel> GetWidgetData(int widgetId)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Find the widget
                var widget = await context.Set<ReportDashboardWidget>()
                    .Include(w => w.CustomReport)
                    .FirstOrDefaultAsync(w => w.Id == widgetId);

                if (widget == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Widget with ID {widgetId} not found";
                    return response;
                }

                // Parse parameters
                var parameters = string.IsNullOrEmpty(widget.Parameters) ?
                    new Dictionary<string, object>() :
                    System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(widget.Parameters);

                // Get data based on the data source
                ResponseModel dataResult;

                if (widget.DataSource == "CustomReport" && widget.CustomReportId.HasValue && widget.CustomReport != null)
                {
                    // Execute custom report
                    dataResult = await GetCustomReport(widget.CustomReport.Name, parameters);
                }
                else if (widget.DataSource == "StandardReport" && !string.IsNullOrEmpty(widget.StandardReportType))
                {
                    // Execute appropriate standard report method based on widget.StandardReportType
                    dataResult = widget.StandardReportType switch
                    {
                        "dashboardSummary" => await GetDashboardSummary(
                            Convert.ToInt32(parameters["companyId"]),
                            parameters.ContainsKey("branchId") ? Convert.ToInt32(parameters["branchId"]) : null),

                        "financialDashboard" => await GetFinancialDashboard(
                            Convert.ToInt32(parameters["companyId"]),
                            parameters.ContainsKey("branchId") ? Convert.ToInt32(parameters["branchId"]) : null),

                        "maintenanceDashboard" => await GetMaintenanceDashboard(
                            Convert.ToInt32(parameters["companyId"]),
                            parameters.ContainsKey("branchId") ? Convert.ToInt32(parameters["branchId"]) : null),

                        "occupancyDashboard" => await GetOccupancyDashboard(
                            Convert.ToInt32(parameters["companyId"]),
                            parameters.ContainsKey("branchId") ? Convert.ToInt32(parameters["branchId"]) : null),

                        "arrearsReport" => await GetArrearsReport(
                            Convert.ToInt32(parameters["companyId"]),
                            parameters.ContainsKey("branchId") ? Convert.ToInt32(parameters["branchId"]) : null),

                        "leaseExpiryReport" => await GetLeaseExpiryReport(
                            Convert.ToInt32(parameters["companyId"]),
                            parameters.ContainsKey("branchId") ? Convert.ToInt32(parameters["branchId"]) : null,
                            parameters.ContainsKey("monthsAhead") ? Convert.ToInt32(parameters["monthsAhead"]) : 3),

                        _ => new ResponseModel
                        {
                            ResponseInfo = new ResponseInfo
                            {
                                Success = false,
                                Message = $"Unsupported standard report type: {widget.StandardReportType}"
                            }
                        }
                    };
                }
                else
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Invalid widget data source configuration";
                    return response;
                }

                if (!dataResult.ResponseInfo.Success)
                {
                    return dataResult;
                }

                // Update last refreshed timestamp
                widget.LastRefreshed = DateTime.Now;
                context.Update(widget);
                await context.SaveChangesAsync();

                response.Response = new
                {
                    WidgetId = widgetId,
                    WidgetType = widget.WidgetType,
                    Title = widget.Title,
                    LastRefreshed = widget.LastRefreshed,
                    Data = dataResult.Response
                };

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Widget data retrieved successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving widget data: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving widget data: {ex.Message}";
            }

            return response;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Calculate the next run date for a scheduled report
        /// </summary>
        private DateTime CalculateNextRunDate(int frequencyTypeId, int? dayOfWeek, int? dayOfMonth, TimeSpan executionTime)
        {
            var now = DateTime.Now;
            var today = now.Date.Add(executionTime);

            // If today's execution time has passed, start from tomorrow
            var baseDate = now > today ? DateTime.Now.AddDays(1).Date : DateTime.Now.Date;
            baseDate = baseDate.Add(executionTime);

            switch (frequencyTypeId)
            {
                case 1: // Daily
                    return baseDate;

                case 2: // Weekly
                    if (!dayOfWeek.HasValue || dayOfWeek.Value < 1 || dayOfWeek.Value > 7)
                        dayOfWeek = 1; // Monday by default

                    int daysToAdd = (dayOfWeek.Value - (int)baseDate.DayOfWeek + 7) % 7;
                    return baseDate.AddDays(daysToAdd == 0 && now > today ? 7 : daysToAdd);

                case 3: // Monthly
                    if (!dayOfMonth.HasValue || dayOfMonth.Value < 1 || dayOfMonth.Value > 31)
                        dayOfMonth = 1; // First day by default

                    var firstDayOfMonth = new DateTime(baseDate.Year, baseDate.Month, 1).Add(executionTime);

                    // If the specified day has passed this month, move to next month
                    if (dayOfMonth.Value < baseDate.Day || (dayOfMonth.Value == baseDate.Day && now > today))
                    {
                        firstDayOfMonth = firstDayOfMonth.AddMonths(1);
                    }

                    // Make sure the day is valid for the month
                    var daysInMonth = DateTime.DaysInMonth(firstDayOfMonth.Year, firstDayOfMonth.Month);
                    var targetDay = Math.Min(dayOfMonth.Value, daysInMonth);

                    return new DateTime(firstDayOfMonth.Year, firstDayOfMonth.Month, targetDay,
                        executionTime.Hours, executionTime.Minutes, executionTime.Seconds);

                case 4: // Quarterly
                    if (!dayOfMonth.HasValue || dayOfMonth.Value < 1 || dayOfMonth.Value > 31)
                        dayOfMonth = 1; // First day by default

                    var currentQuarter = (baseDate.Month - 1) / 3;
                    var nextQuarterMonth = currentQuarter * 3 + 1;

                    // If we're already past the specified day in this quarter, move to the next quarter
                    if (baseDate.Month % 3 == 0 && baseDate.Day >= dayOfMonth.Value && now > today)
                    {
                        nextQuarterMonth = ((currentQuarter + 1) % 4) * 3 + 1;
                    }

                    // If the month is past the quarter month, move to next quarter
                    if (baseDate.Month > nextQuarterMonth)
                    {
                        nextQuarterMonth = ((currentQuarter + 1) % 4) * 3 + 1;
                    }

                    // If we're in the quarter month but after the target day, move to next quarter
                    if (baseDate.Month == nextQuarterMonth && baseDate.Day > dayOfMonth.Value)
                    {
                        nextQuarterMonth = ((currentQuarter + 1) % 4) * 3 + 1;
                    }

                    // Move to next year if needed
                    var targetYear = baseDate.Year;
                    if (nextQuarterMonth < baseDate.Month)
                    {
                        targetYear++;
                    }

                    // Make sure the day is valid for the month
                    var daysInTargetMonth = DateTime.DaysInMonth(targetYear, nextQuarterMonth);
                    var targetDayOfMonth = Math.Min(dayOfMonth.Value, daysInTargetMonth);

                    return new DateTime(targetYear, nextQuarterMonth, targetDayOfMonth,
                        executionTime.Hours, executionTime.Minutes, executionTime.Seconds);

                case 5: // Yearly
                    if (!dayOfMonth.HasValue || dayOfMonth.Value < 1 || dayOfMonth.Value > 31)
                        dayOfMonth = 1;

                    // Check if this year's date has passed
                    var targetDate = new DateTime(baseDate.Year, 1, Math.Min(dayOfMonth.Value, 31),
                        executionTime.Hours, executionTime.Minutes, executionTime.Seconds);

                    if (targetDate < now)
                    {
                        targetDate = new DateTime(baseDate.Year + 1, 1, Math.Min(dayOfMonth.Value, 31),
                            executionTime.Hours, executionTime.Minutes, executionTime.Seconds);
                    }

                    return targetDate;

                default:
                    return baseDate; // Default to daily if frequency type is unknown
            }
        }

        /// <summary>
        /// Determine content type from export format
        /// </summary>
        private string GetContentType(string exportFormat)
        {
            return exportFormat.ToLower() switch
            {
                "csv" => "text/csv",
                "json" => "application/json",
                "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "pdf" => "application/pdf",
                _ => "application/octet-stream"
            };
        }

        #endregion
    }
}