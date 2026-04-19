using System.Collections.Generic;
using InsectWars.Data;
using UnityEngine;
using UnityEngine.Rendering;

namespace InsectWars.RTS
{
    /// <summary>
    /// SC2-style fog of war.
    /// Fog texture: R = ever explored, G = current vision (this frame).
    /// Player-side: enemy units hidden outside current vision; enemy buildings stay
    /// visible once explored (dimmed by shader) and disappear only when destroyed
    /// AND re-explored.
    /// Enemy-side: lightweight intel tracking so the AI must scout to discover the
    /// player base instead of map-hacking.
    /// </summary>
    public class FogOfWarSystem : MonoBehaviour
    {
        public static FogOfWarSystem Instance { get; private set; }

        public Texture2D FogTexture => _tex;

        const int TexRes = 256;
        const float HighGroundThreshold = 0.5f;
        const float CloakDetectionRange = 8f;

        public static readonly int FogTexId = Shader.PropertyToID("_IW_FogWarTex");
        static readonly int FogBoundsId = Shader.PropertyToID("_IW_FogBounds");
        static readonly int FogActiveId = Shader.PropertyToID("_IW_FogActive");

        [SerializeField] float hiveVisionRadius = 16f;
        [SerializeField] float buildingVisionRadius = 14f;
        [SerializeField] float visionFalloffWorld = 2.2f;

        Texture2D _tex;
        Color32[] _pix;
        Color32 _black;

        // ──── Player-side: enemy unit renderer cache ────
        readonly Dictionary<InsectUnit, Renderer[]> _enemyRenderers = new();

        // ──── Player-side: enemy building/hive renderer cache ────
        readonly Dictionary<ProductionBuilding, Renderer[]> _enemyBuildingRenderers = new();
        Renderer[] _enemyHiveRenderers;
        HiveDeposit _trackedEnemyHive;

        // SC2-style ghost tracking: buildings that were alive last time we had vision.
        // When we re-explore and the building is gone, we finally hide the renderers.
        readonly HashSet<ProductionBuilding> _knownAliveBuildings = new();
        bool _enemyHiveKnownAlive;

        // ──── Enemy-side intel (for AI fairness) ────
        Vector3? _knownPlayerHivePos;
        public Vector3? KnownPlayerHivePos => _knownPlayerHivePos;
        public bool PlayerHiveDiscovered => _knownPlayerHivePos.HasValue;

        void OnEnable()
        {
            Instance = this;
            Shader.SetGlobalFloat(FogActiveId, 1f);
            _black = new Color32(0, 0, 0, 255);
            _pix = new Color32[TexRes * TexRes];
            for (var i = 0; i < _pix.Length; i++)
                _pix[i] = _black;
            _tex = new Texture2D(TexRes, TexRes, TextureFormat.RGB24, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "FogOfWarData"
            };
            _tex.SetPixels32(_pix);
            _tex.Apply(false, false);
            Shader.SetGlobalTexture(FogTexId, _tex);
            PushBounds();
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            if (Instance == this) Instance = null;
            Shader.SetGlobalFloat(FogActiveId, 0f);
            _enemyRenderers.Clear();
            _enemyBuildingRenderers.Clear();
            _enemyHiveRenderers = null;
            _trackedEnemyHive = null;
            _knownAliveBuildings.Clear();
            _enemyHiveKnownAlive = false;
            _knownPlayerHivePos = null;
            if (_tex != null)
                Destroy(_tex);
            _tex = null;
            _pix = null;
        }

        void OnBeginCameraRendering(ScriptableRenderContext ctx, Camera cam)
        {
            if (cam == null || !cam.CompareTag("MainCamera")) return;
            if (!SkirmishPlayArea.HasBounds || _tex == null || _pix == null) return;

            PushBounds();
            if ((Time.frameCount & 1) == 0)
            {
                RebuildVisionGrid();
                _tex.SetPixels32(_pix);
                _tex.Apply(false, false);
            }

            UpdateEnemyVisibility();
            UpdateEnemyBuildingVisibility();
            UpdateEnemyIntel();
        }

        // ═══════════════════════════════════════════════════════
        //  PLAYER FOG GRID
        // ═══════════════════════════════════════════════════════

