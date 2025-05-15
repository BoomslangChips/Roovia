using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Roovia.Data;
using Roovia.Interfaces;
using Roovia.Models.BusinessHelperModels;
using Roovia.Models.BusinessModels;
using Roovia.Models.UserCompanyModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Roovia.Services
{
    public class PaymentService : IPayment
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<PaymentService> _logger;
        private readonly IEmailService _emailService;
        private readonly ICdnService _cdnService;

        public PaymentService(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            ILogger<PaymentService> logger,
            IEmailService emailService,
            ICdnService cdnService)
        {
            _contextFactory = contextFactory;
            _logger = logger;
            _emailService = emailService;
            _cdnService = cdnService;
        }

        public async Task<ResponseModel> CreatePropertyPayment(PropertyPayment payment, int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                // Verify the property exists and belongs to the company
                var property = await context.Properties
                    .Include(p => p.Owner)
                    .FirstOrDefaultAsync(p => p.Id == payment.PropertyId && p.CompanyId == companyId && !p.IsRemoved);

                if (property == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Property not found or does not belong to the company.";
                    return response;
                }

                // Generate unique payment reference
                payment.PaymentReference = await GenerateUniquePaymentReference(context);
                payment.CompanyId = companyId;
                payment.CreatedOn = DateTime.Now;

                // Calculate late fees if applicable
                if (payment.DueDate < DateTime.Now)
                {
                    payment.IsLate = true;
                    payment.DaysLate = (int)(DateTime.Now - payment.DueDate).TotalDays;
                    payment.LateFee = await CalculateLateFee(context, companyId, payment.Amount, payment.DaysLate.Value);
                }

                // Calculate processing fees if payment method is specified
                if (payment.PaymentMethodId.HasValue)
                {
                    payment.ProcessingFee = await CalculateProcessingFee(context, payment.PaymentMethodId.Value, payment.Amount);
                }

                // Calculate net amount
                payment.NetAmount = payment.Amount + (payment.LateFee ?? 0) + (payment.ProcessingFee ?? 0);

                // Add the payment
                await context.PropertyPayments.AddAsync(payment);
                await context.SaveChangesAsync();

                // Reload with related data
                var createdPayment = await GetPaymentWithDetails(context, payment.Id);

                // Send notifications
                await SendPaymentNotifications(createdPayment, property);

                response.Response = createdPayment;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Property payment created successfully.";

                _logger.LogInformation("Property payment created with ID: {PaymentId} for property {PropertyId}", 
                    payment.Id, payment.PropertyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating property payment");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while creating the payment: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetPropertyPaymentById(int id, int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var payment = await context.PropertyPayments
                    .Include(p => p.Property)
                        .ThenInclude(pr => pr.Owner)
                    .Include(p => p.Tenant)
                    .Include(p => p.PaymentType)
                    .Include(p => p.Status)
                    .Include(p => p.PaymentMethod)
                    .Include(p => p.ReceiptDocument)
                    .Include(p => p.Allocations)
                        .ThenInclude(a => a.Beneficiary)
                    .Include(p => p.Allocations)
                        .ThenInclude(a => a.AllocationType)
                    .Where(p => p.Id == id && p.CompanyId == companyId)
                    .FirstOrDefaultAsync();

                if (payment != null)
                {
                    response.Response = payment;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Property payment retrieved successfully.";
                }
                else
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Property payment not found.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving property payment {PaymentId} for company {CompanyId}", id, companyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving the payment: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> UpdatePropertyPaymentStatus(int paymentId, int statusId, string userId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var payment = await context.PropertyPayments
                    .FirstOrDefaultAsync(p => p.Id == paymentId);

                if (payment == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Property payment not found.";
                    return response;
                }

                var oldStatusId = payment.StatusId;
                payment.StatusId = statusId;
                payment.UpdatedDate = DateTime.Now;
                payment.UpdatedBy = userId;

                // If payment is confirmed, set payment date
                if (statusId == 2) // Assuming 2 is Paid/Confirmed status
                {
                    payment.PaymentDate = DateTime.Now;
                }

                await context.SaveChangesAsync();

                // If payment is confirmed, create allocations
                if (statusId == 2 && oldStatusId != 2)
                {
                    await AllocatePayment(payment.Id);
                }

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Payment status updated successfully.";

                _logger.LogInformation("Payment status updated: {PaymentId} to status {StatusId} by {UserId}", 
                    paymentId, statusId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating payment status {PaymentId}", paymentId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while updating payment status: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> AllocatePayment(int paymentId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var payment = await context.PropertyPayments
                    .Include(p => p.Property)
                        .ThenInclude(pr => pr.Beneficiaries.Where(b => b.IsActive))
                    .Include(p => p.Property)
                        .ThenInclude(pr => pr.Owner)
                    .Include(p => p.Allocations)
                    .FirstOrDefaultAsync(p => p.Id == paymentId);

                if (payment == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Property payment not found.";
                    return response;
                }

                if (payment.IsAllocated)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Payment is already allocated.";
                    return response;
                }

                // Get allocation rules
                var rules = await GetPaymentRules(context, payment.CompanyId);
                
                // Calculate allocations based on beneficiaries
                var allocations = new List<PaymentAllocation>();
                var remainingAmount = payment.Amount;

                foreach (var beneficiary in payment.Property.Beneficiaries.Where(b => b.BenStatusId == 1)) // Active beneficiaries
                {
                    decimal allocationAmount = 0;

                    if (beneficiary.CommissionTypeId == 1) // Percentage
                    {
                        allocationAmount = payment.Amount * (beneficiary.CommissionValue / 100);
                    }
                    else if (beneficiary.CommissionTypeId == 2) // Fixed amount
                    {
                        allocationAmount = beneficiary.CommissionValue;
                    }

                    var allocation = new PaymentAllocation
                    {
                        PaymentId = paymentId,
                        BeneficiaryId = beneficiary.Id,
                        Amount = Math.Round(allocationAmount, 2),
                        Percentage = beneficiary.CommissionTypeId == 1 ? beneficiary.CommissionValue : 0,
                        Description = $"Payment allocation for {beneficiary.Name}",
                        AllocationDate = DateTime.Now,
                        AllocatedBy = payment.UpdatedBy,
                        AllocationTypeId = 1 // Standard allocation
                    };

                    allocations.Add(allocation);
                    remainingAmount -= allocation.Amount;
                }

                // Allocate remaining amount to property owner if any
                if (remainingAmount > 0)
                {
                    var ownerAllocation = new PaymentAllocation
                    {
                        PaymentId = paymentId,
                        Amount = remainingAmount,
                        Percentage = 0,
                        Description = $"Remaining payment to property owner",
                        AllocationDate = DateTime.Now,
                        AllocatedBy = payment.UpdatedBy,
                        AllocationTypeId = 2 // Owner allocation
                    };

                    allocations.Add(ownerAllocation);
                }

                // Save allocations
                await context.PaymentAllocations.AddRangeAsync(allocations);

                // Mark payment as allocated
                payment.IsAllocated = true;
                payment.AllocationDate = DateTime.Now;
                payment.AllocatedBy = payment.UpdatedBy;

                await context.SaveChangesAsync();

                // Create beneficiary payment records
                await CreateBeneficiaryPayments(allocations, payment);

                response.Response = allocations;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Payment allocated successfully.";

                _logger.LogInformation("Payment allocated: {PaymentId} with {Count} allocations", 
                    paymentId, allocations.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error allocating payment {PaymentId}", paymentId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while allocating the payment: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> CreateBeneficiaryPayment(BeneficiaryPayment payment)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                // Verify the beneficiary exists
                var beneficiary = await context.PropertyBeneficiaries
                    .FirstOrDefaultAsync(b => b.Id == payment.BeneficiaryId && b.IsActive);

                if (beneficiary == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Beneficiary not found.";
                    return response;
                }

                // Generate unique payment reference
                payment.PaymentReference = await GenerateUniqueBeneficiaryPaymentReference(context);
                payment.CreatedOn = DateTime.Now;

                // Add the payment
                await context.BeneficiaryPayments.AddAsync(payment);
                await context.SaveChangesAsync();

                // Reload with related data
                var createdPayment = await context.BeneficiaryPayments
                    .Include(bp => bp.Beneficiary)
                    .Include(bp => bp.PaymentAllocation)
                    .Include(bp => bp.Status)
                    .FirstOrDefaultAsync(bp => bp.Id == payment.Id);

                response.Response = createdPayment;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Beneficiary payment created successfully.";

                _logger.LogInformation("Beneficiary payment created with ID: {PaymentId} for beneficiary {BeneficiaryId}", 
                    payment.Id, payment.BeneficiaryId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating beneficiary payment");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while creating the payment: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> ProcessBeneficiaryPayment(int paymentId, string transactionReference, string userId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var payment = await context.BeneficiaryPayments
                    .Include(bp => bp.Beneficiary)
                    .FirstOrDefaultAsync(bp => bp.Id == paymentId);

                if (payment == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Beneficiary payment not found.";
                    return response;
                }

                payment.StatusId = 2; // Paid
                payment.PaymentDate = DateTime.Now;
                payment.TransactionReference = transactionReference;

                await context.SaveChangesAsync();

                // Send payment notification
                await SendBeneficiaryPaymentNotification(payment);

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Beneficiary payment processed successfully.";

                _logger.LogInformation("Beneficiary payment processed: {PaymentId} by {UserId}", paymentId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing beneficiary payment {PaymentId}", paymentId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while processing the payment: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> CreatePaymentSchedule(PaymentSchedule schedule, int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                // Verify the property and tenant exist
                var property = await context.Properties
                    .FirstOrDefaultAsync(p => p.Id == schedule.PropertyId && p.CompanyId == companyId && !p.IsRemoved);

                if (property == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Property not found.";
                    return response;
                }

                var tenant = await context.PropertyTenants
                    .FirstOrDefaultAsync(t => t.Id == schedule.TenantId && t.PropertyId == schedule.PropertyId && !t.IsRemoved);

                if (tenant == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Tenant not found or not associated with the property.";
                    return response;
                }

                // Calculate next due date
                schedule.NextDueDate = CalculateNextDueDate(schedule.StartDate, schedule.FrequencyId, schedule.DayOfMonth);
                schedule.CreatedOn = DateTime.Now;

                // Add the schedule
                await context.PaymentSchedules.AddAsync(schedule);
                await context.SaveChangesAsync();

                // Reload with related data
                var createdSchedule = await context.PaymentSchedules
                    .Include(s => s.Property)
                    .Include(s => s.Tenant)
                    .Include(s => s.Frequency)
                    .FirstOrDefaultAsync(s => s.Id == schedule.Id);

                response.Response = createdSchedule;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Payment schedule created successfully.";

                _logger.LogInformation("Payment schedule created with ID: {ScheduleId} for property {PropertyId}", 
                    schedule.Id, schedule.PropertyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment schedule");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while creating the schedule: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GenerateScheduledPayments(int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var schedules = await context.PaymentSchedules
                    .Include(s => s.Property)
                    .Include(s => s.Tenant)
                    .Include(s => s.Frequency)
                    .Where(s => s.Property.CompanyId == companyId &&
                               s.IsActive &&
                               s.AutoGenerate &&
                               s.NextDueDate.HasValue &&
                               s.NextDueDate.Value.AddDays(-s.DaysBeforeDue) <= DateTime.Today)
                    .ToListAsync();

                var paymentsGenerated = 0;

                foreach (var schedule in schedules)
                {
                    // Check if payment already exists for this period
                    var existingPayment = await context.PropertyPayments
                        .AnyAsync(p => p.PropertyId == schedule.PropertyId &&
                                      p.TenantId == schedule.TenantId &&
                                      p.DueDate == schedule.NextDueDate.Value);

                    if (existingPayment)
                        continue;

                    // Create the payment
                    var payment = new PropertyPayment
                    {
                        PropertyId = schedule.PropertyId,
                        CompanyId = companyId,
                        TenantId = schedule.TenantId,
                        PaymentReference = await GenerateUniquePaymentReference(context),
                        PaymentTypeId = 1, // Rent payment
                        Amount = schedule.Amount,
                        Currency = "USD",
                        StatusId = 1, // Pending
                        DueDate = schedule.NextDueDate.Value,
                        CreatedOn = DateTime.Now,
                        CreatedBy = "System"
                    };

                    await context.PropertyPayments.AddAsync(payment);

                    // Update schedule
                    schedule.LastGeneratedDate = DateTime.Now;
                    schedule.NextDueDate = CalculateNextDueDate(schedule.NextDueDate.Value, 
                        schedule.FrequencyId, schedule.DayOfMonth);

                    paymentsGenerated++;
                }

                await context.SaveChangesAsync();

                response.Response = new { PaymentsGenerated = paymentsGenerated };
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = $"{paymentsGenerated} scheduled payments generated successfully.";

                _logger.LogInformation("Generated {Count} scheduled payments for company {CompanyId}", 
                    paymentsGenerated, companyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating scheduled payments for company {CompanyId}", companyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while generating scheduled payments: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> CreatePaymentRule(PaymentRule rule, int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                rule.CompanyId = companyId;
                rule.CreatedOn = DateTime.Now;

                await context.PaymentRules.AddAsync(rule);
                await context.SaveChangesAsync();

                // Reload with related data
                var createdRule = await context.PaymentRules
                    .Include(r => r.RuleType)
                    .FirstOrDefaultAsync(r => r.Id == rule.Id);

                response.Response = createdRule;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Payment rule created successfully.";

                _logger.LogInformation("Payment rule created with ID: {RuleId} for company {CompanyId}", 
                    rule.Id, companyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment rule");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while creating the rule: " + ex.Message;
            }

            return response;
        }

        public async Task<ResponseModel> GetPaymentStatistics(int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var payments = await context.PropertyPayments
                    .Where(p => p.CompanyId == companyId)
                    .ToListAsync();

                var statistics = new
                {
                    TotalPayments = payments.Count,
                    PendingPayments = payments.Count(p => p.StatusId == 1),
                    CompletedPayments = payments.Count(p => p.StatusId == 2),
                    LatePayments = payments.Count(p => p.IsLate),
                    TotalAmount = payments.Sum(p => p.Amount),
                    TotalCollected = payments.Where(p => p.StatusId == 2).Sum(p => p.Amount),
                    TotalPending = payments.Where(p => p.StatusId == 1).Sum(p => p.Amount),
                    TotalLateFees = payments.Sum(p => p.LateFee ?? 0),
                    TotalProcessingFees = payments.Sum(p => p.ProcessingFee ?? 0),
                    PaymentsByType = payments.GroupBy(p => p.PaymentTypeId)
                        .Select(g => new { TypeId = g.Key, Count = g.Count(), Amount = g.Sum(p => p.Amount) })
                        .ToList(),
                    PaymentsByMonth = GenerateMonthlyPaymentStats(payments),
                    PaymentMethods = payments.Where(p => p.PaymentMethodId.HasValue)
                        .GroupBy(p => p.PaymentMethodId)
                        .Select(g => new { MethodId = g.Key, Count = g.Count(), Amount = g.Sum(p => p.Amount) })
                        .ToList()
                };

                response.Response = statistics;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Payment statistics retrieved successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving payment statistics for company {CompanyId}", companyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving statistics: " + ex.Message;
            }

            return response;
        }

        // Helper methods
        private async Task<PropertyPayment> GetPaymentWithDetails(ApplicationDbContext context, int paymentId)
        {
            return await context.PropertyPayments
                .Include(p => p.Property)
                    .ThenInclude(pr => pr.Owner)
                .Include(p => p.Tenant)
                .Include(p => p.PaymentType)
                .Include(p => p.Status)
                .Include(p => p.PaymentMethod)
                .Include(p => p.ReceiptDocument)
                .Include(p => p.Allocations)
                    .ThenInclude(a => a.Beneficiary)
                .Include(p => p.Allocations)
                    .ThenInclude(a => a.AllocationType)
                .FirstOrDefaultAsync(p => p.Id == paymentId);
        }

        private async Task<string> GenerateUniquePaymentReference(ApplicationDbContext context)
        {
            string reference;
            do
            {
                reference = $"PAY-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}";
            }
            while (await context.PropertyPayments.AnyAsync(p => p.PaymentReference == reference));

            return reference;
        }

        private async Task<string> GenerateUniqueBeneficiaryPaymentReference(ApplicationDbContext context)
        {
            string reference;
            do
            {
                reference = $"BPN-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}";
            }
            while (await context.BeneficiaryPayments.AnyAsync(p => p.PaymentReference == reference));

            return reference;
        }

        private async Task<decimal> CalculateLateFee(ApplicationDbContext context, int companyId, decimal amount, int daysLate)
        {
            var rule = await context.PaymentRules
                .FirstOrDefaultAsync(r => r.CompanyId == companyId && 
                                         r.IsActive && 
                                         r.RuleTypeId == 1); // Late fee rule

            if (rule == null)
                return 0;

            if (daysLate < rule.GracePeriodDays)
                return 0;

            decimal lateFee = 0;

            if (rule.LateFeeAmount.HasValue)
                lateFee += rule.LateFeeAmount.Value;

            if (rule.LateFeePercentage.HasValue)
                lateFee += amount * (rule.LateFeePercentage.Value / 100);

            return Math.Round(lateFee, 2);
        }

        private async Task<decimal> CalculateProcessingFee(ApplicationDbContext context, int paymentMethodId, decimal amount)
        {
            var method = await context.PaymentMethods
                .FirstOrDefaultAsync(m => m.Id == paymentMethodId);

            if (method == null)
                return 0;

            decimal fee = 0;

            if (method.ProcessingFeeFixed.HasValue)
                fee += method.ProcessingFeeFixed.Value;

            if (method.ProcessingFeePercentage.HasValue)
                fee += amount * (method.ProcessingFeePercentage.Value / 100);

            return Math.Round(fee, 2);
        }

        private DateTime CalculateNextDueDate(DateTime currentDate, int frequencyId, int dayOfMonth)
        {
            // This is a simplified calculation - should be enhanced based on frequency type
            return frequencyId switch
            {
                1 => currentDate.AddMonths(1), // Monthly
                2 => currentDate.AddDays(14), // Bi-weekly
                3 => currentDate.AddDays(7), // Weekly
                4 => currentDate.AddMonths(3), // Quarterly
                5 => currentDate.AddYears(1), // Annually
                _ => currentDate.AddMonths(1)
            };
        }

        private async Task<List<PaymentRule>> GetPaymentRules(ApplicationDbContext context, int companyId)
        {
            return await context.PaymentRules
                .Where(r => r.CompanyId == companyId && r.IsActive)
                .ToListAsync();
        }

        private async Task CreateBeneficiaryPayments(List<PaymentAllocation> allocations, PropertyPayment payment)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var beneficiaryPayments = new List<BeneficiaryPayment>();

            foreach (var allocation in allocations.Where(a => a.BeneficiaryId.HasValue))
            {
                var beneficiaryPayment = new BeneficiaryPayment
                {
                    BeneficiaryId = allocation.BeneficiaryId.Value,
                    PaymentAllocationId = allocation.Id,
                    PaymentReference = await GenerateUniqueBeneficiaryPaymentReference(context),
                    Amount = allocation.Amount,
                    StatusId = 1, // Pending
                    CreatedOn = DateTime.Now,
                    CreatedBy = allocation.AllocatedBy
                };

                beneficiaryPayments.Add(beneficiaryPayment);
            }

            await context.BeneficiaryPayments.AddRangeAsync(beneficiaryPayments);
            await context.SaveChangesAsync();
        }

        private async Task SendPaymentNotifications(PropertyPayment payment, Property property)
        {
            try
            {
                // Notify tenant if applicable
                if (payment.Tenant?.PrimaryEmail != null)
                {
                    await _emailService.SendEmailAsync(
                        payment.Tenant.PrimaryEmail,
                        $"Payment Confirmation - {property.PropertyName}",
                        $"Your payment has been recorded.\n\n" +
                        $"Reference: {payment.PaymentReference}\n" +
                        $"Amount: {payment.Currency} {payment.Amount:F2}\n" +
                        $"Property: {property.PropertyName}\n" +
                        $"Date: {payment.CreatedOn:dd/MM/yyyy}\n\n" +
                        $"Thank you for your payment."
                    );
                }

                // Notify property owner
                if (property.Owner?.PrimaryEmail != null)
                {
                    await _emailService.SendEmailAsync(
                        property.Owner.PrimaryEmail,
                        $"New Payment Received - {property.PropertyName}",
                        $"A new payment has been received for your property.\n\n" +
                        $"Reference: {payment.PaymentReference}\n" +
                        $"Amount: {payment.Currency} {payment.Amount:F2}\n" +
                        $"Tenant: {payment.Tenant?.FullName}\n" +
                        $"Date: {payment.CreatedOn:dd/MM/yyyy}"
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending payment notifications for payment {PaymentId}", payment.Id);
            }
        }

        private async Task SendBeneficiaryPaymentNotification(BeneficiaryPayment payment)
        {
            try
            {
                if (payment.Beneficiary?.PrimaryEmail != null)
                {
                    await _emailService.SendEmailAsync(
                        payment.Beneficiary.PrimaryEmail,
                        $"Payment Processed - {payment.PaymentReference}",
                        $"Your payment has been processed.\n\n" +
                        $"Reference: {payment.PaymentReference}\n" +
                        $"Amount: {payment.Amount:F2}\n" +
                        $"Transaction: {payment.TransactionReference}\n" +
                        $"Date: {payment.PaymentDate:dd/MM/yyyy}\n\n" +
                        $"The funds will be reflected in your account within 2-3 business days."
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending beneficiary payment notification for payment {PaymentId}", payment.Id);
            }
        }

        private object GenerateMonthlyPaymentStats(List<PropertyPayment> payments)
        {
            return payments
                .Where(p => p.CreatedOn >= DateTime.Now.AddMonths(-12))
                .GroupBy(p => new { p.CreatedOn.Year, p.CreatedOn.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    TotalPayments = g.Count(),
                    CompletedPayments = g.Count(p => p.StatusId == 2),
                    TotalAmount = g.Sum(p => p.Amount),
                    CollectedAmount = g.Where(p => p.StatusId == 2).Sum(p => p.Amount),
                    LateFees = g.Sum(p => p.LateFee ?? 0)
                })
                .OrderBy(m => m.Year)
                .ThenBy(m => m.Month)
                .ToList();
        }
    }
}