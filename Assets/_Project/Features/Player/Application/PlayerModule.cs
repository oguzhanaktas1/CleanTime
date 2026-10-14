using CleanTime.Player.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Player.Application
{
    public sealed class PlayerModule : FeatureModule
    {
        private readonly PlayerProfile _profile;

        public PlayerModule(PlayerProfile profile = null)
        {
            _profile = profile ?? new PlayerProfile(new PlayerId("player.local"), "Player");
        }

        public override string Name => "Player";

        public override void Register(IServiceRegistry services)
        {
            services.RegisterInstance(_profile);
            services.RegisterSingleton(r => new PlayerState(r.Resolve<PlayerProfile>()));
        }
    }
}
