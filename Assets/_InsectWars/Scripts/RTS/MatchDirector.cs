using InsectWars.Core;
using UnityEngine;
using UnityEngine.UI;

namespace InsectWars.RTS
{
    /// <summary>
    /// Elimination skirmish: win/lose when all buildings and hive of a team are destroyed.
    /// </summary>
    public class MatchDirector : MonoBehaviour
    {
        public static bool MatchEnded { get; private set; }

        enum MatchState { Playing, Won, Lost }

        MatchState _state = MatchState.Playing;
        GameObject _overlayRoot;
        Text _overlayText;
        float _graceTimer = 10f;

        void Update()
        {
            if (_state != MatchState.Playing) return;
            if (PauseController.IsPaused) return;
            if (Core.GameSession.IsLearningMode) return;

            if (_graceTimer > 0f)
            {
                _graceTimer -= Time.deltaTime;
                return;
            }

            if (IsEnemyDefeated())
                EndMatch(MatchState.Won);
            else if (IsPlayerDefeated())
                EndMatch(MatchState.Lost);
        }

        static bool IsEnemyDefeated()
        {
            if (RtsSimRegistry.CountAlive(Team.Enemy) == 0)
                return true;

            var hive = HiveDeposit.EnemyHive;
            if (hive != null && hive.IsAlive) return false;
            foreach (var bld in ProductionBuilding.All)
            {
                if (bld != null && bld.IsAlive && bld.Team == Team.Enemy) return false;
            }
            return true;
        }

        static bool IsPlayerDefeated()
        {
            var hive = HiveDeposit.PlayerHive;
            if (hive != null && hive.IsAlive) return false;
            foreach (var bld in ProductionBuilding.All)
            {
                if (bld != null && bld.IsAlive && bld.Team == Team.Player) return false;
            }
            return true;
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
            dim.color = new Color(0f, 0f, 0f, 0.6f);
            dim.raycastTarget = true;

            var panel = new GameObject("Panel");
            panel.transform.SetParent(_overlayRoot.transform, false);
            var prt = panel.AddComponent<RectTransform>();
            prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(420, 240);
            var pImg = panel.AddComponent<Image>();
            pImg.color = new Color(0.12f, 0.1f, 0.18f, 0.95f);

            var border = panel.AddComponent<Outline>();
            border.effectColor = new Color(0.85f, 0.7f, 0.3f, 0.9f);
            border.effectDistance = new Vector2(2f, 2f);

            var textGo = new GameObject("Banner");
            textGo.transform.SetParent(panel.transform, false);
            var trt = textGo.AddComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 0.55f);
            trt.anchorMax = new Vector2(1f, 0.95f);
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            _overlayText = textGo.AddComponent<Text>();
            _overlayText.font = UiFontHelper.GetFont();
            _overlayText.fontSize = 42;
            _overlayText.alignment = TextAnchor.MiddleCenter;
            _overlayText.color = new Color(1f, 0.92f, 0.5f);
            _overlayText.fontStyle = FontStyle.Bold;

            var btnGo = new GameObject("MenuBtn");
            btnGo.transform.SetParent(panel.transform, false);
            var brt = btnGo.AddComponent<RectTransform>();
            brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.22f);
            brt.sizeDelta = new Vector2(240, 52);
            var bImg = btnGo.AddComponent<Image>();
            bImg.color = new Color(0.28f, 0.22f, 0.4f, 1f);
            var btn = btnGo.AddComponent<Button>();
            var btnColors = btn.colors;
            btnColors.highlightedColor = new Color(0.4f, 0.32f, 0.55f);
            btnColors.pressedColor = new Color(0.2f, 0.15f, 0.3f);
            btn.colors = btnColors;

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
