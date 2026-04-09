using System.Collections.Generic;
using InsectWars.Data;
using UnityEngine;

namespace InsectWars.RTS
{
    /// <summary>
    /// Runtime component attached to every terrain feature zone.
    /// Holds gameplay properties and provides spatial queries.
    /// </summary>
    public class TerrainFeatureZone : MonoBehaviour
    {
        public TerrainFeatureType FeatureType { get; private set; }
        public float Radius { get; private set; }
        public Vector2 BoxHalfExtents { get; private set; }
        public float RotationDeg { get; private set; }

        public float SpeedMultiplier { get; private set; }
        public float VisionMultiplier { get; private set; }
        public float ConcealmentDetectionRadius { get; private set; }
        public float DamagePerSecond { get; private set; }
        public bool BlocksPathing { get; private set; }
        public bool BlocksVision { get; private set; }

        public void Configure(TerrainFeaturePlaced placed)
        {
            FeatureType = placed.type;
            Radius = placed.radius;
            BoxHalfExtents = placed.boxHalfExtents;
            RotationDeg = placed.rotation;

            SpeedMultiplier = TerrainFeatureProperties.GetSpeedMultiplier(placed.type);
            VisionMultiplier = TerrainFeatureProperties.GetVisionMultiplier(placed.type);
            ConcealmentDetectionRadius = TerrainFeatureProperties.GetConcealmentDetectionRadius(placed.type);
            DamagePerSecond = TerrainFeatureProperties.GetDamagePerSecond(placed.type);
            BlocksPathing = TerrainFeatureProperties.BlocksPathing(placed.type);
            BlocksVision = TerrainFeatureProperties.BlocksVision(placed.type);
        }

        bool IsBox => BoxHalfExtents.x > 0.01f && BoxHalfExtents.y > 0.01f;

        public bool Contains(Vector3 worldPos)
        {
            var local = worldPos - transform.position;
            local.y = 0f;

            if (IsBox)
            {
                if (Mathf.Abs(RotationDeg) > 0.1f)
                {
                    float rad = -RotationDeg * Mathf.Deg2Rad;
                    float c = Mathf.Cos(rad), s = Mathf.Sin(rad);
                    float rx = local.x * c - local.z * s;
                    float rz = local.x * s + local.z * c;
                    return Mathf.Abs(rx) <= BoxHalfExtents.x && Mathf.Abs(rz) <= BoxHalfExtents.y;
                }
                return Mathf.Abs(local.x) <= BoxHalfExtents.x && Mathf.Abs(local.z) <= BoxHalfExtents.y;
            }

            return local.x * local.x + local.z * local.z <= Radius * Radius;
        }

        /// <summary>
        /// True if the XZ line segment from <paramref name="a"/> to <paramref name="b"/>
        /// passes through this zone. Used for rocky ridge vision blocking.
        /// </summary>
        public bool IntersectsLineXZ(Vector2 a, Vector2 b)
        {
            if (!BlocksVision) return false;

            var center = new Vector2(transform.position.x, transform.position.z);
            float r = IsBox ? Mathf.Max(BoxHalfExtents.x, BoxHalfExtents.y) : Radius;

            var d = b - a;
            var f = a - center;
            float aa = Vector2.Dot(d, d);
            if (aa < 0.0001f) return false;
            float bb = 2f * Vector2.Dot(f, d);
            float cc = Vector2.Dot(f, f) - r * r;
            float disc = bb * bb - 4f * aa * cc;
            if (disc < 0f) return false;

            float sq = Mathf.Sqrt(disc);
            float t1 = (-bb - sq) / (2f * aa);
            float t2 = (-bb + sq) / (2f * aa);
            return (t1 >= 0f && t1 <= 1f) || (t2 >= 0f && t2 <= 1f) || (t1 < 0f && t2 > 1f);
        }

        void OnEnable() => TerrainFeatureRegistry.Register(this);
        void OnDisable() => TerrainFeatureRegistry.Unregister(this);
    }

    /// <summary>
    /// Static registry of all active <see cref="TerrainFeatureZone"/>s
    /// with fast spatial queries for movement, vision, and damage.
    /// </summary>
    public static class TerrainFeatureRegistry
    {
        static readonly List<TerrainFeatureZone> s_zones = new();
        static bool s_hasBlockers;

        public static IReadOnlyList<TerrainFeatureZone> Zones => s_zones;
        public static bool HasVisionBlockers => s_hasBlockers;

        public static void Register(TerrainFeatureZone z)
        {
            if (s_zones.Contains(z)) return;
            s_zones.Add(z);
            if (z.BlocksVision) s_hasBlockers = true;
        }

        public static void Unregister(TerrainFeatureZone z)
        {
            s_zones.Remove(z);
            RefreshBlockerFlag();
        }

        static void RefreshBlockerFlag()
        {
            s_hasBlockers = false;
            for (int i = 0; i < s_zones.Count; i++)
                if (s_zones[i] != null && s_zones[i].BlocksVision) { s_hasBlockers = true; return; }
        }

        /// <summary>Smallest speed multiplier among all overlapping zones (1 = no effect).</summary>
        public static float GetSpeedMultiplier(Vector3 pos)
        {
            float m = 1f;
            for (int i = 0; i < s_zones.Count; i++)
            {
                var z = s_zones[i];
                if (z != null && z.SpeedMultiplier < 1f && z.Contains(pos))
                    m = Mathf.Min(m, z.SpeedMultiplier);
            }
            return m;
        }

        /// <summary>Smallest vision multiplier among all overlapping zones (1 = full vision).</summary>
        public static float GetVisionMultiplier(Vector3 pos)
        {
            float m = 1f;
            for (int i = 0; i < s_zones.Count; i++)
            {
                var z = s_zones[i];
                if (z != null && z.VisionMultiplier < 1f && z.Contains(pos))
                    m = Mathf.Min(m, z.VisionMultiplier);
            }
            return m;
        }

        /// <summary>
        /// Returns the concealment detection radius when <paramref name="pos"/> is inside a
        /// concealment zone, or 0 when not concealed.
        /// </summary>
        public static float GetConcealmentRadius(Vector3 pos)
        {
            for (int i = 0; i < s_zones.Count; i++)
            {
                var z = s_zones[i];
                if (z != null && z.ConcealmentDetectionRadius > 0f && z.Contains(pos))
                    return z.ConcealmentDetectionRadius;
            }
            return 0f;
        }

        /// <summary>Highest DPS among all overlapping damage zones.</summary>
        public static float GetDamagePerSecond(Vector3 pos)
        {
            float dps = 0f;
            for (int i = 0; i < s_zones.Count; i++)
            {
                var z = s_zones[i];
                if (z != null && z.DamagePerSecond > 0f && z.Contains(pos))
                    dps = Mathf.Max(dps, z.DamagePerSecond);
            }
            return dps;
        }

        /// <summary>True if any vision-blocking zone lies on the XZ line from A to B.</summary>
        public static bool IsVisionBlocked(Vector2 from, Vector2 to)
        {
            if (!s_hasBlockers) return false;
            for (int i = 0; i < s_zones.Count; i++)
            {
                var z = s_zones[i];
                if (z != null && z.IntersectsLineXZ(from, to))
                    return true;
            }
            return false;
        }
    }
}
