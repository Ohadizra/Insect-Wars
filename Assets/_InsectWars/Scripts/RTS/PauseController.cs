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

        void Update()
        {
            if (Keyboard.current == null) return;
            if (!Keyboard.current.escapeKey.wasPressedThisFrame) return;
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
