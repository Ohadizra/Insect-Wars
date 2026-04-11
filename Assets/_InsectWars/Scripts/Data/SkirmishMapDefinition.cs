using System;
using UnityEngine;

namespace InsectWars.Data
{
    [Serializable]
    public struct ClayPlaced
    {
        public Vector3 position;
        public Vector3 scale;
    }

    [Serializable]
    public struct FruitPlaced
    {
        public Vector3 position;
        public int calories;
        public int gatherPerTick;
        public float gatherSeconds;
    }

    [Serializable]
    public struct HighGroundPlaced
    {
        /// <summary>UV coordinates (0-1) on the terrain, where (0,0) is terrain origin corner.</summary>
        public Vector2 uv;
        /// <summary>Radius for circular plateaus. Set to 0 to use boxSize instead.</summary>
        public float radius;
        /// <summary>Width/Height in UV space for rectangular plateaus.</summary>
        public Vector2 boxSize;
        /// <summary>Rotation in degrees for rectangular plateaus.</summary>
        public float rotation;
        /// <summary>Ramp width in UV space — wider = gentler slope NavMesh can walk.</summary>
        public float rampWidth;
        /// <summary>Height as fraction of terrain max height (e.g. 0.08 = 8% of 20m = 1.6m).</summary>
        public float heightFraction;
    }

    /// <summary>
    /// Serialized map layout for SkirmishDirector. Assign on the director or leave null for built-in Demo 0 defaults.
    /// </summary>
    [CreateAssetMenu(fileName = "SkirmishMap", menuName = "Insect Wars/Skirmish Map Definition")]
    public class SkirmishMapDefinition : ScriptableObject
    {
        public string displayName = "Unnamed Map";
        public string description = "";

        /// <summary>Unity scene name to load for this map (must be in Build Settings).</summary>
        public string sceneName = "";

        public float mapHalfExtent = 88f;
        public Vector3 playerHivePosition = new Vector3(-62f, 1f, -52f);
        public Vector3 enemyHivePosition = new Vector3(62f, 1f, 52f);
        public Vector3 bigApplePosition = new Vector3(-50f, 1.5f, -42f);
        public Vector3 enemyBigApplePosition = new Vector3(50f, 1.5f, 42f);
        public int passiveScatterSeed = 18427;
        public ClayPlaced[] clay = Array.Empty<ClayPlaced>();

        /// <summary>Per-map clay wall prefab override. When set, used instead of UnitVisualLibrary.clayWallPrefab.</summary>
        [NonSerialized] public GameObject clayWallPrefabOverride;
        public FruitPlaced[] fruits = Array.Empty<FruitPlaced>();
        /// <summary>Terrain elevation features. Empty = flat terrain. Null falls back to demo defaults.</summary>
        public HighGroundPlaced[] highGrounds;

        /// <summary>Strategic terrain zones: water, tall grass, mud, thorns, rocky ridges.</summary>
        public TerrainFeaturePlaced[] terrainFeatures = Array.Empty<TerrainFeaturePlaced>();
    }
}
