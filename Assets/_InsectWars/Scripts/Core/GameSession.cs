using UnityEngine;

namespace InsectWars.Core
{
    public enum DemoDifficulty
    {
        Easy = 0,
        Normal = 1,
        Hard = 2
    }

    /// <summary>
    /// Cross-scene session data (Demo 0). Difficulty scales enemy HP / spawn pressure.
    /// </summary>
    public static class GameSession
    {
        public const string PrefDifficulty = "IW_Difficulty";
        public const string PrefMasterVolume = "IW_MasterVolume";
        public const string PrefFullscreen = "IW_Fullscreen";
        public const string PrefQuality = "IW_Quality";

        public static DemoDifficulty Difficulty { get; private set; } = DemoDifficulty.Normal;

        public static float DifficultyEnemyHpMultiplier => Difficulty switch
        {
            DemoDifficulty.Easy => 0.75f,
            DemoDifficulty.Normal => 1f,
            DemoDifficulty.Hard => 1.35f,
            _ => 1f
        };

        public static void LoadPrefs()
        {
            Difficulty = (DemoDifficulty)Mathf.Clamp(PlayerPrefs.GetInt(PrefDifficulty, 1), 0, 2);
        }

        public static void SetDifficulty(DemoDifficulty d)
        {
            Difficulty = d;
            PlayerPrefs.SetInt(PrefDifficulty, (int)d);
            PlayerPrefs.Save();
        }

        public static float GetSavedMasterVolume()
        {
            return PlayerPrefs.GetFloat(PrefMasterVolume, 0.85f);
        }

        public static void SetMasterVolume(float v)
        {
            v = Mathf.Clamp01(v);
            PlayerPrefs.SetFloat(PrefMasterVolume, v);
            PlayerPrefs.Save();
            AudioListener.volume = v;
        }

        public static bool GetSavedFullscreen()
        {
            return PlayerPrefs.GetInt(PrefFullscreen, Screen.fullScreen ? 1 : 0) == 1;
        }

        public static void SetFullscreen(bool full)
        {
            PlayerPrefs.SetInt(PrefFullscreen, full ? 1 : 0);
            PlayerPrefs.Save();
            Screen.fullScreen = full;
        }

        public static int GetSavedQuality()
        {
            return PlayerPrefs.GetInt(PrefQuality, QualitySettings.GetQualityLevel());
        }

        public static void SetQualityLevel(int level)
        {
            level = Mathf.Clamp(level, 0, QualitySettings.names.Length - 1);
            QualitySettings.SetQualityLevel(level, true);
            PlayerPrefs.SetInt(PrefQuality, level);
            PlayerPrefs.Save();
        }
    }
}
