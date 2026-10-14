using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using CleanTime.Economy.Application;
using CleanTime.Economy.Domain;
using CleanTime.Equipment.Application;
using CleanTime.Equipment.Domain;
using CleanTime.Equipment.Infrastructure;
using CleanTime.Shared.Kernel;
using CleanTime.Shared.TestUtilities;

namespace CleanTime.Equipment.Tests
{
    internal sealed class FakeWallet : IWalletService
    {
        public Money Balance { get; set; }
        public List<(Money Amount, TransactionReason Reason)> Payments { get; } = new List<(Money, TransactionReason)>();
        public bool CanAfford(Money price) => Balance >= price;
        public Result Credit(Money amount, TransactionReason reason, string referenceId = "") { Balance += amount; return Result.Success(); }
        public Result Debit(Money amount, TransactionReason reason, string referenceId = "") => Pay(amount, reason, referenceId);

        public Result Pay(Money price, TransactionReason reason, string referenceId = "")
        {
            if (!CanAfford(price)) return Result.Failure(EconomyErrors.InsufficientFunds, "no money");
            Balance -= price;
            Payments.Add((price, reason));
            return Result.Success();
        }
    }

    internal sealed class Definitions : IEquipmentDefinitionProvider
    {
        private readonly List<EquipmentDefinitionSnapshot> _items = new List<EquipmentDefinitionSnapshot>
        {
            new EquipmentDefinitionSnapshot(new EquipmentId("equipment.broom.basic"), "k", Money.Zero, 5f, new[] { "dirt.dust" }, isStarter: true, maxUpgradeLevel: 2, powerPerUpgradeLevel: 1f),
            new EquipmentDefinitionSnapshot(new EquipmentId("equipment.mop.basic"), "k", Money.Zero, 5f, new[] { "dirt.stain" }, isStarter: true),
            new EquipmentDefinitionSnapshot(new EquipmentId("equipment.vacuum.tier1"), "k", new Money(5000), 8f, new[] { "dirt.dust", "dirt.trash" }),
        };

        public Result<EquipmentDefinitionSnapshot> Get(EquipmentId id)
        {
            var found = _items.FirstOrDefault(i => i.Id == id);
            return found != null ? Result<EquipmentDefinitionSnapshot>.Success(found) : Result<EquipmentDefinitionSnapshot>.Failure("x", "x");
        }

        public IReadOnlyCollection<EquipmentDefinitionSnapshot> GetAll() => _items;
    }

    internal sealed class EquipmentFixture
    {
        public EventBus Bus { get; } = new EventBus();
        public FakeWallet Wallet { get; } = new FakeWallet { Balance = new Money(10000) };
        public InMemoryEquipmentRepository Repository { get; }
        public EquipmentId Broom { get; } = new EquipmentId("equipment.broom.basic");
        public EquipmentId Mop { get; } = new EquipmentId("equipment.mop.basic");
        public EquipmentId Vacuum { get; } = new EquipmentId("equipment.vacuum.tier1");

        public EquipmentFixture()
        {
            Repository = new InMemoryEquipmentRepository(new Definitions(), new InMemorySnapshotStore());
        }

        public PurchaseEquipmentHandler Purchase => new PurchaseEquipmentHandler(Repository, new DefaultEquipmentPricePolicy(), Wallet, Bus);
        public UpgradeEquipmentHandler Upgrade => new UpgradeEquipmentHandler(Repository, new LinearEquipmentUpgradePolicy(), Wallet, Bus);
        public SwitchEquipmentHandler Switch => new SwitchEquipmentHandler(Repository, Bus);
        public EquipItemHandler Equip => new EquipItemHandler(Repository, Bus);
    }

    public sealed class EquipmentLoadoutTests
    {
        [Test]
        public void FirstItem_BecomesSelected()
        {
            var loadout = new EquipmentLoadout();
            loadout.Add(new EquipmentId("a"));
            loadout.Add(new EquipmentId("b"));

            Assert.AreEqual(new EquipmentId("a"), loadout.Selected);
        }

        [Test]
        public void Switch_WrapsInBothDirections()
        {
            var loadout = new EquipmentLoadout();
            loadout.Add(new EquipmentId("a"));
            loadout.Add(new EquipmentId("b"));

            loadout.Switch(SwitchDirection.Previous);
            Assert.AreEqual(new EquipmentId("b"), loadout.Selected);

            loadout.Switch(SwitchDirection.Next);
            Assert.AreEqual(new EquipmentId("a"), loadout.Selected);
        }

        [Test]
        public void EmptyLoadout_CannotSwitch()
        {
            Assert.AreEqual(EquipmentErrors.LoadoutEmpty, new EquipmentLoadout().Switch(SwitchDirection.Next).ErrorCode);
        }
    }

