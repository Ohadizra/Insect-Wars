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

        // ── Parchment / Wooden Palette (matches Home menu) ──
        static readonly Color ColAmber = new(0.96f, 0.90f, 0.78f);
        static readonly Color ColSub = new(0.83f, 0.69f, 0.44f);
        static readonly Color ColVictory = new(1f, 0.92f, 0.5f);
        static readonly Color ColDefeat = new(1f, 0.4f, 0.35f);
        static readonly Color ColWhite = Color.white;
        static readonly Color ColDimBg = new(0f, 0f, 0f, 0.70f);
        static readonly Color ColBtnHighlight = new(1f, 0.9f, 0.7f, 1f);
        static readonly Color ColBtnPressed = new(0.8f, 0.7f, 0.5f, 1f);
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
            var sepSprite = GameHUD.LoadSpriteFromResources("UI/Extracted/frame_ornate");

            // Fullscreen dim backdrop
            _overlayRoot = new GameObject("MatchOverlay");
            _overlayRoot.transform.SetParent(parent, false);
            var rt = _overlayRoot.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var dim = _overlayRoot.AddComponent<Image>();
            dim.color = ColDimBg;
            dim.raycastTarget = true;

            // Center panel — wooden frame
            var panel = new GameObject("Panel");
            panel.transform.SetParent(_overlayRoot.transform, false);
            var prt = panel.AddComponent<RectTransform>();
            prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(550, 520);
            var pImg = panel.AddComponent<Image>();
            pImg.sprite = frameSprite;
            pImg.color = ColWhite;
            pImg.type = Image.Type.Sliced;

            bool won = end == MatchState.Won;

            // Banner title
            var bannerColor = won ? ColVictory : ColDefeat;
            var bannerText = won ? "VICTORY" : "DEFEAT";
            CreateLabel(panel.transform, "Banner", bannerText, 46, bannerColor, FontStyle.Bold,
                new Vector2(0f, 0.82f), new Vector2(1f, 0.96f), TextAnchor.MiddleCenter);

            // Separator below banner
            MakeSeparator(panel.transform, sepSprite, new Vector2(0.08f, 0.80f), new Vector2(0.92f, 0.80f));

            // Stats section
            int minutes = Mathf.FloorToInt(MatchStats.ElapsedTime / 60f);
            int seconds = Mathf.FloorToInt(MatchStats.ElapsedTime % 60f);
            string durationStr = $"{minutes}:{seconds:D2}";

            CreateLabel(panel.transform, "StatDuration",
                $"Match Duration:   {durationStr}", 22, ColSub, FontStyle.Normal,
                new Vector2(0.1f, 0.62f), new Vector2(0.9f, 0.76f), TextAnchor.MiddleLeft);

            CreateLabel(panel.transform, "StatKills",
                $"Enemy Units Killed:   {MatchStats.EnemyUnitsKilled:N0}", 22, ColSub, FontStyle.Normal,
                new Vector2(0.1f, 0.48f), new Vector2(0.9f, 0.62f), TextAnchor.MiddleLeft);

            CreateLabel(panel.transform, "StatCalories",
                $"Calories Gathered:   {MatchStats.CaloriesGathered:N0}", 22, ColSub, FontStyle.Normal,
                new Vector2(0.1f, 0.34f), new Vector2(0.9f, 0.48f), TextAnchor.MiddleLeft);

            // Separator above buttons
            MakeSeparator(panel.transform, sepSprite, new Vector2(0.08f, 0.30f), new Vector2(0.92f, 0.30f));

            // Play Again button
            float btnY = -380f;
            const float btnGap = 70f;
            CreateWoodenButton(panel.transform, btnSprite, ref btnY, btnGap, "PLAY AGAIN", () =>
            {
                Time.timeScale = 1f;
                SceneLoader.LoadSkirmishDemo();
            });

            // Main Menu button
            CreateWoodenButton(panel.transform, btnSprite, ref btnY, btnGap, "MAIN MENU", () =>
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
            t.text = content;
            t.supportRichText = true;
            t.raycastTarget = false;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = ColOutline;
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            return t;
        }

        void MakeSeparator(Transform parent, Sprite sepSprite, Vector2 anchorMin, Vector2 anchorMax)
        {
            var sep = new GameObject("Sep");
            sep.transform.SetParent(parent, false);
            var srt = sep.AddComponent<RectTransform>();
            srt.anchorMin = anchorMin;
            srt.anchorMax = anchorMax;
            srt.sizeDelta = new Vector2(0f, 12f);
            var sImg = sep.AddComponent<Image>();
            sImg.sprite = sepSprite;
            sImg.color = ColSub;
        }

        void CreateWoodenButton(Transform parent, Sprite btnSprite, ref float y, float gap,
            string label, UnityEngine.Events.UnityAction onClick)
        {
            var btnGo = new GameObject(label + "Btn");
            btnGo.transform.SetParent(parent, false);
            var brt = btnGo.AddComponent<RectTransform>();
            brt.anchorMin = brt.anchorMax = brt.pivot = new Vector2(0.5f, 1f);
            brt.anchoredPosition = new Vector2(0, y);
            brt.sizeDelta = new Vector2(360, 60);
            y -= gap;

            var img = btnGo.AddComponent<Image>();
            img.sprite = btnSprite;
            img.color = ColWhite;
            img.type = Image.Type.Sliced;

            var btn = btnGo.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = ColBtnHighlight;
            colors.pressedColor = ColBtnPressed;
            btn.colors = colors;
            btn.onClick.AddListener(onClick);

            var lbl = new GameObject("Label").AddComponent<Text>();
            lbl.transform.SetParent(btnGo.transform, false);
            lbl.font = UiFontHelper.GetFont();
            lbl.fontSize = 22;
            lbl.fontStyle = FontStyle.Bold;
            lbl.color = ColAmber;
            lbl.alignment = TextAnchor.MiddleCenter;
            lbl.text = label;
            lbl.raycastTarget = false;
            var lrt = lbl.rectTransform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;

            var outline = lbl.gameObject.AddComponent<Outline>();
            outline.effectColor = ColOutline;
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }

        void OnDestroy()
        {
            MatchEnded = false;
            if (_state != MatchState.Playing)
                Time.timeScale = 1f;
        }
    }
}
