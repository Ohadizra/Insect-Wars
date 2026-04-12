using InsectWars.Data;
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
    /// Cross-scene session data (Demo 0). Difficulty scales enemy durability, AI cadence, opening counts, and economy.
    /// </summary>
    public static class GameSession
    {
        public const string PrefDifficulty = "IW_Difficulty";
        public const string PrefMasterVolume = "IW_MasterVolume";
        public const string PrefFullscreen = "IW_Fullscreen";
        public const string PrefQuality = "IW_Quality";

        public static DemoDifficulty Difficulty { get; private set; } = DemoDifficulty.Normal;

        /// <summary>Map selected from the skirmish lobby. Null falls back to SkirmishDirector's serialized field.</summary>
        public static SkirmishMapDefinition SelectedMap { get; private set; }

        public static void SetSelectedMap(SkirmishMapDefinition map)
        {
            SelectedMap = map;
        }

        public static float DifficultyEnemyHpMultiplier => Difficulty switch
        {
            DemoDifficulty.Easy => 0.75f,
            DemoDifficulty.Normal => 1f,
            DemoDifficulty.Hard => 1.35f,
            _ => 1f
        };

        /// <summary>Multiplier on enemy skirmish unit count at match start (&gt;= 1 rounds up).</summary>
        public static float DifficultyEnemySpawnMultiplier => Difficulty switch
        {
            DemoDifficulty.Easy => 0.85f,
            DemoDifficulty.Normal => 1f,
            DemoDifficulty.Hard => 1.25f,
            _ => 1f
        };

        /// <summary>Multiplier on enemy AI think delay; higher = slower reactions.</summary>
        public static float DifficultyEnemyAiThinkIntervalMultiplier => Difficulty switch
        {
            DemoDifficulty.Easy => 1.35f,
            DemoDifficulty.Normal => 1f,
            DemoDifficulty.Hard => 0.75f,
            _ => 1f
        };

        /// <summary>Applied to PlayerResources starting stockpile.</summary>
        public static float DifficultyStartingCaloriesMultiplier => Difficulty switch
        {
            DemoDifficulty.Easy => 1.15f,
            DemoDifficulty.Normal => 1f,
            DemoDifficulty.Hard => 0.9f,
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
