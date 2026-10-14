using UnityEngine;
using UnityEngine.InputSystem;
using CleanTime.DebugTools.Application;
using CleanTime.Economy.Application;
using CleanTime.GameTime.Application;
using CleanTime.GameTime.Domain;
using CleanTime.Save.Application;
using CleanTime.Save.Domain;
using CleanTime.Shared.Kernel;
using CleanTime.Shared.Unity;

namespace CleanTime.DebugTools.Presentation
{
    /// <summary>
    /// Developer overlay (F1): live diagnostics and a tiny command line. The body is compiled out of
    /// release builds, so the component stays harmless if it is left in a scene.
    /// </summary>
    public sealed class DebugPanel : MonoBehaviour, ISceneService
    {
#if UNITY_EDITOR || DEBUG || CLEANTIME_DEBUG
        private DebugCommandRegistry _registry;
        private ISaveCoordinator _save;
        private IWalletService _wallet;
        private IQueryHandler<GetCurrentGameDateQuery, GameDate> _date;
        private bool _visible;
        private string _input = string.Empty;
        private string _lastResult = string.Empty;

        public void Construct(IServiceResolver services)
        {
            _registry = services.Resolve<DebugCommandRegistry>();
            _save = services.Resolve<ISaveCoordinator>();
            _wallet = services.Resolve<IWalletService>();
            _date = services.Resolve<IQueryHandler<GetCurrentGameDateQuery, GameDate>>();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
                _visible = !_visible;
        }

        private void OnGUI()
        {
            if (!_visible || _registry == null)
                return;

            GUILayout.BeginArea(new Rect(10, 10, 460, Screen.height - 20), GUI.skin.box);
            GUILayout.Label("Clean Time debug");
            GUILayout.Label($"Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
            GUILayout.Label($"Save: {(_save.CurrentSaveId.IsEmpty ? "(none)" : _save.CurrentSaveId.ToString())}  schema v{SaveSchema.CurrentVersion}");
            GUILayout.Label($"Date: {_date.Handle(new GetCurrentGameDateQuery())}");
            GUILayout.Label($"Wallet: {_wallet.Balance}");
            GUILayout.Space(6);

            foreach (var command in _registry.Commands)
                GUILayout.Label("  " + command.Usage);

            _input = GUILayout.TextField(_input);

            if (GUILayout.Button("Run") && !string.IsNullOrWhiteSpace(_input))
            {
                var parts = _input.Trim().Split(' ');
                var result = _registry.Execute(parts[0], parts.Length > 1 ? parts[1..] : new string[0]);
                _lastResult = result.IsSuccess ? "ok" : result.Error.ToString();
            }

            GUILayout.Label(_lastResult);
            GUILayout.EndArea();
        }
#else
        public void Construct(IServiceResolver services)
        {
        }
#endif
    }
}
