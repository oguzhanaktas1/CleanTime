using System;
using CleanTime.Shared.Kernel;

namespace CleanTime.Settings.Domain
{
    public sealed class AudioSettings
    {
        public AudioSettings(float master = 1f, float music = 0.8f, float sfx = 1f)
        {
            Master = Clamp01(master);
            Music = Clamp01(music);
            Sfx = Clamp01(sfx);
        }

        public float Master { get; }

        public float Music { get; }

        public float Sfx { get; }

        private static float Clamp01(float value) => Math.Max(0f, Math.Min(1f, value));
    }

    public sealed class InputSettings
    {
        public InputSettings(string bindingProfile = "default")
        {
            BindingProfile = string.IsNullOrWhiteSpace(bindingProfile) ? "default" : bindingProfile;
        }

        public string BindingProfile { get; }
    }

    public sealed class GraphicsSettings
    {
        public GraphicsSettings(bool fullscreen = true, int width = 1920, int height = 1080)
        {
            Fullscreen = fullscreen;
            Width = Math.Max(640, width);
            Height = Math.Max(360, height);
        }

        public bool Fullscreen { get; }

        public int Width { get; }

        public int Height { get; }
    }

    /// <summary>Accessibility knobs from architecture §146; none of them changes domain rules.</summary>
    public sealed class AccessibilitySettings
    {
        public AccessibilitySettings(bool holdToClean = false, bool reduceScreenShake = false, float uiScale = 1f)
        {
            HoldToClean = holdToClean;
            ReduceScreenShake = reduceScreenShake;
            UiScale = Math.Max(0.75f, Math.Min(2f, uiScale));
        }

        public bool HoldToClean { get; }

        public bool ReduceScreenShake { get; }

        public float UiScale { get; }
    }

    /// <summary>All player settings. Immutable: change by building a new value.</summary>
    public sealed class GameSettings
    {
        public GameSettings(
            AudioSettings audio = null,
            InputSettings input = null,
            GraphicsSettings graphics = null,
            AccessibilitySettings accessibility = null,
            string languageCode = "en")
        {
            Audio = audio ?? new AudioSettings();
            Input = input ?? new InputSettings();
            Graphics = graphics ?? new GraphicsSettings();
            Accessibility = accessibility ?? new AccessibilitySettings();
            LanguageCode = string.IsNullOrWhiteSpace(languageCode) ? "en" : languageCode;
        }

        public AudioSettings Audio { get; }

        public InputSettings Input { get; }

        public GraphicsSettings Graphics { get; }

        public AccessibilitySettings Accessibility { get; }

        /// <summary>Localization language (two-letter code); text itself always comes from localization keys.</summary>
        public string LanguageCode { get; }

        public static GameSettings Default => new GameSettings();
    }
}
