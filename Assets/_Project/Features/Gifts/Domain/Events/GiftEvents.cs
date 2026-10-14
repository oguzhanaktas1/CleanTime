using CleanTime.Shared.Kernel;

namespace CleanTime.Gifts.Domain
{
    public sealed record GiftPurchasedEvent(GiftId GiftId, Money Price) : IDomainEvent;

    public sealed record GiftGivenEvent(NpcId NpcId, GiftId GiftId, int RelationshipGain, int GameDay) : IDomainEvent;

    /// <summary>Raised when a gift unlocks dialogue (see <see cref="GiftEffect.DialogueUnlockKey"/>).</summary>
    public sealed record DialogueUnlockedEvent(NpcId NpcId, string DialogueKey) : IDomainEvent;
}
