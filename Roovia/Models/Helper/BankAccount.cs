using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace Roovia.Models.Helper
{
    public class BankAccount
    {
        [StringLength(100)]
        public string? AccountType { get; set; }

        [StringLength(10)]
        public string? AccountNumber { get; set; }

        public BankName BankName { get; set; }

        [StringLength(6)]
        public string? BranchCode { get; set; }
    }

    public enum BankName
    {
        Absa,
        Capitec,
        FNB,
        Nedbank,
        StandardBank,
    }

    // Keep the validator class - no changes needed
    public class BankAccountValidator : AbstractValidator<BankAccount>
    {
        public BankAccountValidator()
        {
            RuleFor(account => account.AccountType)
                .NotEmpty().WithMessage("Account type is required.")
                .MaximumLength(100).WithMessage("Account name must not exceed 100 characters.");
            RuleFor(account => account.AccountNumber)
                .NotEmpty().WithMessage("Account number is required.")
                .Matches(@"^\d{10}$").WithMessage("Account number must be a valid 10-digit number.");
            RuleFor(account => account.BranchCode)
                .NotEmpty().WithMessage("Branch code is required.")
                .Matches(@"^\d{6}$").WithMessage("Branch code must be a valid 6-digit number.");
        }
    }
}