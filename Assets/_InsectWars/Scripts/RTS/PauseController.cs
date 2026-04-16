using InsectWars.Core;
using UnityEngine;

namespace InsectWars.RTS
{
    /// <summary>
    /// Manages pause state. Toggled via the in-game settings panel, not a hotkey.
    /// </summary>
    public class PauseController : MonoBehaviour
    {
        public static bool IsPaused { get; private set; }

        /// <summary>Reset pause state — called by MapDirector to clear stale static state.</summary>
        public static void ForceUnpause()
        {
            IsPaused = false;
            Time.timeScale = 1f;
        }

        /// <summary>Toggle pause on/off. Called from the settings panel UI.</summary>
        public static void TogglePause()
        {
            if (MatchDirector.MatchEnded) return;
            IsPaused = !IsPaused;
            Time.timeScale = IsPaused ? 0f : 1f;
            GameAudio.PlayUi(GameAudio.UiKind.PauseToggle);
        }

        void OnDestroy()
        {
            if (IsPaused && !MatchDirector.MatchEnded)
                Time.timeScale = 1f;
            IsPaused = false;
        }
    }
}
