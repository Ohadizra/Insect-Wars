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
        ScatterTheme _scatterTheme;
        Color _clayColor;
        Color _mapBoundsColor;
        TerrainLayer _mapBaseLayer;
        TerrainLayer _mapSecondaryLayer;
        Material _mapAppleMaterial;

        ClayPlaced[] _clayLayout;
        FruitPlaced[] _fruitLayout;
        TerrainFeaturePlaced[] _terrainFeatureLayout;
        DecorativePrefabPlaced[] _decorativePrefabs;

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

        /// <summary>Used by units for projectile settings when not using a prefab-only pipeline.</summary>
        public static UnitVisualLibrary ActiveVisualLibrary { get; private set; }

        /// <summary>NavMesh agent type ID for units that can climb high grounds (StickSpy).</summary>
        public static int ClimberAgentTypeID { get; private set; }

        static Material s_lit;
        static readonly Dictionary<int, Material> s_tintCache = new();

        void OnDestroy()
        {
            if (ActiveVisualLibrary == visualLibrary)
                ActiveVisualLibrary = null;
            SkirmishPlayArea.Clear();
        }

        static void SafeDestroy(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying)
                Object.Destroy(obj);
            else
                Object.DestroyImmediate(obj);
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
            _scatterTheme = m != null ? m.scatterTheme : ScatterTheme.Default;
            _clayColor = m != null ? m.clayColor : new Color(0.45f, 0.32f, 0.22f);
            _mapBoundsColor = m != null ? m.mapBoundsColor : new Color(0.35f, 0.28f, 0.18f);
            _mapBaseLayer = m != null ? m.baseTerrainLayer : null;
            _mapSecondaryLayer = m != null ? m.secondaryTerrainLayer : null;
            _mapAppleMaterial = m != null ? m.bigAppleMaterial : null;

            _clayLayout = m != null && m.clay != null && m.clay.Length > 0 ? m.clay : DefaultClayList;
            _fruitLayout = m != null && m.fruits != null && m.fruits.Length > 0 ? m.fruits : DefaultFruitList;
            _terrainFeatureLayout = m != null && m.terrainFeatures != null && m.terrainFeatures.Length > 0
                ? m.terrainFeatures
                : System.Array.Empty<TerrainFeaturePlaced>();
            _decorativePrefabs = m != null && m.decorativePrefabs != null ? m.decorativePrefabs : System.Array.Empty<DecorativePrefabPlaced>();
        }

        void Start()
        {
            GameSession.LoadPrefs();
            EnemyResources.Reset();
            BuildWorldPreview();

            if (GameSession.IsLearningMode)
            {
                SpawnLearningModeUnits();
            }
            else
            {
                SpawnNormalModeUnits();
            }

            var camCtrl = FindFirstObjectByType<RTSCameraController>();
            if (camCtrl != null)
                camCtrl.FocusWorldPosition(_camFocus);
        }

        void SpawnNormalModeUnits()
        {
            const float WorkerSpawnRadius = 5f;
            const float CombatSpawnRadius = 10f;
            var playerHiveXZ = new Vector3(_playerHive.x, 0f, _playerHive.z);
            for (var i = 0; i < 5; i++)
            {
                var angle = i * 72f * Mathf.Deg2Rad;
                var offset = new Vector3(Mathf.Cos(angle) * WorkerSpawnRadius, 0f, Mathf.Sin(angle) * WorkerSpawnRadius);
                SpawnUnit(playerHiveXZ + offset, Team.Player, UnitArchetype.Worker);
            }
            SpawnUnit(playerHiveXZ + new Vector3(CombatSpawnRadius, 0, 0f), Team.Player, UnitArchetype.BasicFighter);
            SpawnUnit(playerHiveXZ + new Vector3(-CombatSpawnRadius, 0, 0f), Team.Player, UnitArchetype.BasicFighter);
            SpawnUnit(playerHiveXZ + new Vector3(0f, 0, CombatSpawnRadius), Team.Player, UnitArchetype.BasicRanged);
            SpawnUnit(playerHiveXZ + new Vector3(0f, 0, -CombatSpawnRadius), Team.Player, UnitArchetype.BasicRanged);

            var enemyHiveXZ = new Vector3(_enemyHive.x, 0f, _enemyHive.z);
            var nWorkers = Mathf.RoundToInt(4f * GameSession.DifficultyEnemySpawnMultiplier);
            for (var i = 0; i < nWorkers; i++)
            {
                var angle = i * 72f * Mathf.Deg2Rad;
                var offset = new Vector3(Mathf.Cos(angle) * WorkerSpawnRadius, 0f, Mathf.Sin(angle) * WorkerSpawnRadius);
                SpawnUnit(enemyHiveXZ + offset, Team.Enemy, UnitArchetype.Worker);
            }
            var nCombat = Mathf.Clamp(Mathf.RoundToInt(3f * GameSession.DifficultyEnemySpawnMultiplier), 1, 8);
            var archCycle = new[] { UnitArchetype.BasicFighter, UnitArchetype.BasicFighter, UnitArchetype.BasicRanged };
            for (var i = 0; i < nCombat; i++)
            {
                var angle = (i + nWorkers) * 60f * Mathf.Deg2Rad;
                var offset = new Vector3(Mathf.Cos(angle) * CombatSpawnRadius, 0f, Mathf.Sin(angle) * CombatSpawnRadius);
                var arch = archCycle[Mathf.Min(i, archCycle.Length - 1)];
                SpawnUnit(enemyHiveXZ + offset, Team.Enemy, arch);
            }
        }

        void SpawnLearningModeUnits()
        {
            var hiveXZ = new Vector3(_playerHive.x, 0f, _playerHive.z);

            SpawnUnit(hiveXZ + new Vector3(3f, 0f, 3f), Team.Player, UnitArchetype.Worker);
            SpawnUnit(hiveXZ + new Vector3(6f, 0f, 0f), Team.Player, UnitArchetype.BasicFighter);
            SpawnUnit(hiveXZ + new Vector3(0f, 0f, 6f), Team.Player, UnitArchetype.BasicRanged);
            SpawnUnit(hiveXZ + new Vector3(-6f, 0f, 0f), Team.Player, UnitArchetype.BlackWidow);
            SpawnUnit(hiveXZ + new Vector3(0f, 0f, -6f), Team.Player, UnitArchetype.StickSpy);
            SpawnUnit(hiveXZ + new Vector3(-6f, 0f, -6f), Team.Player, UnitArchetype.GiantStagBeetle);

            var dummyPos = hiveXZ + new Vector3(20f, 0f, 20f);
            var dummyGo = SpawnUnit(dummyPos, Team.Enemy, UnitArchetype.BasicFighter);
            var dummyUnit = dummyGo.GetComponent<InsectUnit>();
            if (dummyUnit != null)
            {
                var def = UnitDefinition.CreateRuntimeDefault(UnitArchetype.BasicFighter,
                    TeamPalette.UnitBody(Team.Enemy, UnitArchetype.BasicFighter));
                def.maxHealth = 100000f;
                dummyUnit.Configure(Team.Enemy, def);
            }
            dummyUnit.gameObject.AddComponent<TrainingDummy>();
            }

        public void BuildWorldPreview()
        {
            ApplyMapLayout();
            SkirmishPlayArea.Configure(_mapHalfExtent, _mapHalfExtent);
            ActiveVisualLibrary = visualLibrary;

            EnsureLitShader();
            var world = GameObject.Find("WorldRoot");
            SafeDestroy(world);

            world = new GameObject("WorldRoot");
            var surface = world.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
            surface.ignoreNavMeshObstacle = false;

            BuildTerrain(world.transform, _mapHalfExtent);
            AddMapBounds(world.transform, _mapHalfExtent, 0.45f, _mapBoundsColor);

            foreach (var c in _clayLayout)
                AddClay(world.transform, c.position, c.scale, _clayColor, c.variant);

            BuildHive(world.transform, _playerHive, Team.Player, "PlayerHive");
            if (!GameSession.IsLearningMode)
                BuildHive(world.transform, _enemyHive, Team.Enemy, "EnemyHive");

            AddRottingApple(world.transform, _applePos);
            if (!GameSession.IsLearningMode)
                AddRottingApple(world.transform, _enemyApplePos);

            foreach (var f in _fruitLayout)
                AddFruit(world.transform, f);

            foreach (var tf in _terrainFeatureLayout)
                AddTerrainFeature(world.transform, tf);

            foreach (var dp in _decorativePrefabs)
                SpawnDecorativePrefab(world.transform, dp);

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
            SkirmishPassiveScatter.Scatter(world.transform, _mapHalfExtent, _scatterSeed, exclusions, _scatterTheme);

            PlaceStarterPlayerBuildings(world.transform);

            RegisterBuildZones();

            surface.BuildNavMesh();

            BuildClimberNavMesh(world);

            var systems = GameObject.Find("Systems");
            SafeDestroy(systems);
systems = new GameObject("Systems");
            systems.AddComponent<PlayerResources>();
            systems.AddComponent<SelectionController>();
            systems.AddComponent<CommandController>();
            systems.AddComponent<GameHUD>();
            systems.AddComponent<BottomBar>();
            systems.AddComponent<Minimap>();
            systems.AddComponent<FogOfWarSystem>();
            systems.AddComponent<MatchDirector>();
            if (!GameSession.IsLearningMode)
                systems.AddComponent<EnemyCommander>();
            systems.AddComponent<PauseController>();
            systems.AddComponent<GameAudio>();
            systems.AddComponent<ControlGroupManager>();
            systems.AddComponent<ControlGroupBar>();
        }


        void PlaceStarterPlayerBuildings(Transform worldRoot)
        {
            if (!GameSession.IsLearningMode) return;

            var hiveXZ = new Vector3(_playerHive.x, 0f, _playerHive.z);
            ProductionBuilding.Place(hiveXZ + new Vector3(-12f, 0f, 4f), BuildingType.Underground, Team.Player, startBuilt: true);
            ProductionBuilding.Place(hiveXZ + new Vector3(4f, 0f, -12f), BuildingType.RootCellar, Team.Player, startBuilt: true);
            ProductionBuilding.Place(hiveXZ + new Vector3(12f, 0f, 4f), BuildingType.SkyTower, Team.Player, startBuilt: true);

            PlaceEnemyDummyBuildings();
        }

        void PlaceEnemyDummyBuildings()
        {
            var dummyCenter = new Vector3(_playerHive.x + 20f, 0f, _playerHive.z + 20f);
            ProductionBuilding.Place(dummyCenter + new Vector3(-8f, 0f, 6f), BuildingType.Underground, Team.Enemy, startBuilt: true);
            ProductionBuilding.Place(dummyCenter + new Vector3(8f, 0f, 6f), BuildingType.SkyTower, Team.Enemy, startBuilt: true);
            ProductionBuilding.Place(dummyCenter + new Vector3(0f, 0f, -8f), BuildingType.AntNest, Team.Enemy, startBuilt: true);
        }

        void RegisterBuildZones()
        {
            BuildZoneRegistry.Clear();
            BuildZoneRegistry.Register(
                new Vector3(_playerHive.x, 0f, _playerHive.z), BuildZoneRegistry.HiveRadius);
            BuildZoneRegistry.Register(
                new Vector3(_enemyHive.x, 0f, _enemyHive.z), BuildZoneRegistry.HiveRadius);

            var allFruit = FindObjectsByType<RottingFruitNode>(FindObjectsSortMode.None);
            foreach (var f in allFruit)
            {
                if (f == null) continue;
                BuildZoneRegistry.Register(f.transform.position, BuildZoneRegistry.FruitRadius);
            }
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
            foreach (var tf in _terrainFeatureLayout)
            {
                float spread = tf.boxHalfExtents.x > 0.01f
                    ? Mathf.Max(tf.boxHalfExtents.x, tf.boxHalfExtents.y) + 2f
                    : tf.radius + 2f;
                z.Add(new SkirmishPassiveScatter.ExclusionZone(new Vector2(tf.position.x, tf.position.z), spread));
            }
            return z;
        }

        void SpawnDecorativePrefab(Transform parent, DecorativePrefabPlaced dp)
        {
            if (string.IsNullOrEmpty(dp.prefabPath)) return;
            string resPath = dp.prefabPath;
            if (resPath.Contains("Assets/_InsectWars/"))
            {
                resPath = resPath.Replace("Assets/_InsectWars/", "");
                if (resPath.EndsWith(".prefab")) resPath = resPath[..^".prefab".Length];
            }
            GameObject prefab = Resources.Load<GameObject>(resPath);
        #if UNITY_EDITOR
            if (prefab == null)
                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(dp.prefabPath);
        #endif
            if (prefab == null) return;

            var go = Instantiate(prefab, parent);
            go.name = "Decorative_" + prefab.name;
            go.transform.position = dp.position;
            go.transform.rotation = Quaternion.Euler(dp.rotation);
            go.transform.localScale = dp.scale;

            // Ensure it doesn't block pathing or show up in NavMesh
            var mod = go.GetComponent<NavMeshModifier>();
            if (mod == null) mod = go.AddComponent<NavMeshModifier>();
            mod.ignoreFromBuild = true;

            var obs = go.GetComponent<NavMeshObstacle>();
            if (obs != null) obs.enabled = false;

            // Ignore from build recursive
            SetIgnoreNavMeshRecursive(go);
        }

        static void AddMapBounds(Transform parent, float extent, float thickness, Color boundsColor)
        {
            var lib = ActiveVisualLibrary;
            Material oobMat = (lib != null && lib.outOfBoundsMaterial != null) ? lib.outOfBoundsMaterial : null;
            Material barrierMat = (lib != null && lib.mapBarrierMaterial != null) ? lib.mapBarrierMaterial : null;

            bool isFrozen = (lib != null && lib.baseSoilLayer != null && lib.baseSoilLayer.name.Contains("Frozen"));
            // Better check for frozen theme
            if (!isFrozen)
            {
                var m = GameSession.SelectedMap;
                if (m != null && m.scatterTheme == ScatterTheme.Frozen) isFrozen = true;
            }

            float mapWallTopY = isFrozen ? 5.0f : 2.5f;
            float mapWallBottomY = -5.0f;
            float barrierHeight = mapWallTopY - mapWallBottomY;
float len = extent * 2f;

            void Edge(string name, Vector3 pos, Vector3 scale)
            {
                var container = new GameObject(name);
                container.transform.SetParent(parent);
                container.transform.position = pos;

                // Create a single solid cube for the entire edge to ensure it is perfectly straight
                var e = GameObject.CreatePrimitive(PrimitiveType.Cube);
                e.name = name + "_Solid";
                e.transform.SetParent(container.transform);
                
                // Align the top of the wall to a fixed world height (mapWallTopY)
                // and extend deep into the ground (mapWallBottomY) to handle all levels
                float centerY = (mapWallTopY + mapWallBottomY) * 0.5f;
                e.transform.position = new Vector3(pos.x, centerY, pos.z);
                e.transform.localScale = new Vector3(scale.x, barrierHeight, scale.z);
                
                if (barrierMat != null) e.GetComponent<Renderer>().sharedMaterial = barrierMat;
                else ApplyMat(e, boundsColor);
                
                SafeDestroy(e.GetComponent<Collider>());
            }

            Edge("MapEdge_N", new Vector3(0f, 0f, extent), new Vector3(len, thickness, thickness));
            Edge("MapEdge_S", new Vector3(0f, 0f, -extent), new Vector3(len, thickness, thickness));
            Edge("MapEdge_E", new Vector3(extent, 0f, 0f), new Vector3(thickness, thickness, len));
            Edge("MapEdge_W", new Vector3(-extent, 0f, 0f), new Vector3(thickness, thickness, len));

            if (oobMat != null)
            {
                var skirt = GameObject.CreatePrimitive(PrimitiveType.Plane);
                skirt.name = "OutOfBounds_Skirt";
                skirt.transform.SetParent(parent);
                skirt.transform.position = new Vector3(0f, -0.05f, 0f);
                skirt.transform.localScale = new Vector3(400f, 1f, 400f); 
                skirt.GetComponent<Renderer>().sharedMaterial = oobMat;
                SafeDestroy(skirt.GetComponent<Collider>());

                if (isFrozen && lib != null && lib.iciclePrefab != null)
                {
                    // Bold and crazy: Scatter huge icicles in the abyss
                    // EXCLUDE SOUTH to avoid camera occlusion
                    Random.InitState(42); 
                    for (int i = 0; i < 150; i++)
                    {
                        Vector2 p = Random.insideUnitCircle.normalized * (extent + Random.Range(35f, 160f));
                        
                        // Avoid spawning in the south sector entirely
                        if (p.y < -extent + 10f) continue; 
                        
                        // Keep further away from playable bounds due to huge scale
                        if (Mathf.Abs(p.x) < extent + 25f && Mathf.Abs(p.y) < extent + 25f) continue;

                        var icicle = Instantiate(lib.iciclePrefab, parent);
                        icicle.name = "Abyss_Icicle_" + i;
                        icicle.transform.position = new Vector3(p.x, -2f, p.y);
                        icicle.transform.localScale = Vector3.one * Random.Range(15f, 45f);
                        icicle.transform.rotation = Quaternion.Euler(Random.Range(-10f, 10f), Random.Range(0, 360), Random.Range(-10f, 10f));
                    }
                }
}
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

        static void BuildStickSpyVisual(Transform root, Color body, Team team)
        {
            var skin = TeamPalette.GetShellColor(team);
            var accent = TeamPalette.GetTeamColor(team);
            var bark = new Color(0.38f, 0.28f, 0.16f);

            // Tall elongated body — biggest insect (1.5x mantis)
            AddPrimitivePart(root, PrimitiveType.Capsule, new Vector3(0f, 0.75f, 0f),
                new Vector3(0.15f, 0.75f, 0.15f), Quaternion.identity, bark);

            // Head
            AddPrimitivePart(root, PrimitiveType.Sphere, new Vector3(0f, 1.5f, 0f),
                Vector3.one * 0.21f, Quaternion.identity, Color.Lerp(bark, skin, 0.3f));

            // Long thin legs (x4)
            for (int i = 0; i < 4; i++)
            {
                float angle = (i * 90f + 45f) * Mathf.Deg2Rad;
                float cx = Mathf.Cos(angle) * 0.18f;
                float cz = Mathf.Sin(angle) * 0.18f;
                AddPrimitivePart(root, PrimitiveType.Capsule,
                    new Vector3(cx, 0.3f, cz),
                    new Vector3(0.06f, 0.33f, 0.06f),
                    Quaternion.Euler(0f, 0f, i < 2 ? 25f : -25f), bark * 0.85f);
            }

            // Accent band — team identifier
            AddPrimitivePart(root, PrimitiveType.Cylinder, new Vector3(0f, 0.975f, 0f),
                new Vector3(0.21f, 0.045f, 0.21f), Quaternion.identity, accent);
        }

        static void BuildStagBeetleVisual(Transform root, Color body, Team team)
        {
            var skin = TeamPalette.GetShellColor(team);
            var accent = TeamPalette.GetTeamColor(team);
            var chitin = new Color(0.25f, 0.18f, 0.10f);

            // Wide, low, heavily-armored carapace
            AddPrimitivePart(root, PrimitiveType.Capsule, new Vector3(0f, 0.35f, 0f),
                new Vector3(0.7f, 0.4f, 0.55f), Quaternion.Euler(10f, 0f, 0f), skin);

            // Head (front-facing, slightly lower)
            AddPrimitivePart(root, PrimitiveType.Sphere, new Vector3(0f, 0.55f, 0.3f),
                Vector3.one * 0.35f, Quaternion.identity, Color.Lerp(skin, chitin, 0.4f));

            // Giant mandibles / antlers (left + right)
            AddPrimitivePart(root, PrimitiveType.Capsule, new Vector3(-0.15f, 0.6f, 0.55f),
                new Vector3(0.08f, 0.25f, 0.08f), Quaternion.Euler(50f, 20f, 0f), chitin);
            AddPrimitivePart(root, PrimitiveType.Capsule, new Vector3(0.15f, 0.6f, 0.55f),
                new Vector3(0.08f, 0.25f, 0.08f), Quaternion.Euler(50f, -20f, 0f), chitin);

            // Thick legs (x6)
            for (int i = 0; i < 6; i++)
            {
                float side = (i % 2 == 0) ? -1f : 1f;
                float zOff = (i / 2 - 1) * 0.25f;
                AddPrimitivePart(root, PrimitiveType.Capsule,
                    new Vector3(side * 0.45f, 0.12f, zOff),
                    new Vector3(0.1f, 0.18f, 0.1f),
                    Quaternion.Euler(0f, 0f, side * 35f), chitin * 0.9f);
            }

            // Accent straps — heavy pauldrons on each side
            AddPrimitivePart(root, PrimitiveType.Cube, new Vector3(-0.4f, 0.45f, 0f),
                new Vector3(0.15f, 0.12f, 0.35f), Quaternion.Euler(0f, 0f, 15f), accent);
            AddPrimitivePart(root, PrimitiveType.Cube, new Vector3(0.4f, 0.45f, 0f),
                new Vector3(0.15f, 0.12f, 0.35f), Quaternion.Euler(0f, 0f, -15f), accent);

            // Top accent ring
            AddPrimitivePart(root, PrimitiveType.Cylinder, new Vector3(0f, 0.55f, 0f),
                new Vector3(0.6f, 0.04f, 0.6f), Quaternion.identity, accent);
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

        static void AddClay(Transform parent, Vector3 pos, Vector3 scale, Color clayColor, int variant = 0)
        {
            var lib = ActiveVisualLibrary;
            GameObject clay;
            GameObject prefab = null;
            if (lib != null)
            {
                if (variant == 1 && lib.clayWallCornerPrefab != null) prefab = lib.clayWallCornerPrefab;
                else if (variant == 2 && lib.clayWallPillarPrefab != null) prefab = lib.clayWallPillarPrefab;
                else prefab = lib.clayWallPrefab;
            }

            if (prefab != null)
            {
                clay = Instantiate(prefab, parent);
                clay.name = "Clay";
                clay.tag = "Clay";
                
                // Align top to a fixed world height (2.4f) and extend deep (-3.0f) to handle high/low ground
                float topY = 2.4f;
                float bottomY = -3.0f;
float height = topY - bottomY;
                clay.transform.position = new Vector3(pos.x, (topY + bottomY) * 0.5f, pos.z);
                clay.transform.localScale = new Vector3(scale.x, height, scale.z);
foreach (var r in clay.GetComponentsInChildren<Renderer>())
                {
                    if (!Application.isPlaying) continue;
                    var mats = r.materials;
                    for (int mi = 0; mi < mats.Length; mi++)
                    {
                        mats[mi] = new Material(mats[mi]);
                        if (mats[mi].HasProperty("_BaseColor"))
                            mats[mi].SetColor("_BaseColor", clayColor);
                        else if (mats[mi].HasProperty("_Color"))
                            mats[mi].SetColor("_Color", clayColor);
                    }
                    r.materials = mats;
                }
}
            else
            {
                clay = GameObject.CreatePrimitive(PrimitiveType.Cube);
                clay.name = "Clay";
                clay.tag = "Clay";
                clay.transform.SetParent(parent);
                
                // Align top to a fixed world height (2.4f) and extend deep (-3.0f) to handle high/low ground
                float topY = 2.4f;
                float bottomY = -3.0f;
float height = topY - bottomY;
                clay.transform.position = new Vector3(pos.x, (topY + bottomY) * 0.5f, pos.z);
                clay.transform.localScale = new Vector3(scale.x, height, scale.z);
                
                // Use theme material for frozen walls
                bool isFrozen = (lib != null && lib.baseSoilLayer != null && lib.baseSoilLayer.name.Contains("Frozen"));
                if (isFrozen && lib.mapBarrierMaterial != null)
                    clay.GetComponent<Renderer>().sharedMaterial = lib.mapBarrierMaterial;
                else
                    ApplyMat(clay, clayColor);
}

            var obs = clay.GetComponent<NavMeshObstacle>();
            if (obs == null) obs = clay.AddComponent<NavMeshObstacle>();
            obs.carving = true;
            obs.shape = NavMeshObstacleShape.Box;
            obs.size = Vector3.one;
            obs.center = Vector3.zero;
        }

        static float SampleMaxTerrainHeight(Vector3 center, float footprint)
        {
            if (s_terrain == null) return 0f;
            float best = s_terrain.SampleHeight(center);
            float half = footprint * 0.5f;
            best = Mathf.Max(best, s_terrain.SampleHeight(center + new Vector3(half, 0f, half)));
            best = Mathf.Max(best, s_terrain.SampleHeight(center + new Vector3(-half, 0f, half)));
            best = Mathf.Max(best, s_terrain.SampleHeight(center + new Vector3(half, 0f, -half)));
            best = Mathf.Max(best, s_terrain.SampleHeight(center + new Vector3(-half, 0f, -half)));
            return best;
        }

        /// <summary>
        /// Lifts a game object so the bottom of its combined renderer bounds
        /// sits exactly at <paramref name="groundY"/>.
        /// </summary>
        static void PlaceOnGround(GameObject go, float groundY)
        {
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                var pos = go.transform.position;
                go.transform.position = new Vector3(pos.x, groundY, pos.z);
                return;
            }
            var combined = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                combined.Encapsulate(renderers[i].bounds);

            float bottomY = combined.min.y;
            float lift = groundY - bottomY;
            var p = go.transform.position;
            go.transform.position = new Vector3(p.x, p.y + lift, p.z);
        }

        void BuildHive(Transform parent, Vector3 worldPos, Team team, string name)
        {
            const float hiveScale = 2.7f;
            float groundY = SampleMaxTerrainHeight(worldPos, 10f);

            GameObject hive;
            if (visualLibrary != null && visualLibrary.hivePrefab != null)
            {
                hive = Instantiate(visualLibrary.hivePrefab, parent);
                hive.name = name;
                if (!hive.CompareTag("Hive")) hive.tag = "Hive";
                hive.transform.position = new Vector3(worldPos.x, 0f, worldPos.z);
                hive.transform.localScale *= hiveScale;
                PlaceOnGround(hive, groundY);
                
                var deposit = hive.GetComponent<HiveDeposit>();
                if (deposit == null) deposit = hive.AddComponent<HiveDeposit>();
                
                // CRITICAL: Configure before any logic runs to set correct static refs
                deposit.Configure(team);
                if (team == Team.Player) HiveDeposit.SetMainPlayerHive(deposit);
                else if (team == Team.Enemy) HiveDeposit.SetMainEnemyHive(deposit);
                if (team == Team.Player) HiveDeposit.SetMainPlayerHive(deposit);
                else if (team == Team.Enemy) HiveDeposit.SetMainEnemyHive(deposit);
                if (team == Team.Player) HiveDeposit.SetMainPlayerHive(deposit);
                else if (team == Team.Enemy) HiveDeposit.SetMainEnemyHive(deposit);
                if (team == Team.Player) HiveDeposit.SetMainPlayerHive(deposit);
                else if (team == Team.Enemy) HiveDeposit.SetMainEnemyHive(deposit);
                if (team == Team.Player) HiveDeposit.SetMainPlayerHive(deposit);
                else if (team == Team.Enemy) HiveDeposit.SetMainEnemyHive(deposit);
                if (team == Team.Player) HiveDeposit.SetMainPlayerHive(deposit);
                else if (team == Team.Enemy) HiveDeposit.SetMainEnemyHive(deposit);
                if (team == Team.Player) HiveDeposit.SetMainPlayerHive(deposit);
                else if (team == Team.Enemy) HiveDeposit.SetMainEnemyHive(deposit);
                
                if (hive.GetComponent<HiveVisual>() == null) hive.AddComponent<HiveVisual>();
                
                // Apply skin color to prefab renderers
                var skinColor = TeamPalette.GetShellColor(team);
                foreach (var renderer in hive.GetComponentsInChildren<Renderer>(true))
                {
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

                var prod = hive.AddComponent<ProductionBuilding>();
                prod.Initialize(BuildingType.AntNest, team);
            }
            else
            {
                hive = GameObject.CreatePrimitive(PrimitiveType.Cube);
                hive.name = name;
                hive.tag = "Hive";
                hive.transform.SetParent(parent);
                hive.transform.localScale = new Vector3(4f, 2.5f, 4f) * hiveScale;
                hive.transform.position = new Vector3(worldPos.x, 0f, worldPos.z);
                PlaceOnGround(hive, groundY);
                ApplyMat(hive, new Color(0.32f, 0.52f, 0.88f));
                
                // Add straps to the primitive Hive
                var strapColor = TeamPalette.GetTeamColor(team);
                AddPrimitivePart(hive.transform, PrimitiveType.Cube, new Vector3(0f, 0.52f, 0f), new Vector3(1.05f, 0.08f, 1.05f), 
                    Quaternion.identity, strapColor);
                AddPrimitivePart(hive.transform, PrimitiveType.Cube, new Vector3(0f, 0f, 0.52f), new Vector3(0.5f, 1.05f, 0.08f), 
                    Quaternion.identity, strapColor);
                AddPrimitivePart(hive.transform, PrimitiveType.Cube, new Vector3(0f, 0f, -0.52f), new Vector3(0.5f, 1.05f, 0.08f), 
                    Quaternion.identity, strapColor);

                var deposit = hive.AddComponent<HiveDeposit>();
                deposit.Configure(team);
                hive.AddComponent<HiveVisual>();

                var prod = hive.AddComponent<ProductionBuilding>();
                prod.Initialize(BuildingType.AntNest, team);
            }

            SetIgnoreNavMeshRecursive(hive);
            AddHiveObstacle(hive);
        }

        void BuildTerrain(Transform parent, float mapHalfExtent)
        {
            float size = mapHalfExtent * 2f;
            int resolution = 256;

            TerrainData terrainData = new TerrainData();
            terrainData.heightmapResolution = resolution + 1;
            terrainData.size = new Vector3(size, 20f, size);
            terrainData.alphamapResolution = 128;

            if (_mapBaseLayer != null && _mapSecondaryLayer != null)
                terrainData.terrainLayers = new[] { _mapBaseLayer, _mapSecondaryLayer };
            else if (visualLibrary != null && visualLibrary.baseSoilLayer != null && visualLibrary.drySoilLayer != null)
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
                        if (hg.radius <= 0.001f) continue;

                        float dist = Vector2.Distance(new Vector2(u, v), hg.uv);
                        float rw = Mathf.Max(hg.rampWidth, 0.01f);
                        if (dist < hg.radius)
                        {
                            maxH = Mathf.Max(maxH, hg.heightFraction);
                        }
                        else
                        {
                            // Directional ramp logic using hg.rotation
                            float angle = Mathf.Atan2(v - hg.uv.y, u - hg.uv.x) * Mathf.Rad2Deg;
                            float angleDiff = Mathf.Abs(Mathf.DeltaAngle(angle, hg.rotation));
                            float rampHalfAngle = (rw / hg.radius) * Mathf.Rad2Deg * 0.5f;

                            if (angleDiff < rampHalfAngle)
                            {
                                // Gentle walkable ramp
                                if (dist < hg.radius + rw)
                                {
                                    float t = (dist - hg.radius) / rw;
                                    maxH = Mathf.Max(maxH, Mathf.Lerp(hg.heightFraction, 0f, t));
                                }
                            }
                            else
                            {
                                // Steep cliff — climbable only by high-slope agents (StickSpy)
                                const float cliffWidth = 0.025f;
                                if (dist < hg.radius + cliffWidth)
                                {
                                    float t = (dist - hg.radius) / cliffWidth;
                                    maxH = Mathf.Max(maxH, Mathf.Lerp(hg.heightFraction, 0f, Mathf.Pow(t, 2)));
                                }
                            }
                        }
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
                        if (hg.radius <= 0.001f) continue;

                        float dist = Vector2.Distance(new Vector2(u, v), hg.uv);
                        float rw = Mathf.Max(hg.rampWidth, 0.01f);
                        if (dist < hg.radius)
                        {
                            h = 1f;
                        }
                        else
                        {
                            float angle = Mathf.Atan2(v - hg.uv.y, u - hg.uv.x) * Mathf.Rad2Deg;
                            float angleDiff = Mathf.Abs(Mathf.DeltaAngle(angle, hg.rotation));
                            float rampHalfAngle = (rw / hg.radius) * Mathf.Rad2Deg * 0.5f;

                            if (angleDiff < rampHalfAngle)
                            {
                                if (dist < hg.radius + rw)
                                    h = Mathf.Max(h, Mathf.InverseLerp(hg.radius + rw, hg.radius, dist));
                            }
                            else
                            {
                                const float cliffWidth = 0.008f;
                                if (dist < hg.radius + cliffWidth)
                                    h = Mathf.Max(h, Mathf.Pow(Mathf.InverseLerp(hg.radius + cliffWidth, hg.radius, dist), 4));
                            }
                        }
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

        void AddRottingApple(Transform parent, Vector3 pos)
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
                apple.transform.position = new Vector3(pos.x, posY, pos.z);
                apple.transform.localScale = new Vector3(4f, appleHeight, 4f);

                Material appleMat = _mapAppleMaterial != null ? _mapAppleMaterial : lib.bigAppleMaterial;
                if (appleMat != null)
                {
                    foreach (var rend in apple.GetComponentsInChildren<Renderer>())
                        rend.sharedMaterial = appleMat;
                }
            }
            else
            {
                apple = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                apple.name = "RottingApple";
                apple.tag = "Fruit";
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

            int resLayer = LayerMask.NameToLayer("Resources");
            if (resLayer >= 0)
                SetLayerRecursive(apple, resLayer);

            SetIgnoreNavMeshRecursive(apple);

            AddFruitObstacle(apple);

            if (apple.GetComponent<RottingFruitNode>() == null)
                apple.AddComponent<RottingFruitNode>();
        }

        void AddFruit(Transform parent, FruitPlaced f)
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
                fruit.transform.position = new Vector3(pos.x, posY, pos.z);
                fruit.transform.localScale = Vector3.one * fruitHeight;

                Material appleMat = _mapAppleMaterial != null ? _mapAppleMaterial : lib.bigAppleMaterial;
                if (appleMat != null)
                {
                    foreach (var rend in fruit.GetComponentsInChildren<Renderer>())
                        rend.sharedMaterial = appleMat;
                }
            }
            else
            {
                fruit = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                fruit.name = "RottingFruit";
                fruit.tag = "Fruit";
                fruit.transform.SetParent(parent);
                fruit.transform.position = new Vector3(pos.x, posY, pos.z);
                fruit.transform.localScale = Vector3.one * fruitHeight;
                ApplyMat(fruit, new Color(0.65f, 0.2f, 0.55f));
            }

            int resLayer = LayerMask.NameToLayer("Resources");
            if (resLayer >= 0)
                SetLayerRecursive(fruit, resLayer);

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

            // Replace any prefab colliders with a single SphereCollider that
            // matches the renderer bounds so ants are physically stopped at
            // the apple surface but not too far away.
            foreach (var col in fruit.GetComponentsInChildren<Collider>())
            {
                if (Application.isPlaying) Object.Destroy(col);
                else Object.DestroyImmediate(col);
            }

            var rend = fruit.GetComponentInChildren<Renderer>();
            float visualRadius = 1f;
            if (rend != null)
                visualRadius = Mathf.Max(rend.bounds.extents.x, rend.bounds.extents.z);
            else
                visualRadius = Mathf.Max(fruit.transform.localScale.x, fruit.transform.localScale.z) * 0.5f;

            var sc = fruit.AddComponent<SphereCollider>();
            sc.center = rend != null
                ? fruit.transform.InverseTransformPoint(rend.bounds.center)
                : Vector3.zero;
            // Radius in local space: world visual radius / max XZ scale
            float maxXZScale = Mathf.Max(fruit.transform.lossyScale.x, fruit.transform.lossyScale.z);
            sc.radius = maxXZScale > 0.001f ? visualRadius / maxXZScale : 0.5f;

            var s = fruit.transform.localScale;
            var obsGo = new GameObject("NavObstacle");
            obsGo.transform.SetParent(fruit.transform, false);
            obsGo.transform.localPosition = Vector3.zero;
            obsGo.transform.localScale = new Vector3(1f / s.x, 1f / s.y, 1f / s.z);

            var obs = obsGo.AddComponent<NavMeshObstacle>();
            obs.carving = true;

            if (rend != null)
            {
                var b = rend.bounds;
                obs.shape = NavMeshObstacleShape.Box;
                obs.size = b.size * 0.5f;
                obs.center = b.center - fruit.transform.position;
            }
            else
            {
                obs.shape = NavMeshObstacleShape.Capsule;
                obs.radius = Mathf.Max(s.x, s.z) * 0.45f;
                obs.height = s.y;
                obs.center = Vector3.zero;
            }
        }

        static void AddHiveObstacle(GameObject hive)
        {
            var existing = hive.GetComponentInChildren<NavMeshObstacle>();
            if (existing != null)
            {
                if (Application.isPlaying) Object.Destroy(existing);
                else Object.DestroyImmediate(existing);
            }

            var s = hive.transform.localScale;
            var obsGo = new GameObject("NavObstacle");
            obsGo.transform.SetParent(hive.transform, false);
            obsGo.transform.localPosition = Vector3.zero;
            obsGo.transform.localScale = new Vector3(1f / s.x, 1f / s.y, 1f / s.z);

            var obs = obsGo.AddComponent<NavMeshObstacle>();
            obs.carving = true;

            var rend = hive.GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                var b = rend.bounds;
                obs.shape = NavMeshObstacleShape.Box;
                obs.size = new Vector3(b.size.x * 0.55f, b.size.y, b.size.z * 0.55f);
                obs.center = b.center - hive.transform.position;
            }
            else
            {
                obs.shape = NavMeshObstacleShape.Box;
                obs.size = new Vector3(s.x * 0.55f, s.y, s.z * 0.55f);
                obs.center = new Vector3(0f, s.y * 0.5f, 0f);
            }
        }

        static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
                SetLayerRecursive(child.gameObject, layer);
        }

        static void SetIgnoreNavMeshRecursive(GameObject go)
        {
            var mod = go.GetComponent<NavMeshModifier>();
            if (mod == null) mod = go.AddComponent<NavMeshModifier>();
            mod.ignoreFromBuild = true;
            foreach (Transform child in go.transform)
                SetIgnoreNavMeshRecursive(child.gameObject);
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
            var color = TerrainFeatureProperties.GetBaseColor(placed.type, _scatterTheme);
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

        static NavMeshData s_climberNavData;

        static void BuildClimberNavMesh(GameObject worldRoot)
        {
            var settings = NavMesh.CreateSettings();
            settings.agentSlope = 85f;
            settings.agentClimb = 2.5f;
            settings.agentRadius = 0.25f;
            settings.agentHeight = 0.8f;
            ClimberAgentTypeID = settings.agentTypeID;

            var sources = new List<NavMeshBuildSource>();
            var markups = new List<NavMeshBuildMarkup>();
            NavMeshBuilder.CollectSources(
                worldRoot.transform, ~0,
                NavMeshCollectGeometry.PhysicsColliders,
                0, markups, sources);

            var bounds = new Bounds(Vector3.zero, Vector3.one * 500f);

            s_climberNavData = NavMeshBuilder.BuildNavMeshData(
                settings, sources, bounds,
                worldRoot.transform.position, worldRoot.transform.rotation);

            if (s_climberNavData != null)
            {
                s_climberNavData.name = "ClimberNavMesh";
                NavMesh.AddNavMeshData(s_climberNavData);
                Debug.Log($"[Insect Wars] Climber NavMesh baked — agentTypeID={settings.agentTypeID}, slope=85°, sources={sources.Count}");
            }
            else
            {
                Debug.LogError("[Insect Wars] Failed to build climber NavMesh!");
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

                var agent = go.GetComponent<NavMeshAgent>();
                if (agent != null) agent.enabled = false;
                go.transform.position = finalPos;
                if (agent != null)
                {
                    agent.enabled = true;
                    if (NavMesh.SamplePosition(finalPos, out var hit, 5f, NavMesh.AllAreas))
                        agent.Warp(hit.position);
                    else
                        agent.Warp(finalPos);
                }

                var unit = go.GetComponent<InsectUnit>();
                if (unit == null) unit = go.AddComponent<InsectUnit>();
                
                Color shellColor = TeamPalette.GetShellColor(team);
                var def = UnitDefinition.CreateRuntimeDefault(arch, shellColor);
                unit.Configure(team, def);
                
                bool needsStrongTint = arch == UnitArchetype.BlackWidow
                                    || arch == UnitArchetype.GiantStagBeetle;

                foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer.sharedMaterial == null) continue;

                    if (needsStrongTint)
                    {
                        var mat = new Material(renderer.sharedMaterial);
                        if (mat.HasProperty("_BaseColor"))
                        {
                            Color orig = mat.GetColor("_BaseColor");
                            mat.SetColor("_BaseColor", Color.Lerp(orig, shellColor, 0.55f));
                        }
                        if (mat.HasProperty("_Color"))
                        {
                            Color orig = mat.GetColor("_Color");
                            mat.SetColor("_Color", Color.Lerp(orig, shellColor, 0.55f));
                        }
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", shellColor * 0.18f);
                        renderer.material = mat;
                    }
                    else
                    {
                        var block = new MaterialPropertyBlock();
                        renderer.GetPropertyBlock(block);
                        if (renderer.sharedMaterial.HasProperty("_BaseColor"))
                            block.SetColor("_BaseColor", shellColor);
                        if (renderer.sharedMaterial.HasProperty("_Color"))
                            block.SetColor("_Color", shellColor);
                        if (renderer.sharedMaterial.HasProperty("_EmissionColor"))
                            block.SetColor("_EmissionColor", shellColor * 0.45f);
                        renderer.SetPropertyBlock(block);
                    }
                }

                if (go.GetComponent<UnitAnimationDriver>() == null)
                    go.AddComponent<UnitAnimationDriver>();
                if (arch == UnitArchetype.BlackWidow && go.GetComponent<WebNetAbility>() == null)
                    go.AddComponent<WebNetAbility>();
                if (arch == UnitArchetype.StickSpy)
                {
                    if (go.GetComponent<StickStealth>() == null)
                        go.AddComponent<StickStealth>();
                    if (agent != null)
                        agent.agentTypeID = ClimberAgentTypeID;
                }
                if (arch == UnitArchetype.GiantStagBeetle && go.GetComponent<GroundStomp>() == null)
                    go.AddComponent<GroundStomp>();
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
                case UnitArchetype.BlackWidow:
                    BuildFighterVisual(visualRoot.transform, body2, team);
                    break;
                case UnitArchetype.StickSpy:
                    BuildStickSpyVisual(visualRoot.transform, body2, team);
                    break;
                case UnitArchetype.GiantStagBeetle:
                    BuildFighterVisual(visualRoot.transform, body2, team);
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
                case UnitArchetype.BlackWidow:
                    col.center = new Vector3(0f, 0.25f, 0f);
                    col.radius = 0.4f;
                    col.height = 0.6f;
                    break;
                case UnitArchetype.StickSpy:
                    col.center = new Vector3(0f, 0.825f, 0f);
                    col.radius = 0.18f;
                    col.height = 1.8f;
                    break;
                case UnitArchetype.GiantStagBeetle:
                    col.center = new Vector3(0f, 0.45f, 0f);
                    col.radius = 0.55f;
                    col.height = 1.0f;
                    break;
                default:
                    col.center = new Vector3(0f, 0.55f, 0f);
                    col.radius = 0.28f;
                    col.height = 1.1f;
                    break;
            }

            go2.transform.position = finalPos;

            var agent2 = go2.AddComponent<NavMeshAgent>();
            agent2.enabled = false;
            agent2.acceleration = 48f;
            agent2.angularSpeed = 520f;
            agent2.autoBraking = false;
            switch (arch)
            {
                case UnitArchetype.Worker:
                    agent2.height = 0.92f;
                    agent2.radius = 0.4f;
                    agent2.avoidancePriority = 50;
                    break;
                case UnitArchetype.BasicFighter:
                    agent2.height = 0.5f;
                    agent2.radius = 0.85f;
                    agent2.avoidancePriority = 40;
                    break;
                case UnitArchetype.BlackWidow:
                    agent2.height = 0.55f;
                    agent2.radius = 0.55f;
                    agent2.avoidancePriority = 35;
                    break;
                case UnitArchetype.StickSpy:
                    agent2.height = 1.35f;
                    agent2.radius = 0.22f;
                    agent2.avoidancePriority = 60;
                    break;
                case UnitArchetype.GiantStagBeetle:
                    agent2.height = 0.9f;
                    agent2.radius = 0.7f;
                    agent2.avoidancePriority = 20;
                    break;
                default:
                    agent2.height = 1.12f;
                    agent2.radius = 0.35f;
                    agent2.avoidancePriority = 45;
                    break;
            }
            agent2.enabled = true;
            if (NavMesh.SamplePosition(finalPos, out var hit2, 5f, NavMesh.AllAreas))
                agent2.Warp(hit2.position);
            else
                agent2.Warp(finalPos);

            var unit2 = go2.AddComponent<InsectUnit>();
            var def2 = UnitDefinition.CreateRuntimeDefault(arch, body2);
            unit2.Configure(team, def2);
            go2.AddComponent<UnitAnimationDriver>();
            if (arch == UnitArchetype.BlackWidow)
                go2.AddComponent<WebNetAbility>();
            if (arch == UnitArchetype.StickSpy)
            {
                go2.AddComponent<StickStealth>();
                agent2.agentTypeID = ClimberAgentTypeID;
            }
            if (arch == UnitArchetype.GiantStagBeetle)
                go2.AddComponent<GroundStomp>();
            if (team == Team.Enemy && go2.GetComponent<SimpleEnemyAi>() == null)
                go2.AddComponent<SimpleEnemyAi>();
            return unit2;
        }
    }
}
