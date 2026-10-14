using UnityEngine;
using CleanTime.Save.Application;
using CleanTime.Shared.Kernel;
using CleanTime.Shared.Unity;

namespace CleanTime.Save.Presentation
{
    /// <summary>
    /// Requests a checkpoint save when the app loses focus, is suspended (mobile) or quits.
    /// Place one in the persistent bootstrap scene.
    /// </summary>
    public sealed class AutosaveLifecycleHooks : MonoBehaviour, ISceneService
    {
        private IAutosaveTrigger _autosave;

        public void Construct(IServiceResolver services)
        {
            _autosave = services.Resolve<IAutosaveTrigger>();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
                Save();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
                Save();
        }

        private void OnApplicationQuit() => Save();

        private void Save() => _autosave?.SaveIfDirty();
    }
}
