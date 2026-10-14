using System;
using CleanTime.Shared.Kernel;

namespace CleanTime.Economy.Domain
{
    public enum TransactionReason
    {
        JobReward,
        EmployeeRevenue,
        QuestReward,
        EquipmentPurchase,
        EquipmentUpgrade,
        GiftPurchase,
        EmployeeHire,
        CompanyUpgrade,
        DebugGrant,
        MigrationAdjustment,
    }

    /// <summary>Immutable ledger entry. Every wallet mutation produces exactly one.</summary>
    public sealed class Transaction
    {
        public Transaction(
            string id,
            Money amount,
            Money balanceAfter,
            TransactionReason reason,
            string referenceId,
            int gameDay,
            DateTime createdAtUtc)
        {
            Id = Guard.NotEmpty(id, nameof(id));
            Amount = amount;
            BalanceAfter = balanceAfter;
            Reason = reason;
            ReferenceId = referenceId ?? string.Empty;
            GameDay = gameDay;
            CreatedAtUtc = createdAtUtc;
        }

        public string Id { get; }

        /// <summary>Signed: positive for credits, negative for debits.</summary>
        public Money Amount { get; }

        public Money BalanceAfter { get; }

        public TransactionReason Reason { get; }

        public string ReferenceId { get; }

        public int GameDay { get; }

        public DateTime CreatedAtUtc { get; }
    }
}
