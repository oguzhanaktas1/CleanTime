using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CleanTime.Shared.Kernel;
using CleanTime.Shared.Unity;

namespace CleanTime.Bootstrap
{
    /// <summary>
    /// Put one in every scene that contains <see cref="ISceneService"/> components. It waits for the
    /// project scope, creates a child container for the scene and hands it to each scene service.
    /// Scene services must initialise in Construct (not Awake / Start), which runs before their Start.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class SceneLifetimeScope : MonoBehaviour
    {
        [SerializeField] private float timeoutSeconds = 10f;

        private ServiceContainer _sceneServices;

        public IServiceResolver Services => _sceneServices;

        private IEnumerator Start()
        {
            var waited = 0f;

            while (ProjectLifetimeScope.Instance == null || !ProjectLifetimeScope.Instance.IsReady)
            {
                waited += Time.unscaledDeltaTime;

                if (waited > timeoutSeconds)
                {
                    Debug.LogError(
                        $"[CleanTime] '{name}': the project scope never became ready. " +
                        "Start the game from the Bootstrap scene (or add a GameBootstrapper to this scene).");
                    yield break;
                }

                yield return null;
            }

            _sceneServices = new ServiceContainer(ProjectLifetimeScope.Instance.Services);

            var services = new List<ISceneService>();

            foreach (var root in gameObject.scene.GetRootGameObjects())
                services.AddRange(root.GetComponentsInChildren<ISceneService>(true));

            foreach (var service in services)
                service.Construct(_sceneServices);
        }

        private void OnDestroy() => _sceneServices?.Dispose();
    }
}
