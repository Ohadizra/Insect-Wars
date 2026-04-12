using InsectWars.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace InsectWars.Core
{
    public static class SceneLoader
    {
        public const string HomeScene = "Home";
        public const string GameplayScene = "Gameplay";

        /// <summary>Dev-only test scene; will be removed after development.</summary>
        public const string DevScene = "SkirmishDemo";

        public static void LoadHome()
        {
            SceneManager.LoadScene(HomeScene);
        }

        /// <summary>Stores the selected map and loads the shared Gameplay scene.</summary>
        public static void LoadMap(MapDefinition map)
        {
            if (map == null)
            {
                Debug.LogError("[SceneLoader] Cannot load map — MapDefinition is null.");
                return;
            }
            GameSession.SetSelectedMap(map);
            SceneManager.LoadScene(GameplayScene);
        }

        /// <summary>Dev shortcut — loads the SkirmishDemo scene directly.</summary>
        public static void LoadDevScene()
        {
            SceneManager.LoadScene(DevScene);
        }
    }
}
