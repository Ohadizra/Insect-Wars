using InsectWars.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace InsectWars.Core
{
    public static class SceneLoader
    {
        public const string HomeScene = "Home";

        /// <summary>Dev-only skirmish scene; will be removed after development.</summary>
        public const string SkirmishScene = "SkirmishDemo";

        public static void LoadHome()
        {
            SceneManager.LoadScene(HomeScene);
        }

        /// <summary>Loads the scene associated with the given map definition.</summary>
        public static void LoadMap(SkirmishMapDefinition map)
        {
            if (map == null || string.IsNullOrEmpty(map.sceneName))
            {
                Debug.LogError("[SceneLoader] Cannot load map — no sceneName set.");
                return;
            }
            GameSession.SetSelectedMap(map);
            SceneManager.LoadScene(map.sceneName);
        }

        /// <summary>Dev shortcut — loads the SkirmishDemo scene directly.</summary>
        public static void LoadSkirmishDemo()
        {
            SceneManager.LoadScene(SkirmishScene);
        }
    }
}
