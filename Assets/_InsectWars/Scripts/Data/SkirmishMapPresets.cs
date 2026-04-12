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
            s_maps = new[] { CreateThornBasin(), CreateLalush(), CreateFrozenExpanse() };
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
        /// Medium 1v1 map faithfully recreated from the classic SC2 Lalush layout.
        /// Corner bases with tight ramp exits, a massive diagonal bridge through the
        /// center, flanking third-base pockets, and swamp lowlands on every edge.
        /// </summary>
        static SkirmishMapDefinition CreateLalush()
        {
            var map = ScriptableObject.CreateInstance<SkirmishMapDefinition>();
            map.name = "Lalush";
            map.displayName = "Lalush Depths";
            map.description =
                "Swamp lowlands split by a diagonal ridge bridge.\n" +
                "Corner bases, tight ramp chokes, and contested third-base pockets on each flank.";

            map.mapHalfExtent = 78f;

            // ── Spawns (diagonal mirror: negate both X and Z) ──
            map.playerHivePosition       = new Vector3(-60f, 1f, -54f);
            map.enemyHivePosition        = new Vector3( 60f, 1f,  54f);
            map.playerArmyStart          = new Vector3(-50f, 0f, -46f);
            map.enemyArmyStart           = new Vector3( 50f, 0f,  46f);
            map.cameraFocusWorld         = new Vector3(-46f, 0f, -40f);
            map.bigApplePosition         = new Vector3(-52f, 1.5f, -40f);
            map.enemyBigApplePosition    = new Vector3( 52f, 1.5f,  40f);

            map.passiveScatterSeed = 73519;

            // ────────────────────── Elevation ──────────────────────
            // UV (0,0) = terrain SW corner; (1,1) = NE corner.
            // pos→uv: (pos + 78) / 156
            map.highGrounds = new[]
            {
                // Player main base — large corner plateau (SW)
                new HighGroundPlaced { uv = new Vector2(0.14f, 0.17f), radius = 0.10f, rampWidth = 0.025f, heightFraction = 0.12f },
                // Enemy main base — mirror (NE)
                new HighGroundPlaced { uv = new Vector2(0.86f, 0.83f), radius = 0.10f, rampWidth = 0.025f, heightFraction = 0.12f },

                // Player natural expansion — small ridge just NE of player base
                new HighGroundPlaced { uv = new Vector2(0.27f, 0.36f), radius = 0.05f, rampWidth = 0.035f, heightFraction = 0.07f },
                // Enemy natural expansion — mirror
                new HighGroundPlaced { uv = new Vector2(0.73f, 0.64f), radius = 0.05f, rampWidth = 0.035f, heightFraction = 0.07f },

                // Central diagonal bridge — three overlapping plateaus along the SW→NE diagonal
                new HighGroundPlaced { uv = new Vector2(0.40f, 0.42f), radius = 0.065f, rampWidth = 0.03f, heightFraction = 0.09f },
                new HighGroundPlaced { uv = new Vector2(0.50f, 0.50f), radius = 0.055f, rampWidth = 0.03f, heightFraction = 0.09f },
                new HighGroundPlaced { uv = new Vector2(0.60f, 0.58f), radius = 0.065f, rampWidth = 0.03f, heightFraction = 0.09f },

                // Third-base pocket — upper-left (NW)
                new HighGroundPlaced { uv = new Vector2(0.16f, 0.76f), radius = 0.08f, rampWidth = 0.035f, heightFraction = 0.08f },
                // Third-base pocket — lower-right (SE), mirror
                new HighGroundPlaced { uv = new Vector2(0.84f, 0.24f), radius = 0.08f, rampWidth = 0.035f, heightFraction = 0.08f },

                // Small watchtower-area bumps (match green circle positions in image)
                new HighGroundPlaced { uv = new Vector2(0.38f, 0.68f), radius = 0.035f, rampWidth = 0.03f, heightFraction = 0.04f },
                new HighGroundPlaced { uv = new Vector2(0.62f, 0.32f), radius = 0.035f, rampWidth = 0.03f, heightFraction = 0.04f },
            };

            // ────────────────────── Clay walls ──────────────────────
            map.clay = new[]
            {
                // Player base ramp choke — tight exit from the corner plateau
                new ClayPlaced { position = new Vector3(-40f, 0f, -32f), scale = new Vector3(4f, 3f, 2.5f) },
                new ClayPlaced { position = new Vector3(-36f, 0f, -38f), scale = new Vector3(2.5f, 3f, 4f) },
                // Enemy base ramp choke (mirror)
                new ClayPlaced { position = new Vector3( 40f, 0f,  32f), scale = new Vector3(4f, 3f, 2.5f) },
                new ClayPlaced { position = new Vector3( 36f, 0f,  38f), scale = new Vector3(2.5f, 3f, 4f) },

                // Player natural approach wall — blocks shortcut from natural to center
                new ClayPlaced { position = new Vector3(-22f, 0f, -20f), scale = new Vector3(3f, 2.8f, 5f) },
                // Mirror
                new ClayPlaced { position = new Vector3( 22f, 0f,  20f), scale = new Vector3(3f, 2.8f, 5f) },

                // Bridge side-wall south — forces traffic onto/around the bridge
                new ClayPlaced { position = new Vector3(-14f, 0f,  10f), scale = new Vector3(3.5f, 2.8f, 4f) },
                // Bridge side-wall north (mirror)
                new ClayPlaced { position = new Vector3( 14f, 0f, -10f), scale = new Vector3(3.5f, 2.8f, 4f) },

                // Third-base pocket entrance walls (NW pocket)
                new ClayPlaced { position = new Vector3(-30f, 0f,  28f), scale = new Vector3(4.5f, 2.5f, 2.5f) },
                new ClayPlaced { position = new Vector3(-24f, 0f,  34f), scale = new Vector3(2.5f, 2.5f, 4f) },
                // SE pocket (mirror)
                new ClayPlaced { position = new Vector3( 30f, 0f, -28f), scale = new Vector3(4.5f, 2.5f, 2.5f) },
                new ClayPlaced { position = new Vector3( 24f, 0f, -34f), scale = new Vector3(2.5f, 2.5f, 4f) },

                // Far-edge lane walls — narrow the outer routes
                new ClayPlaced { position = new Vector3(-56f, 0f,  16f), scale = new Vector3(3f, 2.5f, 5f) },
                new ClayPlaced { position = new Vector3( 56f, 0f, -16f), scale = new Vector3(3f, 2.5f, 5f) },

                // Diagonal barrier pushing traffic away from corners
                new ClayPlaced { position = new Vector3( 12f, 0f, -40f), scale = new Vector3(4f, 2.2f, 3f) },
                new ClayPlaced { position = new Vector3(-12f, 0f,  40f), scale = new Vector3(4f, 2.2f, 3f) },
            };

            // ────────────────────── Resources ──────────────────────
            map.fruits = new[]
            {
                // Player natural expansion fruit (rich — incentivizes early expand)
                new FruitPlaced { position = new Vector3(-32f, 0.6f, -12f), calories = 10000, gatherPerTick = 10, gatherSeconds = 5f },
                // Mirror
                new FruitPlaced { position = new Vector3( 32f, 0.6f,  12f), calories = 10000, gatherPerTick = 10, gatherSeconds = 5f },

                // Center fruit on the bridge — high-value, highly contested
                new FruitPlaced { position = new Vector3(0f, 0.6f, 0f), calories = 8000, gatherPerTick = 10, gatherSeconds = 5f },

                // Third-base pocket fruits (NW / SE)
                new FruitPlaced { position = new Vector3(-40f, 0.6f,  40f), calories = 8000, gatherPerTick = 8, gatherSeconds = 5f },
                new FruitPlaced { position = new Vector3( 40f, 0.6f, -40f), calories = 8000, gatherPerTick = 8, gatherSeconds = 5f },

                // Far-edge fruits — risky, far from bases
                new FruitPlaced { position = new Vector3(-60f, 0.6f,  32f), calories = 5000, gatherPerTick = 8, gatherSeconds = 5f },
                new FruitPlaced { position = new Vector3( 60f, 0.6f, -32f), calories = 5000, gatherPerTick = 8, gatherSeconds = 5f },

                // Off-bridge contested fruits (between natural and bridge)
                new FruitPlaced { position = new Vector3(-18f, 0.6f, -28f), calories = 6000, gatherPerTick = 8, gatherSeconds = 5f },
                new FruitPlaced { position = new Vector3( 18f, 0.6f,  28f), calories = 6000, gatherPerTick = 8, gatherSeconds = 5f },

                // Swamp-edge fruits — behind the thorn patches, reward brave flankers
                new FruitPlaced { position = new Vector3(-66f, 0.6f, -24f), calories = 4000, gatherPerTick = 8, gatherSeconds = 5f },
                new FruitPlaced { position = new Vector3( 66f, 0.6f,  24f), calories = 4000, gatherPerTick = 8, gatherSeconds = 5f },
            };

            // ────────────────────── Terrain features ──────────────────────
            map.terrainFeatures = new[]
            {
                // ── Water puddles — the swamp lowlands visible on all map edges ──

                // West edge swamp (large, runs along left side)
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3(-68f, 0f, -8f), radius = 9f },
                // East edge swamp (mirror)
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3( 68f, 0f,  8f), radius = 9f },
                // South edge swamp
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3( 18f, 0f, -64f), radius = 8f },
                // North edge swamp (mirror)
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3(-18f, 0f,  64f), radius = 8f },
                // Central low area beneath the bridge
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3(  0f, 0f,   0f), radius = 4f },
                // Swamp between player natural and west edge
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3(-54f, 0f, -28f), radius = 6f },
                // Mirror
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3( 54f, 0f,  28f), radius = 6f },

                // ── Tall grass — watchtower / Xel'Naga tower positions (green circles in image) ──

                // Upper watchtower (between NW pocket and bridge)
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3(-16f, 0f,  30f), radius = 5f },
                // Lower watchtower (mirror, between SE pocket and bridge)
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3( 16f, 0f, -30f), radius = 5f },
                // Left-center scouting bush (purple tower position in image)
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3(-48f, 0f,   6f), radius = 4f },
                // Right-center scouting bush (mirror)
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3( 48f, 0f,  -6f), radius = 4f },

                // ── Rocky ridges — the cliff walls that define lane boundaries ──

                // West cliff: separates player base from NW pocket
                new TerrainFeaturePlaced
                {
                    type = TerrainFeatureType.RockyRidge,
                    position = new Vector3(-50f, 0f, -4f),
                    rotation = 70f,
                    boxHalfExtents = new Vector2(6f, 2f)
                },
                // East cliff (mirror): separates enemy base from SE pocket
                new TerrainFeaturePlaced
                {
                    type = TerrainFeatureType.RockyRidge,
                    position = new Vector3( 50f, 0f,  4f),
                    rotation = 70f,
                    boxHalfExtents = new Vector2(6f, 2f)
                },
                // Bridge south cliff face — prevents walking under the bridge
                new TerrainFeaturePlaced
                {
                    type = TerrainFeatureType.RockyRidge,
                    position = new Vector3(-8f, 0f, -16f),
                    rotation = 40f,
                    boxHalfExtents = new Vector2(5f, 1.8f)
                },
                // Bridge north cliff face (mirror)
                new TerrainFeaturePlaced
                {
                    type = TerrainFeatureType.RockyRidge,
                    position = new Vector3( 8f, 0f,  16f),
                    rotation = 40f,
                    boxHalfExtents = new Vector2(5f, 1.8f)
                },
                // NW pocket rear cliff — blocks easy backdoor from north edge
                new TerrainFeaturePlaced
                {
                    type = TerrainFeatureType.RockyRidge,
                    position = new Vector3(-36f, 0f,  52f),
                    rotation = 10f,
                    boxHalfExtents = new Vector2(5f, 2f)
                },
                // SE pocket rear cliff (mirror)
                new TerrainFeaturePlaced
                {
                    type = TerrainFeatureType.RockyRidge,
                    position = new Vector3( 36f, 0f, -52f),
                    rotation = 10f,
                    boxHalfExtents = new Vector2(5f, 2f)
                },

                // ── Mud patches — slows armies pushing through key chokes ──

                // Bridge ramp approach (player side)
                new TerrainFeaturePlaced { type = TerrainFeatureType.MudPatch, position = new Vector3(-20f, 0f, -12f), radius = 5f },
                // Bridge ramp approach (enemy side, mirror)
                new TerrainFeaturePlaced { type = TerrainFeatureType.MudPatch, position = new Vector3( 20f, 0f,  12f), radius = 5f },
                // NW pocket entrance mud
                new TerrainFeaturePlaced { type = TerrainFeatureType.MudPatch, position = new Vector3(-28f, 0f,  20f), radius = 3.5f },
                // SE pocket entrance mud (mirror)
                new TerrainFeaturePlaced { type = TerrainFeatureType.MudPatch, position = new Vector3( 28f, 0f, -20f), radius = 3.5f },

                // ── Thorn patches — dangerous side routes ──

                // West-lane thorns — punish flanking through the far west
                new TerrainFeaturePlaced { type = TerrainFeatureType.ThornPatch, position = new Vector3(-62f, 0f,   8f), radius = 4f },
                // East-lane thorns (mirror)
                new TerrainFeaturePlaced { type = TerrainFeatureType.ThornPatch, position = new Vector3( 62f, 0f,  -8f), radius = 4f },
                // South corner thorns — guard the SE pocket backdoor
                new TerrainFeaturePlaced { type = TerrainFeatureType.ThornPatch, position = new Vector3( 46f, 0f, -48f), radius = 3f },
                // North corner thorns (mirror)
                new TerrainFeaturePlaced { type = TerrainFeatureType.ThornPatch, position = new Vector3(-46f, 0f,  48f), radius = 3f },
            };

            return map;
            }

            /// <summary>
            /// Large competitive frozen tundra map with diagonal mirror symmetry.
            /// Features protected main bases, natural expansions, and treacherous frozen terrain.
            /// Art Direction: High-fidelity realistic winter garden floor.
            /// </summary>
            static SkirmishMapDefinition CreateFrozenExpanse()
            {
            var map = ScriptableObject.CreateInstance<SkirmishMapDefinition>();
            map.name = "FrozenExpanse";
            map.displayName = "Frozen Expanse";
            map.description =
                "A vast frozen clearing at insect scale, where frost-covered stone barriers and icicle ridges define the battlefield.\n" +
                "Navigate treacherous frozen brambles and ice sheets to secure the central frosted apples.";

            map.mapHalfExtent = 120f; // 240x240 playable area

            // ── Spawns (Diagonal Mirror Symmetry: negate both X and Z) ──
            map.playerHivePosition = new Vector3(-105f, 1f, -100f);
            map.enemyHivePosition = new Vector3(105f, 1f, 100f);
            map.playerArmyStart = new Vector3(-95f, 0f, -90f);
            map.enemyArmyStart = new Vector3(95f, 0f, 90f);
            map.cameraFocusWorld = new Vector3(-90f, 0f, -85f);
            map.bigApplePosition = new Vector3(-98f, 1.5f, -88f);
            map.enemyBigApplePosition = new Vector3(98f, 1.5f, 88f);

            map.passiveScatterSeed = 98213;

            // ── Elevation (High-Ground Plateaus) ──
            // UV = (worldPos + 120) / 240
            map.highGrounds = new[]
            {
                // Main bases — frost plateaus with single icy ramps
                new HighGroundPlaced { uv = new Vector2(0.0625f, 0.0833f), radius = 0.10f, rampWidth = 0.022f, heightFraction = 0.12f },
                new HighGroundPlaced { uv = new Vector2(0.9375f, 0.9167f), radius = 0.10f, rampWidth = 0.022f, heightFraction = 0.12f },

                // Natural expansions — semi-walled frost plateaus
                new HighGroundPlaced { uv = new Vector2(0.1875f, 0.2292f), radius = 0.06f, rampWidth = 0.03f, heightFraction = 0.08f },
                new HighGroundPlaced { uv = new Vector2(0.8125f, 0.7708f), radius = 0.06f, rampWidth = 0.03f, heightFraction = 0.08f },

                // Exposed Third Bases
                new HighGroundPlaced { uv = new Vector2(0.3333f, 0.1042f), radius = 0.065f, rampWidth = 0.035f, heightFraction = 0.07f },
                new HighGroundPlaced { uv = new Vector2(0.6667f, 0.8958f), radius = 0.065f, rampWidth = 0.035f, heightFraction = 0.07f },

                // Pocket Fourth Bases
                new HighGroundPlaced { uv = new Vector2(0.0417f, 0.3333f), radius = 0.06f, rampWidth = 0.025f, heightFraction = 0.08f },
                new HighGroundPlaced { uv = new Vector2(0.9583f, 0.6667f), radius = 0.06f, rampWidth = 0.025f, heightFraction = 0.08f },

                // Elevated Siege Positions overlooking main paths
                new HighGroundPlaced { uv = new Vector2(0.3958f, 0.4167f), radius = 0.04f, rampWidth = 0.025f, heightFraction = 0.05f },
                new HighGroundPlaced { uv = new Vector2(0.6042f, 0.5833f), radius = 0.04f, rampWidth = 0.025f, heightFraction = 0.05f },
                new HighGroundPlaced { uv = new Vector2(0.4583f, 0.2917f), radius = 0.04f, rampWidth = 0.025f, heightFraction = 0.05f },
                new HighGroundPlaced { uv = new Vector2(0.5417f, 0.7083f), radius = 0.04f, rampWidth = 0.025f, heightFraction = 0.05f },

                // Small central frost bumps
                new HighGroundPlaced { uv = new Vector2(0.4375f, 0.5625f), radius = 0.035f, rampWidth = 0.03f, heightFraction = 0.04f },
                new HighGroundPlaced { uv = new Vector2(0.5625f, 0.4375f), radius = 0.035f, rampWidth = 0.03f, heightFraction = 0.04f },
            };

            // ── Clay Walls (Frozen Stone Barriers) ──
            map.clay = new[]
            {
                // Main base ramp chokes — narrowing entrance
                new ClayPlaced { position = new Vector3(-85f, 0f, -80f), scale = new Vector3(6f, 3f, 3f) },
                new ClayPlaced { position = new Vector3(-75f, 0f, -90f), scale = new Vector3(3f, 3f, 6f) },
                new ClayPlaced { position = new Vector3(85f, 0f, 80f), scale = new Vector3(6f, 3f, 3f) },
                new ClayPlaced { position = new Vector3(75f, 0f, 90f), scale = new Vector3(3f, 3f, 6f) },

                // Natural expansion defensive barriers
                new ClayPlaced { position = new Vector3(-60f, 0f, -50f), scale = new Vector3(5f, 2.8f, 3f) },
                new ClayPlaced { position = new Vector3(-50f, 0f, -60f), scale = new Vector3(3f, 2.8f, 5f) },
                new ClayPlaced { position = new Vector3(60f, 0f, 50f), scale = new Vector3(5f, 2.8f, 3f) },
                new ClayPlaced { position = new Vector3(50f, 0f, 60f), scale = new Vector3(3f, 2.8f, 5f) },

                // Lane Dividers and Choke Formations
                new ClayPlaced { position = new Vector3(-20f, 0f, -40f), scale = new Vector3(8f, 3f, 3f) },
                new ClayPlaced { position = new Vector3(20f, 0f, 40f), scale = new Vector3(8f, 3f, 3f) },
                new ClayPlaced { position = new Vector3(-40f, 0f, -20f), scale = new Vector3(3f, 3f, 8f) },
                new ClayPlaced { position = new Vector3(40f, 0f, 20f), scale = new Vector3(3f, 3f, 8f) },

                // Central frost-stone dividers
                new ClayPlaced { position = new Vector3(10f, 0f, -10f), scale = new Vector3(4f, 2.5f, 10f) },
                new ClayPlaced { position = new Vector3(-10f, 0f, 10f), scale = new Vector3(4f, 2.5f, 10f) },

                // Pocket fourth entrance walls
                new ClayPlaced { position = new Vector3(-90f, 0f, -30f), scale = new Vector3(5f, 2.5f, 3f) },
                new ClayPlaced { position = new Vector3(90f, 0f, 30f), scale = new Vector3(5f, 2.5f, 3f) },

                // Decorative lane barriers for non-linear paths
                new ClayPlaced { position = new Vector3(-110f, 0f, 60f), scale = new Vector3(4f, 2.2f, 8f) },
                new ClayPlaced { position = new Vector3(110f, 0f, -60f), scale = new Vector3(4f, 2.2f, 8f) },
                new ClayPlaced { position = new Vector3(-115f, 0f, 0f), scale = new Vector3(4f, 2.5f, 6f) },
                new ClayPlaced { position = new Vector3(115f, 0f, 0f), scale = new Vector3(4f, 2.5f, 6f) },

                // Near third base
                new ClayPlaced { position = new Vector3(-50f, 0f, -90f), scale = new Vector3(5f, 2.5f, 3f) },
                new ClayPlaced { position = new Vector3(50f, 0f, 90f), scale = new Vector3(5f, 2.5f, 3f) },

                // Random jagged frost-stone obstacles
                new ClayPlaced { position = new Vector3(-70f, 0f, 30f), scale = new Vector3(4f, 2.4f, 4f) },
                new ClayPlaced { position = new Vector3(70f, 0f, -30f), scale = new Vector3(4f, 2.4f, 4f) },
                new ClayPlaced { position = new Vector3(-20f, 0f, 75f), scale = new Vector3(6f, 2f, 2f) },
                new ClayPlaced { position = new Vector3(20f, 0f, -75f), scale = new Vector3(6f, 2f, 2f) },
            };

            // ── Resources (Frosted Apples) ──
            map.fruits = new[]
            {
                // Natural expansion frosted apples (Rich expansion)
                new FruitPlaced { position = new Vector3(-75f, 0.6f, -65f), calories = 12000, gatherPerTick = 10, gatherSeconds = 5f },
                new FruitPlaced { position = new Vector3(75f, 0.6f, 65f), calories = 12000, gatherPerTick = 10, gatherSeconds = 5f },

                // Third base frosted apples (Exposed)
                new FruitPlaced { position = new Vector3(-40f, 0.6f, -95f), calories = 8500, gatherPerTick = 8, gatherSeconds = 5f },
                new FruitPlaced { position = new Vector3(40f, 0.6f, 95f), calories = 8500, gatherPerTick = 8, gatherSeconds = 5f },

                // Pocket fourth frosted apples
                new FruitPlaced { position = new Vector3(-110f, 0.6f, -40f), calories = 7500, gatherPerTick = 8, gatherSeconds = 5f },
                new FruitPlaced { position = new Vector3(110f, 0.6f, 40f), calories = 7500, gatherPerTick = 8, gatherSeconds = 5f },

                // Exposed far frosted apples (Risky)
                new FruitPlaced { position = new Vector3(-100f, 0.6f, 90f), calories = 6500, gatherPerTick = 8, gatherSeconds = 5f },
                new FruitPlaced { position = new Vector3(100f, 0.6f, -90f), calories = 6500, gatherPerTick = 8, gatherSeconds = 5f },

                // Central contested frosted apples
                new FruitPlaced { position = new Vector3(0f, 0.6f, 0f), calories = 6000, gatherPerTick = 10, gatherSeconds = 5f },
                new FruitPlaced { position = new Vector3(-20f, 0.6f, 20f), calories = 5500, gatherPerTick = 10, gatherSeconds = 5f },
                new FruitPlaced { position = new Vector3(20f, 0.6f, -20f), calories = 5500, gatherPerTick = 10, gatherSeconds = 5f },
                new FruitPlaced { position = new Vector3(-40f, 0.6f, 40f), calories = 5000, gatherPerTick = 10, gatherSeconds = 5f },
                new FruitPlaced { position = new Vector3(40f, 0.6f, -40f), calories = 5000, gatherPerTick = 10, gatherSeconds = 5f },

                // Flanking far edge fruits
                new FruitPlaced { position = new Vector3(-60f, 0.6f, 60f), calories = 4500, gatherPerTick = 8, gatherSeconds = 5f },
                new FruitPlaced { position = new Vector3(60f, 0.6f, -60f), calories = 4500, gatherPerTick = 8, gatherSeconds = 5f },
            };

            // ── Terrain Features ──
            map.terrainFeatures = new[]
            {
                // Ice Sheets — treacherous frozen puddles
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3(0f, 0f, 0f), radius = 12f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3(-45f, 0f, 45f), radius = 8f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3(45f, 0f, -45f), radius = 8f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3(-90f, 0f, 20f), radius = 6f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3(90f, 0f, -20f), radius = 6f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3(-20f, 0f, -80f), radius = 5f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.WaterPuddle, position = new Vector3(20f, 0f, 80f), radius = 5f },

                // Dead Winter Grass — ambush points in the frost
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3(-30f, 0f, -30f), radius = 7f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3(30f, 0f, 30f), radius = 7f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3(-60f, 0f, 10f), radius = 5f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3(60f, 0f, -10f), radius = 5f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3(-50f, 0f, -80f), radius = 6f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.TallGrass, position = new Vector3(50f, 0f, 80f), radius = 6f },

                // Frozen Slush — slowing mud patches at chokes
                new TerrainFeaturePlaced { type = TerrainFeatureType.MudPatch, position = new Vector3(-15f, 0f, -50f), radius = 6f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.MudPatch, position = new Vector3(15f, 0f, 50f), radius = 6f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.MudPatch, position = new Vector3(-50f, 0f, -15f), radius = 6f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.MudPatch, position = new Vector3(50f, 0f, 15f), radius = 6f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.MudPatch, position = new Vector3(0f, 0f, 40f), radius = 5f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.MudPatch, position = new Vector3(0f, 0f, -40f), radius = 5f },

                // Frozen Brambles — punishing thorn patches on flanks
                new TerrainFeaturePlaced { type = TerrainFeatureType.ThornPatch, position = new Vector3(-100f, 0f, -15f), radius = 5f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.ThornPatch, position = new Vector3(100f, 0f, 15f), radius = 5f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.ThornPatch, position = new Vector3(-15f, 0f, -100f), radius = 5f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.ThornPatch, position = new Vector3(15f, 0f, 100f), radius = 5f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.ThornPatch, position = new Vector3(-80f, 0f, 50f), radius = 4f },
                new TerrainFeaturePlaced { type = TerrainFeatureType.ThornPatch, position = new Vector3(80f, 0f, -50f), radius = 4f },

                // Icicle Ridges — jagged rocky ridges (blocks vision/pathing)
                new TerrainFeaturePlaced { type = TerrainFeatureType.RockyRidge, position = new Vector3(-35f, 0f, 35f), rotation = 45f, boxHalfExtents = new Vector2(8f, 2.5f) },
                new TerrainFeaturePlaced { type = TerrainFeatureType.RockyRidge, position = new Vector3(35f, 0f, -35f), rotation = 45f, boxHalfExtents = new Vector2(8f, 2.5f) },
                new TerrainFeaturePlaced { type = TerrainFeatureType.RockyRidge, position = new Vector3(0f, 0f, -70f), rotation = 0f, boxHalfExtents = new Vector2(10f, 3f) },
                new TerrainFeaturePlaced { type = TerrainFeatureType.RockyRidge, position = new Vector3(0f, 0f, 70f), rotation = 0f, boxHalfExtents = new Vector2(10f, 3f) },
                new TerrainFeaturePlaced { type = TerrainFeatureType.RockyRidge, position = new Vector3(-70f, 0f, 0f), rotation = 90f, boxHalfExtents = new Vector2(10f, 3f) },
                new TerrainFeaturePlaced { type = TerrainFeatureType.RockyRidge, position = new Vector3(70f, 0f, 0f), rotation = 90f, boxHalfExtents = new Vector2(10f, 3f) },
            };

            return map;
            }
            }
            }