        void RebuildVisionGrid()
        {
            for (var i = 0; i < _pix.Length; i++)
                _pix[i].g = 0;

            foreach (var u in RtsSimRegistry.Units)
            {
                if (u == null || !u.IsAlive || u.Team != Team.Player) continue;
                var r = u.Definition != null ? u.Definition.visionRadius : 12f;
                float visMult = TerrainFeatureRegistry.GetVisionMultiplier(u.transform.position);
                bool canSeeHighGround = u.Archetype == UnitArchetype.StickSpy || IsOnHighGround(u.transform.position);
                StampVision(u.transform.position, r * visMult, canSeeHighGround);
            }

            if (HiveDeposit.PlayerHive != null)
            {
                bool hiveOnHigh = IsOnHighGround(HiveDeposit.PlayerHive.transform.position);
                StampVision(HiveDeposit.PlayerHive.transform.position, hiveVisionRadius, hiveOnHigh);
            }

            foreach (var bld in ProductionBuilding.All)
            {
                if (bld == null || !bld.IsAlive || bld.Team != Team.Player) continue;
                bool bldOnHigh = IsOnHighGround(bld.transform.position);
                StampVision(bld.transform.position, buildingVisionRadius, bldOnHigh);
            }

            for (var i = 0; i < _pix.Length; i++)
            {
                if (_pix[i].g > _pix[i].r)
                    _pix[i].r = _pix[i].g;
            }
        }

        public static bool IsOnHighGround(Vector3 pos)
        {
            var t = Terrain.activeTerrain;
            if (t == null) return false;
            return t.SampleHeight(pos) > HighGroundThreshold;
        }

        /// <summary>
        /// True if an observer can see a target considering high-ground elevation.
        /// Low-ground observers cannot see high-ground targets unless they are StickSpy.
        /// </summary>
        public static bool CanSeeOverHighGround(Vector3 observerPos, Vector3 targetPos, UnitArchetype archetype)
        {
            if (!IsOnHighGround(targetPos)) return true;
            if (archetype == UnitArchetype.StickSpy) return true;
            return IsOnHighGround(observerPos);
        }

        // ═══════════════════════════════════════════════════════
        //  PLAYER-SIDE: ENEMY UNIT VISIBILITY
        // ═══════════════════════════════════════════════════════

        void UpdateEnemyVisibility()
        {
            List<InsectUnit> toRemove = null;
            foreach (var kv in _enemyRenderers)
            {
                var u = kv.Key;
                if (u == null || !u.IsAlive)
                {
                    toRemove ??= new List<InsectUnit>();
                    toRemove.Add(u);
                }
            }
            if (toRemove != null)
            {
                foreach (var u in toRemove)
                    _enemyRenderers.Remove(u);
            }

            foreach (var u in RtsSimRegistry.Units)
            {
                if (u == null || !u.IsAlive || u.Team != Team.Enemy) continue;
                if (!_enemyRenderers.TryGetValue(u, out var renderers))
                {
                    renderers = u.GetComponentsInChildren<Renderer>(true);
                    _enemyRenderers[u] = renderers;
                }

                bool show;
                if (u.IsCloaked)
                {
                    bool proximityReveal = false;
                    foreach (var pu in RtsSimRegistry.Units)
                    {
                        if (pu == null || !pu.IsAlive || pu.Team != Team.Player) continue;
                        if (Vector3.Distance(pu.transform.position, u.transform.position) <= CloakDetectionRange)
                        { proximityReveal = true; break; }
                    }
                    show = proximityReveal;
                }
                else
                {
                    float concealment = TerrainFeatureRegistry.GetConcealmentRadius(u.transform.position);
                    if (concealment > 0f)
                    {
                        show = false;
                        foreach (var pu in RtsSimRegistry.Units)
                        {
                            if (pu == null || !pu.IsAlive || pu.Team != Team.Player) continue;
                            if (Vector3.Distance(pu.transform.position, u.transform.position) <= concealment)
                            { show = true; break; }
                        }
                    }
                    else
                    {
                        show = IsInCurrentVision(u.transform.position);
                    }
                }

                foreach (var r in renderers)
                {
                    if (r != null) r.enabled = show;
                }
            }
        }

        // ═══════════════════════════════════════════════════════
        //  PLAYER-SIDE: ENEMY BUILDING / HIVE VISIBILITY
        //  SC2 rules: buildings stay visible once explored (dimmed
        //  by the fog shader). They disappear only when destroyed
        //  AND the player re-explores (gets current vision on)
        //  that cell — so the player "sees" it's gone.
        // ═══════════════════════════════════════════════════════

