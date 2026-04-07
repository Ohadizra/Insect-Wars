using UnityEngine;
using UnityEngine.SceneManagement;

namespace InsectWars.Core
{
    public static class SceneLoader
    {
        public const string HomeScene = "Home";
        public const string SkirmishScene = "SkirmishDemo";

        public static void LoadHome()
        {
            SceneManager.LoadScene(HomeScene);
        }

        public static void LoadSkirmishDemo()
        {
            SceneManager.LoadScene(SkirmishScene);
        }
    }
}
