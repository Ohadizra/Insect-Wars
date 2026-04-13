using InsectWars.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace InsectWars.RTS
{
    /// <summary>
    /// Escape toggles pause. Ignored while a match end screen is active.
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

        void LateUpdate()
        {
            if (Keyboard.current == null) return;
            if (!Keyboard.current.escapeKey.wasPressedThisFrame) return;
            if (MatchDirector.MatchEnded) return;
            if (BottomBar.WouldConsumeEscape) return;

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
