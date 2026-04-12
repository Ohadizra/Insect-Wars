using InsectWars.Core;
using UnityEngine;
using UnityEngine.UI;

namespace InsectWars.RTS
{
    /// <summary>
    /// SC2-style control group tab strip displayed just above the bottom bar's
    /// center SelectionBlock. Shows numbered tabs for each non-empty group;
    /// clicking a tab recalls that group.
    /// </summary>
    public class ControlGroupBar : MonoBehaviour
    {
        const int GroupCount = 10;
        const float TabWidth = 38f;
        const float TabHeight = 24f;
        const float TabSpacing = 3f;
        const float BarBottomOffset = 263.5f; // matches BottomBar.barHeight
        const float MinimapSlot = 357f;

        static readonly Color ColActive   = new(0.96f, 0.90f, 0.78f, 0.95f);
        static readonly Color ColOccupied = new(0.45f, 0.40f, 0.32f, 0.75f);
        static readonly Color ColEmpty    = new(0.25f, 0.22f, 0.18f, 0.35f);
        static readonly Color ColTextActive   = new(0.12f, 0.10f, 0.08f);
        static readonly Color ColTextNormal   = new(0.83f, 0.69f, 0.44f);
        static readonly Color ColOutline  = new(0.1f, 0.08f, 0.06f, 0.8f);

        Image[] _tabImages;
        Text[] _tabLabels;
        Text[] _tabCounts;
        Font _font;

        void Awake()
        {
            _font = UiFontHelper.GetFont();
        }

        void Start()
        {
            BuildBar();
        }

        void BuildBar()
        {
            var hud = GameHUD.HudCanvasRect;
            if (hud == null) return;

            var strip = new GameObject("ControlGroupStrip");
            strip.transform.SetParent(hud, false);
            var rt = strip.AddComponent<RectTransform>();
            // Position just above the bottom bar, aligned with the SelectionBlock
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            float totalWidth = GroupCount * TabWidth + (GroupCount - 1) * TabSpacing;
            rt.anchoredPosition = new Vector2(MinimapSlot + 8f, BarBottomOffset);
            rt.sizeDelta = new Vector2(totalWidth, TabHeight);

            _tabImages = new Image[GroupCount];
            _tabLabels = new Text[GroupCount];
            _tabCounts = new Text[GroupCount];

            for (int i = 0; i < GroupCount; i++)
            {
                int displayNum = (i + 1) % 10; // 1-9, then 0
                var tab = new GameObject($"CG_{displayNum}");
                tab.transform.SetParent(strip.transform, false);

                var tabRt = tab.AddComponent<RectTransform>();
                tabRt.anchorMin = new Vector2(0f, 0f);
                tabRt.anchorMax = new Vector2(0f, 1f);
                tabRt.pivot = new Vector2(0f, 0f);
                tabRt.anchoredPosition = new Vector2(i * (TabWidth + TabSpacing), 0f);
                tabRt.sizeDelta = new Vector2(TabWidth, 0f);

                var img = tab.AddComponent<Image>();
                img.color = ColEmpty;
                _tabImages[i] = img;

                var btn = tab.AddComponent<Button>();
                var colors = btn.colors;
                colors.highlightedColor = new Color(0.6f, 0.55f, 0.42f, 0.85f);
                colors.pressedColor = new Color(0.85f, 0.75f, 0.65f, 0.9f);
                btn.colors = colors;
                int groupIndex = i;
                btn.onClick.AddListener(() => OnTabClicked(groupIndex));

                // Number label (top-left)
                var numText = CreateText($"Num", tab.transform, 11, ColTextNormal, TextAnchor.UpperLeft);
                numText.text = displayNum.ToString();
                var nrt = numText.rectTransform;
                nrt.anchorMin = Vector2.zero;
                nrt.anchorMax = Vector2.one;
                nrt.offsetMin = new Vector2(3f, 1f);
                nrt.offsetMax = new Vector2(-1f, -1f);
                _tabLabels[i] = numText;

                // Unit count (bottom-right)
                var countText = CreateText($"Count", tab.transform, 9, ColTextNormal, TextAnchor.LowerRight);
                countText.text = "";
                var crt = countText.rectTransform;
                crt.anchorMin = Vector2.zero;
                crt.anchorMax = Vector2.one;
                crt.offsetMin = new Vector2(1f, 0f);
                crt.offsetMax = new Vector2(-3f, -1f);
                _tabCounts[i] = countText;
            }
        }

        Text CreateText(string name, Transform parent, int size, Color color, TextAnchor anchor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = _font;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.raycastTarget = false;
            var rt = t.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = ColOutline;
            outline.effectDistance = new Vector2(1f, -1f);
            return t;
        }

        void OnTabClicked(int index)
        {
            if (ControlGroupManager.Instance == null) return;
            ControlGroupManager.Instance.RecallGroup(index);
        }

        void LateUpdate()
        {
            RefreshTabs();
        }

        void RefreshTabs()
        {
            if (_tabImages == null) return;
            var mgr = ControlGroupManager.Instance;

            for (int i = 0; i < GroupCount; i++)
            {
                int count = 0;
                if (mgr != null)
                {
                    var group = mgr.GetGroup(i);
                    if (group != null) count = group.Count;
                }

                bool isActive = mgr != null && mgr.ActiveGroup == i && count > 0;
                bool isOccupied = count > 0;

                _tabImages[i].color = isActive ? ColActive
                    : isOccupied ? ColOccupied
                    : ColEmpty;

                _tabLabels[i].color = isActive ? ColTextActive : ColTextNormal;
                _tabCounts[i].color = isActive ? ColTextActive : ColTextNormal;
                _tabCounts[i].text = isOccupied ? count.ToString() : "";
            }
        }
    }
}
