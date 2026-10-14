using CleanTime.Shared.Kernel;

namespace CleanTime.Bootstrap
{
    /// <summary>The Kernel services every module relies on. Production and tests register them through here.</summary>
    public static class CoreServices
    {
        public static IEventBus Register(
            IServiceRegistry services,
            ILogger logger,
            IUtcClock utcClock,
            IRandomSource random,
            IIdGenerator ids)
        {
            var eventBus = new EventBus(logger);

            services.RegisterInstance(logger);
            services.RegisterInstance<IEventBus>(eventBus);
            services.RegisterInstance(utcClock);
            services.RegisterInstance(random);
            services.RegisterInstance(ids);

            return eventBus;
        }
    }
}
