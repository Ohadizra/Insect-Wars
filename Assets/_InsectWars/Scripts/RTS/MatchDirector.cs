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

        static readonly Color ColAmber = new(0.96f, 0.90f, 0.78f);
        static readonly Color ColStat = new(0.83f, 0.78f, 0.68f);
        static readonly Color ColVictory = new(1f, 0.92f, 0.5f);
        static readonly Color ColDefeat = new(1f, 0.4f, 0.35f);
        static readonly Color ColPanelTint = new(0.08f, 0.1f, 0.06f, 0.95f);
        static readonly Color ColBtnFill = new(0.15f, 0.18f, 0.12f, 0.92f);
        static readonly Color ColBtnHighlight = new(0.85f, 1f, 0.85f);
        static readonly Color ColBtnPressed = new(0.6f, 0.8f, 0.6f);
        static readonly Color ColOutline = new(0.1f, 0.08f, 0.05f, 0.8f);

        MatchState _state = MatchState.Playing;
        GameObject _overlayRoot;
        float _graceTimer = 10f;

        void Awake()
        {
            MatchStats.Reset();
        }

        void Update()
        {
            if (_state != MatchState.Playing) return;
            if (PauseController.IsPaused) return;
            if (Core.GameSession.IsLearningMode) return;
            if (Core.GameSession.IsTutorialMode) return;

            MatchStats.ElapsedTime += Time.deltaTime;

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
            Time.timeScale = 0f;
            BuildOverlay(end);
            GameAudio.PlayUi(end == MatchState.Won ? GameAudio.UiKind.MatchVictory : GameAudio.UiKind.MatchDefeat);
        }

        void BuildOverlay(MatchState end)
        {
            if (_overlayRoot != null) return;
            var parent = GameHUD.HudCanvasRect;
            if (parent == null)
            {
                var fallbackCanvas = Object.FindFirstObjectByType<Canvas>();
                if (fallbackCanvas != null)
                    parent = fallbackCanvas.GetComponent<RectTransform>();
            }
            if (parent == null)
            {
                Debug.LogWarning("MatchDirector: No canvas found, loading home to avoid soft-lock.");
                Time.timeScale = 1f;
                SceneLoader.LoadHome();
                return;
            }

            var frameSprite = GameHUD.LoadSpriteFromResources("UI/Extracted/frame_square_panel");
            var btnSprite = GameHUD.LoadSpriteFromResources("UI/Extracted/frame_square_panel");

            // Fullscreen dim backdrop
            _overlayRoot = new GameObject("MatchOverlay");
            _overlayRoot.transform.SetParent(parent, false);
            var rt = _overlayRoot.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var dim = _overlayRoot.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.6f);
            dim.raycastTarget = true;

            // Center panel
            var panel = new GameObject("Panel");
            panel.transform.SetParent(_overlayRoot.transform, false);
            var prt = panel.AddComponent<RectTransform>();
            prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(460, 340);
            var pImg = panel.AddComponent<Image>();
            if (frameSprite != null)
            {
                pImg.sprite = frameSprite;
                pImg.type = Image.Type.Sliced;
            }
            pImg.color = ColPanelTint;

            bool won = end == MatchState.Won;

            // Banner title
            var bannerColor = won ? ColVictory : ColDefeat;
            var bannerText = won ? "VICTORY" : "DEFEAT";
            CreateLabel(panel.transform, "Banner", bannerText, 42, bannerColor, FontStyle.Bold,
                new Vector2(0f, 0.72f), new Vector2(1f, 0.95f), TextAnchor.MiddleCenter);

            // Stats section
            int minutes = Mathf.FloorToInt(MatchStats.ElapsedTime / 60f);
            int seconds = Mathf.FloorToInt(MatchStats.ElapsedTime % 60f);
            string durationStr = $"{minutes}:{seconds:D2}";

            CreateLabel(panel.transform, "StatDuration",
                $"Match Duration:   {durationStr}", 20, ColStat, FontStyle.Normal,
                new Vector2(0.1f, 0.52f), new Vector2(0.9f, 0.66f), TextAnchor.MiddleLeft);

            CreateLabel(panel.transform, "StatKills",
                $"Enemy Units Killed:   {MatchStats.EnemyUnitsKilled:N0}", 20, ColStat, FontStyle.Normal,
                new Vector2(0.1f, 0.38f), new Vector2(0.9f, 0.52f), TextAnchor.MiddleLeft);

            CreateLabel(panel.transform, "StatCalories",
                $"Calories Gathered:   {MatchStats.CaloriesGathered:N0}", 20, ColStat, FontStyle.Normal,
                new Vector2(0.1f, 0.24f), new Vector2(0.9f, 0.38f), TextAnchor.MiddleLeft);

            // Play Again button
            CreateButton(panel.transform, "PlayAgainBtn", "Play Again", btnSprite,
                new Vector2(0.5f, 0.12f), new Vector2(0, 52),
                () =>
                {
                    Time.timeScale = 1f;
                    SceneLoader.LoadSkirmishDemo();
                });

            // Main Menu button
            CreateButton(panel.transform, "MenuBtn", "Main Menu", btnSprite,
                new Vector2(0.5f, -0.02f), new Vector2(0, -4),
                () =>
                {
                    Time.timeScale = 1f;
                    SceneLoader.LoadHome();
                });
        }

        Text CreateLabel(Transform parent, string name, string content, int fontSize, Color color,
            FontStyle style, Vector2 anchorMin, Vector2 anchorMax, TextAnchor alignment)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var trt = go.AddComponent<RectTransform>();
            trt.anchorMin = anchorMin;
            trt.anchorMax = anchorMax;
            trt.offsetMin = trt.offsetMax = Vector2.zero;

            var t = go.AddComponent<Text>();
            t.font = UiFontHelper.GetFont();
            t.fontSize = fontSize;
            t.color = color;
            t.fontStyle = style;
            t.alignment = alignment;
            t.raycastTarget = false;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = ColOutline;
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            return t;
        }

        void CreateButton(Transform parent, string name, string label, Sprite btnSprite,
            Vector2 anchor, Vector2 posOffset, UnityEngine.Events.UnityAction onClick)
        {
            var btnGo = new GameObject(name);
            btnGo.transform.SetParent(parent, false);
            var brt = btnGo.AddComponent<RectTransform>();
            brt.anchorMin = brt.anchorMax = anchor;
            brt.anchoredPosition = posOffset;
            brt.sizeDelta = new Vector2(250, 50);

            var img = btnGo.AddComponent<Image>();
            if (btnSprite != null)
            {
                img.sprite = btnSprite;
                img.type = Image.Type.Sliced;
            }
            img.color = ColBtnFill;

            var btn = btnGo.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = ColBtnHighlight;
            colors.pressedColor = ColBtnPressed;
            btn.colors = colors;

            var lbl = new GameObject("Label").AddComponent<Text>();
            lbl.transform.SetParent(btnGo.transform, false);
            lbl.font = UiFontHelper.GetFont();
            lbl.fontSize = 22;
            lbl.color = ColAmber;
            lbl.alignment = TextAnchor.MiddleCenter;
            lbl.text = label;
            lbl.raycastTarget = false;
            var lrt = lbl.rectTransform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;

            btn.onClick.AddListener(onClick);
        }

        void OnDestroy()
        {
            MatchEnded = false;
            if (_state != MatchState.Playing)
                Time.timeScale = 1f;
        }
    }
}
