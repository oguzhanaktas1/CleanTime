using System;

namespace CleanTime.Shared.Kernel
{
    /// <summary>Marker for facts published by a module after state changed.</summary>
    public interface IDomainEvent
    {
    }

    public interface IEventBus
    {
        void Publish<T>(T evt) where T : IDomainEvent;

        IDisposable Subscribe<T>(Action<T> handler) where T : IDomainEvent;
    }
}
