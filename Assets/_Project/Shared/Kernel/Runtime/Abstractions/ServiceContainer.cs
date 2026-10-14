using System;
using System.Collections.Generic;
using System.Linq;

namespace CleanTime.Shared.Kernel
{
    public interface IServiceResolver
    {
        T Resolve<T>();

        object Resolve(Type serviceType);

        bool TryResolve<T>(out T service);

        /// <summary>All registrations of the service type (parent scope first), in registration order.</summary>
        IReadOnlyList<T> ResolveAll<T>();
    }

    public interface IServiceRegistry
    {
        void RegisterInstance<T>(T instance);

        void RegisterSingleton<T>(Func<IServiceResolver, T> factory);

        void RegisterTransient<T>(Func<IServiceResolver, T> factory);
    }

    public static class ServiceRegistryExtensions
    {
        /// <summary>Exposes an already registered implementation under an additional service type.</summary>
        public static void Forward<TService, TImplementation>(this IServiceRegistry registry)
            where TImplementation : TService
        {
            registry.RegisterSingleton<TService>(resolver => resolver.Resolve<TImplementation>());
        }
    }

    public sealed class ServiceNotRegisteredException : InvalidOperationException
    {
        public ServiceNotRegisteredException(Type serviceType)
            : base($"No service registered for {serviceType.FullName}.")
        {
            ServiceType = serviceType;
        }

        public Type ServiceType { get; }
    }

    /// <summary>
    /// Lightweight composition root. Registrations are explicit factories (no reflection, IL2CPP safe,
    /// works with internal implementation types). Swap for VContainer behind IServiceRegistry if desired.
    /// </summary>
    public sealed class ServiceContainer : IServiceRegistry, IServiceResolver, IDisposable
    {
        private readonly ServiceContainer _parent;
        private readonly Dictionary<Type, List<Registration>> _registrations = new Dictionary<Type, List<Registration>>();
        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private readonly List<Type> _resolutionStack = new List<Type>();
        private bool _disposed;

        public ServiceContainer(ServiceContainer parent = null)
        {
            _parent = parent;
        }

        public void RegisterInstance<T>(T instance)
        {
            var registration = new Registration(typeof(T), _ => instance, singleton: true);
            registration.SetInstance(instance);
            Add(registration);
        }

        public void RegisterSingleton<T>(Func<IServiceResolver, T> factory)
        {
            Guard.NotNull(factory, nameof(factory));
            Add(new Registration(typeof(T), r => factory(r), singleton: true));
        }

        public void RegisterTransient<T>(Func<IServiceResolver, T> factory)
        {
            Guard.NotNull(factory, nameof(factory));
            Add(new Registration(typeof(T), r => factory(r), singleton: false));
        }

        public T Resolve<T>() => (T)Resolve(typeof(T));

        public object Resolve(Type serviceType)
        {
            ThrowIfDisposed();

            if (!TryFindLast(serviceType, out var owner, out var registration))
                throw new ServiceNotRegisteredException(serviceType);

            return owner.Create(registration);
        }

        public bool TryResolve<T>(out T service)
        {
            ThrowIfDisposed();

            if (TryFindLast(typeof(T), out var owner, out var registration))
            {
                service = (T)owner.Create(registration);
                return true;
            }

            service = default;
            return false;
        }

        public IReadOnlyList<T> ResolveAll<T>()
        {
            ThrowIfDisposed();

            var result = new List<T>();
            Collect(typeof(T), result);
            return result;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            for (var i = _disposables.Count - 1; i >= 0; i--)
                _disposables[i].Dispose();

            _disposables.Clear();
            _registrations.Clear();
        }

        private void Collect<T>(Type type, List<T> into)
        {
            _parent?.Collect(type, into);

            if (!_registrations.TryGetValue(type, out var list))
                return;

            foreach (var registration in list.ToArray())
                into.Add((T)Create(registration));
        }

        private bool TryFindLast(Type type, out ServiceContainer owner, out Registration registration)
        {
            for (var scope = this; scope != null; scope = scope._parent)
            {
                if (scope._registrations.TryGetValue(type, out var list) && list.Count > 0)
                {
                    owner = scope;
                    registration = list[list.Count - 1];
                    return true;
                }
            }

            owner = null;
            registration = null;
            return false;
        }

        private object Create(Registration registration)
        {
            if (registration.Singleton && registration.HasInstance)
                return registration.Instance;

            if (_resolutionStack.Contains(registration.ServiceType))
            {
                var chain = string.Join(" -> ", _resolutionStack.Select(t => t.Name).Concat(new[] { registration.ServiceType.Name }));
                throw new InvalidOperationException($"Circular dependency: {chain}");
            }

            _resolutionStack.Add(registration.ServiceType);

            try
            {
                var instance = registration.Factory(this);

                if (instance is IDisposable disposable)
                    _disposables.Add(disposable);

                if (registration.Singleton)
                    registration.SetInstance(instance);

                return instance;
            }
            finally
            {
                _resolutionStack.RemoveAt(_resolutionStack.Count - 1);
            }
        }

        private void Add(Registration registration)
        {
            ThrowIfDisposed();

            if (!_registrations.TryGetValue(registration.ServiceType, out var list))
            {
                list = new List<Registration>();
                _registrations[registration.ServiceType] = list;
            }

            list.Add(registration);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ServiceContainer));
        }

        private sealed class Registration
        {
            public Registration(Type serviceType, Func<IServiceResolver, object> factory, bool singleton)
            {
                ServiceType = serviceType;
                Factory = factory;
                Singleton = singleton;
            }

            public Type ServiceType { get; }

            public Func<IServiceResolver, object> Factory { get; }

            public bool Singleton { get; }

            public bool HasInstance { get; private set; }

            public object Instance { get; private set; }

            public void SetInstance(object instance)
            {
                Instance = instance;
                HasInstance = true;
            }
        }
    }
}