        void UpdateEnemyBuildingVisibility()
        {
            // --- Enemy production buildings ---
            List<ProductionBuilding> bldToRemove = null;
            foreach (var kv in _enemyBuildingRenderers)
            {
                if (kv.Key == null)
                {
                    bldToRemove ??= new List<ProductionBuilding>();
                    bldToRemove.Add(kv.Key);
                }
            }
            if (bldToRemove != null)
            {
                foreach (var b in bldToRemove)
                    _enemyBuildingRenderers.Remove(b);
            }

            foreach (var bld in ProductionBuilding.All)
            {
                if (bld == null || bld.Team != Team.Enemy) continue;
                if (!_enemyBuildingRenderers.TryGetValue(bld, out var renderers))
                {
                    renderers = bld.GetComponentsInChildren<Renderer>(true);
                    _enemyBuildingRenderers[bld] = renderers;
                }

                bool show = ResolveBuildingShow(bld.transform.position, bld.IsAlive,
                    _knownAliveBuildings.Contains(bld));

                if (show && bld.IsAlive)
                    _knownAliveBuildings.Add(bld);

                if (IsInCurrentVision(bld.transform.position) && !bld.IsAlive)
                {
                    _knownAliveBuildings.Remove(bld);
                    show = false;
                }

                foreach (var r in renderers)
                {
                    if (r != null) r.enabled = show;
                }
            }

            // --- Enemy hive ---
            var enemyHive = HiveDeposit.EnemyHive;
            if (enemyHive != null && enemyHive != _trackedEnemyHive)
            {
                _trackedEnemyHive = enemyHive;
                _enemyHiveRenderers = enemyHive.GetComponentsInChildren<Renderer>(true);
            }

            if (_trackedEnemyHive != null && _enemyHiveRenderers != null)
            {
                bool show = ResolveBuildingShow(_trackedEnemyHive.transform.position,
                    _trackedEnemyHive.IsAlive, _enemyHiveKnownAlive);

                if (show && _trackedEnemyHive.IsAlive)
                    _enemyHiveKnownAlive = true;

                if (IsInCurrentVision(_trackedEnemyHive.transform.position) && !_trackedEnemyHive.IsAlive)
                {
                    _enemyHiveKnownAlive = false;
                    show = false;
                }

                foreach (var r in _enemyHiveRenderers)
                {
                    if (r != null) r.enabled = show;
                }
            }
        }

        /// <summary>
        /// SC2 building visibility logic:
        /// - Never explored → hidden.
        /// - Explored (R) but no current vision (G) → show if we last knew it alive.
        /// - Current vision → show if actually alive.
        /// </summary>
        bool ResolveBuildingShow(Vector3 pos, bool actuallyAlive, bool knownAlive)
        {
            bool explored = IsExplored(pos);
            bool inVision = IsInCurrentVision(pos);

            if (!explored) return false;
            if (inVision) return actuallyAlive;
            return knownAlive;
        }

        // ═══════════════════════════════════════════════════════
        //  ENEMY-SIDE INTEL (AI fairness — no map hack)
        // ═══════════════════════════════════════════════════════

        void UpdateEnemyIntel()
        {
            if (_knownPlayerHivePos.HasValue) return;
            var playerHive = HiveDeposit.PlayerHive;
            if (playerHive == null || !playerHive.IsAlive) return;

            if (IsVisibleToEnemy(playerHive.transform.position))
                _knownPlayerHivePos = playerHive.transform.position;
        }

        /// <summary>
        /// True if any enemy unit or building currently has line-of-sight to the position.
        /// Used by the AI commander so it must scout rather than cheat.
        /// </summary>
        public bool IsVisibleToEnemy(Vector3 pos)
        {
            foreach (var u in RtsSimRegistry.Units)
            {
                if (u == null || !u.IsAlive || u.Team != Team.Enemy) continue;
                float vr = u.Definition != null ? u.Definition.visionRadius : 12f;
                if (Vector3.Distance(u.transform.position, pos) > vr) continue;
                if (CanSeeOverHighGround(u.transform.position, pos, u.Archetype))
                    return true;
            }

            foreach (var bld in ProductionBuilding.All)
            {
                if (bld == null || !bld.IsAlive || bld.Team != Team.Enemy) continue;
                if (Vector3.Distance(bld.transform.position, pos) <= buildingVisionRadius)
                    return true;
            }

            var eHive = HiveDeposit.EnemyHive;
            if (eHive != null && eHive.IsAlive &&
                Vector3.Distance(eHive.transform.position, pos) <= hiveVisionRadius)
                return true;

            return false;
        }

