using System.Collections.Generic;
using InsectWars.Data;
using InsectWars.RTS;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace InsectWars.UI
{
    /// <summary>
    /// Full-screen spotlight / encyclopedia panel built at runtime.
    /// Shows a 3D turntable preview of each unit and building with stat details.
    /// Instantiated by <see cref="HomeMenuBootstrap"/> as a sub-panel of Play-Ground.
    /// </summary>
    public class SpotlightPanel : MonoBehaviour
    {
        // ── Palette (matches HomeMenuBootstrap) ──
        static readonly Color ColTitle = new(0.96f, 0.90f, 0.78f);
        static readonly Color ColSub   = new(0.83f, 0.69f, 0.44f);
        static readonly Color ColDim   = new(0.18f, 0.15f, 0.12f, 0.92f);
        static readonly Color ColBar   = new(0.55f, 0.78f, 0.32f);
        static readonly Color ColBarBg = new(0.12f, 0.10f, 0.08f, 0.85f);
        static readonly Color ColBtn   = new(0.30f, 0.26f, 0.20f, 0.95f);
        static readonly Color ColBtnHi = new(0.50f, 0.42f, 0.30f, 1f);

        const float DefaultRotSpeed = 40f;
        const float MinRotSpeed = 0f;
        const float MaxRotSpeed = 120f;
        const float PreviewDistance = 4.5f;
        const float PreviewFov = 30f;
        const float MinZoom = 0.4f;
        const float MaxZoom = 3f;
        const float MinPitch = 0.05f;
        const float MaxPitch = 0.85f;
        static readonly Vector3 PreviewWorldOrigin = new(500f, 0f, 500f);

        UnitVisualLibrary _lib;
        Font _font;
        System.Action _onBack;

        RectTransform _root;
        GameObject _previewGo;
        Camera _previewCam;
        RenderTexture _previewRT;
        RawImage _previewImage;
        Text _nameLabel, _descLabel, _statsLabel;
        float _orbitAngle;
        float _previewHeight = 1f;
        float _rotSpeed = DefaultRotSpeed;
        float _zoomFactor = 1f;
        float _pitchFactor = 0.6f;
        Slider _speedSlider;
        Text _speedLabel;

        readonly List<Button> _unitButtons = new();
        readonly List<Button> _buildingButtons = new();
        Button _activeBtn;

        enum Tab { Units, Buildings }
        Tab _tab = Tab.Units;
        GameObject _unitTabContent, _buildingTabContent;
        Button _unitTabBtn, _buildingTabBtn;

        public static SpotlightPanel Create(Transform canvasRoot, UnitVisualLibrary lib, Font font, Sprite btnSprite, System.Action onBack)
        {
            var go = new GameObject("SpotlightPanel");
            go.transform.SetParent(canvasRoot, false);
            var rt = go.AddComponent<RectTransform>();
            Stretch(rt);
            go.SetActive(false);

            var sp = go.AddComponent<SpotlightPanel>();
            sp._lib = lib;
            sp._font = font;
            sp._onBack = onBack;
            sp._root = rt;
            sp.Build(btnSprite);
            return sp;
        }

        void Build(Sprite btnSprite)
        {
            // ── dim background ──
            var dim = MakeChild("Dim", _root);
            Stretch(dim);
            dim.gameObject.AddComponent<Image>().color = ColDim;

            // ── layout: left = preview, right = details ──
            var container = MakeChild("Container", _root);
            container.anchorMin = new Vector2(0.03f, 0.03f);
            container.anchorMax = new Vector2(0.97f, 0.97f);
            container.offsetMin = container.offsetMax = Vector2.zero;

            BuildPreviewArea(container);
            BuildDetailsArea(container, btnSprite);

            SetupPreviewCamera();
            ShowUnit(UnitArchetype.Worker);
        }

        // ────────────────── Preview (left half) ──────────────────

        void BuildPreviewArea(RectTransform parent)
        {
            var area = MakeChild("PreviewArea", parent);
            area.anchorMin = Vector2.zero;
            area.anchorMax = new Vector2(0.48f, 1f);
            area.offsetMin = area.offsetMax = Vector2.zero;

            // border frame
            var frame = area.gameObject.AddComponent<Image>();
            frame.color = new Color(0.25f, 0.22f, 0.18f, 0.6f);

            _previewRT = new RenderTexture(512, 512, 24);
            _previewRT.Create();

            var imgGo = MakeChild("PreviewImg", area);
            imgGo.anchorMin = new Vector2(0.02f, 0.02f);
            imgGo.anchorMax = new Vector2(0.98f, 0.98f);
            imgGo.offsetMin = imgGo.offsetMax = Vector2.zero;
            _previewImage = imgGo.gameObject.AddComponent<RawImage>();
            _previewImage.texture = _previewRT;
            _previewImage.color = Color.white;

            var hint = Txt(area, "LMB rotate · RMB pitch · Scroll zoom", 12, ColSub, TextAnchor.LowerCenter);
            var hrt = hint.rectTransform;
            hrt.anchorMin = new Vector2(0f, 0f);
            hrt.anchorMax = new Vector2(1f, 0f);
            hrt.pivot = new Vector2(0.5f, 0f);
            hrt.anchoredPosition = new Vector2(0, 8f);
            hrt.sizeDelta = new Vector2(0, 24);

            BuildSpeedSlider(area);
        }

        void BuildSpeedSlider(RectTransform parent)
        {
            var row = MakeChild("SpeedRow", parent);
            row.anchorMin = new Vector2(0.05f, 0.93f);
            row.anchorMax = new Vector2(0.95f, 0.98f);
            row.offsetMin = row.offsetMax = Vector2.zero;

            _speedLabel = Txt(row, "Speed", 12, ColSub, TextAnchor.MiddleLeft);
            var slrt = _speedLabel.rectTransform;
            slrt.anchorMin = Vector2.zero;
            slrt.anchorMax = new Vector2(0.22f, 1f);
            slrt.offsetMin = slrt.offsetMax = Vector2.zero;

            var sliderGo = new GameObject("SpeedSlider");
            sliderGo.transform.SetParent(row, false);
            var srt = sliderGo.AddComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.23f, 0.2f);
            srt.anchorMax = new Vector2(1f, 0.8f);
            srt.offsetMin = srt.offsetMax = Vector2.zero;

            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(sliderGo.transform, false);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.15f, 0.12f, 0.10f, 0.8f);
            var bgRt = bgImg.rectTransform;
            bgRt.anchorMin = new Vector2(0f, 0.35f);
            bgRt.anchorMax = new Vector2(1f, 0.65f);
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;

            var fillArea = MakeChild("FillArea", sliderGo.GetComponent<RectTransform>());
            fillArea.anchorMin = new Vector2(0f, 0.25f);
            fillArea.anchorMax = new Vector2(1f, 0.75f);
            fillArea.offsetMin = new Vector2(5f, 0f);
            fillArea.offsetMax = new Vector2(-5f, 0f);
            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(fillArea, false);
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.color = ColSub;
            var fillRt = fillImg.rectTransform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;

            var handleArea = MakeChild("HandleSlideArea", sliderGo.GetComponent<RectTransform>());
            handleArea.anchorMin = Vector2.zero;
            handleArea.anchorMax = Vector2.one;
            handleArea.offsetMin = new Vector2(5f, 0f);
            handleArea.offsetMax = new Vector2(-5f, 0f);
            var handleGo = new GameObject("Handle");
            handleGo.transform.SetParent(handleArea, false);
            var handleImg = handleGo.AddComponent<Image>();
            handleImg.color = ColTitle;
            var handleRt = handleImg.rectTransform;
            handleRt.sizeDelta = new Vector2(10f, 0f);
            handleRt.anchorMin = new Vector2(0f, 0f);
            handleRt.anchorMax = new Vector2(0f, 1f);

            _speedSlider = sliderGo.AddComponent<Slider>();
            _speedSlider.fillRect = fillRt;
            _speedSlider.handleRect = handleRt;
            _speedSlider.targetGraphic = handleImg;
            _speedSlider.minValue = MinRotSpeed;
            _speedSlider.maxValue = MaxRotSpeed;
            _speedSlider.value = _rotSpeed;
            _speedSlider.onValueChanged.AddListener(v =>
            {
                _rotSpeed = v;
                UpdateSpeedLabel();
            });
            UpdateSpeedLabel();
        }

        void UpdateSpeedLabel()
        {
            if (_speedLabel != null)
                _speedLabel.text = _rotSpeed < 1f ? "Paused" : $"Speed {_rotSpeed:0}";
        }

        // ────────────────── Details (right half) ──────────────────

        void BuildDetailsArea(RectTransform parent, Sprite btnSprite)
        {
            var area = MakeChild("DetailsArea", parent);
            area.anchorMin = new Vector2(0.50f, 0f);
            area.anchorMax = Vector2.one;
            area.offsetMin = area.offsetMax = Vector2.zero;

            // tab row (below the back button)
            var tabRow = MakeChild("TabRow", area);
            tabRow.anchorMin = new Vector2(0f, 0.85f);
            tabRow.anchorMax = new Vector2(1f, 0.92f);
            tabRow.offsetMin = tabRow.offsetMax = Vector2.zero;

            _unitTabBtn = MakeTabButton(tabRow, "UNITS", 0f, 0.48f, btnSprite, () => SwitchTab(Tab.Units));
            _buildingTabBtn = MakeTabButton(tabRow, "BUILDINGS", 0.52f, 1f, btnSprite, () => SwitchTab(Tab.Buildings));

            // name
            _nameLabel = Txt(area, "", 32, ColTitle, TextAnchor.UpperLeft);
            _nameLabel.fontStyle = FontStyle.Bold;
            var nrt = _nameLabel.rectTransform;
            nrt.anchorMin = new Vector2(0.02f, 0.76f);
            nrt.anchorMax = new Vector2(0.98f, 0.84f);
            nrt.offsetMin = nrt.offsetMax = Vector2.zero;

            // description
            _descLabel = Txt(area, "", 16, ColSub, TextAnchor.UpperLeft);
            var drt = _descLabel.rectTransform;
            drt.anchorMin = new Vector2(0.02f, 0.62f);
            drt.anchorMax = new Vector2(0.98f, 0.76f);
            drt.offsetMin = drt.offsetMax = Vector2.zero;

            // stats
            _statsLabel = Txt(area, "", 18, ColTitle, TextAnchor.UpperLeft);
            var srt = _statsLabel.rectTransform;
            srt.anchorMin = new Vector2(0.02f, 0.18f);
            srt.anchorMax = new Vector2(0.98f, 0.62f);
            srt.offsetMin = srt.offsetMax = Vector2.zero;

            // item buttons (unit list)
            _unitTabContent = new GameObject("UnitList");
            _unitTabContent.transform.SetParent(area, false);
            var ulrt = _unitTabContent.AddComponent<RectTransform>();
            ulrt.anchorMin = new Vector2(0f, 0f);
            ulrt.anchorMax = new Vector2(1f, 0.17f);
            ulrt.offsetMin = ulrt.offsetMax = Vector2.zero;

            float btnW = 1f / 6f;
            int idx = 0;
            foreach (UnitArchetype arch in System.Enum.GetValues(typeof(UnitArchetype)))
            {
                var a = arch;
                var btn = MakeItemButton(ulrt, ProductionBuilding.GetUnitName(arch), idx * btnW, (idx + 1) * btnW, btnSprite, () => ShowUnit(a));
                _unitButtons.Add(btn);
                idx++;
            }

            // item buttons (building list)
            _buildingTabContent = new GameObject("BuildingList");
            _buildingTabContent.transform.SetParent(area, false);
            var blrt = _buildingTabContent.AddComponent<RectTransform>();
            blrt.anchorMin = new Vector2(0f, 0f);
            blrt.anchorMax = new Vector2(1f, 0.17f);
            blrt.offsetMin = blrt.offsetMax = Vector2.zero;

            BuildingType[] buildings = { BuildingType.Hive, BuildingType.Underground, BuildingType.AntNest, BuildingType.SkyTower, BuildingType.RootCellar };
            float bbtnW = 1f / buildings.Length;
            for (int i = 0; i < buildings.Length; i++)
            {
                var bt = buildings[i];
                var btn = MakeItemButton(blrt, ProductionBuilding.GetDisplayName(bt), i * bbtnW, (i + 1) * bbtnW, btnSprite, () => ShowBuilding(bt));
                _buildingButtons.Add(btn);
            }

            // back button
            var backRt = MakeChild("BackBtn", area);
            backRt.anchorMin = new Vector2(0.75f, 0.92f);
            backRt.anchorMax = new Vector2(1f, 1f);
            backRt.offsetMin = backRt.offsetMax = Vector2.zero;
            var backImg = backRt.gameObject.AddComponent<Image>();
            backImg.sprite = btnSprite;
            backImg.color = Color.white;
            backImg.type = Image.Type.Sliced;
            var backBtn = backRt.gameObject.AddComponent<Button>();
            var bcols = backBtn.colors;
            bcols.highlightedColor = new Color(1, 0.9f, 0.7f, 1f);
            bcols.pressedColor = new Color(0.8f, 0.7f, 0.5f, 1f);
            backBtn.colors = bcols;
            backBtn.onClick.AddListener(() => _onBack?.Invoke());
            var btxt = Txt(backRt, "BACK", 18, ColTitle, TextAnchor.MiddleCenter);
            btxt.fontStyle = FontStyle.Bold;
            Stretch(btxt.rectTransform);

            SwitchTab(Tab.Units);
        }

        // ────────────────── 3-D Preview ──────────────────

        void SetupPreviewCamera()
        {
            var camGo = new GameObject("SpotlightCam");
            camGo.transform.position = PreviewWorldOrigin + new Vector3(0, 1.5f, -PreviewDistance);
            camGo.transform.LookAt(PreviewWorldOrigin + Vector3.up * 1f);
            _previewCam = camGo.AddComponent<Camera>();
            _previewCam.targetTexture = _previewRT;
            _previewCam.fieldOfView = PreviewFov;
            _previewCam.clearFlags = CameraClearFlags.SolidColor;
            _previewCam.backgroundColor = new Color(0.08f, 0.07f, 0.06f, 1f);
            _previewCam.cullingMask = LayerMask.GetMask("Default");
            _previewCam.nearClipPlane = 0.1f;
            _previewCam.farClipPlane = 100f;
            _previewCam.depth = -10;
            _previewCam.enabled = false;

            var lightGo = new GameObject("SpotlightLight");
            lightGo.transform.SetParent(camGo.transform, false);
            lightGo.transform.localPosition = new Vector3(2f, 4f, -2f);
            lightGo.transform.LookAt(PreviewWorldOrigin + Vector3.up);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.color = new Color(1f, 0.95f, 0.88f);

            var fillGo = new GameObject("FillLight");
            fillGo.transform.SetParent(camGo.transform, false);
            fillGo.transform.localPosition = new Vector3(-3f, 2f, 1f);
            fillGo.transform.LookAt(PreviewWorldOrigin + Vector3.up * 0.5f);
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.5f;
            fill.color = new Color(0.7f, 0.8f, 1f);
        }

        void SpawnPreviewModel(GameObject prefab, float heightOffset)
        {
            ClearPreview();
            if (prefab == null) return;

            _previewGo = Instantiate(prefab);
            _previewGo.name = "SpotlightPreview";
            _previewGo.transform.position = PreviewWorldOrigin + Vector3.up * heightOffset;
            _previewGo.transform.rotation = Quaternion.identity;

            // strip gameplay components so it's just a visual model
            foreach (var nav in _previewGo.GetComponentsInChildren<UnityEngine.AI.NavMeshAgent>(true))
                nav.enabled = false;
            foreach (var rb in _previewGo.GetComponentsInChildren<Rigidbody>(true))
                rb.isKinematic = true;
            foreach (var col in _previewGo.GetComponentsInChildren<Collider>(true))
                col.enabled = false;
            var unit = _previewGo.GetComponent<InsectUnit>();
            if (unit) Destroy(unit);
            var healthBar = _previewGo.GetComponentInChildren<UnitHealthBar>(true);
            if (healthBar) Destroy(healthBar.gameObject);
            var building = _previewGo.GetComponent<ProductionBuilding>();
            if (building) Destroy(building);
            var buildingHb = _previewGo.GetComponentInChildren<BuildingHealthBar>(true);
            if (buildingHb) Destroy(buildingHb.gameObject);
            var hive = _previewGo.GetComponent<HiveDeposit>();
            if (hive) Destroy(hive);

            // play idle animation if available
            var anim = _previewGo.GetComponentInChildren<Animator>(true);
            if (anim != null)
            {
                anim.updateMode = AnimatorUpdateMode.UnscaledTime;
                anim.applyRootMotion = false;
            }

            _previewHeight = EstimateModelHeight(_previewGo);
            _orbitAngle = 0f;
            _zoomFactor = 1f;
            _pitchFactor = 0.6f;
            UpdateCameraOrbit();
        }

        void ClearPreview()
        {
            if (_previewGo != null)
            {
                Destroy(_previewGo);
                _previewGo = null;
            }
        }

        float EstimateModelHeight(GameObject go)
        {
            float maxY = 1f;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                float top = r.bounds.max.y - PreviewWorldOrigin.y;
                if (top > maxY) maxY = top;
            }
            return maxY;
        }

        void UpdateCameraOrbit()
        {
            if (_previewCam == null) return;
            float rad = _orbitAngle * Mathf.Deg2Rad;
            float camHeight = _previewHeight * _pitchFactor;
            float baseDist = Mathf.Max(PreviewDistance, _previewHeight * 2.2f);
            float dist = baseDist / _zoomFactor;
            var offset = new Vector3(Mathf.Sin(rad) * dist, camHeight, Mathf.Cos(rad) * dist);
            _previewCam.transform.position = PreviewWorldOrigin + offset;
            _previewCam.transform.LookAt(PreviewWorldOrigin + Vector3.up * camHeight * 0.6f);
        }

        // ────────────────── Show Unit / Building ──────────────────

        void ShowUnit(UnitArchetype arch)
        {
            SwitchTab(Tab.Units);
            HighlightButton(_unitButtons, (int)arch);

            var def = UnitDefinition.CreateRuntimeDefault(arch, Color.white);
            _nameLabel.text = ProductionBuilding.GetUnitName(arch).ToUpper();

            string role = arch switch
            {
                UnitArchetype.Worker => "Gatherer & builder. The backbone of your colony.",
                UnitArchetype.BasicFighter => "Melee assault. Fast and aggressive frontline fighter.",
                UnitArchetype.BasicRanged => "Ranged bombardier. Sprays acid from a distance.",
                UnitArchetype.BlackWidow => "Elite assassin. Powerful but expensive.",
                UnitArchetype.StickSpy => "Invisible scout. Cannot attack but reveals the fog of war.",
                UnitArchetype.GiantStagBeetle => "Heavy tank. Massive HP and devastating mandible strikes.",
                _ => ""
            };
            _descLabel.text = role;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"<color=#D4A849>HP</color>          {def.maxHealth:F0}");
            sb.AppendLine($"<color=#D4A849>Damage</color>      {def.attackDamage:F0}");
            sb.AppendLine($"<color=#D4A849>Range</color>       {def.attackRange:F1}");
            sb.AppendLine($"<color=#D4A849>Cooldown</color>    {def.attackCooldown:F2}s");
            sb.AppendLine($"<color=#D4A849>Speed</color>       {def.moveSpeed:F1}");
            sb.AppendLine($"<color=#D4A849>Vision</color>      {def.visionRadius:F0}");
            sb.AppendLine($"<color=#D4A849>Cost</color>        {ProductionBuilding.GetUnitCost(arch)} cal");
            sb.AppendLine($"<color=#D4A849>Train Time</color>  {ProductionBuilding.GetBuildTime(arch):F0}s");
            sb.AppendLine($"<color=#D4A849>Supply</color>      {ColonyCapacity.GetUnitCCCost(arch)}");
            if (def.canGather) sb.AppendLine("<color=#8BC34A>Can Gather</color>");
            if (!def.canAttack) sb.AppendLine("<color=#FF7043>Cannot Attack</color>");
            _statsLabel.text = sb.ToString();

            var prefab = _lib != null ? _lib.GetUnitPrefab(arch) : null;
            SpawnPreviewModel(prefab, 0f);
            DestroyImmediate(def);
        }

        void ShowBuilding(BuildingType type)
        {
            SwitchTab(Tab.Buildings);
            BuildingType[] order = { BuildingType.Hive, BuildingType.Underground, BuildingType.AntNest, BuildingType.SkyTower, BuildingType.RootCellar };
            int idx = System.Array.IndexOf(order, type);
            HighlightButton(_buildingButtons, idx);

            _nameLabel.text = ProductionBuilding.GetDisplayName(type).ToUpper();
            _descLabel.text = ProductionBuilding.GetDescription(type);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"<color=#D4A849>HP</color>              {ProductionBuilding.GetMaxHealth(type):F0}");
            sb.AppendLine($"<color=#D4A849>Build Cost</color>      {ProductionBuilding.GetBuildCost(type)} cal");
            sb.AppendLine($"<color=#D4A849>Build Time</color>      {ProductionBuilding.GetConstructionTime(type):F0}s");
            sb.AppendLine($"<color=#D4A849>Footprint</color>       {ProductionBuilding.GetFootprintRadius(type):F1}");

            var tempBuilding = ScriptableObject.CreateInstance<ProductionBuildingProxy>();
            tempBuilding.type = type;
            var trainable = tempBuilding.GetProducibleUnits();
            if (trainable.Length > 0)
            {
                sb.Append("\n<color=#D4A849>Trains:</color>  ");
                for (int i = 0; i < trainable.Length; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(ProductionBuilding.GetUnitName(trainable[i]));
                }
                sb.AppendLine();
            }
            DestroyImmediate(tempBuilding);

            _statsLabel.text = sb.ToString();

            GameObject prefab = null;
            if (_lib != null)
            {
                prefab = type == BuildingType.Hive ? _lib.hivePrefab : _lib.GetBuildingPrefab(type);
            }
            SpawnPreviewModel(prefab, 0f);
        }

        // ────────────────── Tabs ──────────────────

        void SwitchTab(Tab tab)
        {
            _tab = tab;
            _unitTabContent.SetActive(tab == Tab.Units);
            _buildingTabContent.SetActive(tab == Tab.Buildings);

            SetTabHighlight(_unitTabBtn, tab == Tab.Units);
            SetTabHighlight(_buildingTabBtn, tab == Tab.Buildings);
        }

        static void SetTabHighlight(Button btn, bool active)
        {
            var img = btn.GetComponent<Image>();
            if (img) img.color = active ? new Color(0.45f, 0.38f, 0.28f, 1f) : new Color(0.25f, 0.22f, 0.18f, 0.8f);
        }

        void HighlightButton(List<Button> buttons, int idx)
        {
            for (int i = 0; i < buttons.Count; i++)
            {
                var img = buttons[i].GetComponent<Image>();
                if (img) img.color = i == idx ? ColBtnHi : ColBtn;
            }
        }

        // ────────────────── Update ──────────────────

        void OnEnable()
        {
            if (_previewCam != null) _previewCam.enabled = true;
        }

        void OnDisable()
        {
            if (_previewCam != null) _previewCam.enabled = false;
        }

        void OnDestroy()
        {
            ClearPreview();
            if (_previewCam != null) Destroy(_previewCam.gameObject);
            if (_previewRT != null) _previewRT.Release();
        }

        void Update()
        {
            if (_previewGo == null) return;

            var mouse = Mouse.current;
            bool overPreview = mouse != null &&
                RectTransformUtility.RectangleContainsScreenPoint(
                    _previewImage.rectTransform, mouse.position.ReadValue(), null);

            if (mouse != null && overPreview)
            {
                var delta = mouse.delta.ReadValue();

                if (mouse.leftButton.isPressed)
                    _orbitAngle -= delta.x * 0.3f;
                else
                    _orbitAngle += _rotSpeed * Time.unscaledDeltaTime;

                if (mouse.rightButton.isPressed)
                    _pitchFactor = Mathf.Clamp(_pitchFactor - delta.y * 0.004f, MinPitch, MaxPitch);

                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                    _zoomFactor = Mathf.Clamp(_zoomFactor + scroll * 0.001f, MinZoom, MaxZoom);
            }
            else
            {
                _orbitAngle += _rotSpeed * Time.unscaledDeltaTime;
            }

            UpdateCameraOrbit();
        }

        // ────────────────── UI Factory Helpers ──────────────────

        Button MakeTabButton(RectTransform parent, string label, float xMin, float xMax, Sprite spr, UnityEngine.Events.UnityAction onClick)
        {
            var rt = MakeChild(label, parent);
            rt.anchorMin = new Vector2(xMin, 0f);
            rt.anchorMax = new Vector2(xMax, 1f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = spr;
            img.color = ColBtn;
            img.type = Image.Type.Sliced;

            var btn = rt.gameObject.AddComponent<Button>();
            var cols = btn.colors;
            cols.highlightedColor = ColBtnHi;
            cols.pressedColor = new Color(0.4f, 0.35f, 0.25f, 1f);
            btn.colors = cols;
            btn.onClick.AddListener(onClick);

            var txt = Txt(rt, label, 18, ColTitle, TextAnchor.MiddleCenter);
            txt.fontStyle = FontStyle.Bold;
            Stretch(txt.rectTransform);
            return btn;
        }

        Button MakeItemButton(RectTransform parent, string label, float xMin, float xMax, Sprite spr, UnityEngine.Events.UnityAction onClick)
        {
            var rt = MakeChild(label, parent);
            rt.anchorMin = new Vector2(xMin + 0.005f, 0.05f);
            rt.anchorMax = new Vector2(xMax - 0.005f, 0.95f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = spr;
            img.color = ColBtn;
            img.type = Image.Type.Sliced;

            var btn = rt.gameObject.AddComponent<Button>();
            var cols = btn.colors;
            cols.highlightedColor = ColBtnHi;
            cols.pressedColor = new Color(0.4f, 0.35f, 0.25f, 1f);
            btn.colors = cols;
            btn.onClick.AddListener(onClick);

            var txt = Txt(rt, label, 13, ColTitle, TextAnchor.MiddleCenter);
            txt.fontStyle = FontStyle.Bold;
            Stretch(txt.rectTransform);
            return btn;
        }

        Text Txt(RectTransform parent, string text, int size, Color color, TextAnchor anchor)
        {
            return Txt((Transform)parent, text, size, color, anchor);
        }

        Text Txt(Transform parent, string text, int size, Color color, TextAnchor anchor)
        {
            var go = new GameObject("T");
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = _font;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.text = text;
            t.supportRichText = true;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0.1f, 0.08f, 0.06f, 0.8f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            return t;
        }

        static RectTransform MakeChild(string name, RectTransform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<RectTransform>();
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// Tiny helper to get the producible units array without a live <see cref="ProductionBuilding"/> instance.
        /// </summary>
        class ProductionBuildingProxy : ScriptableObject
        {
            public BuildingType type;
            public UnitArchetype[] GetProducibleUnits() => type switch
            {
                BuildingType.Underground => new[] { UnitArchetype.BasicFighter, UnitArchetype.BasicRanged, UnitArchetype.GiantStagBeetle },
                BuildingType.AntNest => new[] { UnitArchetype.Worker },
                BuildingType.Hive => new[] { UnitArchetype.Worker },
                BuildingType.SkyTower => new[] { UnitArchetype.BlackWidow, UnitArchetype.StickSpy },
                _ => System.Array.Empty<UnitArchetype>()
            };
        }
    }
}
