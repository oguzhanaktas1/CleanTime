using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using CleanTime.Inventory.Application;
using CleanTime.Inventory.Domain;
using CleanTime.Inventory.Infrastructure;
using CleanTime.Shared.Kernel;
using CleanTime.Shared.TestUtilities;

namespace CleanTime.Inventory.Tests
{
    internal sealed class Items : IItemDefinitionProvider
    {
        public static readonly ItemId Soap = new ItemId("item.soap");

        private readonly ItemDefinitionSnapshot _soap = new ItemDefinitionSnapshot(Soap, "k", maxStack: 3);

        public Result<ItemDefinitionSnapshot> Get(ItemId id) => id == Soap
            ? Result<ItemDefinitionSnapshot>.Success(_soap)
            : Result<ItemDefinitionSnapshot>.Failure("x", "x");

        public IReadOnlyCollection<ItemDefinitionSnapshot> GetAll() => new[] { _soap };
    }

    public sealed class InventoryTests
    {
        private static (AddItemHandler Add, RemoveItemHandler Remove, InMemoryInventoryRepository Repo, EventBus Bus) Create()
        {
            var bus = new EventBus();
            var repository = new InMemoryInventoryRepository();

            return (
                new AddItemHandler(new Items(), repository, new FakeGameClock(), bus),
                new RemoveItemHandler(repository, bus),
                repository,
                bus);
        }

        [Test]
        public void Add_StacksQuantities()
        {
            var (add, _, repository, _) = Create();

            add.Handle(new AddItemCommand(Items.Soap, 2));
            add.Handle(new AddItemCommand(Items.Soap));

            Assert.AreEqual(3, repository.Get().GetQuantity(Items.Soap));
        }

        [Test]
        public void Add_RespectsTheStackLimit()
        {
            var (add, _, repository, _) = Create();
            add.Handle(new AddItemCommand(Items.Soap, 3));

            Assert.AreEqual(InventoryErrors.StackFull, add.Handle(new AddItemCommand(Items.Soap)).ErrorCode);
            Assert.AreEqual(3, repository.Get().GetQuantity(Items.Soap));
        }

        [Test]
        public void Add_RejectsUnknownItemsAndBadQuantities()
        {
            var (add, _, _, _) = Create();

            Assert.AreEqual(InventoryErrors.UnknownItem, add.Handle(new AddItemCommand(new ItemId("nope"))).ErrorCode);
            Assert.AreEqual(InventoryErrors.InvalidQuantity, add.Handle(new AddItemCommand(Items.Soap, 0)).ErrorCode);
        }

        [Test]
        public void Remove_ReducesAndDropsEmptyEntries()
        {
            var (add, remove, repository, bus) = Create();
            add.Handle(new AddItemCommand(Items.Soap, 2));
            using var removed = bus.Record<ItemRemovedEvent>();

            remove.Handle(new RemoveItemCommand(Items.Soap, 2));

            Assert.IsFalse(repository.Get().Has(Items.Soap));
            Assert.AreEqual(0, repository.Get().Items.Count);
            Assert.AreEqual(0, removed.Last.NewTotal);
        }

        [Test]
        public void Remove_CannotTakeMoreThanOwned()
        {
            var (add, remove, repository, _) = Create();
            add.Handle(new AddItemCommand(Items.Soap, 1));

            Assert.AreEqual(InventoryErrors.NotEnough, remove.Handle(new RemoveItemCommand(Items.Soap, 2)).ErrorCode);
            Assert.AreEqual(1, repository.Get().GetQuantity(Items.Soap));
        }

        [Test]
        public void Query_ListsItemsInStableOrder()
        {
            var (add, _, repository, _) = Create();
            add.Handle(new AddItemCommand(Items.Soap));

            var items = new GetInventoryHandler(repository).Handle(new GetInventoryQuery());

            Assert.AreEqual(1, items.Count);
            Assert.AreEqual(Items.Soap, items.Single().ItemId);
        }
    }
}
