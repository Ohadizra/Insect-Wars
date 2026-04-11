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

        public static void LoadSkirmishDemo(string sceneName = SkirmishScene)
        {
            // #region agent log
            { var _lp = System.IO.Path.Combine(Application.dataPath, "../.cursor/debug-a7153f.log"); try { System.IO.File.AppendAllText(_lp, "{\"sid\":\"a7153f\",\"h\":\"H1\",\"loc\":\"SceneLoader\",\"sceneName\":\"" + sceneName + "\"}\n"); } catch {} Debug.Log("[DBG-a7153f] SceneLoader loading=" + sceneName); }
            // #endregion
            SceneManager.LoadScene(sceneName);
        }
}
}
