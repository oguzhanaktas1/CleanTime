using CleanTime.Shared.Kernel;

namespace CleanTime.Economy.Domain
{
    public sealed record MoneyCreditedEvent(
        Money Amount,
        Money BalanceAfter,
        TransactionReason Reason,
        string ReferenceId) : IDomainEvent;

    public sealed record MoneyDebitedEvent(
        Money Amount,
        Money BalanceAfter,
        TransactionReason Reason,
        string ReferenceId) : IDomainEvent;

    public sealed record PurchaseCompletedEvent(
        Money Price,
        TransactionReason Reason,
        string ReferenceId) : IDomainEvent;

    public sealed record PurchaseRejectedEvent(
        Money Price,
        TransactionReason Reason,
        string ReferenceId,
        string ErrorCode) : IDomainEvent;
}
