using CleanTime.Economy.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Economy.Application
{
    public sealed record CreditMoneyCommand(
        Money Amount,
        TransactionReason Reason,
        string ReferenceId = "") : ICommand;

    public sealed record DebitMoneyCommand(
        Money Amount,
        TransactionReason Reason,
        string ReferenceId = "") : ICommand;

    public sealed record GetPlayerWalletQuery : IQuery<Money>;
}
