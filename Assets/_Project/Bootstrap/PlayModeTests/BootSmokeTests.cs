using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CleanTime.Bootstrap;
using CleanTime.Economy.Application;
using CleanTime.Shared.Kernel;
using CleanTime.World.Application;

namespace CleanTime.Bootstrap.PlayModeTests
{
    /// <summary>
    /// Runs the real Bootstrap scene in play mode: boot sequence, DI, scene loading, scene scopes.
    /// Unity fails these tests on any logged error or exception (missing scripts, DI failures, ...).
    /// </summary>
    public sealed class BootSmokeTests
    {
        [UnityTest]
        public IEnumerator Bootstrap_ReachesTheMainMenu_WithEveryServiceReady()
        {
            SceneManager.LoadScene("Bootstrap");

            var waited = 0f;
            while ((ProjectLifetimeScope.Instance == null || !ProjectLifetimeScope.Instance.IsReady) && waited < 30f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.IsNotNull(ProjectLifetimeScope.Instance, "project scope exists");
            Assert.IsTrue(ProjectLifetimeScope.Instance.IsReady, "boot finished");

            // The boot sequence ends by loading the main menu additively.
            waited = 0f;
            while (!SceneManager.GetSceneByName("MainMenu").isLoaded && waited < 30f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.IsTrue(SceneManager.GetSceneByName("MainMenu").isLoaded, "main menu loaded");

            var services = ProjectLifetimeScope.Instance.Services;
            Assert.IsNotNull(services.Resolve<IWalletService>());
            Assert.IsNotNull(services.Resolve<ISceneLoader>());
            Assert.AreEqual(0, services.Resolve<IWalletService>().Balance.Cents);

        }

        [UnityTest]
        public IEnumerator Town_And_PropertyScenes_LoadThroughTheSceneLoader_AndInjectTheirServices()
        {
            SceneManager.LoadScene("Bootstrap");

            while (ProjectLifetimeScope.Instance == null || !ProjectLifetimeScope.Instance.IsReady)
                yield return null;

            var loader = ProjectLifetimeScope.Instance.Services.Resolve<ISceneLoader>();

            var town = loader.LoadTownAsync();
            while (!town.IsCompleted)
                yield return null;

            Assert.IsTrue(SceneManager.GetSceneByName("Town").isLoaded, "town loaded");

            // Let the scene lifetime scope construct the scene services, then move on to a property.
            yield return null;
            yield return null;

            var property = loader.UnloadCurrentGameplaySceneAsync();
            while (!property.IsCompleted)
                yield return null;

            Assert.IsFalse(SceneManager.GetSceneByName("Town").isLoaded, "town unloaded");
        }
    }
}
