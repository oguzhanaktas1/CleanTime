using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using CleanTime.Economy.Application;
using CleanTime.Economy.Domain;
using CleanTime.Gifts.Application;
using CleanTime.Gifts.Domain;
using CleanTime.Gifts.Infrastructure;
using CleanTime.Relationships.Domain;
using CleanTime.Shared.Kernel;
using CleanTime.Shared.TestUtilities;

namespace CleanTime.Gifts.Tests
{
    internal sealed class FakeWallet : IWalletService
    {
        public Money Balance { get; set; } = new Money(10000);
        public bool CanAfford(Money price) => Balance >= price;
        public Result Credit(Money amount, TransactionReason reason, string referenceId = "") { Balance += amount; return Result.Success(); }
        public Result Debit(Money amount, TransactionReason reason, string referenceId = "") => Pay(amount, reason, referenceId);

        public Result Pay(Money price, TransactionReason reason, string referenceId = "")
        {
            if (!CanAfford(price)) return Result.Failure(EconomyErrors.InsufficientFunds, "no money");
            Balance -= price;
            return Result.Success();
        }
    }

    internal sealed class FakeRelationships : IRelationshipRepository
    {
        private readonly Dictionary<NpcId, Relationship> _items = new Dictionary<NpcId, Relationship>();

        public Result<Relationship> Get(NpcId npcId)
        {
            if (!_items.TryGetValue(npcId, out var r))
                _items[npcId] = r = new Relationship(npcId);
            return Result<Relationship>.Success(r);
        }

        public IReadOnlyList<Relationship> GetAll() => _items.Values.ToList();

        public Result Save(Relationship relationship) { _items[relationship.NpcId] = relationship; return Result.Success(); }
    }

    internal sealed class GiftCatalog : IGiftDefinitionProvider
    {
        public static readonly NpcId Mother = new NpcId("npc.mother");
        public static readonly GiftId Cup = new GiftId("gift.mother.cup.001");
        public static readonly GiftId Locket = new GiftId("gift.mother.locket.001");
        public static readonly GiftId Flowers = new GiftId("gift.any.flowers");

        private readonly List<GiftDefinitionSnapshot> _items = new List<GiftDefinitionSnapshot>
        {
            new GiftDefinitionSnapshot(Cup, "k", new Money(2000), Mother, new GiftEffect(15)),
            new GiftDefinitionSnapshot(Locket, "k", new Money(9000), Mother, new GiftEffect(40, "dialogue.mother.locket"), new GiftRequirement(2)),
            new GiftDefinitionSnapshot(Flowers, "k", new Money(500), default, new GiftEffect(5)),
        };

        public Result<GiftDefinitionSnapshot> Get(GiftId id)
        {
            var found = _items.FirstOrDefault(i => i.Id == id);
            return found != null ? Result<GiftDefinitionSnapshot>.Success(found) : Result<GiftDefinitionSnapshot>.Failure("x", "x");
        }

        public IReadOnlyCollection<GiftDefinitionSnapshot> GetAll() => _items;
    }

    internal sealed class GiftFixture
    {
        public EventBus Bus { get; } = new EventBus();
        public FakeWallet Wallet { get; } = new FakeWallet();
        public FakeRelationships Relationships { get; } = new FakeRelationships();
        public InMemoryGiftOwnershipRepository Ownership { get; } = new InMemoryGiftOwnershipRepository();
        public PurchaseGiftHandler Purchase { get; }
        public GiveGiftHandler Give { get; }

        public GiftFixture()
        {
            Purchase = new PurchaseGiftHandler(new GiftCatalog(), Ownership, Wallet, Bus);
            Give = new GiveGiftHandler(new GiftCatalog(), Ownership, new DefinitionGiftEffectResolver(), Relationships, new FakeGameClock(), Bus);
        }
    }

    public sealed class GiftTests
    {
        [Test]
        public void Purchase_PaysAndAddsToTheOwnedQuantity()
        {
            var f = new GiftFixture();
            using var purchased = f.Bus.Record<GiftPurchasedEvent>();

            Assert.IsTrue(f.Purchase.Handle(new PurchaseGiftCommand(GiftCatalog.Cup)).IsSuccess);
            Assert.IsTrue(f.Purchase.Handle(new PurchaseGiftCommand(GiftCatalog.Cup)).IsSuccess);

            Assert.AreEqual(2, f.Ownership.Get(GiftCatalog.Cup).Quantity);
            Assert.AreEqual(6000, f.Wallet.Balance.Cents);
            Assert.AreEqual(2, purchased.Count);
        }

