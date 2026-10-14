namespace CleanTime.Shared.Kernel
{
    /// <summary>
    /// A module's composition entry point. Each feature assembly exposes one public module per layer
    /// so internal handlers never leak across assembly boundaries.
    /// </summary>
    public interface IFeatureModule
    {
        string Name { get; }

        /// <summary>Phase 1: register services. Must not resolve anything.</summary>
        void Register(IServiceRegistry services);

        /// <summary>Phase 2: resolve and start long-lived subscribers (projections, coordinators).</summary>
        void Initialize(IServiceResolver services);
    }

    public abstract class FeatureModule : IFeatureModule
    {
        public abstract string Name { get; }

        public abstract void Register(IServiceRegistry services);

        public virtual void Initialize(IServiceResolver services)
        {
        }
    }
}
