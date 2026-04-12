using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace InsectWars.RTS
{
    /// <summary>
    /// Fog map: R = ever explored, G = current vision (this frame). Rebuilt before the main camera renders.
    /// Enemies are hidden unless their cell has current vision (SC2-style).
    /// </summary>
    public class FogOfWarSystem : MonoBehaviour
    {
        public static FogOfWarSystem Instance { get; private set; }

        const int TexRes = 256;
        public static readonly int FogTexId = Shader.PropertyToID("_IW_FogWarTex");
        static readonly int FogBoundsId = Shader.PropertyToID("_IW_FogBounds");
        static readonly int FogActiveId = Shader.PropertyToID("_IW_FogActive");

        [SerializeField] float hiveVisionRadius = 16f;
        [SerializeField] float buildingVisionRadius = 14f;
        [SerializeField] float visionFalloffWorld = 2.2f;

        Texture2D _tex;
        Color32[] _pix;
        Color32 _black;
        readonly Dictionary<InsectUnit, Renderer[]> _enemyRenderers = new();

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
            // Must stay CPU-writable so LateUpdate/beginCamera can update every frame.
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
        }

        void RebuildVisionGrid()
        {
            for (var i = 0; i < _pix.Length; i++)
                _pix[i].g = 0;

            foreach (var u in RtsSimRegistry.Units)
            {
                if (u == null || !u.IsAlive || u.Team != Team.Player) continue;
                var r = u.Definition != null ? u.Definition.visionRadius : 12f;
                float visMult = TerrainFeatureRegistry.GetVisionMultiplier(u.transform.position);
                StampVision(u.transform.position, r * visMult);
            }

            if (HiveDeposit.PlayerHive != null)
                StampVision(HiveDeposit.PlayerHive.transform.position, hiveVisionRadius);

            foreach (var bld in ProductionBuilding.All)
            {
                if (bld == null || !bld.IsAlive || bld.Team != Team.Player) continue;
                StampVision(bld.transform.position, buildingVisionRadius);
            }

            for (var i = 0; i < _pix.Length; i++)
            {
                if (_pix[i].g > _pix[i].r)
                    _pix[i].r = _pix[i].g;
            }
        }

        void UpdateEnemyVisibility()
        {
            var toRemove = (List<InsectUnit>)null;
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

                foreach (var r in renderers)
                {
                    if (r != null) r.enabled = show;
                }
            }
        }

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

        void StampVision(Vector3 world, float radiusWorld)
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

            for (var zz = minZ; zz <= maxZ; zz++)
            {
                for (var xx = minX; xx <= maxX; xx++)
                {
                    var dx = xx - cx;
                    var dz = zz - cz;
                    var d2 = dx * dx + dz * dz;
                    if (d2 > r2) continue;

                    if (checkBlocking)
                    {
                        float twx = xx * invTexToWorld - h;
                        float twz = zz * invTexToWorld - h;
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