    public sealed class EquipmentHandlerTests
    {
        [Test]
        public void StarterEquipment_IsOwnedAndSelectable()
        {
            var f = new EquipmentFixture();

            Assert.AreEqual(2, f.Repository.GetAllOwned().Count);
            Assert.AreEqual(f.Broom, f.Repository.GetLoadout().Selected);
        }

        [Test]
        public void Switch_ChangesSelection_AndAnnouncesIt()
        {
            var f = new EquipmentFixture();
            using var changed = f.Bus.Record<EquipmentChangedEvent>();

            f.Switch.Handle(new SwitchEquipmentCommand(SwitchDirection.Next));

            Assert.AreEqual(f.Mop, f.Repository.GetLoadout().Selected);
            Assert.AreEqual(f.Broom, changed.Last.Previous);
            Assert.AreEqual(f.Mop, changed.Last.Current);
        }

        [Test]
        public void Equip_RequiresOwnership()
        {
            var f = new EquipmentFixture();

            Assert.AreEqual(EquipmentErrors.NotOwned, f.Equip.Handle(new EquipItemCommand(f.Vacuum)).ErrorCode);
            Assert.IsTrue(f.Equip.Handle(new EquipItemCommand(f.Mop)).IsSuccess);
        }

        [Test]
        public void Purchase_PaysAndAddsToLoadout()
        {
            var f = new EquipmentFixture();
            using var purchased = f.Bus.Record<EquipmentPurchasedEvent>();

            var result = f.Purchase.Handle(new PurchaseEquipmentCommand(f.Vacuum));

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(5000, f.Wallet.Balance.Cents);
            Assert.IsTrue(f.Repository.GetOwned(f.Vacuum).IsSuccess);
            Assert.Contains(f.Vacuum, f.Repository.GetLoadout().Items.ToList());
            Assert.AreEqual(1, purchased.Count);
        }

        [Test]
        public void Purchase_FailsWithoutFunds_AndLeavesStateUntouched()
        {
            var f = new EquipmentFixture();
            f.Wallet.Balance = new Money(100);

            var result = f.Purchase.Handle(new PurchaseEquipmentCommand(f.Vacuum));

            Assert.AreEqual(EconomyErrors.InsufficientFunds, result.ErrorCode);
            Assert.IsTrue(f.Repository.GetOwned(f.Vacuum).IsFailure);
        }

        [Test]
        public void Purchase_OfOwnedEquipment_IsRejected()
        {
            var f = new EquipmentFixture();

            Assert.AreEqual(EquipmentErrors.AlreadyOwned, f.Purchase.Handle(new PurchaseEquipmentCommand(f.Broom)).ErrorCode);
            Assert.AreEqual(0, f.Wallet.Payments.Count);
        }

        [Test]
        public void Upgrade_IncreasesPower_UntilTheMaximum()
        {
            var f = new EquipmentFixture();

            Assert.IsTrue(f.Upgrade.Handle(new UpgradeEquipmentCommand(f.Broom)).IsSuccess);
            Assert.AreEqual(6f, f.Repository.GetOwned(f.Broom).Value.EffectiveCleaningPower, 0.001f);

            f.Upgrade.Handle(new UpgradeEquipmentCommand(f.Broom));
            Assert.AreEqual(EquipmentErrors.MaxUpgradeReached, f.Upgrade.Handle(new UpgradeEquipmentCommand(f.Broom)).ErrorCode);
        }

        [Test]
        public void SaveLoadRoundTrip_PreservesOwnershipUpgradesAndSelection()
        {
            var f = new EquipmentFixture();
            f.Purchase.Handle(new PurchaseEquipmentCommand(f.Vacuum));
            f.Upgrade.Handle(new UpgradeEquipmentCommand(f.Broom));
            f.Equip.Handle(new EquipItemCommand(f.Vacuum));
            var slot = SaveId.New();
            f.Repository.Flush(slot);

            f.Repository.Reset();
            Assert.IsTrue(f.Repository.GetOwned(f.Vacuum).IsFailure);

            Assert.IsTrue(f.Repository.Load(slot).IsSuccess);
            Assert.IsTrue(f.Repository.GetOwned(f.Vacuum).IsSuccess);
            Assert.AreEqual(1, f.Repository.GetOwned(f.Broom).Value.UpgradeLevel);
            Assert.AreEqual(f.Vacuum, f.Repository.GetLoadout().Selected);
        }

        [Test]
        public void CompatibilityIsDataDriven()
        {
            var f = new EquipmentFixture();
            var broom = f.Repository.GetOwned(f.Broom).Value.Definition;

            Assert.IsTrue(broom.SupportedTargetIds.Contains("dirt.dust"));
            Assert.IsFalse(broom.SupportedTargetIds.Contains("dirt.stain"));
        }
    }
}
