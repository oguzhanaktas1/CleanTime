using System.Collections.Generic;
using CleanTime.Shared.Kernel;

namespace CleanTime.Gifts.Domain
{
    public interface IGiftDefinitionProvider : IDefinitionProvider<GiftId, GiftDefinitionSnapshot>
    {
    }

    public interface IGiftOwnershipRepository
    {
        /// <summary>Ownership record for the gift (quantity 0 when never bought).</summary>
        GiftOwnership Get(GiftId giftId);

        IReadOnlyList<GiftOwnership> GetAll();

        Result Save(GiftOwnership ownership);
    }

    /// <summary>Resolves the effect a gift has right now (hook for upgrades, preferences, events).</summary>
    public interface IGiftEffectResolver
    {
        GiftEffect Resolve(GiftDefinitionSnapshot gift, NpcId recipient);
    }

    public sealed class DefinitionGiftEffectResolver : IGiftEffectResolver
    {
        public GiftEffect Resolve(GiftDefinitionSnapshot gift, NpcId recipient) => gift.Effect;
    }
}
