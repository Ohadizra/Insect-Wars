using System.Collections.Generic;
using UnityEngine;

namespace InsectWars.RTS
{
    /// <summary>
    /// Tracks circular build zones around hives and resource nodes.
    /// Buildings may only be placed inside a registered zone.
    /// </summary>
    public static class BuildZoneRegistry
    {
        /// <summary>Build-zone radius around each hive (main base area).</summary>
        public const float HiveRadius = 28f; // Original was 14f
        /// <summary>Build-zone radius around each fruit / apple node (expansion area).</summary>
        public const float FruitRadius = 18f;

        struct Zone
        {
            public Vector3 center;
            public float radiusSq;
        }

        static readonly List<Zone> s_zones = new();

        public static void Clear() => s_zones.Clear();

        public static void Register(Vector3 center, float radius)
        {
            s_zones.Add(new Zone { center = center, radiusSq = radius * radius });
        }

        public static bool IsInBuildZone(Vector3 pos)
        {
            for (int i = 0; i < s_zones.Count; i++)
            {
                var z = s_zones[i];
                float dx = pos.x - z.center.x;
                float dz = pos.z - z.center.z;
                if (dx * dx + dz * dz <= z.radiusSq)
                    return true;
            }
            return false;
        }

        public static int Count => s_zones.Count;

        public static void GetZone(int index, out Vector3 center, out float radius)
        {
            var z = s_zones[index];
            center = z.center;
            radius = Mathf.Sqrt(z.radiusSq);
        }
    }
}
