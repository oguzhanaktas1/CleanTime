using System;
using UnityEngine;

namespace CleanTime.Bootstrap
{
    /// <summary>
    /// Minimal recoverable error screen shown when boot fails. Uses IMGUI so it works with no assets
    /// or scene UI; replaced by a proper localized screen later. Never shows stack traces.
    /// </summary>
    public sealed class BootErrorPresenter : MonoBehaviour
    {
        private string _stage = string.Empty;
        private string _message = string.Empty;
        private Action _retry;
        private bool _visible;

        public void Show(string stage, string message, Action retry)
        {
            _stage = stage;
            _message = message;
            _retry = retry;
            _visible = true;
        }

        public void Hide() => _visible = false;

        private void OnGUI()
        {
            if (!_visible)
                return;

            var width = Mathf.Min(520f, Screen.width - 40f);
            var area = new Rect((Screen.width - width) / 2f, Screen.height * 0.3f, width, 220f);

            GUILayout.BeginArea(area, GUI.skin.box);
            GUILayout.Label("Clean Time could not start");
            GUILayout.Label($"Step: {_stage}");
            GUILayout.Label(_message);

            if (GUILayout.Button("Try again"))
                _retry?.Invoke();

            GUILayout.EndArea();
        }
    }
}