        // ═══════════════════════════════════════════════════════
        //  PUBLIC QUERIES
        // ═══════════════════════════════════════════════════════

        /// <summary>True if this XZ is lit by current player vision (not merely explored).</summary>
        public bool IsInCurrentVision(Vector3 world)
        {
            if (_pix == null || !SkirmishPlayArea.HasBounds) return true;
            if (!TryTexel(world, out var x, out var z)) return false;
            return _pix[z * TexRes + x].g >= 96;
        }

        /// <summary>True if tile was ever seen (explored fog or visible).</summary>
        public bool IsExplored(Vector3 world)
        {
            if (_pix == null || !SkirmishPlayArea.HasBounds) return true;
            if (!TryTexel(world, out var x, out var z)) return false;
            return _pix[z * TexRes + x].r >= 96;
        }

        // ═══════════════════════════════════════════════════════
        //  INTERNALS
        // ═══════════════════════════════════════════════════════

        bool TryTexel(Vector3 world, out int x, out int z)
        {
            var h = SkirmishPlayArea.HalfExtent;
            x = 0;
            z = 0;
            if (h < 1f) return false;
            var fx = (world.x + h) / (2f * h) * TexRes;
            var fz = (world.z + h) / (2f * h) * TexRes;
            x = Mathf.Clamp(Mathf.FloorToInt(fx), 0, TexRes - 1);
            z = Mathf.Clamp(Mathf.FloorToInt(fz), 0, TexRes - 1);
            return true;
        }

        void PushBounds()
        {
            var h = SkirmishPlayArea.HalfExtent;
            if (h < 1f) return;
            var inv = 1f / (2f * h);
            Shader.SetGlobalVector(FogBoundsId, new Vector4(-h, -h, inv, inv));
        }

        void StampVision(Vector3 world, float radiusWorld, bool canSeeHighGround)
        {
            var h = SkirmishPlayArea.HalfExtent;
            var cx = (world.x + h) / (2f * h) * TexRes;
            var cz = (world.z + h) / (2f * h) * TexRes;
            var rTex = radiusWorld / (2f * h) * TexRes;
            if (rTex < 0.35f) rTex = 0.35f;
            var r2 = rTex * rTex;
            var minX = Mathf.Max(0, Mathf.FloorToInt(cx - rTex - 2f));
            var maxX = Mathf.Min(TexRes - 1, Mathf.CeilToInt(cx + rTex + 2f));
            var minZ = Mathf.Max(0, Mathf.FloorToInt(cz - rTex - 2f));
            var maxZ = Mathf.Min(TexRes - 1, Mathf.CeilToInt(cz + rTex + 2f));
            var inner = Mathf.Max(0.5f, rTex - visionFalloffWorld / (2f * h) * TexRes);
            inner = Mathf.Min(inner, rTex - 0.25f);
            var band = rTex - inner;

            bool checkBlocking = TerrainFeatureRegistry.HasVisionBlockers;
            var unitXZ = new Vector2(world.x, world.z);
            float invTexToWorld = (2f * h) / TexRes;
            var terrain = canSeeHighGround ? null : Terrain.activeTerrain;

            for (var zz = minZ; zz <= maxZ; zz++)
            {
                for (var xx = minX; xx <= maxX; xx++)
                {
                    var dx = xx - cx;
                    var dz = zz - cz;
                    var d2 = dx * dx + dz * dz;
                    if (d2 > r2) continue;

                    float twx = xx * invTexToWorld - h;
                    float twz = zz * invTexToWorld - h;

                    if (terrain != null)
                    {
                        float pixelHeight = terrain.SampleHeight(new Vector3(twx, 0f, twz));
                        if (pixelHeight > HighGroundThreshold)
                            continue;
                    }

                    if (checkBlocking)
                    {
                        if (TerrainFeatureRegistry.IsVisionBlocked(unitXZ, new Vector2(twx, twz)))
                            continue;
                    }

                    var d = Mathf.Sqrt(d2);
                    byte v;
                    if (d <= inner)
                        v = 255;
                    else
                        v = (byte)Mathf.Clamp(Mathf.RoundToInt(255f * (1f - (d - inner) / (band + 0.001f))), 0, 255);
                    var idx = zz * TexRes + xx;
                    if (v > _pix[idx].g)
                        _pix[idx].g = v;
                }
            }
        }
    }
}
