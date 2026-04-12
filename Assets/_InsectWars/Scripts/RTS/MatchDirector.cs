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

        float _matchTimer;
        const float MatchStartupGrace = 4.0f;

        void OnEnable()
        {
            HiveDeposit.OnDestroyed += OnHiveDestroyed;
        }

        void OnDisable()
        {
            HiveDeposit.OnDestroyed -= OnHiveDestroyed;
        }

        void OnHiveDestroyed(HiveDeposit hive)
        {
            if (_state != MatchState.Playing) return;
            if (hive.Team == Team.Player)
            {
                Debug.Log("[MatchDirector] Defeat: Player hive destroyed.");
                EndMatch(MatchState.Lost);
            }
            else if (hive.Team == Team.Enemy)
            {
                Debug.Log("[MatchDirector] Victory: Enemy hive destroyed.");
                EndMatch(MatchState.Won);
            }
        }

        void Update()
        {
            if (_state != MatchState.Playing) return;
            if (PauseController.IsPaused) return;

            _matchTimer += Time.deltaTime;
            if (_matchTimer < MatchStartupGrace) return;

            if (RtsSimRegistry.CountAlive(Team.Enemy) == 0 && HiveDeposit.EnemyHive == null)
            {
                Debug.Log("[MatchDirector] Victory: No Enemy combatants or hive found.");
                EndMatch(MatchState.Won);
            }
            else if (RtsSimRegistry.CountAlive(Team.Player) == 0 && HiveDeposit.PlayerHive == null)
            {
                Debug.Log("[MatchDirector] Defeat: No Player combatants or hive found.");
                EndMatch(MatchState.Lost);
            }
        }

        void EndMatch(MatchState end)
        {
            _state = end;
            MatchEnded = true;
            bool won = end == MatchState.Won;
            BuildOverlay(won);
            Time.timeScale = 0f;
            GameAudio.PlayUi(won ? GameAudio.UiKind.MatchVictory : GameAudio.UiKind.MatchDefeat);
        }

        void BuildOverlay(bool victory)
        {
            if (_overlayRoot != null) return;
            var parent = GameHUD.HudCanvasRect;
            if (parent == null) return;

            // Full-screen dim
            _overlayRoot = new GameObject("MatchOverlay");
            _overlayRoot.transform.SetParent(parent, false);
            Stretch(_overlayRoot.AddComponent<RectTransform>());
            var dim = _overlayRoot.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.6f);
            dim.raycastTarget = true;

            // Centered popup panel
            var panel = new GameObject("PopupPanel");
            panel.transform.SetParent(_overlayRoot.transform, false);
            var prt = panel.AddComponent<RectTransform>();
            prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(480, 300);
            var panelImg = panel.AddComponent<Image>();
            panelImg.color = new Color(0.12f, 0.10f, 0.16f, 0.95f);

            // Accent stripe across the top of the panel
            var stripe = new GameObject("Stripe");
            stripe.transform.SetParent(panel.transform, false);
            var srt = stripe.AddComponent<RectTransform>();
            srt.anchorMin = new Vector2(0f, 1f);
            srt.anchorMax = Vector2.one;
            srt.pivot = new Vector2(0.5f, 1f);
            srt.sizeDelta = new Vector2(0f, 6f);
            var stripeImg = stripe.AddComponent<Image>();
            stripeImg.color = victory
                ? new Color(0.85f, 0.75f, 0.25f)
                : new Color(0.75f, 0.22f, 0.22f);
            stripeImg.raycastTarget = false;

            // Title
            var title = CreateOverlayText("Title", panel.transform,
                new Vector2(0.5f, 0.75f), new Vector2(440, 80),
                victory ? "VICTORY" : "DEFEAT", 54,
                victory ? new Color(1f, 0.92f, 0.5f) : new Color(1f, 0.45f, 0.4f));

            // Subtitle
            CreateOverlayText("Subtitle", panel.transform,
                new Vector2(0.5f, 0.52f), new Vector2(400, 50),
                victory ? "The enemy colony has been crushed!" : "Your colony has fallen...", 22,
                new Color(0.78f, 0.74f, 0.68f));

            // Main Menu button
            BuildMenuButton(panel.transform, new Vector2(0.5f, 0.2f), new Vector2(240, 52));
        }

        Text CreateOverlayText(string name, Transform parent, Vector2 anchor, Vector2 size,
            string content, int fontSize, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.sizeDelta = size;
            var txt = go.AddComponent<Text>();
            txt.font = UiFontHelper.GetFont();
            txt.fontSize = fontSize;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = color;
            txt.text = content;
            txt.raycastTarget = false;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.7f);
            outline.effectDistance = new Vector2(2f, -2f);
            return txt;
        }

        void BuildMenuButton(Transform parent, Vector2 anchor, Vector2 size)
        {
            var btnGo = new GameObject("MenuBtn");
            btnGo.transform.SetParent(parent, false);
            var brt = btnGo.AddComponent<RectTransform>();
            brt.anchorMin = brt.anchorMax = brt.pivot = anchor;
            brt.sizeDelta = size;

            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = new Color(0.28f, 0.55f, 0.22f, 1f);

            var btn = btnGo.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.35f, 0.65f, 0.28f);
            colors.pressedColor = new Color(0.20f, 0.42f, 0.16f);
            btn.colors = colors;

            var lbl = new GameObject("Lbl").AddComponent<Text>();
            lbl.transform.SetParent(btnGo.transform, false);
            lbl.font = UiFontHelper.GetFont();
            lbl.fontSize = 24;
            lbl.color = Color.white;
            lbl.alignment = TextAnchor.MiddleCenter;
            lbl.text = "Main Menu";
            Stretch(lbl.rectTransform);

            btn.onClick.AddListener(() =>
            {
                Time.timeScale = 1f;
                SceneLoader.LoadHome();
            });
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        void OnDestroy()
        {
            MatchEnded = false;
            if (_state != MatchState.Playing)
                Time.timeScale = 1f;
        }
    }
}
