using System.Collections.Generic;
using System.Linq;
using CleanTime.Economy.Application;
using CleanTime.Economy.Domain;
using CleanTime.Gifts.Domain;
using CleanTime.Relationships.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Gifts.Application
{
    internal sealed class PurchaseGiftHandler : ICommandHandler<PurchaseGiftCommand>
    {
        private readonly IGiftDefinitionProvider _definitions;
        private readonly IGiftOwnershipRepository _ownership;
        private readonly IWalletService _wallet;
        private readonly IEventBus _events;

        public PurchaseGiftHandler(
            IGiftDefinitionProvider definitions,
            IGiftOwnershipRepository ownership,
            IWalletService wallet,
            IEventBus events)
        {
            _definitions = definitions;
            _ownership = ownership;
            _wallet = wallet;
            _events = events;
        }

        public Result Handle(PurchaseGiftCommand command)
        {
            var gift = _definitions.Get(command.GiftId);
            if (gift.IsFailure)
                return Result.Failure(GiftErrors.UnknownGift, $"Unknown gift '{command.GiftId}'.");

            var paid = _wallet.Pay(gift.Value.Price, TransactionReason.GiftPurchase, command.GiftId.Value);
            if (paid.IsFailure)
                return paid;

            var ownership = _ownership.Get(command.GiftId);
            ownership.Add();
            _ownership.Save(ownership);

            _events.Publish(new GiftPurchasedEvent(command.GiftId, gift.Value.Price));
            return Result.Success();
        }
    }

    internal sealed class GiveGiftHandler : ICommandHandler<GiveGiftCommand>
    {
        private readonly IGiftDefinitionProvider _definitions;
        private readonly IGiftOwnershipRepository _ownership;
        private readonly IGiftEffectResolver _effects;
        private readonly IRelationshipRepository _relationships;
        private readonly IGameClock _clock;
        private readonly IEventBus _events;

        public GiveGiftHandler(
            IGiftDefinitionProvider definitions,
            IGiftOwnershipRepository ownership,
            IGiftEffectResolver effects,
            IRelationshipRepository relationships,
            IGameClock clock,
            IEventBus events)
        {
            _definitions = definitions;
            _ownership = ownership;
            _effects = effects;
            _relationships = relationships;
            _clock = clock;
            _events = events;
        }

        public Result Handle(GiveGiftCommand command)
        {
            if (command.NpcId.IsEmpty)
                return Result.Failure(GiftErrors.InvalidRecipient, "A recipient is required.");

            var gift = _definitions.Get(command.GiftId);
            if (gift.IsFailure)
                return Result.Failure(GiftErrors.UnknownGift, $"Unknown gift '{command.GiftId}'.");

            if (!gift.Value.AcceptedBy(command.NpcId))
                return Result.Failure(GiftErrors.WrongRecipient, "This NPC does not accept that gift.");

            var relationship = _relationships.Get(command.NpcId);
            if (relationship.IsFailure || relationship.Value.Level < gift.Value.Requirement.MinRelationshipLevel)
                return Result.Failure(GiftErrors.RequirementNotMet, "The relationship is not strong enough yet.");

            var ownership = _ownership.Get(command.GiftId);
            var consumed = ownership.Consume();
            if (consumed.IsFailure)
                return consumed;

            _ownership.Save(ownership);

            var effect = _effects.Resolve(gift.Value, command.NpcId);
            _events.Publish(new GiftGivenEvent(command.NpcId, command.GiftId, effect.RelationshipGain, _clock.Now.Day));

            if (effect.DialogueUnlockKey.Length > 0)
                _events.Publish(new DialogueUnlockedEvent(command.NpcId, effect.DialogueUnlockKey));

            return Result.Success();
        }
    }

    internal sealed class GetAvailableGiftsHandler : IQueryHandler<GetAvailableGiftsQuery, IReadOnlyList<GiftDefinitionSnapshot>>
    {
        private readonly IGiftDefinitionProvider _definitions;
        private readonly IRelationshipRepository _relationships;

        public GetAvailableGiftsHandler(IGiftDefinitionProvider definitions, IRelationshipRepository relationships)
        {
            _definitions = definitions;
            _relationships = relationships;
        }

        public IReadOnlyList<GiftDefinitionSnapshot> Handle(GetAvailableGiftsQuery query)
        {
            var relationship = _relationships.Get(query.NpcId);
            var level = relationship.IsSuccess ? relationship.Value.Level : 0;

            return _definitions.GetAll()
                .Where(g => g.AcceptedBy(query.NpcId) && level >= g.Requirement.MinRelationshipLevel)
                .OrderBy(g => g.Price.Cents)
                .ToList();
        }
    }
}
