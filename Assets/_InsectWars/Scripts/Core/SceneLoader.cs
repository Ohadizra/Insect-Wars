using UnityEngine;
using UnityEngine.SceneManagement;

namespace InsectWars.Core
{
    public static class SceneLoader
    {
        public const string HomeScene = "Home";
        public const string GameScene = "Game";

        /// <summary>Dev-only skirmish scene; will be removed after development.</summary>
        public const string SkirmishScene = "SkirmishDemo";

        public static void LoadHome()
        {
            SceneManager.LoadScene(HomeScene);
        }

        public static void LoadGame()
        {
            SceneManager.LoadScene(GameScene);
        }

        /// <summary>Dev shortcut — loads the SkirmishDemo scene directly.</summary>
        public static void LoadSkirmishDemo()
        {
            SceneManager.LoadScene(SkirmishScene);
        }
    }
}
