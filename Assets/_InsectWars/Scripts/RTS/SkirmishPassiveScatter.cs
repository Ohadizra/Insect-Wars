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

        static void EnsureLit()
        {
            if (s_lit != null) return;
            var sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            s_lit = new Material(sh);
        }

        static Material Mat(Color c)
        {
            EnsureLit();
            var m = new Material(s_lit);
            m.SetColor("_BaseColor", c);
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

        static void SpawnGrassTuft(Transform parent, float x, float z, System.Random rng, ScatterTheme theme)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "GrassTuft";
            go.transform.SetParent(parent, false);
            Object.Destroy(go.GetComponent<Collider>());
            var g = 0.28f + (float)rng.NextDouble() * 0.35f;
            go.transform.position = new Vector3(x, g * 0.5f, z);
            go.transform.localScale = new Vector3(0.35f + (float)rng.NextDouble() * 0.5f, g, 0.35f + (float)rng.NextDouble() * 0.5f);
            go.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);

            Color c;
            if (theme == ScatterTheme.Frozen)
                c = Color.Lerp(new Color(0.65f, 0.58f, 0.38f), new Color(0.78f, 0.72f, 0.55f), (float)rng.NextDouble());
            else
                c = Color.Lerp(new Color(0.18f, 0.42f, 0.14f), new Color(0.28f, 0.52f, 0.2f), (float)rng.NextDouble());

            go.GetComponent<Renderer>().sharedMaterial = Mat(c);
        }

        static void SpawnRock(Transform parent, float x, float z, System.Random rng, ScatterTheme theme)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Rock";
            go.transform.SetParent(parent, false);
            Object.Destroy(go.GetComponent<Collider>());
            var s = 0.25f + (float)rng.NextDouble() * 0.55f;
            go.transform.position = new Vector3(x, s * 0.4f, z);
            go.transform.localScale = new Vector3(s, s * (0.55f + (float)rng.NextDouble() * 0.35f), s * (0.8f + (float)rng.NextDouble() * 0.4f));
            go.transform.rotation = Quaternion.Euler(
                (float)rng.NextDouble() * 20f - 10f,
                (float)rng.NextDouble() * 360f,
                (float)rng.NextDouble() * 20f - 10f);

            if (theme == ScatterTheme.Frozen)
            {
                var mat = Mat(new Color(0.68f, 0.68f, 0.72f));
        #if UNITY_EDITOR
                var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_InsectWars/Textures/RealisticFrozenRock_Generated Maps/BaseMap.jpg");
                if (tex != null) mat.SetTexture("_BaseMap", tex);
        #endif
                go.GetComponent<Renderer>().sharedMaterial = mat;
            }
            else
            {
                var c = Color.Lerp(new Color(0.32f, 0.3f, 0.26f), new Color(0.48f, 0.44f, 0.38f), (float)rng.NextDouble());
                go.GetComponent<Renderer>().sharedMaterial = Mat(c);
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
                Object.Destroy(go.GetComponent<Collider>());
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

            var stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stem.name = "ShroomStem";
            stem.transform.SetParent(parent, false);
            Object.Destroy(stem.GetComponent<Collider>());
            stem.transform.position = new Vector3(x, 0.22f, z);
            stem.transform.localScale = new Vector3(0.12f, 0.22f, 0.12f);
            stem.GetComponent<Renderer>().sharedMaterial = Mat(new Color(0.9f, 0.88f, 0.82f));

            var cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cap.name = "ShroomCap";
            cap.transform.SetParent(parent, false);
            Object.Destroy(cap.GetComponent<Collider>());
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
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.position = new Vector3(x, 0.04f, z);
            go.transform.localScale = new Vector3(0.65f + (float)rng.NextDouble(), 0.06f, 0.08f);
            go.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, (float)rng.NextDouble() * 16f - 8f);

            Color c;
            if (theme == ScatterTheme.Frozen)
                c = new Color(0.32f, 0.28f, 0.26f);
            else
                c = new Color(0.38f, 0.28f, 0.18f);

            go.GetComponent<Renderer>().sharedMaterial = Mat(c);
        }
    }
}
