using System.Collections.Generic;
using InsectWars.Core;
using InsectWars.Data;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace InsectWars.RTS
{
    /// <summary>
    /// Builds Demo 0 skirmish: ground, NavMesh, clay, fruits, hive, both teams + systems.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class SkirmishDirector : MonoBehaviour
    {
        [SerializeField] SkirmishMapDefinition mapDefinition;
        [SerializeField] UnitVisualLibrary visualLibrary;

        float _mapHalfExtent;
        Vector3 _playerStart;
        Vector3 _enemyStart;
        Vector3 _playerHive;
        Vector3 _enemyHive;
        Vector3 _camFocus;
        Vector3 _applePos;
        Vector3 _enemyApplePos;
        int _scatterSeed;
        ClayPlaced[] _clayLayout;
        FruitPlaced[] _fruitLayout;
        CactiSeedPlaced[] _cactiSeedLayout;
        TerrainFeaturePlaced[] _terrainFeatureLayout;

        static readonly ClayPlaced[] DefaultClayList =
        {
            new() { position = new Vector3(-8f, 0f, 22f), scale = new Vector3(6f, 2.2f, 2f) },
            new() { position = new Vector3(12f, 0f, -18f), scale = new Vector3(3f, 2.8f, 9f) },
            new() { position = new Vector3(28f, 0f, 35f), scale = new Vector3(4f, 2f, 5f) },
            new() { position = new Vector3(-35f, 0f, 8f), scale = new Vector3(5f, 2.5f, 3f) },
            new() { position = new Vector3(5f, 0f, -42f), scale = new Vector3(8f, 2f, 2.5f) },
            new() { position = new Vector3(-22f, 0f, -55f), scale = new Vector3(3.5f, 2f, 7f) },
            new() { position = new Vector3(48f, 0f, -12f), scale = new Vector3(2.5f, 3f, 6f) },
            new() { position = new Vector3(-48f, 0f, 48f), scale = new Vector3(6f, 2f, 4f) }
        };

        static readonly FruitPlaced[] DefaultFruitList =
        {
            new() { position = new Vector3(-48f, 0.6f, -28f), calories = 10000, gatherPerTick = 10, gatherSeconds = 5f },
            new() { position = new Vector3(-72f, 0.6f, -15f), calories = 10000, gatherPerTick = 10, gatherSeconds = 5f },
            new() { position = new Vector3(-25f, 0.6f, 8f), calories = 10000, gatherPerTick = 10, gatherSeconds = 5f },
            new() { position = new Vector3(8f, 0.6f, 12f), calories = 10000, gatherPerTick = 10, gatherSeconds = 5f },
            new() { position = new Vector3(42f, 0.6f, 58f), calories = 10000, gatherPerTick = 10, gatherSeconds = 5f },
            new() { position = new Vector3(68f, 0.6f, 28f), calories = 10000, gatherPerTick = 10, gatherSeconds = 5f },
            new() { position = new Vector3(22f, 0.6f, -58f), calories = 10000, gatherPerTick = 10, gatherSeconds = 5f },
            new() { position = new Vector3(-15f, 0.6f, 62f), calories = 10000, gatherPerTick = 10, gatherSeconds = 5f }
        };

        static readonly CactiSeedPlaced[] DefaultCactiSeeds =
        {
            new() { position = new Vector3(0f, 0f, 0f) },
            new() { position = new Vector3(-30f, 0f, 30f) },
            new() { position = new Vector3(30f, 0f, -30f) },
            new() { position = new Vector3(-58f, 0f, 22f) },
            new() { position = new Vector3(58f, 0f, -22f) },
            new() { position = new Vector3(15f, 0f, 48f) },
            new() { position = new Vector3(-15f, 0f, -48f) },
            new() { position = new Vector3(40f, 0f, 8f) },
            new() { position = new Vector3(-40f, 0f, -8f) },
            new() { position = new Vector3(0f, 0f, -65f) },
            new() { position = new Vector3(0f, 0f, 65f) },
            new() { position = new Vector3(65f, 0f, 55f) }
        };

        /// <summary>Used by units for projectile settings when not using a prefab-only pipeline.</summary>
        public static UnitVisualLibrary ActiveVisualLibrary { get; private set; }

        static Material s_lit;
        static readonly Dictionary<int, Material> s_tintCache = new();

        void OnDestroy()
        {
            if (ActiveVisualLibrary == visualLibrary)
                ActiveVisualLibrary = null;
            SkirmishPlayArea.Clear();
        }

        void DestroyObject(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying)
                Destroy(go);
            else
                DestroyImmediate(go);
        }

        void ApplyMapLayout()
        {
            var m = GameSession.SelectedMap != null ? GameSession.SelectedMap : mapDefinition;
            _mapHalfExtent = m != null ? m.mapHalfExtent : 88f;
            _playerStart = m != null ? m.playerArmyStart : new Vector3(-54f, 0f, -44f);
            _enemyStart = m != null ? m.enemyArmyStart : new Vector3(62f, 0f, 52f);
            _playerHive = m != null ? m.playerHivePosition : new Vector3(-62f, 1f, -52f);
            _enemyHive = m != null ? m.enemyHivePosition : new Vector3(62f, 1f, 52f);
            _camFocus = m != null ? m.cameraFocusWorld : new Vector3(-48f, 0f, -38f);
            _applePos = m != null ? m.bigApplePosition : new Vector3(-50f, 1.5f, -42f);
            _enemyApplePos = m != null ? m.enemyBigApplePosition : new Vector3(50f, 1.5f, 42f);
            _scatterSeed = m != null ? m.passiveScatterSeed : 18427;
            _clayLayout = m != null && m.clay != null && m.clay.Length > 0 ? m.clay : DefaultClayList;
            _fruitLayout = m != null && m.fruits != null && m.fruits.Length > 0 ? m.fruits : DefaultFruitList;
            _cactiSeedLayout = m != null && m.cactiSeeds != null && m.cactiSeeds.Length > 0 ? m.cactiSeeds : DefaultCactiSeeds;
            _terrainFeatureLayout = m != null && m.terrainFeatures != null && m.terrainFeatures.Length > 0
                ? m.terrainFeatures
                : System.Array.Empty<TerrainFeaturePlaced>();
        }

        void Start()
        {
            GameSession.LoadPrefs();
            EnemyResources.Reset();
            BuildWorldPreview();

            var playerStart = _playerStart;
            for (var i = 0; i < 5; i++)
            {
                var angle = i * 72f * Mathf.Deg2Rad;
                var offset = new Vector3(Mathf.Cos(angle) * 2.5f, 0f, Mathf.Sin(angle) * 2.5f);
                SpawnUnit(playerStart + offset, Team.Player, UnitArchetype.Worker);
            }
            SpawnUnit(playerStart + new Vector3(1f, 0, 1f), Team.Player, UnitArchetype.BasicFighter);
            SpawnUnit(playerStart + new Vector3(3.5f, 0, 0f), Team.Player, UnitArchetype.BasicFighter);
            SpawnUnit(playerStart + new Vector3(-1f, 0, -1.5f), Team.Player, UnitArchetype.BasicRanged);
            SpawnUnit(playerStart + new Vector3(2f, 0, -2f), Team.Player, UnitArchetype.BasicRanged);

            var enemyStart = _enemyStart;
            var nWorkers = Mathf.RoundToInt(4f * GameSession.DifficultyEnemySpawnMultiplier);
            for (var i = 0; i < nWorkers; i++)
            {
                var angle = i * 72f * Mathf.Deg2Rad;
                var offset = new Vector3(Mathf.Cos(angle) * 2.5f, 0f, Mathf.Sin(angle) * 2.5f);
                SpawnUnit(enemyStart + offset, Team.Enemy, UnitArchetype.Worker);
            }
            var nCombat = Mathf.Clamp(Mathf.RoundToInt(3f * GameSession.DifficultyEnemySpawnMultiplier), 1, 8);
            var archCycle = new[] { UnitArchetype.BasicFighter, UnitArchetype.BasicFighter, UnitArchetype.BasicRanged };
            for (var i = 0; i < nCombat; i++)
            {
                var angle = (i + nWorkers) * 60f * Mathf.Deg2Rad;
                var offset = new Vector3(Mathf.Cos(angle) * 4f, 0f, Mathf.Sin(angle) * 4f);
                var arch = archCycle[Mathf.Min(i, archCycle.Length - 1)];
                SpawnUnit(enemyStart + offset, Team.Enemy, arch);
            }

            var camCtrl = FindFirstObjectByType<RTSCameraController>();
            if (camCtrl != null)
                camCtrl.FocusWorldPosition(_camFocus);
        }

        public void BuildWorldPreview()
        {
            ApplyMapLayout();
            SkirmishPlayArea.Configure(_mapHalfExtent, _mapHalfExtent);
            ActiveVisualLibrary = visualLibrary;

            EnsureLitShader();
            var world = GameObject.Find("WorldRoot");
            DestroyObject(world);

            world = new GameObject("WorldRoot");
            var surface = world.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
            surface.ignoreNavMeshObstacle = false;

            BuildTerrain(world.transform, _mapHalfExtent);
            AddMapBounds(world.transform, _mapHalfExtent, 0.45f);

            foreach (var c in _clayLayout)
                AddClay(world.transform, c.position, c.scale);

            BuildHive(world.transform, _playerHive, Team.Player, "PlayerHive");
            BuildHive(world.transform, _enemyHive, Team.Enemy, "EnemyHive");

            AddRottingApple(world.transform, _applePos);
            AddRottingApple(world.transform, _enemyApplePos);

            foreach (var f in _fruitLayout)
                AddFruit(world.transform, f);

            foreach (var s in _cactiSeedLayout)
                AddCactiSeed(world.transform, s);

            foreach (var tf in _terrainFeatureLayout)
                AddTerrainFeature(world.transform, tf);

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                SpawnUnit(_playerStart + Vector3.forward * 2f, Team.Player, UnitArchetype.Worker).transform.SetParent(world.transform);
                SpawnUnit(_playerStart + Vector3.right * 2f, Team.Player, UnitArchetype.BasicFighter).transform.SetParent(world.transform);
                SpawnUnit(_playerStart + new Vector3(2f, 0f, 2f), Team.Player, UnitArchetype.BasicRanged).transform.SetParent(world.transform);
                SpawnUnit(_enemyStart + Vector3.back * 2f, Team.Enemy, UnitArchetype.BasicFighter).transform.SetParent(world.transform);
                SpawnUnit(_enemyStart + Vector3.left * 2f, Team.Enemy, UnitArchetype.BasicRanged).transform.SetParent(world.transform);
            }
#endif

            var exclusions = BuildExclusionZones();
            SkirmishPassiveScatter.Scatter(world.transform, _mapHalfExtent, _scatterSeed, exclusions);

            PlaceStarterPlayerBuildings(world.transform);

            surface.BuildNavMesh();

            var systems = GameObject.Find("Systems");
            DestroyObject(systems);
            systems = new GameObject("Systems");
            systems.AddComponent<PlayerResources>();
            systems.AddComponent<SelectionController>();
            systems.AddComponent<CommandController>();
            systems.AddComponent<GameHUD>();
            systems.AddComponent<Sc2BottomBar>();
            systems.AddComponent<SkirmishMinimap>();
            systems.AddComponent<FogOfWarSystem>();
            systems.AddComponent<MatchDirector>();
            systems.AddComponent<EnemyCommander>();
            systems.AddComponent<PauseController>();
            systems.AddComponent<GameAudio>();
        }


        void PlaceStarterPlayerBuildings(Transform worldRoot)
        {
        }

        List<SkirmishPassiveScatter.ExclusionZone> BuildExclusionZones()
        {
            var z = new List<SkirmishPassiveScatter.ExclusionZone>
            {
                new(new Vector2(_playerStart.x, _playerStart.z), 26f),
                new(new Vector2(_enemyStart.x, _enemyStart.z), 26f),
                new(new Vector2(_playerHive.x, _playerHive.z), 18f),
                new(new Vector2(_enemyHive.x, _enemyHive.z), 18f),
                new(new Vector2(_applePos.x, _applePos.z), 12f),
                new(new Vector2(_enemyApplePos.x, _enemyApplePos.z), 12f)
            };
            foreach (var f in _fruitLayout)
                z.Add(new SkirmishPassiveScatter.ExclusionZone(new Vector2(f.position.x, f.position.z), 8f));
            foreach (var c in _clayLayout)
            {
                var spread = Mathf.Max(c.scale.x, c.scale.z) * 0.5f + 3f;
                z.Add(new SkirmishPassiveScatter.ExclusionZone(new Vector2(c.position.x, c.position.z), spread));
            }
            foreach (var s in _cactiSeedLayout)
                z.Add(new SkirmishPassiveScatter.ExclusionZone(new Vector2(s.position.x, s.position.z), 4f));
            foreach (var tf in _terrainFeatureLayout)
            {
                float spread = tf.boxHalfExtents.x > 0.01f
                    ? Mathf.Max(tf.boxHalfExtents.x, tf.boxHalfExtents.y) + 2f
                    : tf.radius + 2f;
                z.Add(new SkirmishPassiveScatter.ExclusionZone(new Vector2(tf.position.x, tf.position.z), spread));
            }
            return z;
        }

        static void AddMapBounds(Transform parent, float extent, float thickness)
        {
            var c = new Color(0.35f, 0.28f, 0.18f);
            float y = thickness * 0.5f;
            float len = extent * 2f;
            void Edge(string name, Vector3 pos, Vector3 scale)
            {
                var e = GameObject.CreatePrimitive(PrimitiveType.Cube);
                e.name = name;
                e.transform.SetParent(parent);
                e.transform.position = pos + Vector3.up * y;
                e.transform.localScale = scale;
                ApplyMat(e, c);
                Object.Destroy(e.GetComponent<Collider>());
            }
            Edge("MapEdge_N", new Vector3(0f, 0f, extent), new Vector3(len, thickness, thickness));
            Edge("MapEdge_S", new Vector3(0f, 0f, -extent), new Vector3(len, thickness, thickness));
            Edge("MapEdge_E", new Vector3(extent, 0f, 0f), new Vector3(thickness, thickness, len));
            Edge("MapEdge_W", new Vector3(-extent, 0f, 0f), new Vector3(thickness, thickness, len));
        }

        static void EnsureLitShader()
        {
            if (s_lit != null) return;
            var sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null)
            {
                sh = Shader.Find("Standard");
                if (sh == null) sh = Shader.Find("Sprites/Default");
            }
            s_lit = new Material(sh);
            if (sh.name.Contains("Universal Render Pipeline"))
                s_lit.SetColor("_BaseColor", Color.white);
            else if (s_lit.HasProperty("_Color"))
                s_lit.SetColor("_Color", Color.white);
        }

        static Material GetSharedTinted(Color c)
        {
            var key = (Mathf.RoundToInt(c.r * 255f) << 16) | (Mathf.RoundToInt(c.g * 255f) << 8) |
                      Mathf.RoundToInt(c.b * 255f);
            if (s_tintCache.TryGetValue(key, out var existing))
                return existing;
            EnsureLitShader();
            var m = new Material(s_lit);
            if (m.HasProperty("_BaseColor"))
                m.SetColor("_BaseColor", c);
            else if (m.HasProperty("_Color"))
                m.SetColor("_Color", c);
            s_tintCache[key] = m;
            return m;
        }

        static void ApplyMat(GameObject go, Color c)
        {
            var r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = GetSharedTinted(c);
        }

        static GameObject AddPrimitivePart(Transform parent, PrimitiveType type, Vector3 localPos, Vector3 localScale,
            Quaternion localRot, Color color)
        {
            var p = GameObject.CreatePrimitive(type);
            p.transform.SetParent(parent, false);
            p.transform.localPosition = localPos;
            p.transform.localScale = localScale;
            p.transform.localRotation = localRot;
            Object.Destroy(p.GetComponent<Collider>());
            ApplyMat(p, color);
            return p;
        }

        static void BuildWorkerVisual(Transform root, Color body, Team team)
        {
            var skin = TeamPalette.GetShellColor(team);
            var accent = TeamPalette.GetTeamColor(team);

            AddPrimitivePart(root, PrimitiveType.Cylinder, new Vector3(0f, 0.28f, 0f), new Vector3(0.52f, 0.24f, 0.52f),
                Quaternion.identity, skin);
            AddPrimitivePart(root, PrimitiveType.Sphere, new Vector3(0f, 0.58f, 0f), Vector3.one * 0.3f,
                Quaternion.identity, Color.Lerp(skin, Color.white, 0.2f));
            
            // Strap: a thin ring around the "waist"
            AddPrimitivePart(root, PrimitiveType.Cylinder, new Vector3(0f, 0.28f, 0f), new Vector3(0.55f, 0.05f, 0.55f),
                Quaternion.identity, accent);
        }

        static void BuildFighterVisual(Transform root, Color body, Team team)
        {
            var skin = TeamPalette.GetShellColor(team);
            var accent = TeamPalette.GetTeamColor(team);

            // Body (Thorax)
            AddPrimitivePart(root, PrimitiveType.Capsule, new Vector3(0f, 0.35f, -0.1f), new Vector3(0.45f, 0.6f, 0.45f),
                Quaternion.Euler(45f, 0f, 0f), skin);
            
            // Head
            AddPrimitivePart(root, PrimitiveType.Sphere, new Vector3(0f, 0.85f, 0.3f), Vector3.one * 0.35f,
                Quaternion.identity, skin);
            
            // Eyes (vibrant accent)
            AddPrimitivePart(root, PrimitiveType.Sphere, new Vector3(-0.15f, 0.95f, 0.45f), Vector3.one * 0.12f,
                Quaternion.identity, accent);
            AddPrimitivePart(root, PrimitiveType.Sphere, new Vector3(0.15f, 0.95f, 0.45f), Vector3.one * 0.12f,
                Quaternion.identity, accent);

            // Left Scythe Arm
            var lArm = new GameObject("LeftArm").transform;
            lArm.SetParent(root, false);
            lArm.localPosition = new Vector3(-0.25f, 0.65f, 0.2f);
            var lForearm = AddPrimitivePart(lArm, PrimitiveType.Capsule, new Vector3(0f, -0.2f, 0.15f), new Vector3(0.15f, 0.35f, 0.15f),
                Quaternion.Euler(60f, 0f, 0f), skin);
            lForearm.name = "Forearm";

            // Right Scythe Arm
            var rArm = new GameObject("RightArm").transform;
            rArm.SetParent(root, false);
            rArm.localPosition = new Vector3(0.25f, 0.65f, 0.2f);
            var rForearm = AddPrimitivePart(rArm, PrimitiveType.Capsule, new Vector3(0f, -0.2f, 0.15f), new Vector3(0.15f, 0.35f, 0.15f),
                Quaternion.Euler(60f, 0f, 0f), skin);
            rForearm.name = "Forearm";
            
            // Straps: Shoulders
            AddPrimitivePart(root, PrimitiveType.Cube, new Vector3(-0.2f, 0.55f, 0.1f), new Vector3(0.1f, 0.05f, 0.3f),
                Quaternion.Euler(0f, 0f, 20f), accent);
            AddPrimitivePart(root, PrimitiveType.Cube, new Vector3(0.2f, 0.55f, 0.1f), new Vector3(0.1f, 0.05f, 0.3f),
                Quaternion.Euler(0f, 0f, -20f), accent);
        }

        static void BuildRangedVisual(Transform root, Color body, Team team)
        {
            var skin = TeamPalette.GetShellColor(team);
            var accent = TeamPalette.GetTeamColor(team);

            AddPrimitivePart(root, PrimitiveType.Capsule, new Vector3(0f, 0.52f, 0f), new Vector3(0.42f, 0.5f, 0.42f),
                Quaternion.identity, skin);
            AddPrimitivePart(root, PrimitiveType.Sphere, new Vector3(0f, 1.02f, 0f), Vector3.one * 0.3f,
                Quaternion.identity, Color.Lerp(skin, Color.white, 0.2f));
            AddPrimitivePart(root, PrimitiveType.Cube, new Vector3(0.2f, 0.62f, 0.38f), new Vector3(0.1f, 0.08f, 0.48f),
                Quaternion.identity, accent);
            
            // Strap: A horizontal ring on the main body
            AddPrimitivePart(root, PrimitiveType.Cylinder, new Vector3(0f, 0.72f, 0f), new Vector3(0.45f, 0.04f, 0.45f),
                Quaternion.identity, accent);
            
            var fp = new GameObject("FirePoint");
            fp.transform.SetParent(root, false);
            fp.transform.localPosition = new Vector3(0f, 0.55f, 0.55f);
        }

        static Terrain s_terrain;

        static float GetHeight(Vector3 worldPos)
        {
            if (s_terrain == null) return 0f;
            return s_terrain.SampleHeight(worldPos);
        }

        static void AddClay(Transform parent, Vector3 pos, Vector3 scale)
        {
            var clay = GameObject.CreatePrimitive(PrimitiveType.Cube);
            clay.name = "Clay";
            clay.tag = "Clay";
            clay.transform.SetParent(parent);
            float h = GetHeight(pos);
            clay.transform.position = new Vector3(pos.x, h + (scale.y * 0.5f), pos.z);
            clay.transform.localScale = scale;
            ApplyMat(clay, new Color(0.45f, 0.32f, 0.22f));
            var obs = clay.AddComponent<NavMeshObstacle>();
            obs.carving = true;
            obs.shape = NavMeshObstacleShape.Box;
            obs.size = Vector3.one;
            obs.center = Vector3.zero;
        }

        void BuildHive(Transform parent, Vector3 worldPos, Team team, string name)
        {
            float h = GetHeight(worldPos);
            var placedPos = new Vector3(worldPos.x, h + worldPos.y, worldPos.z);

            GameObject hive;
            if (visualLibrary != null && visualLibrary.hivePrefab != null)
            {
                hive = Instantiate(visualLibrary.hivePrefab, parent);
                hive.name = name;
                if (!hive.CompareTag("Hive")) hive.tag = "Hive";
                hive.transform.position = placedPos;
                var deposit = hive.GetComponent<HiveDeposit>();
                if (deposit == null) deposit = hive.AddComponent<HiveDeposit>();
                deposit.Configure(team);
                if (hive.GetComponent<HiveVisual>() == null) hive.AddComponent<HiveVisual>();
                
                // Apply skin color to prefab renderers
                var skinColor = TeamPalette.GetShellColor(team);
                foreach (var renderer in hive.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer.gameObject.name == "TeamStrap") continue;
                    var mats = renderer.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        if (mats[i] == null) continue;
                        var m = new Material(mats[i]);
                        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", skinColor);
                        else if (m.HasProperty("_Color")) m.color = skinColor;
                        mats[i] = m;
                    }
                    renderer.sharedMaterials = mats;
                }

                // Add strap to prefab Hive
                var strap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                strap.name = "TeamStrap";
                strap.transform.SetParent(hive.transform, false);
                strap.transform.localPosition = new Vector3(0f, 0.8f, 0f);
                strap.transform.localScale = new Vector3(1.5f, 0.08f, 1.5f); // Thicker strap
                Object.Destroy(strap.GetComponent<Collider>());
                ApplyMat(strap, TeamPalette.GetTeamColor(team));

                var prod = hive.AddComponent<ProductionBuilding>();
                prod.Initialize(BuildingType.AntNest, team);
            }
            else
            {
                hive = GameObject.CreatePrimitive(PrimitiveType.Cube);
                hive.name = name;
                hive.tag = "Hive";
                hive.transform.SetParent(parent);
                hive.transform.position = placedPos;
                hive.transform.localScale = new Vector3(4f, 2f, 4f);
                ApplyMat(hive, new Color(0.32f, 0.52f, 0.88f));
                
                // Add straps to the primitive Hive
                var strapColor = TeamPalette.GetTeamColor(team);
                AddPrimitivePart(hive.transform, PrimitiveType.Cube, new Vector3(0f, 0.52f, 0f), new Vector3(1.05f, 0.08f, 1.05f), 
                    Quaternion.identity, strapColor);
                AddPrimitivePart(hive.transform, PrimitiveType.Cube, new Vector3(0f, 0f, 0.52f), new Vector3(0.5f, 1.05f, 0.08f), 
                    Quaternion.identity, strapColor);
                AddPrimitivePart(hive.transform, PrimitiveType.Cube, new Vector3(0f, 0f, -0.52f), new Vector3(0.5f, 1.05f, 0.08f), 
                    Quaternion.identity, strapColor);

                var hiveMod = hive.AddComponent<NavMeshModifier>();
                hiveMod.ignoreFromBuild = true;
                var deposit = hive.AddComponent<HiveDeposit>();
                deposit.Configure(team);
                hive.AddComponent<HiveVisual>();

                var prod = hive.AddComponent<ProductionBuilding>();
                prod.Initialize(BuildingType.AntNest, team);
            }
        }

        void BuildTerrain(Transform parent, float mapHalfExtent)
        {
            float size = mapHalfExtent * 2f;
            int resolution = 128;

            TerrainData terrainData = new TerrainData();
            terrainData.heightmapResolution = resolution + 1;
            terrainData.size = new Vector3(size, 20f, size);
            terrainData.alphamapResolution = 128;

            if (visualLibrary != null && visualLibrary.baseSoilLayer != null && visualLibrary.drySoilLayer != null)
                terrainData.terrainLayers = new[] { visualLibrary.baseSoilLayer, visualLibrary.drySoilLayer };

            var mapDef = GameSession.SelectedMap != null ? GameSession.SelectedMap : mapDefinition;
            bool useCustomHighGrounds = mapDef != null && mapDef.highGrounds != null;

            HighGroundPlaced[] hgList;
            if (useCustomHighGrounds)
            {
                hgList = mapDef.highGrounds;
            }
            else
            {
                hgList = new[]
                {
                    new HighGroundPlaced { uv = new Vector2(0.25f, 0.25f), radius = 0.13f, rampWidth = 0.04f, heightFraction = 0.15f },
                    new HighGroundPlaced { uv = new Vector2(0.75f, 0.75f), radius = 0.13f, rampWidth = 0.04f, heightFraction = 0.15f },
                    new HighGroundPlaced { uv = new Vector2(0.5f, 0.5f), radius = 0.13f, rampWidth = 0.04f, heightFraction = 0.15f },
                };
            }

            float[,] heights = new float[resolution + 1, resolution + 1];
            for (int x = 0; x <= resolution; x++)
            {
                for (int y = 0; y <= resolution; y++)
                {
                    float u = (float)x / resolution;
                    float v = (float)y / resolution;
                    float maxH = 0f;
                    foreach (var hg in hgList)
                    {
                        float dist = Vector2.Distance(new Vector2(u, v), hg.uv);
                        float rw = Mathf.Max(hg.rampWidth, 0.01f);
                        if (dist < hg.radius) maxH = Mathf.Max(maxH, hg.heightFraction);
                        else if (dist < hg.radius + rw) maxH = Mathf.Max(maxH, Mathf.Lerp(hg.heightFraction, 0f, (dist - hg.radius) / rw));
                    }
                    heights[y, x] = maxH;
                }
            }
            terrainData.SetHeights(0, 0, heights);

            float[,,] alpha = new float[128, 128, 2];
            for (int x = 0; x < 128; x++)
            {
                for (int y = 0; y < 128; y++)
                {
                    float u = (float)x / 128f;
                    float v = (float)y / 128f;

                    float h = 0f;
                    foreach (var hg in hgList)
                    {
                        float dist = Vector2.Distance(new Vector2(u, v), hg.uv);
                        float rw = Mathf.Max(hg.rampWidth, 0.01f);
                        if (dist < hg.radius) h = 1f;
                        else if (dist < hg.radius + rw) h = Mathf.Max(h, Mathf.InverseLerp(hg.radius + rw, hg.radius, dist));
                    }

                    float noise = Mathf.PerlinNoise(x * 0.1f, y * 0.1f);
                    float baseWeight = Mathf.Clamp01(1f - h + (noise - 0.5f) * 0.3f);

                    alpha[y, x, 0] = baseWeight;
                    alpha[y, x, 1] = 1f - baseWeight;
                }
            }
            terrainData.SetAlphamaps(0, 0, alpha);

            GameObject terrainObj = Terrain.CreateTerrainGameObject(terrainData);
            terrainObj.name = "Ground";
            terrainObj.transform.SetParent(parent);
            terrainObj.transform.position = new Vector3(-mapHalfExtent, 0f, -mapHalfExtent);
            
            s_terrain = terrainObj.GetComponent<Terrain>();
            
            // Explicitly assign URP Terrain Lit shader to avoid pink fallback
            var terrainShader = Shader.Find("Universal Render Pipeline/Terrain/Lit");
            if (terrainShader != null)
                s_terrain.materialTemplate = new Material(terrainShader);
            else
                s_terrain.materialTemplate = null; // Use default URP Terrain Lit
                
            s_terrain.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }

        static void AddRottingApple(Transform parent, Vector3 pos)
        {
            GameObject apple;
            var lib = ActiveVisualLibrary;
            float h = GetHeight(pos);
            const float appleHeight = 3f;
            // Center Y = h + (Height/6) to bury the bottom 1/3 (assuming pivot at center)
            float posY = h + (appleHeight / 6f);

            if (lib != null && lib.rottingApplePrefab != null)
            {
                apple = Object.Instantiate(lib.rottingApplePrefab, parent);
                apple.name = "RottingApple";
                apple.tag = "Fruit";
                int layer = LayerMask.NameToLayer("Resources");
                if (layer >= 0) apple.layer = layer;
                apple.transform.position = new Vector3(pos.x, posY, pos.z);
                apple.transform.localScale = new Vector3(4f, appleHeight, 4f);
            }
            else
            {
                apple = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                apple.name = "RottingApple";
                apple.tag = "Fruit";
                int layer = LayerMask.NameToLayer("Resources");
                if (layer >= 0) apple.layer = layer;
                apple.transform.SetParent(parent);
                apple.transform.position = new Vector3(pos.x, posY, pos.z);
                apple.transform.localScale = new Vector3(4f, appleHeight, 4f);
                var col = new Color(0.85f, 0.68f, 0.15f);
                var r = apple.GetComponent<Renderer>();
                if (r != null)
                {
                    r.sharedMaterial = GetSharedTinted(col);
                    var pb = new MaterialPropertyBlock();
                    r.GetPropertyBlock(pb);
                    if (r.sharedMaterial != null && r.sharedMaterial.HasProperty("_Smoothness"))
                        pb.SetFloat("_Smoothness", 0.15f);
                    r.SetPropertyBlock(pb);
                }
            }

            SetIgnoreNavMeshRecursive(apple);

            AddFruitObstacle(apple);

            if (apple.GetComponent<RottingFruitNode>() == null)
                apple.AddComponent<RottingFruitNode>();
        }

        static void AddFruit(Transform parent, FruitPlaced f)
        {
            var pos = f.position;
            GameObject fruit;
            var lib = ActiveVisualLibrary;
            float h = GetHeight(pos);
            const float fruitHeight = 1.8f;
            // Center Y = h + (Height/6) to bury the bottom 1/3
            float posY = h + (fruitHeight / 6f);

            if (lib != null && lib.rottingApplePrefab != null)
            {
                fruit = Object.Instantiate(lib.rottingApplePrefab, parent);
                fruit.name = "RottingFruit";
                fruit.tag = "Fruit";
                int layer = LayerMask.NameToLayer("Resources");
                if (layer >= 0) fruit.layer = layer;
                fruit.transform.position = new Vector3(pos.x, posY, pos.z);
                fruit.transform.localScale = Vector3.one * fruitHeight;
            }
            else
            {
                fruit = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                fruit.name = "RottingFruit";
                fruit.tag = "Fruit";
                int layer = LayerMask.NameToLayer("Resources");
                if (layer >= 0) fruit.layer = layer;
                fruit.transform.SetParent(parent);
                fruit.transform.position = new Vector3(pos.x, posY, pos.z);
                fruit.transform.localScale = Vector3.one * fruitHeight;
                ApplyMat(fruit, new Color(0.65f, 0.2f, 0.55f));
            }

            SetIgnoreNavMeshRecursive(fruit);

            AddFruitObstacle(fruit);

            var node = fruit.GetComponent<RottingFruitNode>();
            if (node == null) node = fruit.AddComponent<RottingFruitNode>();
            node.Configure(f.calories, f.gatherPerTick, f.gatherSeconds);
        }

        static void AddFruitObstacle(GameObject fruit)
        {
            var existing = fruit.GetComponentInChildren<NavMeshObstacle>();
            if (existing != null)
            {
                if (Application.isPlaying) Object.Destroy(existing);
                else Object.DestroyImmediate(existing);
            }

            var s = fruit.transform.localScale;
            var obsGo = new GameObject("NavObstacle");
            obsGo.transform.SetParent(fruit.transform, false);
            obsGo.transform.localPosition = Vector3.zero;
            obsGo.transform.localScale = new Vector3(1f / s.x, 1f / s.y, 1f / s.z);

            var obs = obsGo.AddComponent<NavMeshObstacle>();
            obs.carving = true;

            var rend = fruit.GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                var b = rend.bounds;
                obs.shape = NavMeshObstacleShape.Box;
                obs.size = b.size * 1.05f;
                obs.center = b.center - fruit.transform.position;
            }
            else
            {
                obs.shape = NavMeshObstacleShape.Capsule;
                obs.radius = Mathf.Max(s.x, s.z) * 0.55f;
                obs.height = s.y;
                obs.center = Vector3.zero;
            }
        }

        static void SetIgnoreNavMeshRecursive(GameObject go)
        {
            var mod = go.GetComponent<NavMeshModifier>();
            if (mod == null) mod = go.AddComponent<NavMeshModifier>();
            mod.ignoreFromBuild = true;
            foreach (Transform child in go.transform)
                SetIgnoreNavMeshRecursive(child.gameObject);
        }

        static void AddCactiSeed(Transform parent, CactiSeedPlaced s)
        {
            var pos = s.position;
            var seed = new GameObject("CactiSeed");
            int layer = LayerMask.NameToLayer("Resources");
            if (layer >= 0) seed.layer = layer;
            seed.transform.SetParent(parent);
            float h = GetHeight(pos);
            seed.transform.position = new Vector3(pos.x, h + 0.35f, pos.z);

            var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = "SeedBody";
            body.transform.SetParent(seed.transform, false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = Vector3.one * 0.55f;
            Object.Destroy(body.GetComponent<Collider>());
            ApplyMat(body, new Color(0.55f, 0.7f, 0.3f));

            Vector3[] spikeOffsets =
            {
                new(0f, 0.28f, 0f),
                new(0.18f, 0.1f, 0.12f),
                new(-0.15f, 0.08f, -0.16f),
                new(0.1f, -0.1f, -0.18f),
                new(-0.12f, -0.08f, 0.15f)
            };
            foreach (var off in spikeOffsets)
            {
                var spike = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                spike.name = "Spike";
                spike.transform.SetParent(seed.transform, false);
                spike.transform.localPosition = off;
                spike.transform.localScale = new Vector3(0.08f, 0.14f, 0.08f);
                spike.transform.localRotation = Quaternion.LookRotation(off.normalized) * Quaternion.Euler(90f, 0f, 0f);
                Object.Destroy(spike.GetComponent<Collider>());
                ApplyMat(spike, new Color(0.38f, 0.52f, 0.2f));
            }

            var col = seed.AddComponent<SphereCollider>();
            col.center = Vector3.zero;
            col.radius = 0.4f;
            col.isTrigger = true;

            var modifier = seed.AddComponent<NavMeshModifier>();
            modifier.ignoreFromBuild = true;

            seed.AddComponent<CactiSeedNode>();
        }

        // ──────────── Terrain Features ────────────

        void AddTerrainFeature(Transform parent, TerrainFeaturePlaced placed)
        {
            var lib = ActiveVisualLibrary;
            var prefab = lib != null ? lib.GetTerrainFeaturePrefab(placed.type) : null;
            float h = GetHeight(placed.position);
            var worldPos = new Vector3(placed.position.x, h, placed.position.z);

            GameObject root;
            if (prefab != null)
            {
                root = Instantiate(prefab, parent);
                root.name = $"TF_{placed.type}";
                root.transform.position = worldPos;
                root.transform.rotation = Quaternion.Euler(0f, placed.rotation, 0f);
            }
            else
            {
                root = new GameObject($"TF_{placed.type}");
                root.transform.SetParent(parent);
                root.transform.position = worldPos;
                root.transform.rotation = Quaternion.Euler(0f, placed.rotation, 0f);
                BuildProceduralFeatureVisual(root.transform, placed);
            }

            var zone = root.AddComponent<TerrainFeatureZone>();
            zone.Configure(placed);

            if (TerrainFeatureProperties.BlocksPathing(placed.type))
            {
                var obs = root.AddComponent<NavMeshObstacle>();
                obs.carving = true;
                obs.shape = NavMeshObstacleShape.Box;
                if (placed.boxHalfExtents.x > 0.01f)
                    obs.size = new Vector3(placed.boxHalfExtents.x * 2f, 3f, placed.boxHalfExtents.y * 2f);
                else
                    obs.size = new Vector3(placed.radius * 2f, 3f, placed.radius * 2f);
                obs.center = new Vector3(0f, 1.5f, 0f);
            }
            else
            {
                var mod = root.AddComponent<NavMeshModifier>();
                mod.ignoreFromBuild = true;
            }
        }

        void BuildProceduralFeatureVisual(Transform root, TerrainFeaturePlaced placed)
        {
            var color = TerrainFeatureProperties.GetBaseColor(placed.type);
            switch (placed.type)
            {
                case TerrainFeatureType.WaterPuddle:
                    BuildProceduralWater(root, placed, color);
                    break;
                case TerrainFeatureType.TallGrass:
                    BuildProceduralGrass(root, placed, color);
                    break;
                case TerrainFeatureType.MudPatch:
                    BuildProceduralMud(root, placed, color);
                    break;
                case TerrainFeatureType.ThornPatch:
                    BuildProceduralThorns(root, placed, color);
                    break;
                case TerrainFeatureType.RockyRidge:
                    BuildProceduralRockyRidge(root, placed, color);
                    break;
            }
        }

        void BuildProceduralWater(Transform root, TerrainFeaturePlaced p, Color color)
        {
            float r = p.boxHalfExtents.x > 0.01f ? Mathf.Max(p.boxHalfExtents.x, p.boxHalfExtents.y) : p.radius;
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "WaterSurface";
            disc.transform.SetParent(root, false);
            disc.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            disc.transform.localScale = new Vector3(r * 2f, 0.02f, r * 2f);
            Object.Destroy(disc.GetComponent<Collider>());
            var mat = GetSharedTinted(color);
            var rend = disc.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.sharedMaterial = mat;
                var pb = new MaterialPropertyBlock();
                if (mat.HasProperty("_Smoothness")) pb.SetFloat("_Smoothness", 0.85f);
                rend.SetPropertyBlock(pb);
            }
        }

        static void BuildProceduralGrass(Transform root, TerrainFeaturePlaced p, Color color)
        {
            float r = p.boxHalfExtents.x > 0.01f ? Mathf.Max(p.boxHalfExtents.x, p.boxHalfExtents.y) : p.radius;
            int bladeCount = Mathf.Clamp(Mathf.RoundToInt(r * 4f), 6, 30);
            var rng = new System.Random(Mathf.RoundToInt(p.position.x * 100f + p.position.z));
            for (int i = 0; i < bladeCount; i++)
            {
                float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                float dist = (float)rng.NextDouble() * r * 0.9f;
                float height = 0.6f + (float)rng.NextDouble() * 1.2f;
                var blade = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                blade.name = "GrassBlade";
                blade.transform.SetParent(root, false);
                blade.transform.localPosition = new Vector3(
                    Mathf.Cos(angle) * dist,
                    height * 0.5f,
                    Mathf.Sin(angle) * dist);
                blade.transform.localScale = new Vector3(0.12f, height * 0.5f, 0.12f);
                float lean = 5f + (float)rng.NextDouble() * 15f;
                blade.transform.localRotation = Quaternion.Euler(lean, angle * Mathf.Rad2Deg, 0f);
                Object.Destroy(blade.GetComponent<Collider>());
                float shade = 0.85f + (float)rng.NextDouble() * 0.3f;
                ApplyMat(blade, color * shade);
            }
        }

        void BuildProceduralMud(Transform root, TerrainFeaturePlaced p, Color color)
        {
            float r = p.boxHalfExtents.x > 0.01f ? Mathf.Max(p.boxHalfExtents.x, p.boxHalfExtents.y) : p.radius;
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "MudSurface";
            disc.transform.SetParent(root, false);
            disc.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            disc.transform.localScale = new Vector3(r * 2f, 0.015f, r * 2f);
            Object.Destroy(disc.GetComponent<Collider>());
            ApplyMat(disc, color);
        }

        static void BuildProceduralThorns(Transform root, TerrainFeaturePlaced p, Color color)
        {
            float r = p.boxHalfExtents.x > 0.01f ? Mathf.Max(p.boxHalfExtents.x, p.boxHalfExtents.y) : p.radius;
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "ThornBase";
            disc.transform.SetParent(root, false);
            disc.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            disc.transform.localScale = new Vector3(r * 2f, 0.015f, r * 2f);
            Object.Destroy(disc.GetComponent<Collider>());
            ApplyMat(disc, color * 0.8f);

            int spikeCount = Mathf.Clamp(Mathf.RoundToInt(r * 3f), 4, 20);
            var rng = new System.Random(Mathf.RoundToInt(p.position.x * 73f + p.position.z * 31f));
            var spikeColor = new Color(0.35f, 0.22f, 0.08f);
            for (int i = 0; i < spikeCount; i++)
            {
                float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                float dist = (float)rng.NextDouble() * r * 0.85f;
                float height = 0.3f + (float)rng.NextDouble() * 0.6f;
                var spike = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                spike.name = "Thorn";
                spike.transform.SetParent(root, false);
                spike.transform.localPosition = new Vector3(
                    Mathf.Cos(angle) * dist,
                    height * 0.4f,
                    Mathf.Sin(angle) * dist);
                spike.transform.localScale = new Vector3(0.06f, height * 0.35f, 0.06f);
                float tilt = 15f + (float)rng.NextDouble() * 40f;
                spike.transform.localRotation = Quaternion.Euler(tilt, angle * Mathf.Rad2Deg, 0f);
                Object.Destroy(spike.GetComponent<Collider>());
                ApplyMat(spike, spikeColor);
            }
        }

        static void BuildProceduralRockyRidge(Transform root, TerrainFeaturePlaced p, Color color)
        {
            float r = p.boxHalfExtents.x > 0.01f ? Mathf.Max(p.boxHalfExtents.x, p.boxHalfExtents.y) : p.radius;
            int rockCount = Mathf.Clamp(Mathf.RoundToInt(r * 2.5f), 3, 16);
            var rng = new System.Random(Mathf.RoundToInt(p.position.x * 59f + p.position.z * 41f));
            for (int i = 0; i < rockCount; i++)
            {
                float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                float dist = (float)rng.NextDouble() * r * 0.7f;
                float sz = 0.8f + (float)rng.NextDouble() * 1.6f;
                var rock = GameObject.CreatePrimitive(i % 3 == 0 ? PrimitiveType.Sphere : PrimitiveType.Cube);
                rock.name = "Rock";
                rock.transform.SetParent(root, false);
                rock.transform.localPosition = new Vector3(
                    Mathf.Cos(angle) * dist,
                    sz * 0.4f,
                    Mathf.Sin(angle) * dist);
                rock.transform.localScale = new Vector3(sz, sz * (0.6f + (float)rng.NextDouble() * 0.8f), sz);
                rock.transform.localRotation = Quaternion.Euler(
                    (float)rng.NextDouble() * 20f,
                    (float)rng.NextDouble() * 360f,
                    (float)rng.NextDouble() * 20f);
                Object.Destroy(rock.GetComponent<Collider>());
                float shade = 0.7f + (float)rng.NextDouble() * 0.5f;
                ApplyMat(rock, color * shade);
            }
        }

        public static InsectUnit SpawnUnit(Vector3 pos, Team team, UnitArchetype arch)
        {
            var lib = ActiveVisualLibrary;
            var prefab = lib != null ? lib.GetUnitPrefab(arch) : null;
            float h = GetHeight(pos);
            Vector3 finalPos = new Vector3(pos.x, h + 0.02f, pos.z);

            if (prefab != null)
            {
                var go = Object.Instantiate(prefab);
                go.name = $"{team}_{arch}";
                int layer = LayerMask.NameToLayer("Units");
                if (layer >= 0) go.layer = layer;
                go.transform.position = finalPos;
                var unit = go.GetComponent<InsectUnit>();
                if (unit == null) unit = go.AddComponent<InsectUnit>();
                
                Color shellColor = TeamPalette.GetShellColor(team);
                var def = UnitDefinition.CreateRuntimeDefault(arch, shellColor);
                unit.Configure(team, def);
                
                var block = new MaterialPropertyBlock();
                foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer.gameObject.name == "TeamStrap") continue;
                    renderer.GetPropertyBlock(block);
                    if (renderer.sharedMaterial != null)
                    {
                        if (renderer.sharedMaterial.HasProperty("_BaseColor"))
                            block.SetColor("_BaseColor", shellColor);
                        if (renderer.sharedMaterial.HasProperty("_Color"))
                            block.SetColor("_Color", shellColor);
                    }
                    renderer.SetPropertyBlock(block);
                }

                // Add strap to prefab unit (vibrant accent)
                var strapColor = TeamPalette.GetTeamColor(team);
                var strap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                strap.name = "TeamStrap";
                strap.transform.SetParent(go.transform, false);
                strap.transform.localPosition = new Vector3(0f, 0.45f, 0f);
                strap.transform.localScale = new Vector3(1.1f, 0.05f, 1.1f);
                Object.Destroy(strap.GetComponent<Collider>());
                ApplyMat(strap, strapColor);

                if (go.GetComponent<UnitAnimationDriver>() == null)
                    go.AddComponent<UnitAnimationDriver>();
                if (team == Team.Enemy && go.GetComponent<SimpleEnemyAi>() == null)
                    go.AddComponent<SimpleEnemyAi>();
                return unit;
            }

            var go2 = new GameObject($"{team}_{arch}");
            int layer2 = LayerMask.NameToLayer("Units");
            if (layer2 >= 0) go2.layer = layer2;

            var visualRoot = new GameObject("Visual");
            visualRoot.transform.SetParent(go2.transform, false);

            Color body2 = TeamPalette.UnitBody(team, arch);
            switch (arch)
            {
                case UnitArchetype.Worker:
                    BuildWorkerVisual(visualRoot.transform, body2, team);
                    break;
                case UnitArchetype.BasicFighter:
                    BuildFighterVisual(visualRoot.transform, body2, team);
                    break;
                case UnitArchetype.BasicRanged:
                    BuildRangedVisual(visualRoot.transform, body2, team);
                    break;
            }

            var col = go2.AddComponent<CapsuleCollider>();
            switch (arch)
            {
                case UnitArchetype.Worker:
                    col.center = new Vector3(0f, 0.45f, 0f);
                    col.radius = 0.32f;
                    col.height = 0.95f;
                    break;
                case UnitArchetype.BasicFighter:
                    col.center = new Vector3(0f, 0.22f, 0f);
                    col.radius = 0.38f;
                    col.height = 0.55f;
                    break;
                default:
                    col.center = new Vector3(0f, 0.55f, 0f);
                    col.radius = 0.28f;
                    col.height = 1.1f;
                    break;
            }

            go2.transform.position = finalPos;

            var agent = go2.AddComponent<NavMeshAgent>();
            agent.acceleration = 48f;
            agent.angularSpeed = 520f;
            switch (arch)
            {
                case UnitArchetype.Worker:
                    agent.height = 0.92f;
                    agent.radius = 0.3f;
                    break;
                case UnitArchetype.BasicFighter:
                    agent.height = 0.5f;
                    agent.radius = 0.42f;
                    break;
                default:
                    agent.height = 1.12f;
                    agent.radius = 0.27f;
                    break;
            }

            var unit2 = go2.AddComponent<InsectUnit>();
            var def2 = UnitDefinition.CreateRuntimeDefault(arch, body2);
            unit2.Configure(team, def2);
            go2.AddComponent<UnitAnimationDriver>();
            if (team == Team.Enemy && go2.GetComponent<SimpleEnemyAi>() == null)
                go2.AddComponent<SimpleEnemyAi>();
            return unit2;
        }
    }
}
