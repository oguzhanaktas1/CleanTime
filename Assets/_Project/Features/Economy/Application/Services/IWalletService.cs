using CleanTime.Economy.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Economy.Application
{
    /// <summary>
    /// Public economy contract for other modules. All wallet mutations go through here so each one
    /// is ledgered and announced; no other module touches wallet or ledger storage.
    /// </summary>
    public interface IWalletService
    {
        Money Balance { get; }

        bool CanAfford(Money price);

        Result Credit(Money amount, TransactionReason reason, string referenceId = "");

        /// <summary>Plain debit (no purchase semantics).</summary>
        Result Debit(Money amount, TransactionReason reason, string referenceId = "");

        /// <summary>Purchase flow: policy check, debit, ledger, then PurchaseCompleted or PurchaseRejected.</summary>
        Result Pay(Money price, TransactionReason reason, string referenceId = "");
    }
}
