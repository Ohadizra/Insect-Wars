using UnityEngine;

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
            if (s_maps != null) return s_maps;
            s_maps = new[] { CreateFrozenExpanse(), CreateLavaPass() };
            return s_maps;
        }

        /// <summary>
        /// A small, aggressive volcanic map designed for fast-paced rush strategies.
        /// </summary>
        static SkirmishMapDefinition CreateLavaPass()
        {
            var map = ScriptableObject.CreateInstance<SkirmishMapDefinition>();
            map.name = "LavaPass";
            map.displayName = "Lava Pass";
            map.description =
                "A narrow, scorched corridor where hives are mere seconds apart.\n" +
                "Brace for an immediate clash on this compact, basalt-covered rush map.";

            map.mapHalfExtent = 51.75f;

            // ── Spawns ──
            map.playerHivePosition    = new Vector3(-40.25f, 1f, -34.5f);
            map.enemyHivePosition     = new Vector3( 40.25f, 1f,  34.5f);
            map.playerArmyStart       = new Vector3(-40.25f, 0f, -34.5f);
            map.enemyArmyStart        = new Vector3( 40.25f, 0f,  34.5f);
            map.cameraFocusWorld      = new Vector3(-34.5f, 0f, -28.75f);
            map.bigApplePosition      = new Vector3(-32.2f, 1.5f, -25.3f);
            map.enemyBigApplePosition = new Vector3( 32.2f, 1.5f,  25.3f);

            map.passiveScatterSeed = 54321;

            // ── Visual Overrides (Lava Theme) ──
            map.clayColor = new Color(0.25f, 0.20f, 0.18f); // Warm dark stone
            map.mapBoundsColor = new Color(0.65f, 0.15f, 0.02f); // Darker magma glow
            map.scatterTheme = ScatterTheme.Lava;

            map.baseTerrainLayer = Resources.Load<TerrainLayer>("Materials/DrySoil_Layer");
            map.secondaryTerrainLayer = Resources.Load<TerrainLayer>("Materials/VolcanicBasalt_Layer");
            map.bigAppleMaterial = Resources.Load<Material>("Materials/CharredApple");
            #if UNITY_EDITOR
            if (map.baseTerrainLayer == null)
                map.baseTerrainLayer = UnityEditor.AssetDatabase.LoadAssetAtPath<TerrainLayer>(
                    "Assets/_InsectWars/Materials/DrySoil_Layer.terrainlayer");
            if (map.secondaryTerrainLayer == null)
                map.secondaryTerrainLayer = UnityEditor.AssetDatabase.LoadAssetAtPath<TerrainLayer>(
                    "Assets/_InsectWars/Materials/VolcanicBasalt_Layer.terrainlayer");
            if (map.bigAppleMaterial == null)
                map.bigAppleMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
                    "Assets/_InsectWars/Materials/CharredApple.mat");
            #endif

            // ── Elevation ──
            // UV = (worldPos + 51.75) / 103.5
            // Base radius 0.32 matches the 33.12m radius (0.32 * 103.5)
            map.highGrounds = new[]
            {
                // Main base plateaus
                new HighGroundPlaced { uv = new Vector2(0.1111f, 0.1667f), radius = 0.32f, rampWidth = 0.06f, heightFraction = 0.15f, rotation = 45f },
                new HighGroundPlaced { uv = new Vector2(0.8889f, 0.8333f), radius = 0.32f, rampWidth = 0.06f, heightFraction = 0.15f, rotation = 225f },
                // Central contestable ledge
                new HighGroundPlaced { uv = new Vector2(0.5f, 0.5f), radius = 0.094f, rampWidth = 0.07f, heightFraction = 0.12f, rotation = 135f },
            };

            // ── Clay Walls (Basalt Pillars) ──
            map.clay = new[]
            {
                // Narrowing the central pass
                new ClayPlaced { position = new Vector3(-17.25f, 0f, 5.75f), scale = new Vector3(8f, 6f, 3f) },
                new ClayPlaced { position = new Vector3( 17.25f, 0f, -5.75f), scale = new Vector3(8f, 6f, 3f) },
                new ClayPlaced { position = new Vector3(-5.75f, 0f, 17.25f), scale = new Vector3(3f, 6f, 8f) },
                new ClayPlaced { position = new Vector3( 5.75f, 0f, -17.25f), scale = new Vector3(3f, 6f, 8f) },
            };

            map.fruits = new[]
            {
                // Center fruit
                new FruitPlaced { position = new Vector3(0f, 0.6f, 0f), calories = 9000, gatherPerTick = 12, gatherSeconds = 4f },
                // Expansion-lite positions
                new FruitPlaced { position = new Vector3(-23f, 0.6f, 23f), calories = 6000, gatherPerTick = 8, gatherSeconds = 5f },
                new FruitPlaced { position = new Vector3( 23f, 0.6f, -23f), calories = 6000, gatherPerTick = 8, gatherSeconds = 5f },
            };

            map.terrainFeatures = new[]
            {
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3(0f, 0f, 0f), radius = 11.5f }, // Center Lava pool
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3(-25f, 0f, 0f), radius = 6f },  // Side Lava pool
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3( 25f, 0f, 0f), radius = 6f },  // Side Lava pool
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3(-20.7f, 0f, -20.7f), radius = 9.2f }, // Ash husks
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3( 20.7f, 0f,  20.7f), radius = 9.2f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.ThornPatch, position = new Vector3(0f, 0f, 40.25f), radius = 6.9f }, // Scorched earth
                new TerrainFeaturePlaced { type = TerrainFeatureType.ThornPatch, position = new Vector3(0f, 0f, -40.25f), radius = 6.9f },
            };

            map.decorativePrefabs = new[]
            {
                new DecorativePrefabPlaced { prefabPath = "Assets/_InsectWars/Models/VolcanicSpire.prefab", position = new Vector3(-46f, 0f, -46f), rotation = Vector3.zero, scale = Vector3.one * 1.725f },
                new DecorativePrefabPlaced { prefabPath = "Assets/_InsectWars/Models/VolcanicSpire.prefab", position = new Vector3( 46f, 0f,  46f), rotation = Vector3.zero, scale = Vector3.one * 1.725f },
                new DecorativePrefabPlaced { prefabPath = "Assets/_InsectWars/Models/VolcanicSpire.prefab", position = new Vector3(0f, 0f, 28.75f), rotation = new Vector3(0, 90, 0), scale = Vector3.one * 1.38f },
                new DecorativePrefabPlaced { prefabPath = "Assets/_InsectWars/Models/VolcanicSpire.prefab", position = new Vector3(0f, 0f, -28.75f), rotation = new Vector3(0, 270, 0), scale = Vector3.one * 1.38f },
            };

            return map;
        }

        /// <summary>
        /// A small, aggressive frozen map designed for fast-paced rush strategies.
        /// </summary>
        static SkirmishMapDefinition CreateFrozenPass()
        {
            var map = ScriptableObject.CreateInstance<SkirmishMapDefinition>();
            map.name = "FrozenPass";
            map.displayName = "Frozen Pass";
            map.description =
                "A narrow, icy corridor where hives are mere seconds apart.\n" +
                "Brace for an immediate frost-bitten clash on this compact rush map.";

            map.mapHalfExtent = 45f;

            // ── Spawns ──
            map.playerHivePosition    = new Vector3(-35f, 1f, -30f);
            map.enemyHivePosition     = new Vector3( 35f, 1f,  30f);
            map.playerArmyStart       = new Vector3(-35f, 0f, -30f);
            map.enemyArmyStart        = new Vector3( 35f, 0f,  30f);
            map.cameraFocusWorld      = new Vector3(-30f, 0f, -25f);
            map.bigApplePosition      = new Vector3(-28f, 1.5f, -22f);
            map.enemyBigApplePosition = new Vector3( 28f, 1.5f,  22f);

            map.passiveScatterSeed = 12345;

            // ── Visual Overrides ──
            map.clayColor = new Color(0.55f, 0.58f, 0.65f);
            map.mapBoundsColor = new Color(0.48f, 0.52f, 0.58f);
            map.scatterTheme = ScatterTheme.Frozen;

            map.baseTerrainLayer = Resources.Load<TerrainLayer>("Materials/RealisticFrozenEarth_Layer");
            map.secondaryTerrainLayer = Resources.Load<TerrainLayer>("Materials/RealisticSnow_Layer");
            map.bigAppleMaterial = Resources.Load<Material>("Materials/FrostedNastyApple");
        #if UNITY_EDITOR
            if (map.baseTerrainLayer == null)
                map.baseTerrainLayer = UnityEditor.AssetDatabase.LoadAssetAtPath<TerrainLayer>(
                    "Assets/_InsectWars/Materials/RealisticFrozenEarth_Layer.terrainlayer");
            if (map.secondaryTerrainLayer == null)
                map.secondaryTerrainLayer = UnityEditor.AssetDatabase.LoadAssetAtPath<TerrainLayer>(
                    "Assets/_InsectWars/Materials/RealisticSnow_Layer.terrainlayer");
            if (map.bigAppleMaterial == null)
                map.bigAppleMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
                    "Assets/_InsectWars/Materials/FrostedNastyApple.mat");
        #endif

            // ── Elevation ──
            // UV = (worldPos + 45) / 90
            map.highGrounds = new[]
            {
                // Main base plateaus
                new HighGroundPlaced { uv = new Vector2(0.11f, 0.16f), radius = 0.15f, rampWidth = 0.05f, heightFraction = 0.15f, rotation = 45f },
                new HighGroundPlaced { uv = new Vector2(0.89f, 0.84f), radius = 0.15f, rampWidth = 0.05f, heightFraction = 0.15f, rotation = 225f },
                // Central contestable ledge
                new HighGroundPlaced { uv = new Vector2(0.5f, 0.5f), radius = 0.1f, rampWidth = 0.06f, heightFraction = 0.1f, rotation = 135f },
            };

            // ── Clay Walls ──
            map.clay = new[]
            {
                // Narrowing the central pass
                new ClayPlaced { position = new Vector3(-15f, 0f, 5f), scale = new Vector3(8f, 4f, 2f) },
                new ClayPlaced { position = new Vector3( 15f, 0f, -5f), scale = new Vector3(8f, 4f, 2f) },
                new ClayPlaced { position = new Vector3(-5f, 0f, 15f), scale = new Vector3(2f, 4f, 8f) },
                new ClayPlaced { position = new Vector3( 5f, 0f, -15f), scale = new Vector3(3f, 6f, 8f) },
            };

            map.fruits = new[]
            {
                // Center fruit
                new FruitPlaced { position = new Vector3(0f, 0.6f, 0f), calories = 8000, gatherPerTick = 12, gatherSeconds = 4f },
                // Expansion-lite positions
                new FruitPlaced { position = new Vector3(-20f, 0.6f, 20f), calories = 5000, gatherPerTick = 8, gatherSeconds = 5f },
                new FruitPlaced { position = new Vector3( 20f, 0.6f, -20f), calories = 5000, gatherPerTick = 8, gatherSeconds = 5f },
            };

            map.terrainFeatures = new[]
            {
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3(0f, 0f, 0f), radius = 8f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3(-15f, 0f, -15f), radius = 6f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3( 15f, 0f,  15f), radius = 6f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.ThornPatch, position = new Vector3(0f, 0f, 30f), radius = 5f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.ThornPatch, position = new Vector3(0f, 0f, -30f), radius = 5f },
            };

            map.decorativePrefabs = new[]
            {
                new DecorativePrefabPlaced { prefabPath = "Assets/_InsectWars/Models/FrozenHiveSpire.prefab", position = new Vector3(-42f, 0f, -42f), rotation = Vector3.zero, scale = Vector3.one * 1.2f },
                new DecorativePrefabPlaced { prefabPath = "Assets/_InsectWars/Models/FrozenHiveSpire.prefab", position = new Vector3( 42f, 0f,  42f), rotation = Vector3.zero, scale = Vector3.one * 1.2f },
                new DecorativePrefabPlaced { prefabPath = "Assets/_InsectWars/Models/FrozenInsectHorn.prefab", position = new Vector3(0f, 0f, 20f), rotation = new Vector3(0, 90, 0), scale = Vector3.one * 1.5f },
                new DecorativePrefabPlaced { prefabPath = "Assets/_InsectWars/Models/FrozenInsectHorn.prefab", position = new Vector3(0f, 0f, -20f), rotation = new Vector3(0, 270, 0), scale = Vector3.one * 1.5f },
            };

            return map;
        }

        /// <summary>
        /// Large competitive frozen tundra map with diagonal mirror symmetry.
        /// Features protected main bases, natural expansions, and treacherous frozen terrain.
        /// </summary>
        static SkirmishMapDefinition CreateFrozenExpanse()
        {
            var map = ScriptableObject.CreateInstance<SkirmishMapDefinition>();
            map.name = "FrozenExpanse";
            map.displayName = "Frozen Expanse";
            map.description =
                "A vast frozen clearing at insect scale, where frost-covered stone barriers and icicle ridges define the battlefield.\n" +
                "Navigate treacherous frozen brambles and ice sheets to secure the central frosted apples.";

            map.mapHalfExtent = 120f;

            // ── Spawns (Diagonal Mirror Symmetry: negate both X and Z) ──
            // Army start at the hive XZ so units spawn right around the building
            map.playerHivePosition    = new Vector3(-105f, 1f, -100f);
            map.enemyHivePosition     = new Vector3( 105f, 1f,  100f);
            map.playerArmyStart       = new Vector3(-105f, 0f, -100f);
            map.enemyArmyStart        = new Vector3( 105f, 0f,  100f);
            map.cameraFocusWorld      = new Vector3(-100f, 0f, -95f);
            map.bigApplePosition      = new Vector3(-93f, 1.5f, -88f);
            map.enemyBigApplePosition = new Vector3( 93f, 1.5f,  88f);

            map.passiveScatterSeed = 98213;

            // ── Visual Overrides ──
            map.clayColor = new Color(0.55f, 0.58f, 0.65f);
            map.mapBoundsColor = new Color(0.48f, 0.52f, 0.58f);
            map.scatterTheme = ScatterTheme.Frozen;

            map.baseTerrainLayer = Resources.Load<TerrainLayer>("Materials/RealisticFrozenEarth_Layer");
            map.secondaryTerrainLayer = Resources.Load<TerrainLayer>("Materials/RealisticSnow_Layer");
            map.bigAppleMaterial = Resources.Load<Material>("Materials/FrostedNastyApple");
#if UNITY_EDITOR
            if (map.baseTerrainLayer == null)
                map.baseTerrainLayer = UnityEditor.AssetDatabase.LoadAssetAtPath<TerrainLayer>(
                    "Assets/_InsectWars/Materials/RealisticFrozenEarth_Layer.terrainlayer");
            if (map.secondaryTerrainLayer == null)
                map.secondaryTerrainLayer = UnityEditor.AssetDatabase.LoadAssetAtPath<TerrainLayer>(
                    "Assets/_InsectWars/Materials/RealisticSnow_Layer.terrainlayer");
            if (map.bigAppleMaterial == null)
                map.bigAppleMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
                    "Assets/_InsectWars/Materials/FrostedNastyApple.mat");
