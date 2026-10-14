using System.Collections.Generic;
using CleanTime.Gifts.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Gifts.Application
{
    public sealed record PurchaseGiftCommand(GiftId GiftId) : ICommand;

    public sealed record GiveGiftCommand(NpcId NpcId, GiftId GiftId) : ICommand;

    /// <summary>Gifts the player could buy for the NPC right now (requirements met).</summary>
    public sealed record GetAvailableGiftsQuery(NpcId NpcId) : IQuery<IReadOnlyList<GiftDefinitionSnapshot>>;
}
