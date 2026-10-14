using System;
using System.IO;
using UnityEngine;
using CleanTime.Settings.Domain;
using AudioSettings = CleanTime.Settings.Domain.AudioSettings;
using CleanTime.Shared.Kernel;
using KernelLogger = CleanTime.Shared.Kernel.ILogger;

namespace CleanTime.Settings.Infrastructure
{
    public sealed class InMemoryGameSettingsRepository : IGameSettingsRepository
    {
        private GameSettings _settings = GameSettings.Default;

        public GameSettings Load() => _settings;

        public Result Save(GameSettings settings)
        {
            _settings = settings;
            return Result.Success();
        }
    }

    /// <summary>
    /// Settings live outside save slots (architecture §70). Stored as a small JSON file until the
    /// SQLite game_setting table exists; a corrupt or missing file silently yields defaults.
    /// </summary>
    public sealed class JsonFileGameSettingsRepository : IGameSettingsRepository
    {
        private readonly string _path;
        private readonly KernelLogger _logger;

        public JsonFileGameSettingsRepository(string path, KernelLogger logger)
        {
            _path = path;
            _logger = logger;
        }

        public GameSettings Load()
        {
            try
            {
                if (!File.Exists(_path))
                    return GameSettings.Default;

                var dto = JsonUtility.FromJson<SettingsDto>(File.ReadAllText(_path));
                return dto == null ? GameSettings.Default : dto.ToDomain();
            }
            catch (Exception ex)
            {
                _logger.Warning("Could not read settings, using defaults: " + ex.Message);
                return GameSettings.Default;
            }
        }

        public Result Save(GameSettings settings)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path));
                File.WriteAllText(_path, JsonUtility.ToJson(SettingsDto.From(settings), true));
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.Error("Could not write settings.", ex);
                return Result.Failure("settings.write_failed", ex.Message);
            }
        }

        [Serializable]
        private sealed class SettingsDto
        {
            public float master = 1f;
            public float music = 0.8f;
            public float sfx = 1f;
            public string bindingProfile = "default";
            public bool fullscreen = true;
            public int width = 1920;
            public int height = 1080;
            public bool holdToClean;
            public bool reduceScreenShake;
            public float uiScale = 1f;
            public string language = "en";

            public static SettingsDto From(GameSettings s) => new SettingsDto
            {
                master = s.Audio.Master,
                music = s.Audio.Music,
                sfx = s.Audio.Sfx,
                bindingProfile = s.Input.BindingProfile,
                fullscreen = s.Graphics.Fullscreen,
                width = s.Graphics.Width,
                height = s.Graphics.Height,
                holdToClean = s.Accessibility.HoldToClean,
                reduceScreenShake = s.Accessibility.ReduceScreenShake,
                uiScale = s.Accessibility.UiScale,
                language = s.LanguageCode,
            };

            public GameSettings ToDomain() => new GameSettings(
                new AudioSettings(master, music, sfx),
                new InputSettings(bindingProfile),
                new GraphicsSettings(fullscreen, width, height),
                new AccessibilitySettings(holdToClean, reduceScreenShake, uiScale),
                language);
        }
    }

    public sealed class SettingsInfrastructureModule : FeatureModule
    {
        private readonly string _settingsPath;

        /// <param name="settingsPath">Full path of the settings file; null keeps settings in memory only.</param>
        public SettingsInfrastructureModule(string settingsPath = null)
        {
            _settingsPath = settingsPath;
        }

        public override string Name => "Settings.Infrastructure";

        public override void Register(IServiceRegistry services)
        {
            if (string.IsNullOrEmpty(_settingsPath))
                services.RegisterSingleton<IGameSettingsRepository>(_ => new InMemoryGameSettingsRepository());
            else
                services.RegisterSingleton<IGameSettingsRepository>(
                    r => new JsonFileGameSettingsRepository(_settingsPath, r.Resolve<KernelLogger>()));
        }
    }
}