#endif

            // ── Elevation (High-Ground Plateaus) ──
            // UV = (worldPos + 120) / 240
            map.highGrounds = new[]
            {
                new HighGroundPlaced { uv = new Vector2(0.0625f, 0.0833f), radius = 0.12f, rampWidth = 0.022f, heightFraction = 0.20f, rotation = 45f },
                new HighGroundPlaced { uv = new Vector2(0.9375f, 0.9167f), radius = 0.12f, rampWidth = 0.022f, heightFraction = 0.20f, rotation = 225f },
                // Natural expansions
                new HighGroundPlaced { uv = new Vector2(0.1875f, 0.2292f), radius = 0.06f, rampWidth = 0.03f, heightFraction = 0.08f },
                new HighGroundPlaced { uv = new Vector2(0.8125f, 0.7708f), radius = 0.06f, rampWidth = 0.03f, heightFraction = 0.08f },
                // Exposed Third Bases
                new HighGroundPlaced { uv = new Vector2(0.3542f, 0.1875f), radius = 0.065f, rampWidth = 0.035f, heightFraction = 0.07f },
                new HighGroundPlaced { uv = new Vector2(0.6458f, 0.8125f), radius = 0.065f, rampWidth = 0.035f, heightFraction = 0.07f },
                // Pocket Fourth Bases
                new HighGroundPlaced { uv = new Vector2(0.1042f, 0.3750f), radius = 0.06f, rampWidth = 0.025f, heightFraction = 0.08f },
                new HighGroundPlaced { uv = new Vector2(0.8958f, 0.6250f), radius = 0.06f, rampWidth = 0.025f, heightFraction = 0.08f },
                // Elevated Siege Positions
                new HighGroundPlaced { uv = new Vector2(0.4375f, 0.3333f), radius = 0.04f, rampWidth = 0.025f, heightFraction = 0.05f },
                new HighGroundPlaced { uv = new Vector2(0.5625f, 0.6667f), radius = 0.04f, rampWidth = 0.025f, heightFraction = 0.05f },
                new HighGroundPlaced { uv = new Vector2(0.3333f, 0.4375f), radius = 0.04f, rampWidth = 0.025f, heightFraction = 0.05f },
                new HighGroundPlaced { uv = new Vector2(0.6667f, 0.5625f), radius = 0.04f, rampWidth = 0.025f, heightFraction = 0.05f },
                // Neighborhood Overlooks
                new HighGroundPlaced { uv = new Vector2(0.1667f, 0.8333f), radius = 0.03f, rampWidth = 0.02f, heightFraction = 0.04f, rotation = 315f }, // NW Overlook
                new HighGroundPlaced { uv = new Vector2(0.8333f, 0.1667f), radius = 0.03f, rampWidth = 0.02f, heightFraction = 0.04f, rotation = 135f }, // SE Overlook
                new HighGroundPlaced { uv = new Vector2(0.4583f, 0.5417f), radius = 0.025f, rampWidth = 0.02f, heightFraction = 0.03f, rotation = 45f },  // Center Overlook A
                new HighGroundPlaced { uv = new Vector2(0.5417f, 0.4583f), radius = 0.025f, rampWidth = 0.02f, heightFraction = 0.03f, rotation = 225f }, // Center Overlook B
            };

            // ── Clay Walls (Frozen Stone Barriers) ──
            map.clay = new[]
            {
                // Natural expansion defenses
                new ClayPlaced { position = new Vector3(-65f, 0f, -42f), scale = new Vector3(12f, 5f, 2f) },
                new ClayPlaced { position = new Vector3(-42f, 0f, -65f), scale = new Vector3(2f, 5f, 12f) },
                new ClayPlaced { position = new Vector3(-82f, 0f, -48f), scale = new Vector3(8f, 5f, 2f) },
                new ClayPlaced { position = new Vector3(-48f, 0f, -82f), scale = new Vector3(2f, 5f, 8f) },
                new ClayPlaced { position = new Vector3( 65f, 0f,  42f), scale = new Vector3(12f, 5f, 2f) },
                new ClayPlaced { position = new Vector3( 42f, 0f,  65f), scale = new Vector3(2f, 5f, 12f) },
                new ClayPlaced { position = new Vector3( 82f, 0f,  48f), scale = new Vector3(8f, 5f, 2f) },
                new ClayPlaced { position = new Vector3( 48f, 0f,  82f), scale = new Vector3(2f, 5f, 8f) },
                // Mid lane chokes
                new ClayPlaced { position = new Vector3(-15f, 0f, -15f), scale = new Vector3(4f, 5f, 4f) },
                new ClayPlaced { position = new Vector3( 15f, 0f,  15f), scale = new Vector3(4f, 5f, 4f) },
                new ClayPlaced { position = new Vector3(-35f, 0f,  35f), scale = new Vector3(12f, 5f, 2f) },
                new ClayPlaced { position = new Vector3( 35f, 0f, -35f), scale = new Vector3(12f, 5f, 2f) },
                // Flanking Route Chokes
                new ClayPlaced { position = new Vector3(-45f, 0f, 55f), scale = new Vector3(2f, 5f, 8f) },
                new ClayPlaced { position = new Vector3( 45f, 0f, -55f), scale = new Vector3(2f, 5f, 8f) },
                new ClayPlaced { position = new Vector3(-110f, 0f, 30f), scale = new Vector3(8f, 5f, 2f) },
                new ClayPlaced { position = new Vector3( 110f, 0f, -30f), scale = new Vector3(8f, 5f, 2f) },
                // Expansion path chokes
                new ClayPlaced { position = new Vector3(-60f, 0f, -85f), scale = new Vector3(4f, 5f, 4f) },
                new ClayPlaced { position = new Vector3( 60f, 0f,  85f), scale = new Vector3(4f, 5f, 4f) },
            };

            map.fruits = new[]
            {
                new FruitPlaced { position = new Vector3(-75f, 0.6f, -65f), calories = 12000, gatherPerTick = 10, gatherSeconds = 5f },
                new FruitPlaced { position = new Vector3( 75f, 0.6f,  65f), calories = 12000, gatherPerTick = 10, gatherSeconds = 5f },
                new FruitPlaced { position = new Vector3(-35f, 0.6f, -75f), calories = 9000, gatherPerTick = 8, gatherSeconds = 5f },
                new FruitPlaced { position = new Vector3( 35f, 0.6f,  75f), calories = 9000, gatherPerTick = 8, gatherSeconds = 5f },
                new FruitPlaced { position = new Vector3(-95f, 0.6f, -30f), calories = 7000, gatherPerTick = 8, gatherSeconds = 5f },
                new FruitPlaced { position = new Vector3( 95f, 0.6f,  30f), calories = 7000, gatherPerTick = 8, gatherSeconds = 5f },
                new FruitPlaced { position = new Vector3(-105f, 0.6f, 85f), calories = 6000, gatherPerTick = 8, gatherSeconds = 5f },
                new FruitPlaced { position = new Vector3( 105f, 0.6f, -85f), calories = 6000, gatherPerTick = 8, gatherSeconds = 5f },
                new FruitPlaced { position = new Vector3(  0f, 0.6f,   0f), calories = 6000, gatherPerTick = 10, gatherSeconds = 5f },
                new FruitPlaced { position = new Vector3(-45f, 0.6f,  15f), calories = 5500, gatherPerTick = 10, gatherSeconds = 5f },
                new FruitPlaced { position = new Vector3( 45f, 0.6f, -15f), calories = 5500, gatherPerTick = 10, gatherSeconds = 5f },
                new FruitPlaced { position = new Vector3(-15f, 0.6f,  45f), calories = 5000, gatherPerTick = 10, gatherSeconds = 5f },
                new FruitPlaced { position = new Vector3( 15f, 0.6f, -45f), calories = 5000, gatherPerTick = 10, gatherSeconds = 5f },
                // Flanking Resources
                new FruitPlaced { position = new Vector3(-55f, 0.6f,  65f), calories = 4500, gatherPerTick = 8, gatherSeconds = 5f },
                new FruitPlaced { position = new Vector3( 55f, 0.6f, -65f), calories = 4500, gatherPerTick = 8, gatherSeconds = 5f },
                new FruitPlaced { position = new Vector3(-112f, 0.6f,  0f), calories = 3000, gatherPerTick = 8, gatherSeconds = 5f },
                new FruitPlaced { position = new Vector3( 112f, 0.6f,  0f), calories = 3000, gatherPerTick = 8, gatherSeconds = 5f },
                new FruitPlaced { position = new Vector3(-95f, 0.6f, 105f), calories = 3500, gatherPerTick = 8, gatherSeconds = 5f },
                new FruitPlaced { position = new Vector3( 95f, 0.6f,-105f), calories = 3500, gatherPerTick = 8, gatherSeconds = 5f },
            };

            map.terrainFeatures = new[]
            {
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3(0f, 0f, 0f), radius = 15f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3(-60f, 0f, 60f), radius = 10f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3( 60f, 0f, -60f), radius = 10f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3(-55f, 0f, -35f), radius = 8f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3( 55f, 0f,  35f), radius = 8f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3(-35f, 0f, -55f), radius = 8f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3( 35f, 0f,  55f), radius = 8f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.MudPatch, position = new Vector3(-25f, 0f, -25f), radius = 7f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.MudPatch, position = new Vector3( 25f, 0f,  25f), radius = 7f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.ThornPatch, position = new Vector3(-110f, 0f, 20f), radius = 7f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.ThornPatch, position = new Vector3( 110f, 0f, -20f), radius = 7f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.ThornPatch, position = new Vector3( 20f, 0f, -110f), radius = 7f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.ThornPatch, position = new Vector3(-20f, 0f,  110f), radius = 7f },
                // Mid-map lane dividers
                new TerrainFeaturePlaced { type = TerrainFeatureType.RockyRidge, position = new Vector3(-55f, 0f, 5f), rotation = 70f, boxHalfExtents = new Vector2(12f, 4f) },
                new TerrainFeaturePlaced { type = TerrainFeatureType.RockyRidge, position = new Vector3( 55f, 0f, -5f), rotation = 70f, boxHalfExtents = new Vector2(12f, 4f) },
                new TerrainFeaturePlaced { type = TerrainFeatureType.RockyRidge, position = new Vector3( 5f, 0f, -55f), rotation = 160f, boxHalfExtents = new Vector2(12f, 4f) },
                new TerrainFeaturePlaced { type = TerrainFeatureType.RockyRidge, position = new Vector3(-5f, 0f,  55f), rotation = 160f, boxHalfExtents = new Vector2(12f, 4f) },
                
                // --- NEW CONTENT: NW Marsh Neighborhood ---
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3(-70f, 0f, 70f), radius = 9f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3(-50f, 0f, 75f), radius = 7f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3(-65f, 0f, 80f), radius = 6f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.RockyRidge, position = new Vector3(-60f, 0f, 50f), rotation = 30f, boxHalfExtents = new Vector2(8f, 3f) },
                
                // --- NEW CONTENT: SE Marsh Neighborhood (Mirror) ---
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3( 70f, 0f, -70f), radius = 9f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3( 50f, 0f, -75f), radius = 7f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3( 65f, 0f, -80f), radius = 6f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.RockyRidge, position = new Vector3( 60f, 0f, -50f), rotation = 210f, boxHalfExtents = new Vector2(8f, 3f) },
                
                // --- NEW CONTENT: Far Quadrant Pockets ---
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3(-100f, 0f, 100f), radius = 6f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.MudPatch, position = new Vector3(-90f, 0f, 95f), radius = 5f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.RockyRidge, position = new Vector3(-85f, 0f, 110f), rotation = 0f, boxHalfExtents = new Vector2(6f, 2f) },
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3( 100f, 0f, -100f), radius = 6f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.MudPatch, position = new Vector3( 90f, 0f, -95f), radius = 5f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.RockyRidge, position = new Vector3( 85f, 0f, -110f), rotation = 180f, boxHalfExtents = new Vector2(6f, 2f) },
                
                // --- NEW CONTENT: Edge Corridors ---
                new TerrainFeaturePlaced { type = TerrainFeatureType.RockyRidge, position = new Vector3(-110f, 0f, 0f), rotation = 0f, boxHalfExtents = new Vector2(15f, 3f) },
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3(-115f, 0f, -10f), radius = 6f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.MudPatch, position = new Vector3(-110f, 0f, 15f), radius = 5f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.RockyRidge, position = new Vector3( 110f, 0f, 0f), rotation = 0f, boxHalfExtents = new Vector2(15f, 3f) },
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3( 115f, 0f, 10f), radius = 6f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.MudPatch, position = new Vector3( 110f, 0f, -15f), radius = 5f },
                
                // --- NEW CONTENT: Northern/Southern Wastelands ---
                new TerrainFeaturePlaced { type = TerrainFeatureType.ThornPatch, position = new Vector3(-15f, 0f, 100f), radius = 7f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.ThornPatch, position = new Vector3( 15f, 0f, 100f), radius = 7f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3(0f, 0f, 110f), radius = 8f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3(20f, 0f, 110f), radius = 5f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.ThornPatch, position = new Vector3( 15f, 0f, -100f), radius = 7f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.ThornPatch, position = new Vector3(-15f, 0f, -100f), radius = 7f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3(0f, 0f, -110f), radius = 8f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3(-20f, 0f, -110f), radius = 5f },
                
                // --- NEW CONTENT: Center Complexity ---
                new TerrainFeaturePlaced { type = TerrainFeatureType.RockyRidge, position = new Vector3(-8f, 0f, 8f), rotation = 45f, boxHalfExtents = new Vector2(5f, 2f) },
                new TerrainFeaturePlaced { type = TerrainFeatureType.RockyRidge, position = new Vector3( 8f, 0f, -8f), rotation = 45f, boxHalfExtents = new Vector2(5f, 2f) },
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3(10f, 0f, 10f), radius = 4f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3(-10f, 0f, -10f), radius = 4f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.MudPatch, position = new Vector3(0f, 0f, 15f), radius = 4f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.MudPatch, position = new Vector3(0f, 0f, -15f), radius = 4f },
            };

            map.decorativePrefabs = new[]
            {
                new DecorativePrefabPlaced { prefabPath = "Assets/_InsectWars/Models/FrozenHiveSpire.prefab", position = new Vector3(-115f, 0f, -115f), rotation = Vector3.zero, scale = Vector3.one * 1.5f },
                new DecorativePrefabPlaced { prefabPath = "Assets/_InsectWars/Models/FrozenInsectPillar.prefab", position = new Vector3(-70f, 0f, -95f), rotation = new Vector3(0, 45, 0), scale = Vector3.one * 1.2f },
                new DecorativePrefabPlaced { prefabPath = "Assets/_InsectWars/Models/FrozenHiveSpire.prefab", position = new Vector3(115f, 0f, 115f), rotation = Vector3.zero, scale = Vector3.one * 1.5f },
                new DecorativePrefabPlaced { prefabPath = "Assets/_InsectWars/Models/FrozenInsectPillar.prefab", position = new Vector3(70f, 0f, 95f), rotation = new Vector3(0, 225, 0), scale = Vector3.one * 1.2f },
                new DecorativePrefabPlaced { prefabPath = "Assets/_InsectWars/Models/FrozenInsectHorn.prefab", position = new Vector3(0f, 0f, 65f), rotation = new Vector3(0, 90, 0), scale = Vector3.one * 2f },
                new DecorativePrefabPlaced { prefabPath = "Assets/_InsectWars/Models/FrozenInsectHorn.prefab", position = new Vector3(0f, 0f, -65f), rotation = new Vector3(0, 270, 0), scale = Vector3.one * 2f },
                new DecorativePrefabPlaced { prefabPath = "Assets/_InsectWars/Models/FrozenChitinClaw.prefab", position = new Vector3(65f, 0f, 0f), rotation = Vector3.zero, scale = Vector3.one * 1.8f },
                new DecorativePrefabPlaced { prefabPath = "Assets/_InsectWars/Models/FrozenChitinClaw.prefab", position = new Vector3(-65f, 0f, 0f), rotation = new Vector3(0, 180, 0), scale = Vector3.one * 1.8f },
                
                // New Landmark Decoration
                new DecorativePrefabPlaced { prefabPath = "Assets/_InsectWars/Models/FrozenHiveSpire_Organic.prefab", position = new Vector3(0f, 0f, 118f), rotation = new Vector3(0, 180, 0), scale = Vector3.one * 2.2f },
                new DecorativePrefabPlaced { prefabPath = "Assets/_InsectWars/Models/FrozenHiveSpire_Organic.prefab", position = new Vector3(0f, 0f, -118f), rotation = Vector3.zero, scale = Vector3.one * 2.2f },
                new DecorativePrefabPlaced { prefabPath = "Assets/_InsectWars/Models/FrozenInsectPillar.prefab", position = new Vector3(-60f, 0f, 60f), rotation = new Vector3(0, 120, 0), scale = Vector3.one * 1.1f },
                new DecorativePrefabPlaced { prefabPath = "Assets/_InsectWars/Models/FrozenInsectPillar.prefab", position = new Vector3( 60f, 0f, -60f), rotation = new Vector3(0, 300, 0), scale = Vector3.one * 1.1f },
                new DecorativePrefabPlaced { prefabPath = "Assets/_InsectWars/Models/FrozenOrganicPillar.prefab", position = new Vector3(-105f, 0f, 95f), rotation = Vector3.zero, scale = Vector3.one * 1.5f },
                new DecorativePrefabPlaced { prefabPath = "Assets/_InsectWars/Models/FrozenOrganicPillar.prefab", position = new Vector3( 105f, 0f, -95f), rotation = Vector3.zero, scale = Vector3.one * 1.5f },
                new DecorativePrefabPlaced { prefabPath = "Assets/_InsectWars/Models/FrozenChitinClaw.prefab", position = new Vector3(-15f, 0f, 15f), rotation = new Vector3(0, 45, 0), scale = Vector3.one * 1.3f },
                new DecorativePrefabPlaced { prefabPath = "Assets/_InsectWars/Models/FrozenChitinClaw.prefab", position = new Vector3( 15f, 0f, -15f), rotation = new Vector3(0, 225, 0), scale = Vector3.one * 1.3f },
            };

            return map;
            }

        // ────────────────────── Tutorial ──────────────────────

        static SkirmishMapDefinition s_tutorialMap;

        public static SkirmishMapDefinition GetTutorialMap()
        {
            if (s_tutorialMap != null) return s_tutorialMap;
            s_tutorialMap = CreateTutorialMap();
            return s_tutorialMap;
        }

        static SkirmishMapDefinition CreateTutorialMap()
        {
            var map = ScriptableObject.CreateInstance<SkirmishMapDefinition>();
            map.name = "TutorialMap";
            map.displayName = "Tutorial";
            map.description = "A compact guided tutorial map.";

            map.mapHalfExtent = 22f;

            map.playerHivePosition    = new Vector3(0f, 1f, 0f);
            map.enemyHivePosition     = new Vector3(200f, 1f, 200f);
            map.playerArmyStart       = new Vector3(0f, 0f, 0f);
            map.enemyArmyStart        = new Vector3(200f, 0f, 200f);
            map.cameraFocusWorld      = new Vector3(0f, 0f, 0f);
            map.bigApplePosition      = new Vector3(12f, 1.5f, 8f);
            map.enemyBigApplePosition = new Vector3(200f, 1.5f, 200f);

            map.passiveScatterSeed = 77777;

            map.highGrounds = System.Array.Empty<HighGroundPlaced>();
            map.clay = System.Array.Empty<ClayPlaced>();
            map.terrainFeatures = System.Array.Empty<TerrainFeaturePlaced>();
            map.decorativePrefabs = System.Array.Empty<DecorativePrefabPlaced>();
            map.fruits = new[]
            {
                new FruitPlaced { position = new Vector3(12f, 0.6f, 8f), calories = 10000, gatherPerTick = 15, gatherSeconds = 3f },
            };

            return map;
        }

        // ────────────────────── Learning / Sandbox ──────────────────────

        static SkirmishMapDefinition s_learningMap;

        public static SkirmishMapDefinition GetLearningMap()
        {
            if (s_learningMap != null) return s_learningMap;
            s_learningMap = CreateLearningMap();
            return s_learningMap;
        }

        static SkirmishMapDefinition CreateLearningMap()
        {
            var map = ScriptableObject.CreateInstance<SkirmishMapDefinition>();
            map.name = "LearningMap";
            map.displayName = "Play-Ground";
            map.description = "A flat sandbox for learning the basics.\nNo enemy AI — practice building, gathering, and combat on a training dummy.";

            map.mapHalfExtent = 35f;

            map.playerHivePosition    = new Vector3(-10f, 1f, -10f);
            map.enemyHivePosition     = new Vector3(100f, 1f, 100f);
            map.playerArmyStart       = new Vector3(-10f, 0f, -10f);
            map.enemyArmyStart        = new Vector3(100f, 0f, 100f);
            map.cameraFocusWorld      = new Vector3(-6f, 0f, -6f);
            map.bigApplePosition      = new Vector3(-5f, 1.5f, 5f);
            map.enemyBigApplePosition = new Vector3(100f, 1.5f, 100f);

            map.passiveScatterSeed = 55555;

            map.highGrounds = new[]
            {
                // Test plateau — near player start, ramp faces SW so only climbers can reach the top from other sides
                new HighGroundPlaced
                {
                    uv = new Vector2(0.72f, 0.72f),  // NE quadrant of the 70m map → world ~(15, 15)
                    radius = 0.12f,
                    rampWidth = 0.04f,
                    heightFraction = 0.12f,
                    rotation = 225f  // ramp faces SW toward player base
                },
            };
            map.clay = System.Array.Empty<ClayPlaced>();
            map.terrainFeatures = System.Array.Empty<TerrainFeaturePlaced>();
            map.decorativePrefabs = System.Array.Empty<DecorativePrefabPlaced>();

            map.fruits = new[]
            {
                new FruitPlaced { position = new Vector3(-5f, 0.6f, 5f), calories = 10000, gatherPerTick = 10, gatherSeconds = 5f },
            };

            return map;
        }
    }
}

