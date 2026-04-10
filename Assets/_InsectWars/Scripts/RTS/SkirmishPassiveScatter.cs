using System.Collections.Generic;
using UnityEngine;

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

        public static void Scatter(Transform parent, float halfExtent, int seed, IReadOnlyList<ExclusionZone> exclusions)
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
                    SpawnGrassTuft(root.transform, x, z, rng);
                else if (roll < 68)
                    SpawnRock(root.transform, x, z, rng);
                else if (roll < 88)
                    SpawnMushroom(root.transform, x, z, rng);
                else
                    SpawnTwig(root.transform, x, z, rng);
                spawned++;
            }
        }

        static void SpawnGrassTuft(Transform parent, float x, float z, System.Random rng)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "GrassTuft";
            go.transform.SetParent(parent, false);
            Object.Destroy(go.GetComponent<Collider>());
            var g = 0.28f + (float)rng.NextDouble() * 0.35f;
            go.transform.position = new Vector3(x, g * 0.5f, z);
            go.transform.localScale = new Vector3(0.35f + (float)rng.NextDouble() * 0.5f, g, 0.35f + (float)rng.NextDouble() * 0.5f);
            go.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
            var c = Color.Lerp(new Color(0.18f, 0.42f, 0.14f), new Color(0.28f, 0.52f, 0.2f), (float)rng.NextDouble());
            go.GetComponent<Renderer>().sharedMaterial = Mat(c);
        }

        static void SpawnRock(Transform parent, float x, float z, System.Random rng)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Rock";
            go.transform.SetParent(parent, false);
            Object.Destroy(go.GetComponent<Collider>());
            var s = 0.3f + (float)rng.NextDouble() * 0.7f;
            go.transform.position = new Vector3(x, s * 0.4f, z);
            go.transform.localScale = new Vector3(s, s * (0.4f + (float)rng.NextDouble() * 0.6f), s * (0.7f + (float)rng.NextDouble() * 0.5f));
            go.transform.rotation = Quaternion.Euler(
                (float)rng.NextDouble() * 360f,
                (float)rng.NextDouble() * 360f,
                (float)rng.NextDouble() * 360f);
            var c = Color.Lerp(new Color(0.35f, 0.33f, 0.3f), new Color(0.45f, 0.43f, 0.4f), (float)rng.NextDouble());
            go.GetComponent<Renderer>().sharedMaterial = Mat(c);
        }

        static void SpawnMushroom(Transform parent, float x, float z, System.Random rng)
        {
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

        static void SpawnTwig(Transform parent, float x, float z, System.Random rng)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Twig";
            go.transform.SetParent(parent, false);
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.position = new Vector3(x, 0.04f, z);
            go.transform.localScale = new Vector3(0.65f + (float)rng.NextDouble(), 0.06f, 0.08f);
            go.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, (float)rng.NextDouble() * 16f - 8f);
            go.GetComponent<Renderer>().sharedMaterial = Mat(new Color(0.38f, 0.28f, 0.18f));
        }
    }
}
