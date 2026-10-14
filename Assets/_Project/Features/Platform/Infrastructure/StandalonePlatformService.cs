using UnityEngine;
using CleanTime.Platform.Application;
using CleanTime.Shared.Kernel;

namespace CleanTime.Platform.Infrastructure
{
    public sealed class StandalonePlatformService : IPlatformService
    {
        public string PlatformName => UnityEngine.Application.platform.ToString();

        public string DeviceId => SystemInfo.deviceUniqueIdentifier;
    }

    public sealed class PlatformInfrastructureModule : FeatureModule
    {
        public override string Name => "Platform.Infrastructure";

        public override void Register(IServiceRegistry services)
        {
            services.RegisterSingleton<IPlatformService>(_ => new StandalonePlatformService());
        }
    }
}
