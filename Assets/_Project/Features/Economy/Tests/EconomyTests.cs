using NUnit.Framework;
using CleanTime.Economy.Application;
using CleanTime.Economy.Domain;
using CleanTime.Economy.Infrastructure;
using CleanTime.Shared.Kernel;
using CleanTime.Shared.TestUtilities;

namespace CleanTime.Economy.Tests
{
    internal sealed class EconomyFixture
    {
        public EventBus Bus { get; } = new EventBus();
        public InMemorySnapshotStore Snapshots { get; } = new InMemorySnapshotStore();
        public InMemoryWalletRepository Wallets { get; }
        public InMemoryTransactionRepository Ledger { get; } = new InMemoryTransactionRepository();
        public FakeGameClock Clock { get; } = new FakeGameClock();
        public IWalletService Service { get; }

        public EconomyFixture(long startingCents = 0)
        {
            Wallets = new InMemoryWalletRepository(new Money(startingCents), Snapshots);
            Service = new WalletService(
                Wallets, Ledger, new AffordabilityPurchasePolicy(), Bus, Clock, new FakeUtcClock(), new SequentialIdGenerator());
        }
    }

    public sealed class WalletTests
    {
        [Test]
        public void Credit_ShouldIncreaseBalance()
        {
            var wallet = new Wallet(new Money(100));

            Assert.IsTrue(wallet.Credit(new Money(50)).IsSuccess);
            Assert.AreEqual(150, wallet.Balance.Cents);
        }

        [Test]
        public void Debit_ShouldDecreaseBalance()
        {
            var wallet = new Wallet(new Money(100));

            Assert.IsTrue(wallet.Debit(new Money(30)).IsSuccess);
            Assert.AreEqual(70, wallet.Balance.Cents);
        }

        [Test]
        public void Debit_ShouldFail_WhenInsufficientFunds()
        {
            var wallet = new Wallet(new Money(10));

            var result = wallet.Debit(new Money(11));

            Assert.AreEqual(EconomyErrors.InsufficientFunds, result.ErrorCode);
            Assert.AreEqual(10, wallet.Balance.Cents);
        }

        [Test]
        public void NonPositiveAmounts_AreRejected()
        {
            var wallet = new Wallet(new Money(10));

            Assert.AreEqual(EconomyErrors.InvalidCredit, wallet.Credit(Money.Zero).ErrorCode);
            Assert.AreEqual(EconomyErrors.InvalidDebit, wallet.Debit(new Money(-5)).ErrorCode);
        }

        [Test]
        public void NegativeStartingBalance_IsRejected()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new Wallet(new Money(-1)));
        }
    }

    public sealed class WalletServiceTests
    {
        [Test]
        public void Credit_WritesLedgerEntryAndAnnouncesIt()
        {
            var f = new EconomyFixture(1000);
            using var credited = f.Bus.Record<MoneyCreditedEvent>();

            f.Service.Credit(new Money(500), TransactionReason.JobReward, "job-1");

            Assert.AreEqual(1500, f.Service.Balance.Cents);
            Assert.AreEqual(1, f.Ledger.GetAll().Count);
            Assert.AreEqual(500, f.Ledger.GetAll()[0].Amount.Cents);
            Assert.AreEqual(1500, f.Ledger.GetAll()[0].BalanceAfter.Cents);
            Assert.AreEqual("job-1", credited.Last.ReferenceId);
        }

        [Test]
        public void Purchase_ShouldCreateTransaction()
        {
            var f = new EconomyFixture(1000);
            using var completed = f.Bus.Record<PurchaseCompletedEvent>();

            var result = f.Service.Pay(new Money(400), TransactionReason.EquipmentPurchase, "equipment.vacuum");

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(600, f.Service.Balance.Cents);
            Assert.AreEqual(-400, f.Ledger.GetAll()[0].Amount.Cents);
            Assert.AreEqual(TransactionReason.EquipmentPurchase, f.Ledger.GetAll()[0].Reason);
            Assert.AreEqual(1, completed.Count);
        }

        [Test]
        public void Purchase_ShouldNotMutateWallet_WhenRejected()
        {
            var f = new EconomyFixture(100);
            using var rejected = f.Bus.Record<PurchaseRejectedEvent>();

            var result = f.Service.Pay(new Money(400), TransactionReason.EquipmentPurchase, "equipment.vacuum");

            Assert.AreEqual(EconomyErrors.InsufficientFunds, result.ErrorCode);
            Assert.AreEqual(100, f.Service.Balance.Cents);
            Assert.AreEqual(0, f.Ledger.GetAll().Count);
            Assert.AreEqual(1, rejected.Count);
        }

        [Test]
        public void LedgerRecordsTheCurrentGameDay()
        {
            var f = new EconomyFixture(0);
            f.Clock.Now = new GameDate(7, DayPhase.Evening);

            f.Service.Credit(new Money(10), TransactionReason.DebugGrant);

            Assert.AreEqual(7, f.Ledger.GetAll()[0].GameDay);
        }
    }

    public sealed class WalletPersistenceTests
    {
        [Test]
        public void SaveLoadRoundTrip_ShouldPreserveWallet()
        {
            var f = new EconomyFixture(1000);
            f.Service.Credit(new Money(234), TransactionReason.DebugGrant);
            var slot = SaveId.New();
            f.Wallets.Flush(slot);

            f.Wallets.Reset();
            Assert.AreEqual(1000, f.Service.Balance.Cents);

            Assert.IsTrue(f.Wallets.Load(slot).IsSuccess);
            Assert.AreEqual(1234, f.Service.Balance.Cents);
        }

        [Test]
        public void MissingSection_IsReportedAsMissing_NotCorrupt()
        {
            var f = new EconomyFixture();

            Assert.AreEqual("save.missing_section", f.Wallets.Load(SaveId.New()).ErrorCode);
        }

        [Test]
        public void NegativeStoredBalance_IsRejected()
        {
            var f = new EconomyFixture();
            var slot = SaveId.New();
            f.Snapshots.Put(slot, f.Wallets.Key, "-5");

            Assert.AreEqual("save.invalid_section", f.Wallets.Load(slot).ErrorCode);
        }
    }
}
