using System.Collections.Generic;
using UnityEngine;
using InsectWars.Data;

namespace InsectWars.RTS
{
    /// <summary>
    /// Decorative props (no colliders) to fill open ground.
    /// </summary>
    public static class SkirmishPassiveScatter
    {
        public readonly struct ExclusionZone
        {
            public readonly Vector2 Center;
            public readonly float RadiusSq;

            public ExclusionZone(Vector2 c, float r)
            {
                Center = c;
                RadiusSq = r * r;
            }

            public bool Contains(float x, float z)
            {
                var dx = x - Center.x;
                var dz = z - Center.y;
                return dx * dx + dz * dz <= RadiusSq;
            }
        }

        static Material s_lit;
        static readonly Dictionary<int, Material> s_matCache = new();
        static Texture2D s_frozenRockTex;
        static bool s_frozenRockTexLoaded;

        static void EnsureLit()
        {
            if (s_lit != null) return;
            var sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            s_lit = new Material(sh);
        }

        static Material Mat(Color c)
        {
            int key = ((int)(c.r * 255f) << 24) | ((int)(c.g * 255f) << 16) |
                      ((int)(c.b * 255f) << 8) | (int)(c.a * 255f);
            if (s_matCache.TryGetValue(key, out var cached))
                return cached;
            EnsureLit();
            var m = new Material(s_lit);
            m.SetColor("_BaseColor", c);
            s_matCache[key] = m;
            return m;
        }

        static Texture2D GetFrozenRockTexture()
        {
            if (s_frozenRockTexLoaded) return s_frozenRockTex;
            s_frozenRockTexLoaded = true;
            s_frozenRockTex = Resources.Load<Texture2D>("Textures/FrozenRockBaseMap");
        #if UNITY_EDITOR
            if (s_frozenRockTex == null)
                s_frozenRockTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(
                    "Assets/_InsectWars/Textures/RealisticFrozenRock_Generated Maps/BaseMap.jpg");
        #endif
            return s_frozenRockTex;
        }

        static bool Excluded(float x, float z, IReadOnlyList<ExclusionZone> zones)
        {
            for (var i = 0; i < zones.Count; i++)
            {
                if (zones[i].Contains(x, z)) return true;
            }
            return false;
        }

        public static void Scatter(Transform parent, float halfExtent, int seed, IReadOnlyList<ExclusionZone> exclusions, ScatterTheme theme = ScatterTheme.Default)
        {
            var rng = new System.Random(seed);
            var root = new GameObject("PassiveFill");
            root.transform.SetParent(parent, false);
            var pad = 12f;
            var min = -halfExtent + pad;
            var max = halfExtent - pad;
            var target = Mathf.RoundToInt(halfExtent * halfExtent * 0.045f);
            target = Mathf.Clamp(target, 280, 950);

            var spawned = 0;
            for (var attempt = 0; attempt < 4500 && spawned < target; attempt++)
            {
                var x = min + (float)rng.NextDouble() * (max - min);
                var z = min + (float)rng.NextDouble() * (max - min);
                if (Excluded(x, z, exclusions)) continue;

                var roll = rng.Next(100);
                if (roll < 38)
                    SpawnGrassTuft(root.transform, x, z, rng, theme);
                else if (roll < 68)
                    SpawnRock(root.transform, x, z, rng, theme);
                else if (roll < 88)
                    SpawnMushroom(root.transform, x, z, rng, theme);
                else
                    SpawnTwig(root.transform, x, z, rng, theme);
                spawned++;
            }
        }

        static GameObject s_lavaSpikePrefab;
        static GameObject s_volcanicRockPrefab;
        static GameObject s_ashTuftPrefab;

        static void EnsureLavaAssets()
        {
            if (s_lavaSpikePrefab == null) s_lavaSpikePrefab = Resources.Load<GameObject>("Models/LavaSpike");
            if (s_volcanicRockPrefab == null) s_volcanicRockPrefab = Resources.Load<GameObject>("Models/VolcanicRock");
            if (s_ashTuftPrefab == null) s_ashTuftPrefab = Resources.Load<GameObject>("Models/AshTuft");
        }

