using System.Collections.Generic;
using CleanTime.Shared.Kernel;

namespace CleanTime.Economy.Domain
{
    public interface IWalletRepository
    {
        Result<Wallet> GetCurrent();

        Result Save(Wallet wallet);
    }

    public interface ITransactionRepository
    {
        void Add(Transaction transaction);

        IReadOnlyList<Transaction> GetAll();

        IReadOnlyList<Transaction> GetRecent(int count);
    }

    public interface IPurchasePolicy
    {
        Result CanPurchase(Wallet wallet, Money price);
    }
}
