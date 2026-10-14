using System;
using CleanTime.Shared.Kernel;

namespace CleanTime.Economy.Domain
{
    public sealed class Wallet
    {
        public Wallet(Money initialBalance)
        {
            if (initialBalance.IsNegative)
                throw new ArgumentOutOfRangeException(nameof(initialBalance));

            Balance = initialBalance;
        }

        public Money Balance { get; private set; }

        public bool CanAfford(Money amount) => !(Balance - amount).IsNegative;

        public Result Credit(Money amount)
        {
            if (amount.Cents <= 0)
                return Result.Failure(
                    EconomyErrors.InvalidCredit,
                    "Credit amount must be positive.");

            Balance += amount;
            return Result.Success();
        }

        public Result Debit(Money amount)
        {
            if (amount.Cents <= 0)
                return Result.Failure(
                    EconomyErrors.InvalidDebit,
                    "Debit amount must be positive.");

            if ((Balance - amount).IsNegative)
                return Result.Failure(
                    EconomyErrors.InsufficientFunds,
                    "Not enough money.");

            Balance -= amount;
            return Result.Success();
        }
    }

    public static class EconomyErrors
    {
        public const string InvalidCredit = "economy.invalid_credit";
        public const string InvalidDebit = "economy.invalid_debit";
        public const string InsufficientFunds = "economy.insufficient_funds";
    }
}
