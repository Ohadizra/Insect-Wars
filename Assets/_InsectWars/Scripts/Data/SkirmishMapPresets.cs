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
            s_maps = new[] { CreateThornBasin(), CreateLalush() };
            return s_maps;
        }

        /// <summary>
        /// Small 1v1 map with diagonal mirror symmetry. Each side has one main apple (calorie mine)
        /// near their hive, with contested resources at the center.
        /// </summary>
        static SkirmishMapDefinition CreateThornBasin()
        {
            var map = ScriptableObject.CreateInstance<SkirmishMapDefinition>();
            map.name = "ThornBasin";
            map.displayName = "Thorn Basin";
            map.description = "A small clearing between twisted roots.\nEach side controls one calorie deposit. Fight for the contested center.";

            map.mapHalfExtent = 55f;

            map.playerHivePosition = new Vector3(-38f, 1f, -38f);
            map.enemyHivePosition = new Vector3(38f, 1f, 38f);
            map.playerArmyStart = new Vector3(-30f, 0f, -30f);
            map.enemyArmyStart = new Vector3(30f, 0f, 30f);
            map.cameraFocusWorld = new Vector3(-28f, 0f, -26f);

            map.bigApplePosition = new Vector3(-30f, 1.5f, -22f);
            map.enemyBigApplePosition = new Vector3(30f, 1.5f, 22f);

            map.passiveScatterSeed = 44291;

            map.highGrounds = new[]
            {
                new HighGroundPlaced { uv = new Vector2(0.5f, 0.5f), radius = 0.06f, rampWidth = 0.05f, heightFraction = 0.06f },
            };

            map.clay = new[]
            {
                // Central choke — mirrored pair
                new ClayPlaced { position = new Vector3(-12f, 0f, -4f), scale = new Vector3(5f, 2.2f, 2.5f) },
                new ClayPlaced { position = new Vector3(12f, 0f, 4f), scale = new Vector3(5f, 2.2f, 2.5f) },
                // Side walls — mirrored pair
                new ClayPlaced { position = new Vector3(-6f, 0f, 18f), scale = new Vector3(3f, 2.5f, 6f) },
                new ClayPlaced { position = new Vector3(6f, 0f, -18f), scale = new Vector3(3f, 2.5f, 6f) },
                // Flank walls near expansions — mirrored pair
                new ClayPlaced { position = new Vector3(-28f, 0f, 10f), scale = new Vector3(4f, 2f, 3f) },
                new ClayPlaced { position = new Vector3(28f, 0f, -10f), scale = new Vector3(4f, 2f, 3f) },
                // Base entrance narrowing — mirrored pair
                new ClayPlaced { position = new Vector3(-22f, 0f, -18f), scale = new Vector3(3f, 2.4f, 4f) },
                new ClayPlaced { position = new Vector3(22f, 0f, 18f), scale = new Vector3(3f, 2.4f, 4f) },
            };

            map.fruits = new[]
            {
                // Contested center fruit
                new FruitPlaced { position = new Vector3(0f, 0.6f, 0f), calories = 8000, gatherPerTick = 10, gatherSeconds = 5f },
                // Side expansion fruits — mirrored pair
                new FruitPlaced { position = new Vector3(-18f, 0.6f, 14f), calories = 6000, gatherPerTick = 8, gatherSeconds = 5f },
                new FruitPlaced { position = new Vector3(18f, 0.6f, -14f), calories = 6000, gatherPerTick = 8, gatherSeconds = 5f },
                // Far flank fruits — mirrored pair
                new FruitPlaced { position = new Vector3(-40f, 0.6f, 8f), calories = 5000, gatherPerTick = 8, gatherSeconds = 5f },
                new FruitPlaced { position = new Vector3(40f, 0.6f, -8f), calories = 5000, gatherPerTick = 8, gatherSeconds = 5f },
            };

            map.cactiSeeds = new[]
            {
                // Center pair
                new CactiSeedPlaced { position = new Vector3(0f, 0f, -8f) },
                new CactiSeedPlaced { position = new Vector3(0f, 0f, 8f) },
                // Near-base pair
                new CactiSeedPlaced { position = new Vector3(-20f, 0f, -20f) },
                new CactiSeedPlaced { position = new Vector3(20f, 0f, 20f) },
                // Flank pair
                new CactiSeedPlaced { position = new Vector3(-12f, 0f, 28f) },
                new CactiSeedPlaced { position = new Vector3(12f, 0f, -28f) },
                // Outer pair
                new CactiSeedPlaced { position = new Vector3(35f, 0f, 5f) },
                new CactiSeedPlaced { position = new Vector3(-35f, 0f, -5f) },
            };

            map.terrainFeatures = new[]
            {
                // Central water puddle — slows any army that pushes through mid
                new TerrainFeaturePlaced
                {
                    type = TerrainFeatureType.WaterPuddle,
                    position = new Vector3(0f, 0f, 0f),
                    radius = 6f
                },

                // Tall grass flanks — ambush positions on each side (mirrored)
                new TerrainFeaturePlaced
                {
                    type = TerrainFeatureType.TallGrass,
                    position = new Vector3(-18f, 0f, 6f),
                    radius = 5f
                },
                new TerrainFeaturePlaced
                {
                    type = TerrainFeatureType.TallGrass,
                    position = new Vector3(18f, 0f, -6f),
                    radius = 5f
                },

                // Mud patches near expansions — defend your expansion or push through slowly (mirrored)
                new TerrainFeaturePlaced
                {
                    type = TerrainFeatureType.MudPatch,
                    position = new Vector3(-34f, 0f, 2f),
                    radius = 4f
                },
                new TerrainFeaturePlaced
                {
                    type = TerrainFeatureType.MudPatch,
                    position = new Vector3(34f, 0f, -2f),
                    radius = 4f
                },

                // Thorn patches guarding side routes — punish reckless flanking (mirrored)
                new TerrainFeaturePlaced
                {
                    type = TerrainFeatureType.ThornPatch,
                    position = new Vector3(-8f, 0f, 24f),
                    radius = 3.5f
                },
                new TerrainFeaturePlaced
                {
                    type = TerrainFeatureType.ThornPatch,
                    position = new Vector3(8f, 0f, -24f),
                    radius = 3.5f
                },

                // Rocky ridge walls forming a central choke (mirrored)
                new TerrainFeaturePlaced
                {
                    type = TerrainFeatureType.RockyRidge,
                    position = new Vector3(-6f, 0f, -12f),
                    radius = 0f, rotation = 30f,
                    boxHalfExtents = new Vector2(4f, 1.5f)
                },
                new TerrainFeaturePlaced
                {
                    type = TerrainFeatureType.RockyRidge,
                    position = new Vector3(6f, 0f, 12f),
                    radius = 0f, rotation = 30f,
                    boxHalfExtents = new Vector2(4f, 1.5f)
                },
            };

            return map;
        }

        /// <summary>
        /// Medium 1v1 map inspired by the Lalush layout.
        /// Diagonal ridge bisects the map; swampy lowlands along edges force
        /// players to choose between the elevated bridge or slow flanking routes.
        /// </summary>
        static SkirmishMapDefinition CreateLalush()
        {
            var map = ScriptableObject.CreateInstance<SkirmishMapDefinition>();
            map.name = "Lalush";
            map.displayName = "Lalush Depths";
            map.description =
                "Swamp lowlands split by a massive diagonal ridge.\n" +
                "Control the bridge for center dominance, or flank through the waterlogged edges.";

            map.mapHalfExtent = 78f;

            map.playerHivePosition = new Vector3(-62f, 1f, -52f);
            map.enemyHivePosition = new Vector3(62f, 1f, 52f);
            map.playerArmyStart = new Vector3(-52f, 0f, -44f);
            map.enemyArmyStart = new Vector3(52f, 0f, 44f);
            map.cameraFocusWorld = new Vector3(-48f, 0f, -38f);

            map.bigApplePosition = new Vector3(-50f, 1.5f, -38f);
            map.enemyBigApplePosition = new Vector3(50f, 1.5f, 38f);

            map.passiveScatterSeed = 73519;

            // ── Elevation ──
            map.highGrounds = new[]
            {
                // Player main base plateau (bottom-left corner)
                new HighGroundPlaced { uv = new Vector2(0.15f, 0.17f), radius = 0.12f, rampWidth = 0.03f, heightFraction = 0.10f },
                // Enemy main base plateau (top-right corner, mirror)
                new HighGroundPlaced { uv = new Vector2(0.85f, 0.83f), radius = 0.12f, rampWidth = 0.03f, heightFraction = 0.10f },

                // Player natural expansion ridge
                new HighGroundPlaced { uv = new Vector2(0.30f, 0.37f), radius = 0.06f, rampWidth = 0.04f, heightFraction = 0.06f },
                // Enemy natural expansion ridge (mirror)
                new HighGroundPlaced { uv = new Vector2(0.70f, 0.63f), radius = 0.06f, rampWidth = 0.04f, heightFraction = 0.06f },

                // Central diagonal ridge — two overlapping circles forming the bridge
                new HighGroundPlaced { uv = new Vector2(0.42f, 0.43f), radius = 0.08f, rampWidth = 0.035f, heightFraction = 0.09f },
                new HighGroundPlaced { uv = new Vector2(0.58f, 0.57f), radius = 0.08f, rampWidth = 0.035f, heightFraction = 0.09f },

                // Upper-left flank plateau (third base pocket)
                new HighGroundPlaced { uv = new Vector2(0.22f, 0.73f), radius = 0.07f, rampWidth = 0.04f, heightFraction = 0.06f },
                // Lower-right flank plateau (mirror third base pocket)
                new HighGroundPlaced { uv = new Vector2(0.78f, 0.27f), radius = 0.07f, rampWidth = 0.04f, heightFraction = 0.06f },
            };

            // ── Impassable clay walls forming choke points ──
            map.clay = new[]
            {
                // Player base entrance walls
                new ClayPlaced { position = new Vector3(-38f, 0f, -30f), scale = new Vector3(4f, 2.5f, 2.5f) },
                new ClayPlaced { position = new Vector3(-44f, 0f, -28f), scale = new Vector3(2.5f, 2.5f, 5f) },
                // Enemy base entrance walls (mirror)
                new ClayPlaced { position = new Vector3(38f, 0f, 30f), scale = new Vector3(4f, 2.5f, 2.5f) },
                new ClayPlaced { position = new Vector3(44f, 0f, 28f), scale = new Vector3(2.5f, 2.5f, 5f) },

                // Central bridge side walls
                new ClayPlaced { position = new Vector3(-18f, 0f, 6f), scale = new Vector3(3f, 2.8f, 5f) },
                new ClayPlaced { position = new Vector3(18f, 0f, -6f), scale = new Vector3(3f, 2.8f, 5f) },

                // Flank base approach walls
                new ClayPlaced { position = new Vector3(-32f, 0f, 22f), scale = new Vector3(5f, 2.2f, 2.5f) },
                new ClayPlaced { position = new Vector3(32f, 0f, -22f), scale = new Vector3(5f, 2.2f, 2.5f) },

                // Edge walls narrowing side routes
                new ClayPlaced { position = new Vector3(-55f, 0f, 12f), scale = new Vector3(3f, 2.5f, 4f) },
                new ClayPlaced { position = new Vector3(55f, 0f, -12f), scale = new Vector3(3f, 2.5f, 4f) },

                // Outer barrier walls
                new ClayPlaced { position = new Vector3(10f, 0f, -36f), scale = new Vector3(4f, 2.2f, 3f) },
                new ClayPlaced { position = new Vector3(-10f, 0f, 36f), scale = new Vector3(4f, 2.2f, 3f) },
            };

            // ── Resources ──
            map.fruits = new[]
            {
                // Player natural expansion fruit
                new FruitPlaced { position = new Vector3(-30f, 0.6f, -14f), calories = 10000, gatherPerTick = 10, gatherSeconds = 5f },
                // Enemy natural expansion fruit (mirror)
                new FruitPlaced { position = new Vector3(30f, 0.6f, 14f), calories = 10000, gatherPerTick = 10, gatherSeconds = 5f },

                // Contested center fruit (on the bridge)
                new FruitPlaced { position = new Vector3(0f, 0.6f, 0f), calories = 8000, gatherPerTick = 10, gatherSeconds = 5f },

                // Upper-left third base fruit
                new FruitPlaced { position = new Vector3(-38f, 0.6f, 36f), calories = 8000, gatherPerTick = 8, gatherSeconds = 5f },
                // Lower-right third base fruit (mirror)
                new FruitPlaced { position = new Vector3(38f, 0.6f, -36f), calories = 8000, gatherPerTick = 8, gatherSeconds = 5f },

                // Side path bonus fruits
                new FruitPlaced { position = new Vector3(-58f, 0.6f, 30f), calories = 5000, gatherPerTick = 8, gatherSeconds = 5f },
                new FruitPlaced { position = new Vector3(58f, 0.6f, -30f), calories = 5000, gatherPerTick = 8, gatherSeconds = 5f },

                // Near-bridge approach fruits
                new FruitPlaced { position = new Vector3(-20f, 0.6f, -24f), calories = 6000, gatherPerTick = 8, gatherSeconds = 5f },
                new FruitPlaced { position = new Vector3(20f, 0.6f, 24f), calories = 6000, gatherPerTick = 8, gatherSeconds = 5f },
            };

            map.cactiSeeds = new[]
            {
                // Near player base
                new CactiSeedPlaced { position = new Vector3(-44f, 0f, -36f) },
                // Near enemy base (mirror)
                new CactiSeedPlaced { position = new Vector3(44f, 0f, 36f) },
                // Center pair flanking the bridge
                new CactiSeedPlaced { position = new Vector3(-8f, 0f, 12f) },
                new CactiSeedPlaced { position = new Vector3(8f, 0f, -12f) },
                // Natural expansion area
                new CactiSeedPlaced { position = new Vector3(-22f, 0f, -8f) },
                new CactiSeedPlaced { position = new Vector3(22f, 0f, 8f) },
                // Flank area
                new CactiSeedPlaced { position = new Vector3(-46f, 0f, 18f) },
                new CactiSeedPlaced { position = new Vector3(46f, 0f, -18f) },
                // Third base area
                new CactiSeedPlaced { position = new Vector3(-30f, 0f, 28f) },
                new CactiSeedPlaced { position = new Vector3(30f, 0f, -28f) },
                // Outer edge
                new CactiSeedPlaced { position = new Vector3(0f, 0f, -40f) },
                new CactiSeedPlaced { position = new Vector3(0f, 0f, 40f) },
            };

            // ── Terrain features ──
            map.terrainFeatures = new[]
            {
                // Water puddles — swampy lowlands along map edges
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3(-65f, 0f, -10f), radius = 8f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3(65f, 0f, 10f), radius = 8f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3(15f, 0f, -60f), radius = 7f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3(-15f, 0f, 60f), radius = 7f },
                // Central swamp beneath the bridge
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3(0f, 0f, 0f), radius = 5f },

                // Tall grass — watchtower / concealment positions
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3(-8f, 0f, 28f), radius = 5f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3(8f, 0f, -28f), radius = 5f },
                // Flank ambush grass near third bases
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3(-50f, 0f, 38f), radius = 4f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3(50f, 0f, -38f), radius = 4f },

                // Rocky ridges — cliff walls between areas
                new TerrainFeaturePlaced
                {
                    type = TerrainFeatureType.RockyRidge,
                    position = new Vector3(-40f, 0f, -18f),
                    radius = 0f, rotation = 45f,
                    boxHalfExtents = new Vector2(5f, 2f)
                },
                new TerrainFeaturePlaced
                {
                    type = TerrainFeatureType.RockyRidge,
                    position = new Vector3(40f, 0f, 18f),
                    radius = 0f, rotation = 45f,
                    boxHalfExtents = new Vector2(5f, 2f)
                },
                // Bridge-side cliff walls
                new TerrainFeaturePlaced
                {
                    type = TerrainFeatureType.RockyRidge,
                    position = new Vector3(-22f, 0f, 16f),
                    radius = 0f, rotation = 20f,
                    boxHalfExtents = new Vector2(4f, 1.5f)
                },
                new TerrainFeaturePlaced
                {
                    type = TerrainFeatureType.RockyRidge,
                    position = new Vector3(22f, 0f, -16f),
                    radius = 0f, rotation = 20f,
                    boxHalfExtents = new Vector2(4f, 1.5f)
                },

                // Mud patches — bridge approach slowdown
                new TerrainFeaturePlaced { type = TerrainFeatureType.MudPatch, position = new Vector3(-16f, 0f, -14f), radius = 4.5f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.MudPatch, position = new Vector3(16f, 0f, 14f), radius = 4.5f },

                // Thorn patches — guarding side route flanks
                new TerrainFeaturePlaced { type = TerrainFeatureType.ThornPatch, position = new Vector3(-60f, 0f, 4f), radius = 3.5f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.ThornPatch, position = new Vector3(60f, 0f, -4f), radius = 3.5f },
            };

            return map;
        }
    }
}

