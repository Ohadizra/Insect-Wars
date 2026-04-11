using InsectWars.Core;
using UnityEngine;
using UnityEngine.UI;

namespace InsectWars.RTS
{
    /// <summary>
    /// Elimination match: win when no enemy combatants remain, lose when none of yours do.
    /// </summary>
    public class MatchDirector : MonoBehaviour
    {
        public static bool MatchEnded { get; private set; }

        enum MatchState { Playing, Won, Lost }

        MatchState _state = MatchState.Playing;
        GameObject _overlayRoot;
        Text _overlayText;

        float _matchTimer;
        const float MatchStartupGrace = 1.5f;

        void Update()
        {
            if (_state != MatchState.Playing) return;
            if (PauseController.IsPaused) return;

            _matchTimer += Time.deltaTime;
            if (_matchTimer < MatchStartupGrace) return;

            if (RtsSimRegistry.CountAlive(Team.Enemy) == 0)
                EndMatch(MatchState.Won);
            else if (RtsSimRegistry.CountAlive(Team.Player) == 0)
                EndMatch(MatchState.Lost);
        }

        void EndMatch(MatchState end)
        {
            _state = end;
            MatchEnded = true;
            BuildOverlay();
            Time.timeScale = 0f;
            if (_overlayText != null)
                _overlayText.text = end == MatchState.Won ? "Victory" : "Defeat";
            GameAudio.PlayUi(end == MatchState.Won ? GameAudio.UiKind.MatchVictory : GameAudio.UiKind.MatchDefeat);
        }

        void BuildOverlay()
        {
            if (_overlayRoot != null) return;
            var parent = GameHUD.HudCanvasRect;
            if (parent == null) return;

            _overlayRoot = new GameObject("MatchOverlay");
            _overlayRoot.transform.SetParent(parent, false);
            var rt = _overlayRoot.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var dim = _overlayRoot.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.55f);
            dim.raycastTarget = true;

            var textGo = new GameObject("Banner");
            textGo.transform.SetParent(_overlayRoot.transform, false);
            var trt = textGo.AddComponent<RectTransform>();
            trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.55f);
            trt.sizeDelta = new Vector2(800, 120);
            _overlayText = textGo.AddComponent<Text>();
            _overlayText.font = UiFontHelper.GetFont();
            _overlayText.fontSize = 48;
            _overlayText.alignment = TextAnchor.MiddleCenter;
            _overlayText.color = Color.white;

            var btnGo = new GameObject("MenuBtn");
            btnGo.transform.SetParent(_overlayRoot.transform, false);
            var brt = btnGo.AddComponent<RectTransform>();
            brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.35f);
            brt.sizeDelta = new Vector2(220, 48);
            var bImg = btnGo.AddComponent<Image>();
            bImg.color = new Color(0.2f, 0.18f, 0.28f, 0.95f);
            var btn = btnGo.AddComponent<Button>();
            var lbl = new GameObject("Lbl").AddComponent<Text>();
            lbl.transform.SetParent(btnGo.transform, false);
            lbl.font = UiFontHelper.GetFont();
            lbl.fontSize = 22;
            lbl.color = Color.white;
            lbl.alignment = TextAnchor.MiddleCenter;
            lbl.text = "Main Menu";
            var lrt = lbl.rectTransform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            btn.onClick.AddListener(() =>
            {
                Time.timeScale = 1f;
                SceneLoader.LoadHome();
            });
        }

        void OnDestroy()
        {
            MatchEnded = false;
            if (_state != MatchState.Playing)
                Time.timeScale = 1f;
        }
    }
}
