using CleanTime.Economy.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Economy.Application
{
    internal sealed class WalletService : IWalletService
    {
        private readonly IWalletRepository _wallets;
        private readonly ITransactionRepository _transactions;
        private readonly IPurchasePolicy _policy;
        private readonly IEventBus _events;
        private readonly IGameClock _clock;
        private readonly IUtcClock _utcClock;
        private readonly IIdGenerator _ids;

        public WalletService(
            IWalletRepository wallets,
            ITransactionRepository transactions,
            IPurchasePolicy policy,
            IEventBus events,
            IGameClock clock,
            IUtcClock utcClock,
            IIdGenerator ids)
        {
            _wallets = wallets;
            _transactions = transactions;
            _policy = policy;
            _events = events;
            _clock = clock;
            _utcClock = utcClock;
            _ids = ids;
        }

        public Money Balance
        {
            get
            {
                var wallet = _wallets.GetCurrent();
                return wallet.IsSuccess ? wallet.Value.Balance : Money.Zero;
            }
        }

        public bool CanAfford(Money price)
        {
            var wallet = _wallets.GetCurrent();
            return wallet.IsSuccess && wallet.Value.CanAfford(price);
        }

        public Result Credit(Money amount, TransactionReason reason, string referenceId = "")
        {
            var walletResult = _wallets.GetCurrent();
            if (walletResult.IsFailure)
                return walletResult;

            var wallet = walletResult.Value;
            var credit = wallet.Credit(amount);
            if (credit.IsFailure)
                return credit;

            Commit(wallet, amount, reason, referenceId);
            _events.Publish(new MoneyCreditedEvent(amount, wallet.Balance, reason, referenceId ?? string.Empty));
            return Result.Success();
        }

        public Result Debit(Money amount, TransactionReason reason, string referenceId = "")
        {
            var walletResult = _wallets.GetCurrent();
            if (walletResult.IsFailure)
                return walletResult;

            var wallet = walletResult.Value;
            var debit = wallet.Debit(amount);
            if (debit.IsFailure)
                return debit;

            Commit(wallet, new Money(-amount.Cents), reason, referenceId);
            _events.Publish(new MoneyDebitedEvent(amount, wallet.Balance, reason, referenceId ?? string.Empty));
            return Result.Success();
        }

        public Result Pay(Money price, TransactionReason reason, string referenceId = "")
        {
            var walletResult = _wallets.GetCurrent();
            if (walletResult.IsFailure)
                return walletResult;

            var allowed = _policy.CanPurchase(walletResult.Value, price);
            if (allowed.IsFailure)
            {
                _events.Publish(new PurchaseRejectedEvent(price, reason, referenceId ?? string.Empty, allowed.ErrorCode));
                return allowed;
            }

            var debit = Debit(price, reason, referenceId);
            if (debit.IsFailure)
            {
                _events.Publish(new PurchaseRejectedEvent(price, reason, referenceId ?? string.Empty, debit.ErrorCode));
                return debit;
            }

            _events.Publish(new PurchaseCompletedEvent(price, reason, referenceId ?? string.Empty));
            return Result.Success();
        }

        private void Commit(Wallet wallet, Money signedAmount, TransactionReason reason, string referenceId)
        {
            _wallets.Save(wallet);
            _transactions.Add(new Transaction(
                _ids.NewId(),
                signedAmount,
                wallet.Balance,
                reason,
                referenceId,
                _clock.Now.Day,
                _utcClock.UtcNow));
        }
    }
}
