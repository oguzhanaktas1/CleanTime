using System;
using UnityEngine;
using CleanTime.Shared.Kernel;

namespace CleanTime.Bootstrap
{
    /// <summary>
    /// Owner of the project-wide service container (event bus, save, settings, audio, ...). Lives on the
    /// persistent bootstrap object. Scene scopes read it once to create their child containers; nothing
    /// else should reach for it.
    /// </summary>
    public sealed class ProjectLifetimeScope : MonoBehaviour
    {
        public static ProjectLifetimeScope Instance { get; private set; }

        public ServiceContainer Services { get; private set; }

        public bool IsReady => Services != null;

        public static event Action Ready;

        internal static void Publish(ServiceContainer services)
        {
            if (Instance == null)
                throw new InvalidOperationException("ProjectLifetimeScope must exist before services are published.");

            Instance.Services = services;
            Ready?.Invoke();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance != this)
                return;

            Services?.Dispose();
            Services = null;
            Instance = null;
        }
    }
}
