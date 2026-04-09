using System;
using UnityEngine;

namespace InsectWars.Data
{
    public enum TerrainFeatureType
    {
        WaterPuddle = 0,
        TallGrass = 1,
        MudPatch = 2,
        ThornPatch = 3,
        RockyRidge = 4
    }

    [Serializable]
    public struct TerrainFeaturePlaced
    {
        public TerrainFeatureType type;
        public Vector3 position;
        public float radius;
        /// <summary>Y-axis rotation in degrees for elongated/rectangular features.</summary>
        public float rotation;
        /// <summary>When non-zero the zone is a rotated rectangle (half-extents XZ) instead of a circle.</summary>
        public Vector2 boxHalfExtents;
    }

    /// <summary>
    /// Gameplay properties for each terrain feature type.
    /// Tweak these statics to rebalance without touching per-zone data.
    /// </summary>
    public static class TerrainFeatureProperties
    {
        public static float GetSpeedMultiplier(TerrainFeatureType t) => t switch
        {
            TerrainFeatureType.WaterPuddle => 0.50f,
            TerrainFeatureType.MudPatch    => 0.60f,
            TerrainFeatureType.ThornPatch  => 0.70f,
            _ => 1f
        };

        public static float GetVisionMultiplier(TerrainFeatureType t) => t switch
        {
            TerrainFeatureType.WaterPuddle => 0.75f,
            _ => 1f
        };

        /// <summary>
        /// When > 0 units inside this zone are concealed — enemies must be within this
        /// world-space distance to detect them.
        /// </summary>
        public static float GetConcealmentDetectionRadius(TerrainFeatureType t) => t switch
        {
            TerrainFeatureType.TallGrass => 5f,
            _ => 0f
        };

        public static float GetDamagePerSecond(TerrainFeatureType t) => t switch
        {
            TerrainFeatureType.ThornPatch => 2f,
            _ => 0f
        };

        public static bool BlocksPathing(TerrainFeatureType t) =>
            t == TerrainFeatureType.RockyRidge;

        public static bool BlocksVision(TerrainFeatureType t) =>
            t == TerrainFeatureType.RockyRidge;

        public static Color GetBaseColor(TerrainFeatureType t) => t switch
        {
            TerrainFeatureType.WaterPuddle => new Color(0.22f, 0.48f, 0.72f),
            TerrainFeatureType.TallGrass   => new Color(0.30f, 0.58f, 0.22f),
            TerrainFeatureType.MudPatch    => new Color(0.38f, 0.28f, 0.14f),
            TerrainFeatureType.ThornPatch  => new Color(0.22f, 0.40f, 0.12f),
            TerrainFeatureType.RockyRidge  => new Color(0.52f, 0.50f, 0.48f),
            _ => Color.white
        };
    }
}
