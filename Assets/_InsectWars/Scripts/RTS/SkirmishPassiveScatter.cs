using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace InsectWars.RTS
{
    /// <summary>
    /// Decorative props (no colliders) to fill open ground with real-world
    /// objects at insect scale: pebbles, dried leaves, grass blades, twigs.
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
        static readonly Dictionary<uint, Material> s_matCache = new();
        static GameObject s_leafPrefab;
        static GameObject s_pebblePrefab;
        static bool s_prefabsLoaded;

        static void EnsureLit()
        {
            if (s_lit != null) return;
            var sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            s_lit = new Material(sh);
        }

        static Material Mat(Color c)
        {
            EnsureLit();
            uint key = (uint)(Mathf.RoundToInt(c.r * 255f) << 16) |
                       (uint)(Mathf.RoundToInt(c.g * 255f) << 8) |
                       (uint)Mathf.RoundToInt(c.b * 255f);

            if (s_matCache.TryGetValue(key, out var existing))
                return existing;

            var m = new Material(s_lit);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            else if (m.HasProperty("_Color")) m.color = c;
            s_matCache[key] = m;
            return m;
        }

        static bool Excluded(float x, float z, IReadOnlyList<ExclusionZone> zones)
        {
            for (var i = 0; i < zones.Count; i++)
            {
                if (zones[i].Contains(x, z)) return true;
            }
            return false;
        }

        static void LoadPrefabs()
        {
            if (s_prefabsLoaded) return;
            s_prefabsLoaded = true;
#if UNITY_EDITOR
            s_leafPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_InsectWars/Models/Scatter_FallenLeaf.glb");
            s_pebblePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_InsectWars/Models/Scatter_Pebble.glb");
#endif
        }

        public static void Scatter(Transform parent, float halfExtent, int seed, IReadOnlyList<ExclusionZone> exclusions)
        {
            LoadPrefabs();
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
                if (roll < 30)
                    SpawnGrassBlade(root.transform, x, z, rng);
                else if (roll < 55)
                    SpawnPebble(root.transform, x, z, rng);
                else if (roll < 80)
                    SpawnFallenLeaf(root.transform, x, z, rng);
                else
                    SpawnTwig(root.transform, x, z, rng);
                spawned++;
            }
        }

        /// <summary>Single tall grass blade — realistic proportions for insect scale.</summary>
        static void SpawnGrassBlade(Transform parent, float x, float z, System.Random rng)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "GrassBlade";
            go.transform.SetParent(parent, false);
            Object.Destroy(go.GetComponent<Collider>());

            float height = 1.2f + (float)rng.NextDouble() * 2.0f;
            float width = 0.08f + (float)rng.NextDouble() * 0.12f;
            go.transform.position = new Vector3(x, height * 0.5f, z);
            go.transform.localScale = new Vector3(width, height, 0.02f);

            float lean = (float)rng.NextDouble() * 18f - 9f;
            go.transform.rotation = Quaternion.Euler(lean, (float)rng.NextDouble() * 360f, lean * 0.5f);

            var c = Color.Lerp(
                new Color(0.22f, 0.38f, 0.12f),
                new Color(0.45f, 0.55f, 0.18f),
                (float)rng.NextDouble());
            go.GetComponent<Renderer>().sharedMaterial = Mat(c);
        }

        /// <summary>Small pebble — uses Meshy-generated model when available, falls back to stretched sphere.</summary>
        static void SpawnPebble(Transform parent, float x, float z, System.Random rng)
        {
            GameObject go;
            float size = 0.3f + (float)rng.NextDouble() * 0.8f;

            if (s_pebblePrefab != null)
            {
                go = Object.Instantiate(s_pebblePrefab, parent);
                go.name = "Pebble";
                Object.Destroy(go.GetComponent<Collider>());
                go.transform.position = new Vector3(x, size * 0.3f, z);
                go.transform.localScale = Vector3.one * size;
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = "Pebble";
                go.transform.SetParent(parent, false);
                Object.Destroy(go.GetComponent<Collider>());
                go.transform.position = new Vector3(x, size * 0.3f, z);
                float flatness = 0.4f + (float)rng.NextDouble() * 0.5f;
                float stretch = 0.8f + (float)rng.NextDouble() * 0.4f;
                go.transform.localScale = new Vector3(size * stretch, size * flatness, size);
                var c = Color.Lerp(
                    new Color(0.40f, 0.38f, 0.34f),
                    new Color(0.55f, 0.50f, 0.45f),
                    (float)rng.NextDouble());
                go.GetComponent<Renderer>().sharedMaterial = Mat(c);
            }

            go.transform.rotation = Quaternion.Euler(
                (float)rng.NextDouble() * 10f,
                (float)rng.NextDouble() * 360f,
                (float)rng.NextDouble() * 10f);
        }

        /// <summary>Fallen leaf — uses Meshy-generated model when available, falls back to flat disc.</summary>
        static void SpawnFallenLeaf(Transform parent, float x, float z, System.Random rng)
        {
            GameObject go;
            float size = 1.5f + (float)rng.NextDouble() * 2.5f;

            if (s_leafPrefab != null)
            {
                go = Object.Instantiate(s_leafPrefab, parent);
                go.name = "FallenLeaf";
                Object.Destroy(go.GetComponent<Collider>());
                go.transform.position = new Vector3(x, 0.02f, z);
                go.transform.localScale = Vector3.one * size * 0.5f;
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.name = "FallenLeaf";
                go.transform.SetParent(parent, false);
                Object.Destroy(go.GetComponent<Collider>());
                go.transform.position = new Vector3(x, 0.02f, z);
                float elongation = 1.2f + (float)rng.NextDouble() * 0.6f;
                go.transform.localScale = new Vector3(size * elongation, 0.015f, size);
                var c = Color.Lerp(
                    new Color(0.50f, 0.35f, 0.12f),
                    new Color(0.60f, 0.48f, 0.15f),
                    (float)rng.NextDouble());
                go.GetComponent<Renderer>().sharedMaterial = Mat(c);
            }

            go.transform.rotation = Quaternion.Euler(
                (float)rng.NextDouble() * 6f - 3f,
                (float)rng.NextDouble() * 360f,
                (float)rng.NextDouble() * 6f - 3f);
        }

        /// <summary>Small twig fragment — realistic stick proportions for insect scale.</summary>
        static void SpawnTwig(Transform parent, float x, float z, System.Random rng)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "Twig";
            go.transform.SetParent(parent, false);
            Object.Destroy(go.GetComponent<Collider>());

            float length = 1.5f + (float)rng.NextDouble() * 3.0f;
            float thickness = 0.04f + (float)rng.NextDouble() * 0.06f;
            go.transform.position = new Vector3(x, thickness, z);
            go.transform.localScale = new Vector3(thickness, length * 0.5f, thickness);

            go.transform.rotation = Quaternion.Euler(
                88f + (float)rng.NextDouble() * 4f,
                (float)rng.NextDouble() * 360f,
                0f);

            var c = Color.Lerp(
                new Color(0.35f, 0.24f, 0.14f),
                new Color(0.48f, 0.35f, 0.20f),
                (float)rng.NextDouble());
            go.GetComponent<Renderer>().sharedMaterial = Mat(c);
        }
    }
}
