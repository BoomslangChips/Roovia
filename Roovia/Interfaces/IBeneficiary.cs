using Roovia.Models.BusinessHelperModels;
using Roovia.Models.BusinessModels;
using Roovia.Models.UserCompanyModels;
using System;
using System.Threading.Tasks;

namespace Roovia.Interfaces
{
    public interface IBeneficiary
    {
        // CRUD Operations
        Task<ResponseModel> CreateBeneficiary(PropertyBeneficiary beneficiary, int companyId);
        Task<ResponseModel> GetBeneficiaryById(int id, int companyId);
        Task<ResponseModel> UpdateBeneficiary(int id, PropertyBeneficiary updatedBeneficiary, int companyId);
        Task<ResponseModel> DeleteBeneficiary(int id, int companyId, ApplicationUser user);

        // Listing Methods
        Task<ResponseModel> GetBeneficiariesByProperty(int propertyId, int companyId, int page = 1, int pageSize = 20, string sortField = "Name", bool sortAscending = true);
        Task<ResponseModel> GetAllBeneficiaries(int companyId, int page = 1, int pageSize = 20, string sortField = "Name", bool sortAscending = true, string filterField = null, string filterValue = null);
        Task<ResponseModel> GetBeneficiariesByType(int companyId, int beneficiaryTypeId, int page = 1, int pageSize = 20);

        // Contact Management
        Task<ResponseModel> AddEmailAddress(int beneficiaryId, Email email);
        Task<ResponseModel> AddContactNumber(int beneficiaryId, ContactNumber contactNumber);

        // Status and Updates
        Task<ResponseModel> UpdateBeneficiaryStatus(int beneficiaryId, int statusId, string userId);
        Task<ResponseModel> UpdateBankAccount(int beneficiaryId, BankAccount bankAccount, string userId);
        Task<ResponseModel> UpdateBeneficiaryAddress(int beneficiaryId, Address newAddress, string userId);

        // Payment Related
        Task<ResponseModel> CalculateBeneficiaryAmounts(int propertyId, decimal paymentAmount);
        Task<ResponseModel> GetBeneficiaryPaymentHistory(int beneficiaryId, int companyId);

        // Search
        Task<ResponseModel> SearchBeneficiaries(int companyId, string searchTerm, int page = 1, int pageSize = 20);
    }
}