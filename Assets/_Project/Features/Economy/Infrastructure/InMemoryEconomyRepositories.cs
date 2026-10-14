using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CleanTime.Economy.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Economy.Infrastructure
{
    public sealed class InMemoryWalletRepository : IWalletRepository, ISaveParticipant
    {
        private readonly Money _startingBalance;
        private readonly ISnapshotStore _snapshots;
        private Wallet _wallet;

        public InMemoryWalletRepository(Money startingBalance, ISnapshotStore snapshots)
        {
            _startingBalance = startingBalance;
            _snapshots = snapshots;
            _wallet = new Wallet(startingBalance);
        }

        public string Key => "economy.wallet";

        public Result<Wallet> GetCurrent() => Result<Wallet>.Success(_wallet);

        public Result Save(Wallet wallet)
        {
            _wallet = wallet;
            return Result.Success();
        }

        public Result Flush(SaveId saveId)
        {
            _snapshots.Put(saveId, Key, _wallet.Balance.Cents.ToString(CultureInfo.InvariantCulture));
            return Result.Success();
        }

        public Result Load(SaveId saveId)
        {
            if (!_snapshots.TryGet(saveId, Key, out var payload))
                return Result.Failure("save.missing_section", $"No '{Key}' section in save {saveId}.");

            if (!long.TryParse(payload, NumberStyles.Integer, CultureInfo.InvariantCulture, out var cents) || cents < 0)
                return Result.Failure("save.invalid_section", $"Invalid '{Key}' payload.");

            _wallet = new Wallet(new Money(cents));
            return Result.Success();
        }

        public void Reset() => _wallet = new Wallet(_startingBalance);
    }

    public sealed class InMemoryTransactionRepository : ITransactionRepository
    {
        private readonly List<Transaction> _transactions = new List<Transaction>();

        public void Add(Transaction transaction) => _transactions.Add(transaction);

        public IReadOnlyList<Transaction> GetAll() => _transactions;

        public IReadOnlyList<Transaction> GetRecent(int count) =>
            _transactions.Skip(System.Math.Max(0, _transactions.Count - count)).ToList();
    }
}
