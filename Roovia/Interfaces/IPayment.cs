using Roovia.Models.BusinessHelperModels;
using Roovia.Models.BusinessModels;

namespace Roovia.Interfaces
{
    public interface IPayment
    {
        // Property Payment Operations
        Task<ResponseModel> CreatePropertyPayment(PropertyPayment payment, int companyId);

        Task<ResponseModel> GetPropertyPaymentById(int id, int companyId);

        Task<ResponseModel> UpdatePropertyPaymentStatus(int paymentId, int statusId, string userId);

        Task<ResponseModel> UploadPaymentReceipt(int paymentId, IFormFile file, string userId);

        // Payment Allocation
        Task<ResponseModel> AllocatePayment(int paymentId);

        // Beneficiary Payments
        Task<ResponseModel> CreateBeneficiaryPayment(BeneficiaryPayment payment);

        Task<ResponseModel> ProcessBeneficiaryPayment(int paymentId, string transactionReference, string userId);

        Task<ResponseModel> UploadBeneficiaryPaymentProof(int beneficiaryPaymentId, IFormFile file, string userId);

        // Payment Schedules
        Task<ResponseModel> CreatePaymentSchedule(PaymentSchedule schedule, int companyId);

        Task<ResponseModel> GenerateScheduledPayments(int companyId);

        // Payment Rules
        Task<ResponseModel> CreatePaymentRule(PaymentRule rule, int companyId);

        // Documents and Statistics
        Task<ResponseModel> GetPaymentDocuments(int paymentId, int companyId);

        Task<ResponseModel> GetPaymentStatistics(int companyId);
    }
}