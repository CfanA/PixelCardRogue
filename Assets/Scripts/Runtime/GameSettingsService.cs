using System;
using UnityEngine;

namespace SkyCourier
{
    [Serializable]
    public sealed class GameSettingsData
    {
        public int Version = GameSettingsService.CurrentVersion;
        public float MusicVolume = 0.8f;
        public float SfxVolume = 0.9f;
        public int DisplayMode = (int)FullScreenMode.Windowed;
        public int ResolutionWidth = 1600;
        public int ResolutionHeight = 900;
        public bool VSync = true;
        public int FrameRate = 60;
        public float ShakeIntensity = 1f;
        public float FlashIntensity = 1f;
        public int Language = (int)GameLanguage.SimplifiedChinese;
    }

    public static class GameSettingsService
    {
        public const int CurrentVersion = 2;
        private const string SettingsKey = "SkyCourier.Settings.v2";
        private const string PreviousSettingsKey = "SkyCourier.Settings.v1";
        private const string LegacyMusicKey = "SkyCourier.MusicVolume";
        private const string LegacySfxKey = "SkyCourier.SfxVolume";

        public static GameSettingsData Load()
        {
            GameSettingsData settings = null;
            string storedKey = PlayerPrefs.HasKey(SettingsKey) ? SettingsKey :
                PlayerPrefs.HasKey(PreviousSettingsKey) ? PreviousSettingsKey : null;
            if (!string.IsNullOrEmpty(storedKey))
            {
                try
                {
                    settings = JsonUtility.FromJson<GameSettingsData>(PlayerPrefs.GetString(storedKey));
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"SETTINGS_LOAD_FAILED: {exception.Message}");
                }
            }

            settings ??= new GameSettingsData
            {
                MusicVolume = PlayerPrefs.GetFloat(LegacyMusicKey, 0.8f),
                SfxVolume = PlayerPrefs.GetFloat(LegacySfxKey, 0.9f)
            };
            Sanitize(settings);
            return settings;
        }

        public static void Save(GameSettingsData settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            Sanitize(settings);
            PlayerPrefs.SetString(SettingsKey, JsonUtility.ToJson(settings));
            PlayerPrefs.Save();
        }

        public static void Apply(GameSettingsData settings, bool applyDisplay)
        {
            if (settings == null)
                return;
            Sanitize(settings);
            QualitySettings.vSyncCount = settings.VSync ? 1 : 0;
            Application.targetFrameRate = settings.VSync ? -1 : settings.FrameRate;
            if (applyDisplay)
            {
                Screen.SetResolution(settings.ResolutionWidth, settings.ResolutionHeight,
                    (FullScreenMode)settings.DisplayMode);
            }
        }

        public static void Sanitize(GameSettingsData settings)
        {
            settings.Version = CurrentVersion;
            settings.MusicVolume = Mathf.Clamp01(settings.MusicVolume);
            settings.SfxVolume = Mathf.Clamp01(settings.SfxVolume);
            settings.ShakeIntensity = Mathf.Clamp01(settings.ShakeIntensity);
            settings.FlashIntensity = Mathf.Clamp01(settings.FlashIntensity);
            settings.ResolutionWidth = Mathf.Clamp(settings.ResolutionWidth, 960, 7680);
            settings.ResolutionHeight = Mathf.Clamp(settings.ResolutionHeight, 540, 4320);
            if (!Enum.IsDefined(typeof(FullScreenMode), settings.DisplayMode))
                settings.DisplayMode = (int)FullScreenMode.Windowed;
            if (settings.FrameRate != 30 && settings.FrameRate != 60 && settings.FrameRate != 120 &&
                settings.FrameRate != 144 && settings.FrameRate != 240)
                settings.FrameRate = 60;
            if (!Enum.IsDefined(typeof(GameLanguage), settings.Language))
                settings.Language = (int)GameLanguage.SimplifiedChinese;
        }
    }
}
