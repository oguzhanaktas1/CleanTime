using System.Collections.Generic;
using CleanTime.Economy.Application;
using CleanTime.Economy.Domain;
using CleanTime.Equipment.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Equipment.Application
{
    internal sealed class EquipItemHandler : ICommandHandler<EquipItemCommand>
    {
        private readonly IEquipmentRepository _equipment;
        private readonly IEventBus _events;

        public EquipItemHandler(IEquipmentRepository equipment, IEventBus events)
        {
            _equipment = equipment;
            _events = events;
        }

        public Result Handle(EquipItemCommand command)
        {
            var owned = _equipment.GetOwned(command.EquipmentId);
            if (owned.IsFailure)
                return owned;

            var loadout = _equipment.GetLoadout();
            var previous = loadout.Selected;
            var selected = loadout.Select(command.EquipmentId);
            if (selected.IsFailure)
                return selected;

            _equipment.SaveLoadout(loadout);
            _events.Publish(new EquipmentEquippedEvent(command.EquipmentId));

            if (previous != command.EquipmentId)
                _events.Publish(new EquipmentChangedEvent(previous, command.EquipmentId));

            return Result.Success();
        }
    }

    internal sealed class SwitchEquipmentHandler : ICommandHandler<SwitchEquipmentCommand>
    {
        private readonly IEquipmentRepository _equipment;
        private readonly IEventBus _events;

        public SwitchEquipmentHandler(IEquipmentRepository equipment, IEventBus events)
        {
            _equipment = equipment;
            _events = events;
        }

        public Result Handle(SwitchEquipmentCommand command)
        {
            var loadout = _equipment.GetLoadout();
            var previous = loadout.Selected;
            var switched = loadout.Switch(command.Direction);
            if (switched.IsFailure)
                return switched;

            _equipment.SaveLoadout(loadout);

            if (previous != loadout.Selected)
                _events.Publish(new EquipmentChangedEvent(previous, loadout.Selected));

            return Result.Success();
        }
    }

    internal sealed class PurchaseEquipmentHandler : ICommandHandler<PurchaseEquipmentCommand>
    {
        private readonly IEquipmentRepository _equipment;
        private readonly IEquipmentPricePolicy _pricing;
        private readonly IWalletService _wallet;
        private readonly IEventBus _events;

        public PurchaseEquipmentHandler(
            IEquipmentRepository equipment,
            IEquipmentPricePolicy pricing,
            IWalletService wallet,
            IEventBus events)
        {
            _equipment = equipment;
            _pricing = pricing;
            _wallet = wallet;
            _events = events;
        }

        public Result Handle(PurchaseEquipmentCommand command)
        {
            var found = _equipment.Get(command.EquipmentId);
            if (found.IsFailure)
                return found;

            var item = found.Value;
            if (item.IsOwned)
                return Result.Failure(EquipmentErrors.AlreadyOwned, $"'{item.Id}' is already owned.");

            var price = _pricing.GetPurchasePrice(item.Definition);
            var paid = _wallet.Pay(price, TransactionReason.EquipmentPurchase, item.Id.Value);
            if (paid.IsFailure)
                return paid;

            item.MarkOwned();
            _equipment.Save(item);

            var loadout = _equipment.GetLoadout();
            loadout.Add(item.Id);
            _equipment.SaveLoadout(loadout);

            _events.Publish(new EquipmentPurchasedEvent(item.Id, price));
            _events.Publish(new EquipmentUnlockedEvent(item.Id));
            return Result.Success();
        }
    }

    internal sealed class UpgradeEquipmentHandler : ICommandHandler<UpgradeEquipmentCommand>
    {
        private readonly IEquipmentRepository _equipment;
        private readonly IEquipmentUpgradePolicy _upgrades;
        private readonly IWalletService _wallet;
        private readonly IEventBus _events;

        public UpgradeEquipmentHandler(
            IEquipmentRepository equipment,
            IEquipmentUpgradePolicy upgrades,
            IWalletService wallet,
            IEventBus events)
        {
            _equipment = equipment;
            _upgrades = upgrades;
            _wallet = wallet;
            _events = events;
        }

        public Result Handle(UpgradeEquipmentCommand command)
        {
            var owned = _equipment.GetOwned(command.EquipmentId);
            if (owned.IsFailure)
                return owned;

            var item = owned.Value;
            if (!_upgrades.CanUpgrade(item))
                return Result.Failure(EquipmentErrors.MaxUpgradeReached, $"'{item.Id}' cannot be upgraded.");

            var paid = _wallet.Pay(_upgrades.GetUpgradeCost(item), TransactionReason.EquipmentUpgrade, item.Id.Value);
            if (paid.IsFailure)
                return paid;

            item.Upgrade();
            _equipment.Save(item);
            _events.Publish(new EquipmentUpgradedEvent(item.Id, item.UpgradeLevel));
            return Result.Success();
        }
    }

    internal sealed class SelectedEquipmentQuery : ISelectedEquipmentQuery
    {
        private readonly IEquipmentRepository _equipment;

        public SelectedEquipmentQuery(IEquipmentRepository equipment)
        {
            _equipment = equipment;
        }

        public EquipmentId GetSelected() => _equipment.GetLoadout().Selected;
    }

    internal sealed class GetCurrentEquipmentHandler : IQueryHandler<GetCurrentEquipmentQuery, Result<EquipmentProgress>>
    {
        private readonly IEquipmentRepository _equipment;

        public GetCurrentEquipmentHandler(IEquipmentRepository equipment)
        {
            _equipment = equipment;
        }

        public Result<EquipmentProgress> Handle(GetCurrentEquipmentQuery query)
        {
            var selected = _equipment.GetLoadout().Selected;

            return selected.IsEmpty
                ? Result<EquipmentProgress>.Failure(EquipmentErrors.LoadoutEmpty, "Nothing is equipped.")
                : _equipment.GetOwned(selected);
        }
    }

    internal sealed class GetOwnedEquipmentHandler : IQueryHandler<GetOwnedEquipmentQuery, IReadOnlyList<EquipmentProgress>>
    {
        private readonly IEquipmentRepository _equipment;

        public GetOwnedEquipmentHandler(IEquipmentRepository equipment)
        {
            _equipment = equipment;
        }

        public IReadOnlyList<EquipmentProgress> Handle(GetOwnedEquipmentQuery query) => _equipment.GetAllOwned();
    }
}
