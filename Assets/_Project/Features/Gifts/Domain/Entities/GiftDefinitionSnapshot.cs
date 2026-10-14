using CleanTime.Shared.Kernel;

namespace CleanTime.Gifts.Domain
{
    public static class GiftErrors
    {
        public const string UnknownGift = "gifts.unknown_gift";
        public const string NotOwned = "gifts.not_owned";
        public const string WrongRecipient = "gifts.wrong_recipient";
        public const string RequirementNotMet = "gifts.requirement_not_met";
        public const string InvalidRecipient = "gifts.invalid_recipient";
    }

    /// <summary>Condition for a gift to be offered, e.g. a minimum relationship level.</summary>
    public sealed class GiftRequirement
    {
        public GiftRequirement(int minRelationshipLevel)
        {
            MinRelationshipLevel = Guard.NotNegative(minRelationshipLevel, nameof(minRelationshipLevel));
        }

        public int MinRelationshipLevel { get; }

        public static GiftRequirement None => new GiftRequirement(0);
    }

    public sealed class GiftEffect
    {
        public GiftEffect(int relationshipGain, string dialogueUnlockKey = "")
        {
            RelationshipGain = Guard.NotNegative(relationshipGain, nameof(relationshipGain));
            DialogueUnlockKey = dialogueUnlockKey ?? string.Empty;
        }

        public int RelationshipGain { get; }

        public string DialogueUnlockKey { get; }
    }

    public sealed class GiftDefinitionSnapshot
    {
        public GiftDefinitionSnapshot(
            GiftId id,
            string displayNameKey,
            Money price,
            NpcId recipientNpcId,
            GiftEffect effect,
            GiftRequirement requirement = null)
        {
            Id = id;
            DisplayNameKey = displayNameKey ?? string.Empty;
            Price = price;
            RecipientNpcId = recipientNpcId;
            Effect = Guard.NotNull(effect, nameof(effect));
            Requirement = requirement ?? GiftRequirement.None;
        }

        public GiftId Id { get; }

        public string DisplayNameKey { get; }

        public Money Price { get; }

        /// <summary>The NPC this gift is meant for; empty means any NPC accepts it.</summary>
        public NpcId RecipientNpcId { get; }

        public GiftEffect Effect { get; }

        public GiftRequirement Requirement { get; }

        public bool AcceptedBy(NpcId npcId) => RecipientNpcId.IsEmpty || RecipientNpcId == npcId;
    }

    public sealed class GiftOwnership
    {
        public GiftOwnership(GiftId giftId, int quantity = 0)
        {
            GiftId = giftId;
            Quantity = Guard.NotNegative(quantity, nameof(quantity));
        }

        public GiftId GiftId { get; }

        public int Quantity { get; private set; }

        public void Add() => Quantity++;

        public Result Consume()
        {
            if (Quantity <= 0)
                return Result.Failure(GiftErrors.NotOwned, $"Gift '{GiftId}' is not owned.");

            Quantity--;
            return Result.Success();
        }
    }
}
