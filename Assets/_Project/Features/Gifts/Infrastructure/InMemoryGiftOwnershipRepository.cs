using System.Collections.Generic;
using System.Linq;
using CleanTime.Gifts.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Gifts.Infrastructure
{
    public sealed class InMemoryGiftOwnershipRepository : IGiftOwnershipRepository
    {
        private readonly Dictionary<GiftId, GiftOwnership> _ownership = new Dictionary<GiftId, GiftOwnership>();

        public GiftOwnership Get(GiftId giftId)
        {
            if (!_ownership.TryGetValue(giftId, out var ownership))
            {
                ownership = new GiftOwnership(giftId);
                _ownership[giftId] = ownership;
            }

            return ownership;
        }

        public IReadOnlyList<GiftOwnership> GetAll() => _ownership.Values.ToList();

        public Result Save(GiftOwnership ownership)
        {
            _ownership[ownership.GiftId] = ownership;
            return Result.Success();
        }
    }

    public sealed class GiftsInfrastructureModule : FeatureModule
    {
        public override string Name => "Gifts.Infrastructure";

        public override void Register(IServiceRegistry services)
        {
            services.RegisterSingleton<IGiftOwnershipRepository>(_ => new InMemoryGiftOwnershipRepository());
        }
    }
}