        static void SpawnGrassTuft(Transform parent, float x, float z, System.Random rng, ScatterTheme theme)
        {
            if (theme == ScatterTheme.Lava)
            {
                EnsureLavaAssets();
                if (s_ashTuftPrefab != null)
                {
                    var go = Object.Instantiate(s_ashTuftPrefab, parent);
                    go.name = "AshTuft";
                    go.transform.position = new Vector3(x, 0.05f, z);
                    go.transform.localScale = Vector3.one * (0.4f + (float)rng.NextDouble() * 0.6f);
                    go.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
                    return;
                }
            }

            var goPrimitive = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            goPrimitive.name = "GrassTuft";
            goPrimitive.transform.SetParent(parent, false);
            if (Application.isPlaying) Object.Destroy(goPrimitive.GetComponent<Collider>()); else Object.DestroyImmediate(goPrimitive.GetComponent<Collider>());
            var g = 0.28f + (float)rng.NextDouble() * 0.35f;
            goPrimitive.transform.position = new Vector3(x, g * 0.5f, z);
            goPrimitive.transform.localScale = new Vector3(0.35f + (float)rng.NextDouble() * 0.5f, g, 0.35f + (float)rng.NextDouble() * 0.5f);
            goPrimitive.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);

            Color c;
            if (theme == ScatterTheme.Frozen)
                c = Color.Lerp(new Color(0.65f, 0.58f, 0.38f), new Color(0.78f, 0.72f, 0.55f), (float)rng.NextDouble());
            else
                c = Color.Lerp(new Color(0.18f, 0.42f, 0.14f), new Color(0.28f, 0.52f, 0.2f), (float)rng.NextDouble());

            goPrimitive.GetComponent<Renderer>().sharedMaterial = Mat(c);
        }

        static void SpawnRock(Transform parent, float x, float z, System.Random rng, ScatterTheme theme)
        {
            if (theme == ScatterTheme.Lava)
            {
                EnsureLavaAssets();
                if (s_volcanicRockPrefab != null)
                {
                    var go = Object.Instantiate(s_volcanicRockPrefab, parent);
                    go.name = "VolcanicRock";
                    var s = 0.25f + (float)rng.NextDouble() * 0.55f;
                    go.transform.position = new Vector3(x, s * 0.3f, z);
                    go.transform.localScale = Vector3.one * s * 1.5f;
                    go.transform.rotation = Quaternion.Euler(
                        (float)rng.NextDouble() * 360f,
                        (float)rng.NextDouble() * 360f,
                        (float)rng.NextDouble() * 360f);
                    return;
                }
            }

            var goPrimitive = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            goPrimitive.name = "Rock";
            goPrimitive.transform.SetParent(parent, false);
            if (Application.isPlaying) Object.Destroy(goPrimitive.GetComponent<Collider>()); else Object.DestroyImmediate(goPrimitive.GetComponent<Collider>());
            var sVal = 0.25f + (float)rng.NextDouble() * 0.55f;
            goPrimitive.transform.position = new Vector3(x, sVal * 0.4f, z);
            goPrimitive.transform.localScale = new Vector3(sVal, sVal * (0.55f + (float)rng.NextDouble() * 0.35f), sVal * (0.8f + (float)rng.NextDouble() * 0.4f));
            goPrimitive.transform.rotation = Quaternion.Euler(
                (float)rng.NextDouble() * 20f - 10f,
                (float)rng.NextDouble() * 360f,
                (float)rng.NextDouble() * 20f - 10f);

            if (theme == ScatterTheme.Frozen)
            {
                var mat = Mat(new Color(0.68f, 0.68f, 0.72f));
                var tex = GetFrozenRockTexture();
                if (tex != null) mat.SetTexture("_BaseMap", tex);
                goPrimitive.GetComponent<Renderer>().sharedMaterial = mat;
            }
            else
            {
                var c = Color.Lerp(new Color(0.32f, 0.3f, 0.26f), new Color(0.48f, 0.44f, 0.38f), (float)rng.NextDouble());
                goPrimitive.GetComponent<Renderer>().sharedMaterial = Mat(c);
            }
        }

