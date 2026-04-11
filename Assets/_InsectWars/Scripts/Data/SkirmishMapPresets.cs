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
            s_maps = new[] { CreateThornBasin(), CreateLalush(), CreateBraveNewWorld(), CreateShazuBell() };
            return s_maps;
        }

        static SkirmishMapDefinition CreateThornBasin()
        {
            var map = ScriptableObject.CreateInstance<SkirmishMapDefinition>();
            map.hideFlags = HideFlags.HideAndDontSave;
            map.name = "ThornBasin";
            map.displayName = "Thorn Basin";
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
            map.displayName = "Lalush: King Sejong Station";
            map.description = "A massive 1v1 adaptation of the classic SC2 map. Fortified corner mains with tiered paths, secondary high-ground bases, and a central resource-rich low-ground pit.";
            map.mapHalfExtent = 135f;

            // Mains: NW and SE
            map.playerHivePosition    = new Vector3(-110f, 1f, 110f); 
            map.enemyHivePosition     = new Vector3( 110f, 1f, -110f);
            
            // Primary Base Apples (Start)
            map.bigApplePosition      = new Vector3(-95f, 1.5f, 95f);
            map.enemyBigApplePosition = new Vector3( 95f, 1.5f, -95f);
            
            map.passiveScatterSeed = 73519;
            
            // High Grounds (Mirror Balanced)
            // UV range [0,1] for halfExtent 135 (Total 270)
            map.highGrounds = new[]
            {
                // Main Bases (Large corner plateaus)
                new HighGroundPlaced { uv = new Vector2(0.09f, 0.91f), radius = 0.16f, rampWidth = 0.05f, heightFraction = 0.14f },
                new HighGroundPlaced { uv = new Vector2(0.91f, 0.09f), radius = 0.16f, rampWidth = 0.05f, heightFraction = 0.14f },
                
                // Natural Expansions (Outside main ramp)
                new HighGroundPlaced { uv = new Vector2(0.24f, 0.76f), radius = 0.11f, rampWidth = 0.04f, heightFraction = 0.08f },
                new HighGroundPlaced { uv = new Vector2(0.76f, 0.24f), radius = 0.11f, rampWidth = 0.04f, heightFraction = 0.08f },
                
                // Third Base "Islands" (NE and SW flanking ledges)
                new HighGroundPlaced { uv = new Vector2(0.90f, 0.90f), radius = 0.09f, rampWidth = 0.04f, heightFraction = 0.10f },
                new HighGroundPlaced { uv = new Vector2(0.10f, 0.10f), radius = 0.09f, rampWidth = 0.04f, heightFraction = 0.10f },
                
                // Side Expansions (Center edge platforms)
                new HighGroundPlaced { uv = new Vector2(0.10f, 0.50f), radius = 0.08f, rampWidth = 0.04f, heightFraction = 0.06f },
                new HighGroundPlaced { uv = new Vector2(0.90f, 0.50f), radius = 0.08f, rampWidth = 0.04f, heightFraction = 0.06f },
            };
            
            map.fruits = new[]
            {
                // Natural Expansion Apples (Secondary bases)
                new FruitPlaced { position = new Vector3(-70f, 1.5f, 75f), calories = 7000, gatherPerTick = 3, gatherSeconds = 0.7f },
                new FruitPlaced { position = new Vector3( 70f, 1.5f, -75f), calories = 7000, gatherPerTick = 3, gatherSeconds = 0.7f },
                
                // Third Base High Ground Apples
                new FruitPlaced { position = new Vector3( 120f, 1.5f, 120f), calories = 6000, gatherPerTick = 4, gatherSeconds = 0.6f },
                new FruitPlaced { position = new Vector3(-120f, 1.5f, -120f), calories = 6000, gatherPerTick = 4, gatherSeconds = 0.6f },
                
                // Side Expansion Apples
                new FruitPlaced { position = new Vector3(-115f, 1.5f, 0f), calories = 8000, gatherPerTick = 2, gatherSeconds = 0.8f },
                new FruitPlaced { position = new Vector3( 115f, 1.5f, 0f), calories = 8000, gatherPerTick = 2, gatherSeconds = 0.8f },

                // Center Low-Ground Pit (High-Yield Apples)
                new FruitPlaced { position = new Vector3( 20f, 1.5f,  20f), calories = 15000, gatherPerTick = 6, gatherSeconds = 0.4f },
                new FruitPlaced { position = new Vector3(-20f, 1.5f, -20f), calories = 15000, gatherPerTick = 6, gatherSeconds = 0.4f },
                
                // Corner Base expansion (Opposite corner from start)
                new FruitPlaced { position = new Vector3(-115f, 1.5f, -115f), calories = 9000, gatherPerTick = 3, gatherSeconds = 0.8f },
                new FruitPlaced { position = new Vector3( 115f, 1.5f,  115f), calories = 9000, gatherPerTick = 3, gatherSeconds = 0.8f },
            };
            
            map.clay = new[]
            {
                // "Crystals" as tall glass-like shards (Ant's eye: huge minerals)
                // NW Main Ledge
                new ClayPlaced { position = new Vector3(-132f, 0f, 132f), scale = new Vector3(3f, 18f, 3f) },
                new ClayPlaced { position = new Vector3(-130f, 0f, 126f), scale = new Vector3(2f, 12f, 2f) },
                new ClayPlaced { position = new Vector3(-126f, 0f, 130f), scale = new Vector3(2f, 14f, 2f) },
                
                // SE Main Ledge
                new ClayPlaced { position = new Vector3( 132f, 0f, -132f), scale = new Vector3(3f, 18f, 3f) },
                new ClayPlaced { position = new Vector3( 130f, 0f, -126f), scale = new Vector3(2f, 12f, 2f) },
                new ClayPlaced { position = new Vector3( 126f, 0f, -130f), scale = new Vector3(2f, 14f, 2f) },

                // Barrier cliffs blocking certain paths
                new ClayPlaced { position = new Vector3(-55f, 0f, 95f), scale = new Vector3(12f, 6f, 3f) },
                new ClayPlaced { position = new Vector3( 55f, 0f, -95f), scale = new Vector3(12f, 6f, 3f) },
                new ClayPlaced { position = new Vector3(-95f, 0f, 55f), scale = new Vector3(3f, 6f, 12f) },
                new ClayPlaced { position = new Vector3( 95f, 0f, -55f), scale = new Vector3(3f, 6f, 12f) },
                
                // Side path shards
                new ClayPlaced { position = new Vector3(-130f, 0f, 0f), scale = new Vector3(4f, 15f, 4f) },
                new ClayPlaced { position = new Vector3( 130f, 0f, 0f), scale = new Vector3(4f, 15f, 4f) },
            };
            
            map.terrainFeatures = new[]
            {
                // The Great Pit: Large central low-ground depression (using Mud/Water)
                new TerrainFeaturePlaced { type = TerrainFeatureType.MudPatch, position = new Vector3(0, 0, 0), radius = 70f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3(0, 0, 0), radius = 30f },
                
                // Tall Grass for ambushes near expansions
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3(-45f, 0, 45f), radius = 15f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3( 45f, 0, -45f), radius = 15f },
                
                // Rocky Ridge "Cliffs"
                new TerrainFeaturePlaced { type = TerrainFeatureType.RockyRidge, position = new Vector3(-90f, 0, 0f), boxHalfExtents = new Vector2(40f, 5f), rotation = 90f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.RockyRidge, position = new Vector3( 90f, 0, 0f), boxHalfExtents = new Vector2(40f, 5f), rotation = 90f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.RockyRidge, position = new Vector3( 0f, 0, 90f), boxHalfExtents = new Vector2(5f, 40f), rotation = 0f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.RockyRidge, position = new Vector3( 0f, 0, -90f), boxHalfExtents = new Vector2(5f, 40f), rotation = 0f },
            };
            return map;
        }

        static SkirmishMapDefinition CreateBraveNewWorld()
        {
            var map = ScriptableObject.CreateInstance<SkirmishMapDefinition>();
            map.hideFlags = HideFlags.HideAndDontSave;
            map.name = "BraveNewWorld";
            map.displayName = "Brave New World";
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
            map.fruits = System.Array.Empty<FruitPlaced>();
            map.terrainFeatures = new[]
            {
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3( 56f, 0f, -42f), radius = 5f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3(-56f, 0f,  42f), radius = 5f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3( 30f, 0f, -28f), radius = 6f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3(-30f, 0f,  28f), radius = 6f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3(-10f, 0f, -14f), radius = 5f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3( 10f, 0f,  14f), radius = 5f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3( 76f, 0f,  42f), radius = 4.5f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3(-76f, 0f, -42f), radius = 4.5f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3( 88f, 0f, -20f), radius = 4f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3(-88f, 0f,  20f), radius = 4f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.RockyRidge, position = new Vector3( 86f, 0f, -54f), rotation = 50f, boxHalfExtents = new Vector2(7f, 2.5f) },
                new TerrainFeaturePlaced { type = TerrainFeatureType.RockyRidge, position = new Vector3( 56f, 0f, -84f), rotation = 50f, boxHalfExtents = new Vector2(7f, 2.5f) },
                new TerrainFeaturePlaced { type = TerrainFeatureType.RockyRidge, position = new Vector3(-86f, 0f,  54f), rotation = 50f, boxHalfExtents = new Vector2(7f, 2.5f) },
                new TerrainFeaturePlaced { type = TerrainFeatureType.RockyRidge, position = new Vector3(-56f, 0f,  84f), rotation = 50f, boxHalfExtents = new Vector2(7f, 2.5f) },
                new TerrainFeaturePlaced { type = TerrainFeatureType.RockyRidge, position = new Vector3(-14f, 0f, -8f), rotation = 45f, boxHalfExtents = new Vector2(5f, 2f) },
                new TerrainFeaturePlaced { type = TerrainFeatureType.RockyRidge, position = new Vector3( 14f, 0f,  8f), rotation = 45f, boxHalfExtents = new Vector2(5f, 2f) },
                new TerrainFeaturePlaced { type = TerrainFeatureType.RockyRidge, position = new Vector3( 40f, 0f, -22f), rotation = 30f, boxHalfExtents = new Vector2(5f, 1.8f) },
                new TerrainFeaturePlaced { type = TerrainFeatureType.RockyRidge, position = new Vector3(-40f, 0f,  22f), rotation = 30f, boxHalfExtents = new Vector2(5f, 1.8f) },
                new TerrainFeaturePlaced { type = TerrainFeatureType.RockyRidge, position = new Vector3( 80f, 0f,  76f), rotation = 0f, boxHalfExtents = new Vector2(5f, 2f) },
                new TerrainFeaturePlaced { type = TerrainFeatureType.RockyRidge, position = new Vector3(-80f, 0f, -76f), rotation = 0f, boxHalfExtents = new Vector2(5f, 2f) },
                new TerrainFeaturePlaced { type = TerrainFeatureType.RockyRidge, position = new Vector3( 78f, 0f,  20f), rotation = 80f, boxHalfExtents = new Vector2(6f, 2f) },
                new TerrainFeaturePlaced { type = TerrainFeatureType.RockyRidge, position = new Vector3(-78f, 0f, -20f), rotation = 80f, boxHalfExtents = new Vector2(6f, 2f) },
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3(  0f, 0f,   0f), radius = 5f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3(-94f, 0f,   0f), radius = 10f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3( 94f, 0f,   0f), radius = 10f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3( 30f, 0f,  90f), radius = 8f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3(-30f, 0f, -90f), radius = 8f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3( 36f, 0f, -50f), radius = 5f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3(-36f, 0f,  50f), radius = 5f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.MudPatch, position = new Vector3( 48f, 0f, -16f), radius = 5f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.MudPatch, position = new Vector3(-48f, 0f,  16f), radius = 5f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.MudPatch, position = new Vector3( 10f, 0f, -20f), radius = 4f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.MudPatch, position = new Vector3(-10f, 0f,  20f), radius = 4f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.MudPatch, position = new Vector3( 56f, 0f,  44f), radius = 4f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.MudPatch, position = new Vector3(-56f, 0f, -44f), radius = 4f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.MudPatch, position = new Vector3( 82f, 0f,  30f), radius = 4.5f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.MudPatch, position = new Vector3(-82f, 0f, -30f), radius = 4.5f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.ThornPatch, position = new Vector3( 96f, 0f, -44f), radius = 4f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.ThornPatch, position = new Vector3(-96f, 0f,  44f), radius = 4f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.ThornPatch, position = new Vector3( 90f, 0f,  70f), radius = 3.5f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.ThornPatch, position = new Vector3(-90f, 0f, -70f), radius = 3.5f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.ThornPatch, position = new Vector3(-90f, 0f, -88f), radius = 4f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.ThornPatch, position = new Vector3( 90f, 0f,  88f), radius = 4f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.ThornPatch, position = new Vector3(-24f, 0f, -36f), radius = 3f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.ThornPatch, position = new Vector3( 24f, 0f,  36f), radius = 3f },
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
        }
        }