        [Test]
        public void Purchase_FailsWithoutFunds()
        {
            var f = new GiftFixture();
            f.Wallet.Balance = new Money(1);

            Assert.AreEqual(EconomyErrors.InsufficientFunds, f.Purchase.Handle(new PurchaseGiftCommand(GiftCatalog.Cup)).ErrorCode);
            Assert.AreEqual(0, f.Ownership.Get(GiftCatalog.Cup).Quantity);
        }

        [Test]
        public void Giving_ConsumesTheGift_AndAnnouncesTheGain()
        {
            var f = new GiftFixture();
            f.Purchase.Handle(new PurchaseGiftCommand(GiftCatalog.Cup));
            using var given = f.Bus.Record<GiftGivenEvent>();

            var result = f.Give.Handle(new GiveGiftCommand(GiftCatalog.Mother, GiftCatalog.Cup));

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(0, f.Ownership.Get(GiftCatalog.Cup).Quantity);
            Assert.AreEqual(15, given.Last.RelationshipGain);
            Assert.AreEqual(GiftCatalog.Mother, given.Last.NpcId);
        }

        [Test]
        public void Giving_AGiftThatWasNeverBought_Fails()
        {
            var f = new GiftFixture();

            Assert.AreEqual(GiftErrors.NotOwned, f.Give.Handle(new GiveGiftCommand(GiftCatalog.Mother, GiftCatalog.Cup)).ErrorCode);
        }

        [Test]
        public void GiftsForSomeoneElse_AreRefused()
        {
            var f = new GiftFixture();
            f.Purchase.Handle(new PurchaseGiftCommand(GiftCatalog.Cup));

            var result = f.Give.Handle(new GiveGiftCommand(new NpcId("npc.equipment_vendor"), GiftCatalog.Cup));

            Assert.AreEqual(GiftErrors.WrongRecipient, result.ErrorCode);
            Assert.AreEqual(1, f.Ownership.Get(GiftCatalog.Cup).Quantity);
        }

        [Test]
        public void UniversalGifts_AreAcceptedByAnyone()
        {
            var f = new GiftFixture();
            f.Purchase.Handle(new PurchaseGiftCommand(GiftCatalog.Flowers));

            Assert.IsTrue(f.Give.Handle(new GiveGiftCommand(new NpcId("npc.equipment_vendor"), GiftCatalog.Flowers)).IsSuccess);
        }

        [Test]
        public void RelationshipRequirements_GateTheGift()
        {
            var f = new GiftFixture();
            f.Purchase.Handle(new PurchaseGiftCommand(GiftCatalog.Locket));

            Assert.AreEqual(GiftErrors.RequirementNotMet, f.Give.Handle(new GiveGiftCommand(GiftCatalog.Mother, GiftCatalog.Locket)).ErrorCode);

            var relationship = f.Relationships.Get(GiftCatalog.Mother).Value;
            relationship.SetLevel(2);
            f.Relationships.Save(relationship);

            using var unlocked = f.Bus.Record<DialogueUnlockedEvent>();
            Assert.IsTrue(f.Give.Handle(new GiveGiftCommand(GiftCatalog.Mother, GiftCatalog.Locket)).IsSuccess);
            Assert.AreEqual("dialogue.mother.locket", unlocked.Last.DialogueKey);
        }

        [Test]
        public void AvailableGifts_AreFilteredByRecipientAndLevel()
        {
            var f = new GiftFixture();
            var query = new GetAvailableGiftsHandler(new GiftCatalog(), f.Relationships);

            var atLevelZero = query.Handle(new GetAvailableGiftsQuery(GiftCatalog.Mother)).Select(g => g.Id).ToList();
            CollectionAssert.AreEquivalent(new[] { GiftCatalog.Cup, GiftCatalog.Flowers }, atLevelZero);

            var vendorGifts = query.Handle(new GetAvailableGiftsQuery(new NpcId("npc.equipment_vendor"))).Select(g => g.Id).ToList();
            CollectionAssert.AreEquivalent(new[] { GiftCatalog.Flowers }, vendorGifts);
        }
    }
}
