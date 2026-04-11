using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace InsectWars.Data
{
    /// <summary>
    /// Built-in skirmish maps created at runtime. Each map is a mirror-balanced layout.
    /// </summary>
    public static class SkirmishMapPresets
    {
        static SkirmishMapDefinition[] s_maps;

        public static SkirmishMapDefinition[] GetAll()
        {
            if (s_maps != null && s_maps.Length > 0 && s_maps[0] != null) return s_maps;
            s_maps = new[] { CreateThornBasin(), CreateLalush(), CreateBraveNewWorld(), CreateShazuBell(), CreateShazuDen() };
            return s_maps;
        }

        static SkirmishMapDefinition CreateThornBasin()
        {
            var map = ScriptableObject.CreateInstance<SkirmishMapDefinition>();
            map.hideFlags = HideFlags.HideAndDontSave;
            map.name = "ThornBasin";
            map.displayName = "Thorn Basin";
            map.sceneName = "ThornBasin";
            map.description = "A small clearing between twisted roots.\nEach side controls one calorie deposit. Fight for the contested center.";
            map.mapHalfExtent = 55f;
            map.playerHivePosition = new Vector3(-38f, 1f, -38f);
            map.enemyHivePosition = new Vector3(38f, 1f, 38f);
            map.bigApplePosition = new Vector3(-30f, 1.5f, -22f);
            map.enemyBigApplePosition = new Vector3(30f, 1.5f, 22f);
            map.passiveScatterSeed = 44291;
            map.highGrounds = new[] { new HighGroundPlaced { uv = new Vector2(0.5f, 0.5f), radius = 0.06f, rampWidth = 0.05f, heightFraction = 0.06f } };
            map.clay = new[]
            {
                new ClayPlaced { position = new Vector3(-12f, 0f, -4f), scale = new Vector3(5f, 2.2f, 2.5f) },
                new ClayPlaced { position = new Vector3(12f, 0f, 4f), scale = new Vector3(5f, 2.2f, 2.5f) },
                new ClayPlaced { position = new Vector3(-6f, 0f, 18f), scale = new Vector3(3f, 2.5f, 6f) },
                new ClayPlaced { position = new Vector3(6f, 0f, -18f), scale = new Vector3(3f, 2.5f, 6f) },
                new ClayPlaced { position = new Vector3(-28f, 0f, 10f), scale = new Vector3(4f, 2f, 3f) },
                new ClayPlaced { position = new Vector3(28f, 0f, -10f), scale = new Vector3(4f, 2f, 3f) },
                new ClayPlaced { position = new Vector3(-22f, 0f, -18f), scale = new Vector3(3f, 2.4f, 4f) },
                new ClayPlaced { position = new Vector3(22f, 0f, 18f), scale = new Vector3(3f, 2.4f, 4f) },
            };
            map.fruits = System.Array.Empty<FruitPlaced>();
            map.terrainFeatures = new[]
            {
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3(0f, 0f, 0f), radius = 6f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3(-18f, 0f, 6f), radius = 5f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3(18f, 0f, -6f), radius = 5f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.MudPatch, position = new Vector3(-34f, 0f, 2f), radius = 4f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.MudPatch, position = new Vector3(34f, 0f, -2f), radius = 4f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.ThornPatch, position = new Vector3(-8f, 0f, 24f), radius = 3.5f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.ThornPatch, position = new Vector3(8f, 0f, -24f), radius = 3.5f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.RockyRidge, position = new Vector3(-6f, 0f, -12f), rotation = 30f, boxHalfExtents = new Vector2(4f, 1.5f) },
                new TerrainFeaturePlaced { type = TerrainFeatureType.RockyRidge, position = new Vector3(6f, 0f, 12f), rotation = 30f, boxHalfExtents = new Vector2(4f, 1.5f) },
            };
            return map;
        }

        static SkirmishMapDefinition CreateLalush()
        {
            var map = ScriptableObject.CreateInstance<SkirmishMapDefinition>();
            map.hideFlags = HideFlags.HideAndDontSave;
            map.name = "Lalush";
            map.displayName = "Lalush Depths";
            map.sceneName = "Lalush";
            map.description = "Swamp lowlands split by a diagonal ridge bridge.\nCorner bases, tight ramp chokes, and contested third-base pockets on each flank.";
            map.mapHalfExtent = 78f;
            map.playerHivePosition       = new Vector3(-60f, 1f, -54f);
            map.enemyHivePosition        = new Vector3( 60f, 1f,  54f);
            map.bigApplePosition         = new Vector3(-52f, 1.5f, -40f);
            map.enemyBigApplePosition    = new Vector3( 52f, 1.5f,  40f);
            map.passiveScatterSeed = 73519;
            map.highGrounds = new[]
            {
                new HighGroundPlaced { uv = new Vector2(0.14f, 0.17f), radius = 0.10f, rampWidth = 0.025f, heightFraction = 0.12f },
                new HighGroundPlaced { uv = new Vector2(0.86f, 0.83f), radius = 0.10f, rampWidth = 0.025f, heightFraction = 0.12f },
                new HighGroundPlaced { uv = new Vector2(0.27f, 0.36f), radius = 0.05f, rampWidth = 0.035f, heightFraction = 0.07f },
                new HighGroundPlaced { uv = new Vector2(0.73f, 0.64f), radius = 0.05f, rampWidth = 0.035f, heightFraction = 0.07f },
                new HighGroundPlaced { uv = new Vector2(0.40f, 0.42f), radius = 0.065f, rampWidth = 0.03f, heightFraction = 0.09f },
                new HighGroundPlaced { uv = new Vector2(0.50f, 0.50f), radius = 0.055f, rampWidth = 0.03f, heightFraction = 0.09f },
                new HighGroundPlaced { uv = new Vector2(0.60f, 0.58f), radius = 0.065f, rampWidth = 0.03f, heightFraction = 0.09f },
                new HighGroundPlaced { uv = new Vector2(0.16f, 0.76f), radius = 0.08f, rampWidth = 0.035f, heightFraction = 0.08f },
                new HighGroundPlaced { uv = new Vector2(0.84f, 0.24f), radius = 0.08f, rampWidth = 0.035f, heightFraction = 0.08f },
                new HighGroundPlaced { uv = new Vector2(0.38f, 0.68f), radius = 0.035f, rampWidth = 0.03f, heightFraction = 0.04f },
                new HighGroundPlaced { uv = new Vector2(0.62f, 0.32f), radius = 0.035f, rampWidth = 0.03f, heightFraction = 0.04f },
            };
            map.clay = new[]
            {
                new ClayPlaced { position = new Vector3(-40f, 0f, -32f), scale = new Vector3(4f, 3f, 2.5f) },
                new ClayPlaced { position = new Vector3(-36f, 0f, -38f), scale = new Vector3(2.5f, 3f, 4f) },
                new ClayPlaced { position = new Vector3( 40f, 0f,  32f), scale = new Vector3(4f, 3f, 2.5f) },
                new ClayPlaced { position = new Vector3( 36f, 0f,  38f), scale = new Vector3(2.5f, 3f, 4f) },
                new ClayPlaced { position = new Vector3(-22f, 0f, -20f), scale = new Vector3(3f, 2.8f, 5f) },
                new ClayPlaced { position = new Vector3( 22f, 0f,  20f), scale = new Vector3(3f, 2.8f, 5f) },
                new ClayPlaced { position = new Vector3(-14f, 0f,  10f), scale = new Vector3(3.5f, 2.8f, 4f) },
                new ClayPlaced { position = new Vector3( 14f, 0f, -10f), scale = new Vector3(3.5f, 2.8f, 4f) },
                new ClayPlaced { position = new Vector3(-30f, 0f,  28f), scale = new Vector3(4.5f, 2.5f, 2.5f) },
                new ClayPlaced { position = new Vector3(-24f, 0f,  34f), scale = new Vector3(2.5f, 2.5f, 4f) },
                new ClayPlaced { position = new Vector3( 30f, 0f, -28f), scale = new Vector3(4.5f, 2.5f, 2.5f) },
                new ClayPlaced { position = new Vector3( 24f, 0f, -34f), scale = new Vector3(2.5f, 2.5f, 4f) },
                new ClayPlaced { position = new Vector3(-56f, 0f,  16f), scale = new Vector3(3f, 2.5f, 5f) },
                new ClayPlaced { position = new Vector3( 56f, 0f, -16f), scale = new Vector3(3f, 2.5f, 5f) },
                new ClayPlaced { position = new Vector3( 12f, 0f, -40f), scale = new Vector3(4f, 2.2f, 3f) },
                new ClayPlaced { position = new Vector3(-12f, 0f,  40f), scale = new Vector3(4f, 2.2f, 3f) },
            };
            map.fruits = System.Array.Empty<FruitPlaced>();
            map.terrainFeatures = new[]
            {
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3(-68f, 0f, -8f), radius = 9f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3( 68f, 0f,  8f), radius = 9f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3( 18f, 0f, -64f), radius = 8f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3(-18f, 0f,  64f), radius = 8f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3(  0f, 0f,   0f), radius = 4f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3(-54f, 0f, -28f), radius = 6f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3( 54f, 0f,  28f), radius = 6f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3(-16f, 0f,  30f), radius = 5f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3( 16f, 0f, -30f), radius = 5f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3(-48f, 0f,   6f), radius = 4f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3( 48f, 0f,  -6f), radius = 4f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.RockyRidge, position = new Vector3(-50f, 0f, -4f), rotation = 70f, boxHalfExtents = new Vector2(6f, 2f) },
                new TerrainFeaturePlaced { type = TerrainFeatureType.RockyRidge, position = new Vector3( 50f, 0f,  4f), rotation = 70f, boxHalfExtents = new Vector2(6f, 2f) },
                new TerrainFeaturePlaced { type = TerrainFeatureType.RockyRidge, position = new Vector3(-8f, 0f, -16f), rotation = 40f, boxHalfExtents = new Vector2(5f, 1.8f) },
                new TerrainFeaturePlaced { type = TerrainFeatureType.RockyRidge, position = new Vector3( 8f, 0f,  16f), rotation = 40f, boxHalfExtents = new Vector2(5f, 1.8f) },
                new TerrainFeaturePlaced { type = TerrainFeatureType.RockyRidge, position = new Vector3(-36f, 0f,  52f), rotation = 10f, boxHalfExtents = new Vector2(5f, 2f) },
                new TerrainFeaturePlaced { type = TerrainFeatureType.RockyRidge, position = new Vector3( 36f, 0f, -52f), rotation = 10f, boxHalfExtents = new Vector2(5f, 2f) },
                new TerrainFeaturePlaced { type = TerrainFeatureType.MudPatch, position = new Vector3(-20f, 0f, -12f), radius = 5f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.MudPatch, position = new Vector3( 20f, 0f,  12f), radius = 5f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.MudPatch, position = new Vector3(-28f, 0f,  20f), radius = 3.5f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.MudPatch, position = new Vector3( 28f, 0f, -20f), radius = 3.5f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.ThornPatch, position = new Vector3(-62f, 0f,   8f), radius = 4f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.ThornPatch, position = new Vector3( 62f, 0f,  -8f), radius = 4f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.ThornPatch, position = new Vector3( 46f, 0f, -48f), radius = 3f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.ThornPatch, position = new Vector3(-46f, 0f,  48f), radius = 3f },
            };
            return map;
        }

        static SkirmishMapDefinition CreateBraveNewWorld()
        {
            var map = ScriptableObject.CreateInstance<SkirmishMapDefinition>();
            map.hideFlags = HideFlags.HideAndDontSave;
            map.name = "BraveNewWorld";
            map.displayName = "Brave New World";
            map.sceneName = "BraveNewWorld";
            map.description = "A vast battlefield with fortified corner bases and narrow ramp chokes.\nSeize the central plateau, contest multiple expansions, and use vision blockers to ambush.";
            map.mapHalfExtent = 110f;
            map.playerHivePosition    = new Vector3( 82f, 1f, -80f);
            map.enemyHivePosition     = new Vector3(-82f, 1f,  80f);
            map.bigApplePosition      = new Vector3( 72f, 1.5f, -68f);
            map.enemyBigApplePosition = new Vector3(-72f, 1.5f,  68f);
            map.passiveScatterSeed = 91753;
            map.highGrounds = new[]
            {
                new HighGroundPlaced { uv = new Vector2(0.873f, 0.136f), radius = 0.12f, rampWidth = 0.018f, heightFraction = 0.14f },
                new HighGroundPlaced { uv = new Vector2(0.127f, 0.864f), radius = 0.12f, rampWidth = 0.018f, heightFraction = 0.14f },
                new HighGroundPlaced { uv = new Vector2(0.745f, 0.255f), radius = 0.055f, rampWidth = 0.030f, heightFraction = 0.065f },
                new HighGroundPlaced { uv = new Vector2(0.255f, 0.745f), radius = 0.055f, rampWidth = 0.030f, heightFraction = 0.065f },
                new HighGroundPlaced { uv = new Vector2(0.818f, 0.795f), radius = 0.07f, rampWidth = 0.030f, heightFraction = 0.06f },
                new HighGroundPlaced { uv = new Vector2(0.182f, 0.205f), radius = 0.07f, rampWidth = 0.030f, heightFraction = 0.06f },
                new HighGroundPlaced { uv = new Vector2(0.50f, 0.50f), radius = 0.08f, rampWidth = 0.028f, heightFraction = 0.10f },
                new HighGroundPlaced { uv = new Vector2(0.82f, 0.50f), radius = 0.04f, rampWidth = 0.025f, heightFraction = 0.05f },
                new HighGroundPlaced { uv = new Vector2(0.18f, 0.50f), radius = 0.04f, rampWidth = 0.025f, heightFraction = 0.05f },
                new HighGroundPlaced { uv = new Vector2(0.636f, 0.364f), radius = 0.03f, rampWidth = 0.035f, heightFraction = 0.035f },
                new HighGroundPlaced { uv = new Vector2(0.364f, 0.636f), radius = 0.03f, rampWidth = 0.035f, heightFraction = 0.035f },
            };
            map.clay = new[]
            {
                new ClayPlaced { position = new Vector3( 76f, 0f, -50f), scale = new Vector3(8f, 4f, 3f) },
                new ClayPlaced { position = new Vector3( 52f, 0f, -72f), scale = new Vector3(3f, 4f, 8f) },
                new ClayPlaced { position = new Vector3( 58f, 0f, -52f), scale = new Vector3(3f, 3.5f, 3f) },
                new ClayPlaced { position = new Vector3( 66f, 0f, -46f), scale = new Vector3(3f, 3.5f, 3f) },
                new ClayPlaced { position = new Vector3(-76f, 0f,  50f), scale = new Vector3(8f, 4f, 3f) },
                new ClayPlaced { position = new Vector3(-52f, 0f,  72f), scale = new Vector3(3f, 4f, 8f) },
                new ClayPlaced { position = new Vector3(-58f, 0f,  52f), scale = new Vector3(3f, 3.5f, 3f) },
                new ClayPlaced { position = new Vector3(-66f, 0f,  46f), scale = new Vector3(3f, 3.5f, 3f) },
                new ClayPlaced { position = new Vector3( 44f, 0f, -36f), scale = new Vector3(4f, 3f, 3f) },
                new ClayPlaced { position = new Vector3( 54f, 0f, -28f), scale = new Vector3(3f, 3f, 4f) },
                new ClayPlaced { position = new Vector3(-44f, 0f,  36f), scale = new Vector3(4f, 3f, 3f) },
                new ClayPlaced { position = new Vector3(-54f, 0f,  28f), scale = new Vector3(3f, 3f, 4f) },
                new ClayPlaced { position = new Vector3( 62f, 0f,  58f), scale = new Vector3(4f, 3f, 3f) },
                new ClayPlaced { position = new Vector3( 70f, 0f,  52f), scale = new Vector3(3f, 3f, 4.5f) },
                new ClayPlaced { position = new Vector3(-62f, 0f, -58f), scale = new Vector3(4f, 3f, 3f) },
                new ClayPlaced { position = new Vector3(-70f, 0f, -52f), scale = new Vector3(3f, 3f, 4.5f) },
                new ClayPlaced { position = new Vector3( 18f, 0f, -22f), scale = new Vector3(3.5f, 3f, 5f) },
                new ClayPlaced { position = new Vector3(-18f, 0f,  22f), scale = new Vector3(3.5f, 3f, 5f) },
                new ClayPlaced { position = new Vector3( 90f, 0f,  14f), scale = new Vector3(3f, 3f, 6f) },
                new ClayPlaced { position = new Vector3(-90f, 0f, -14f), scale = new Vector3(3f, 3f, 6f) },
                new ClayPlaced { position = new Vector3( 30f, 0f,  40f), scale = new Vector3(5f, 2.5f, 3f) },
                new ClayPlaced { position = new Vector3(-30f, 0f, -40f), scale = new Vector3(5f, 2.5f, 3f) },
                new ClayPlaced { position = new Vector3(-50f, 0f, -96f), scale = new Vector3(4f, 3f, 3f) },
                new ClayPlaced { position = new Vector3( 50f, 0f,  96f), scale = new Vector3(4f, 3f, 3f) },
            };
#if UNITY_EDITOR
            var bnwClay = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_InsectWars/Models/ClayWall_BNW.glb");
            if (bnwClay != null) map.clayWallPrefabOverride = bnwClay;
#endif
            return map;
        }

        static SkirmishMapDefinition CreateShazuBell()
        {
            var map = ScriptableObject.CreateInstance<SkirmishMapDefinition>();
            map.hideFlags = HideFlags.HideAndDontSave;
            map.name = "ShazuBell";
            map.displayName = "Shazu Bell";
            map.sceneName = "ShazuBell";
            map.description = "A strategic frozen outpost with fortified corner mains and tiered lanes.\nCapture the high-calorie Apples at the expansions to fuel your swarm.";
            map.mapHalfExtent = 120f;
            map.playerHivePosition    = new Vector3(-92f, 1f,  88f);
            map.enemyHivePosition     = new Vector3( 92f, 1f, -88f);
            map.bigApplePosition      = new Vector3(-80f, 1.5f, 75f);
            map.enemyBigApplePosition = new Vector3( 80f, 1.5f, -75f);
            map.passiveScatterSeed = 4242;
            map.highGrounds = new[]
            {
                new HighGroundPlaced { uv = new Vector2(0.12f, 0.88f), boxSize = new Vector2(0.18f, 0.18f), rotation = 45f, rampWidth = 0.025f, heightFraction = 0.16f },
                new HighGroundPlaced { uv = new Vector2(0.88f, 0.12f), boxSize = new Vector2(0.18f, 0.18f), rotation = 45f, rampWidth = 0.025f, heightFraction = 0.16f },
                new HighGroundPlaced { uv = new Vector2(0.25f, 0.70f), radius = 0.06f, rampWidth = 0.035f, heightFraction = 0.07f },
                new HighGroundPlaced { uv = new Vector2(0.75f, 0.30f), radius = 0.06f, rampWidth = 0.035f, heightFraction = 0.07f },
                new HighGroundPlaced { uv = new Vector2(0.90f, 0.90f), boxSize = new Vector2(0.12f, 0.12f), rotation = 0f, rampWidth = 0.04f, heightFraction = 0.08f },
                new HighGroundPlaced { uv = new Vector2(0.10f, 0.10f), boxSize = new Vector2(0.12f, 0.12f), rotation = 0f, rampWidth = 0.04f, heightFraction = 0.08f },
                new HighGroundPlaced { uv = new Vector2(0.38f, 0.50f), radius = 0.04f, rampWidth = 0.03f, heightFraction = 0.06f },
                new HighGroundPlaced { uv = new Vector2(0.62f, 0.50f), radius = 0.04f, rampWidth = 0.03f, heightFraction = 0.06f },
                new HighGroundPlaced { uv = new Vector2(0.50f, 0.50f), boxSize = new Vector2(0.06f, 0.15f), rotation = 30f, rampWidth = 0.03f, heightFraction = 0.10f },
            };
            map.fruits = new[]
            {
                new FruitPlaced { position = new Vector3(-62f, 1.5f, 48f), calories = 4500, gatherPerTick = 3, gatherSeconds = 0.8f },
                new FruitPlaced { position = new Vector3( 62f, 1.5f, -48f), calories = 4500, gatherPerTick = 3, gatherSeconds = 0.8f },
                new FruitPlaced { position = new Vector3( 98f, 1.5f, 98f), calories = 5500, gatherPerTick = 2, gatherSeconds = 1.0f },
                new FruitPlaced { position = new Vector3(-98f, 1.5f, -98f), calories = 5500, gatherPerTick = 2, gatherSeconds = 1.0f },
                new FruitPlaced { position = new Vector3(-105f, 1.5f, 0f), calories = 4000, gatherPerTick = 3, gatherSeconds = 0.7f },
                new FruitPlaced { position = new Vector3( 105f, 1.5f, 0f), calories = 4000, gatherPerTick = 3, gatherSeconds = 0.7f },
                new FruitPlaced { position = new Vector3(-20f, 1.5f, 85f), calories = 5000, gatherPerTick = 4, gatherSeconds = 0.6f },
                new FruitPlaced { position = new Vector3( 20f, 1.5f, -85f), calories = 5000, gatherPerTick = 4, gatherSeconds = 0.6f },
                new FruitPlaced { position = new Vector3(0f, 2.5f, 12f), calories = 7000, gatherPerTick = 6, gatherSeconds = 0.5f },
                new FruitPlaced { position = new Vector3(0f, 2.5f, -12f), calories = 7000, gatherPerTick = 6, gatherSeconds = 0.5f },
            };
            map.clay = new[]
            {
                new ClayPlaced { position = new Vector3(-98f, 0f, 65f), scale = new Vector3(4f, 6f, 12f) },
                new ClayPlaced { position = new Vector3(-65f, 0f, 98f), scale = new Vector3(12f, 6f, 4f) },
                new ClayPlaced { position = new Vector3( 98f, 0f, -65f), scale = new Vector3(4f, 6f, 12f) },
                new ClayPlaced { position = new Vector3( 65f, 0f, -98f), scale = new Vector3(12f, 6f, 4f) },
                new ClayPlaced { position = new Vector3(-25f, 0f, 15f), scale = new Vector3(6f, 4f, 6f) },
                new ClayPlaced { position = new Vector3( 25f, 0f, -15f), scale = new Vector3(6f, 4f, 6f) },
                new ClayPlaced { position = new Vector3( 80f, 0f, 70f), scale = new Vector3(5f, 3f, 5f) },
                new ClayPlaced { position = new Vector3(-80f, 0f, -70f), scale = new Vector3(5f, 3f, 5f) },
            };
            map.terrainFeatures = new[]
            {
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3(-45, 0, 85), radius = 10f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3( 45, 0, -85), radius = 10f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3( 0, 0, 45), radius = 8f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3( 0, 0, -45), radius = 8f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.RockyRidge, position = new Vector3(-95, 0, 40), boxHalfExtents = new Vector2(15, 3), rotation = 90f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.RockyRidge, position = new Vector3( 95, 0, -40), boxHalfExtents = new Vector2(15, 3), rotation = 90f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.MudPatch, position = new Vector3(-40, 0, 0), radius = 12f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.MudPatch, position = new Vector3( 40, 0, 0), radius = 12f },
            };
            return map;
        }

        static SkirmishMapDefinition CreateShazuDen()
        {
            var map = ScriptableObject.CreateInstance<SkirmishMapDefinition>();
            map.hideFlags = HideFlags.HideAndDontSave;
            map.name = "ShazuDen";
            map.displayName = "Shazu Den";
            map.sceneName = "ShazuDen";
            map.description = "Frozen earth outpost inspired by ancient battlefields.\nElevated mains with narrow ramps, choked natural expansions, safe pocket thirds behind, and two high-yield gold deposits on exposed low ground at 3 and 9 o'clock. Control the center watchtower to dominate.";
            map.mapHalfExtent = 120f;

            map.playerHivePosition    = new Vector3(-82f, 1f,  82f);
            map.enemyHivePosition     = new Vector3( 82f, 1f, -82f);

            map.bigApplePosition      = new Vector3(-70f, 1.5f, 72f);
            map.enemyBigApplePosition = new Vector3( 70f, 1.5f, -72f);

            map.passiveScatterSeed = 50917;

            // --- Elevation: 7 plateaus with narrow, defensible ramps ---
            map.highGrounds = new[]
            {
                // Main bases — high cliffs (2.8 m), tight 5.3 m ramps
                new HighGroundPlaced { uv = new Vector2(0.158f, 0.842f), radius = 0.095f, rampWidth = 0.022f, heightFraction = 0.14f },
                new HighGroundPlaced { uv = new Vector2(0.842f, 0.158f), radius = 0.095f, rampWidth = 0.022f, heightFraction = 0.14f },

                // Natural expansions — medium elevation, adjacent to main
                new HighGroundPlaced { uv = new Vector2(0.275f, 0.725f), radius = 0.065f, rampWidth = 0.028f, heightFraction = 0.07f },
                new HighGroundPlaced { uv = new Vector2(0.725f, 0.275f), radius = 0.065f, rampWidth = 0.028f, heightFraction = 0.07f },

                // Pocket thirds — behind main in extreme corners, safe but isolated
                new HighGroundPlaced { uv = new Vector2(0.083f, 0.917f), radius = 0.055f, rampWidth = 0.025f, heightFraction = 0.08f },
                new HighGroundPlaced { uv = new Vector2(0.917f, 0.083f), radius = 0.055f, rampWidth = 0.025f, heightFraction = 0.08f },

                // Center watchtower — small contested high ground
                new HighGroundPlaced { uv = new Vector2(0.50f, 0.50f), radius = 0.04f, rampWidth = 0.025f, heightFraction = 0.09f },
            };

            // --- Resources: progression from safe to contested ---
            map.fruits = new[]
            {
                // Natural expansion apples (moderate, accessible)
                new FruitPlaced { position = new Vector3(-50f, 1.5f, 50f),  calories = 5000, gatherPerTick = 3, gatherSeconds = 0.7f },
                new FruitPlaced { position = new Vector3( 50f, 1.5f, -50f), calories = 5000, gatherPerTick = 3, gatherSeconds = 0.7f },

                // Pocket third apples (slow but safe)
                new FruitPlaced { position = new Vector3(-96f, 1.5f,  96f), calories = 4500, gatherPerTick = 2, gatherSeconds = 0.8f },
                new FruitPlaced { position = new Vector3( 96f, 1.5f, -96f), calories = 4500, gatherPerTick = 2, gatherSeconds = 0.8f },

                // Gold bases at 9 and 3 o'clock — high yield, low ground, exposed
                new FruitPlaced { position = new Vector3(-78f, 1.5f,  0f), calories = 10000, gatherPerTick = 6, gatherSeconds = 0.4f },
                new FruitPlaced { position = new Vector3( 78f, 1.5f,  0f), calories = 10000, gatherPerTick = 6, gatherSeconds = 0.4f },

                // Center contested apples (moderate yield, highly fought over)
                new FruitPlaced { position = new Vector3( 5f, 2.5f,  8f), calories = 6000, gatherPerTick = 4, gatherSeconds = 0.5f },
                new FruitPlaced { position = new Vector3(-5f, 2.5f, -8f), calories = 6000, gatherPerTick = 4, gatherSeconds = 0.5f },
            };

            // --- Clay walls: 22 pieces forming strategic choke points ---
            map.clay = new[]
            {
                // Player main ramp choke — flanking the descent from main plateau toward natural
                new ClayPlaced { position = new Vector3(-72f, 0f, 65f), scale = new Vector3(3f, 4f, 5f) },
                new ClayPlaced { position = new Vector3(-63f, 0f, 74f), scale = new Vector3(5f, 4f, 3f) },
                // Enemy main ramp choke (mirror)
                new ClayPlaced { position = new Vector3( 72f, 0f, -65f), scale = new Vector3(3f, 4f, 5f) },
                new ClayPlaced { position = new Vector3( 63f, 0f, -74f), scale = new Vector3(5f, 4f, 3f) },

                // Player natural choke — funneling movement between natural and the open map
                new ClayPlaced { position = new Vector3(-40f, 0f, 44f), scale = new Vector3(3f, 3.5f, 5f) },
                new ClayPlaced { position = new Vector3(-46f, 0f, 38f), scale = new Vector3(5f, 3.5f, 3f) },
                // Enemy natural choke (mirror)
                new ClayPlaced { position = new Vector3( 40f, 0f, -44f), scale = new Vector3(3f, 3.5f, 5f) },
                new ClayPlaced { position = new Vector3( 46f, 0f, -38f), scale = new Vector3(5f, 3.5f, 3f) },

                // Player pocket third access — narrow passage behind main
                new ClayPlaced { position = new Vector3(-90f, 0f, 92f), scale = new Vector3(3f, 3f, 5f) },
                new ClayPlaced { position = new Vector3(-95f, 0f, 86f), scale = new Vector3(4f, 3f, 3f) },
                // Enemy pocket access (mirror)
                new ClayPlaced { position = new Vector3( 90f, 0f, -92f), scale = new Vector3(3f, 3f, 5f) },
                new ClayPlaced { position = new Vector3( 95f, 0f, -86f), scale = new Vector3(4f, 3f, 3f) },

                // Championship choke — center-map corridor
                new ClayPlaced { position = new Vector3(-15f, 0f,  18f), scale = new Vector3(4f, 3f, 3f) },
                new ClayPlaced { position = new Vector3( 15f, 0f, -18f), scale = new Vector3(4f, 3f, 3f) },
                new ClayPlaced { position = new Vector3(-18f, 0f, -12f), scale = new Vector3(3f, 3f, 5f) },
                new ClayPlaced { position = new Vector3( 18f, 0f,  12f), scale = new Vector3(3f, 3f, 5f) },

                // Gold base area walls — partial cover, attackable from multiple angles
                new ClayPlaced { position = new Vector3(-90f, 0f,  10f), scale = new Vector3(3f, 3f, 5f) },
                new ClayPlaced { position = new Vector3(-90f, 0f, -10f), scale = new Vector3(3f, 3f, 5f) },
                new ClayPlaced { position = new Vector3( 90f, 0f,  10f), scale = new Vector3(3f, 3f, 5f) },
                new ClayPlaced { position = new Vector3( 90f, 0f, -10f), scale = new Vector3(3f, 3f, 5f) },

                // Map edge reinforcement near pocket thirds
                new ClayPlaced { position = new Vector3(-115f, 0f,  55f), scale = new Vector3(3f, 4f, 7f) },
                new ClayPlaced { position = new Vector3( 115f, 0f, -55f), scale = new Vector3(3f, 4f, 7f) },
            };

            // --- Terrain features: 19 zones for strategic variety ---
            map.terrainFeatures = new[]
            {
                // Water puddles — frozen dewdrops, slow movement + reduced vision
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3(  0f, 0f,   0f), radius = 4f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3(-72f, 0f,   8f), radius = 5f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3( 72f, 0f,  -8f), radius = 5f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3(-30f, 0f, -30f), radius = 3.5f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3( 30f, 0f,  30f), radius = 3.5f },

                // Tall grass — frost-covered concealment for ambushes
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3(-35f, 0f,  35f), radius = 6f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3( 35f, 0f, -35f), radius = 6f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3(-85f, 0f, -15f), radius = 5f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3( 85f, 0f,  15f), radius = 5f },

                // Rocky ridges — vision blockers forcing scouting
                new TerrainFeaturePlaced { type = TerrainFeatureType.RockyRidge, position = new Vector3(-22f, 0f,   5f), boxHalfExtents = new Vector2(5f, 2f), rotation = 40f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.RockyRidge, position = new Vector3( 22f, 0f,  -5f), boxHalfExtents = new Vector2(5f, 2f), rotation = 40f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.RockyRidge, position = new Vector3(-108f, 0f, 72f), boxHalfExtents = new Vector2(4f, 2f), rotation = 0f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.RockyRidge, position = new Vector3( 108f, 0f, -72f), boxHalfExtents = new Vector2(4f, 2f), rotation = 0f },

                // Mud patches — frozen mud, slowing terrain
                new TerrainFeaturePlaced { type = TerrainFeatureType.MudPatch, position = new Vector3(-20f, 0f,  15f), radius = 5f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.MudPatch, position = new Vector3( 20f, 0f, -15f), radius = 5f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.MudPatch, position = new Vector3(-75f, 0f, -10f), radius = 4f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.MudPatch, position = new Vector3( 75f, 0f,  10f), radius = 4f },

                // Thorn patches — ice crystal patches dealing damage, flanking the center choke
                new TerrainFeaturePlaced { type = TerrainFeatureType.ThornPatch, position = new Vector3(-10f, 0f, -25f), radius = 3.5f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.ThornPatch, position = new Vector3( 10f, 0f,  25f), radius = 3.5f },
            };

#if UNITY_EDITOR
            var denVisuals = AssetDatabase.LoadAssetAtPath<UnitVisualLibrary>("Assets/_InsectWars/Data/VisualLibrary_ShazuDen.asset");
            if (denVisuals != null && denVisuals.clayWallPrefab != null)
                map.clayWallPrefabOverride = denVisuals.clayWallPrefab;
#endif
            return map;
        }
    }
}
