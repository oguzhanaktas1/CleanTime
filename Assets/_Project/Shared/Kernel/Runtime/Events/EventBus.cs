using System;
using System.Collections.Generic;

namespace CleanTime.Shared.Kernel
{
    /// <summary>
    /// Synchronous, main-thread, in-process event bus. Handlers run in subscription order.
    /// A throwing handler is logged and does not stop other handlers or the publisher.
    /// </summary>
    public sealed class EventBus : IEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _handlers = new Dictionary<Type, List<Delegate>>();
        private readonly ILogger _logger;

        public EventBus(ILogger logger = null)
        {
            _logger = logger ?? NullLogger.Instance;
        }

        public void Publish<T>(T evt) where T : IDomainEvent
        {
            if (evt == null)
                throw new ArgumentNullException(nameof(evt));

            if (!_handlers.TryGetValue(typeof(T), out var list) || list.Count == 0)
                return;

            var snapshot = list.ToArray();

            foreach (var handler in snapshot)
            {
                try
                {
                    ((Action<T>)handler)(evt);
                }
                catch (Exception ex)
                {
                    _logger.Error($"Event handler failed for {typeof(T).Name}.", ex);
                }
            }
        }

        public IDisposable Subscribe<T>(Action<T> handler) where T : IDomainEvent
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            if (!_handlers.TryGetValue(typeof(T), out var list))
            {
                list = new List<Delegate>();
                _handlers[typeof(T)] = list;
            }

            list.Add(handler);
            return new Subscription(list, handler);
        }

        private sealed class Subscription : IDisposable
        {
            private List<Delegate> _list;
            private readonly Delegate _handler;

            public Subscription(List<Delegate> list, Delegate handler)
            {
                _list = list;
                _handler = handler;
            }

            public void Dispose()
            {
                _list?.Remove(_handler);
                _list = null;
            }
        }
    }
}