        static void SpawnMushroom(Transform parent, float x, float z, System.Random rng, ScatterTheme theme)
        {
            if (theme == ScatterTheme.Frozen)
            {
                // Spawn ice crystal instead
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "IceCrystal";
                go.transform.SetParent(parent, false);
                if (Application.isPlaying) Object.Destroy(go.GetComponent<Collider>()); else Object.DestroyImmediate(go.GetComponent<Collider>());
                var h = 0.25f + (float)rng.NextDouble() * 0.6f;
                go.transform.position = new Vector3(x, h * 0.45f, z);
                go.transform.localScale = new Vector3(0.15f, h, 0.15f);
                go.transform.rotation = Quaternion.Euler(
                    (float)rng.NextDouble() * 15f,
                    (float)rng.NextDouble() * 360f,
                    (float)rng.NextDouble() * 15f);
                
                var mat = Mat(new Color(0.82f, 0.88f, 0.95f, 0.6f));
                mat.SetFloat("_Surface", 1); // Transparent
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.9f);
                
                go.GetComponent<Renderer>().sharedMaterial = mat;
                return;
            }
            if (theme == ScatterTheme.Lava)
            {
                // Spawn lava spike
                EnsureLavaAssets();
                if (s_lavaSpikePrefab != null)
                {
                    var go = Object.Instantiate(s_lavaSpikePrefab, parent);
                    go.name = "LavaSpike";
                    var h = 0.4f + (float)rng.NextDouble() * 0.8f;
                    go.transform.position = new Vector3(x, h * 0.05f, z);
                    go.transform.localScale = Vector3.one * (0.4f + (float)rng.NextDouble() * 0.6f);
                    go.transform.rotation = Quaternion.Euler(
                        (float)rng.NextDouble() * 10f - 5f,
                        (float)rng.NextDouble() * 360f,
                        (float)rng.NextDouble() * 10f - 5f);
                    return;
                }

                var goPrimitive = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                goPrimitive.name = "LavaSpike";
                goPrimitive.transform.SetParent(parent, false);
                if (Application.isPlaying) Object.Destroy(goPrimitive.GetComponent<Collider>()); else Object.DestroyImmediate(goPrimitive.GetComponent<Collider>());
                var hVal = 0.4f + (float)rng.NextDouble() * 0.8f;
                goPrimitive.transform.position = new Vector3(x, hVal * 0.45f, z);
                goPrimitive.transform.localScale = new Vector3(0.1f, hVal, 0.1f);
                goPrimitive.transform.rotation = Quaternion.Euler(
                    (float)rng.NextDouble() * 10f - 5f,
                    (float)rng.NextDouble() * 360f,
                    (float)rng.NextDouble() * 10f - 5f);
                
                var mat = Mat(new Color(1.00f, 0.45f, 0.05f)); // Glowing Orange
                if (mat.HasProperty("_EmissionColor")) 
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", new Color(1.0f, 0.3f, 0.0f) * 2.0f);
                }
                goPrimitive.GetComponent<Renderer>().sharedMaterial = mat;
                return;
            }

            var stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stem.name = "ShroomStem";
            stem.transform.SetParent(parent, false);
            if (Application.isPlaying) Object.Destroy(stem.GetComponent<Collider>()); else Object.DestroyImmediate(stem.GetComponent<Collider>());
            stem.transform.position = new Vector3(x, 0.22f, z);
            stem.transform.localScale = new Vector3(0.12f, 0.22f, 0.12f);
            stem.GetComponent<Renderer>().sharedMaterial = Mat(new Color(0.9f, 0.88f, 0.82f));

            var cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cap.name = "ShroomCap";
            cap.transform.SetParent(parent, false);
            if (Application.isPlaying) Object.Destroy(cap.GetComponent<Collider>()); else Object.DestroyImmediate(cap.GetComponent<Collider>());
            cap.transform.position = new Vector3(x, 0.42f, z);
            cap.transform.localScale = new Vector3(0.38f, 0.22f, 0.38f);
            var capC = Color.Lerp(new Color(0.75f, 0.2f, 0.22f), new Color(0.55f, 0.15f, 0.45f), (float)rng.NextDouble());
            cap.GetComponent<Renderer>().sharedMaterial = Mat(capC);
            }

            static void SpawnTwig(Transform parent, float x, float z, System.Random rng, ScatterTheme theme)
            {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Twig";
            go.transform.SetParent(parent, false);
            if (Application.isPlaying) Object.Destroy(go.GetComponent<Collider>()); else Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.position = new Vector3(x, 0.04f, z);
            go.transform.localScale = new Vector3(0.65f + (float)rng.NextDouble(), 0.06f, 0.08f);
            go.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, (float)rng.NextDouble() * 16f - 8f);

            Color c;
            if (theme == ScatterTheme.Frozen)
                c = new Color(0.32f, 0.28f, 0.26f);
            else if (theme == ScatterTheme.Lava)
                c = new Color(0.08f, 0.07f, 0.06f); // Charred wood
            else
                c = new Color(0.38f, 0.28f, 0.18f);

            go.GetComponent<Renderer>().sharedMaterial = Mat(c);
            }
    }
}
