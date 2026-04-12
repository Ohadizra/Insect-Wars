using System.Collections;
using System.Collections.Generic;
using InsectWars.Core;
using InsectWars.Data;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace InsectWars.RTS
{
    [DefaultExecutionOrder(-50)]
    public class MapDirector : MonoBehaviour
    {
        [SerializeField] MapDefinition mapDefinition;
        [SerializeField] UnitVisualLibrary visualLibrary;

        float _mapHalfExtent;
        Vector3 _playerHive;
        Vector3 _enemyHive;
        Vector3 _applePos;
        Vector3 _enemyApplePos;
        int _scatterSeed;
        ClayPlaced[] _clayLayout;
        GameObject _clayWallOverride;
        FruitPlaced[] _fruitLayout;
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

        static readonly FruitPlaced[] DefaultFruitList = System.Array.Empty<FruitPlaced>();

        public static UnitVisualLibrary ActiveVisualLibrary { get; private set; }

        static Material s_lit;
        static readonly Dictionary<int, Material> s_tintCache = new();

        void OnDestroy()
        {
            if (ActiveVisualLibrary == visualLibrary)
                ActiveVisualLibrary = null;
            PlayArea.Clear();
        }

        void DestroyObject(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying)
                Destroy(go);
            else
                DestroyImmediate(go);
        }

        const float WorkerSpawnRadius = 9f;
        const float CombatSpawnRadius = 15f;
        const float RecommendedMaxNestToAppleDistance = 25f;
        const float MinNestToAppleDistance = 5f;

        void ApplyMapLayout()
        {
            var m = GameSession.SelectedMap != null ? GameSession.SelectedMap : mapDefinition;
            _mapHalfExtent = m != null ? m.mapHalfExtent : 88f;
            _playerHive = m != null ? m.playerHivePosition : new Vector3(-62f, 1f, -52f);
            _enemyHive = m != null ? m.enemyHivePosition : new Vector3(62f, 1f, 52f);
            _applePos = m != null ? m.bigApplePosition : new Vector3(-50f, 1.5f, -42f);
            _enemyApplePos = m != null ? m.enemyBigApplePosition : new Vector3(50f, 1.5f, 42f);
            _scatterSeed = m != null ? m.passiveScatterSeed : 18427;
            _clayLayout = m != null && m.clay != null && m.clay.Length > 0 ? m.clay : DefaultClayList;
            _clayWallOverride = m != null ? m.clayWallPrefabOverride : null;
            _fruitLayout = m != null && m.fruits != null && m.fruits.Length > 0 ? m.fruits : DefaultFruitList;
            _terrainFeatureLayout = m != null && m.terrainFeatures != null && m.terrainFeatures.Length > 0
                ? m.terrainFeatures
                : System.Array.Empty<TerrainFeaturePlaced>();

            ValidateNestAppleDistance(_playerHive, _applePos, "Player");
            ValidateNestAppleDistance(_enemyHive, _enemyApplePos, "Enemy");
        }

        static Vector3 SpawnPositionAroundBuilding(Vector3 buildingPos, float radius, float angleDeg)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            float baseY = GetHeight(buildingPos);
            var pos = new Vector3(
                buildingPos.x + Mathf.Cos(rad) * radius,
                baseY,
                buildingPos.z + Mathf.Sin(rad) * radius);
            if (NavMesh.SamplePosition(pos, out var hit, 8f, NavMesh.AllAreas))
                return hit.position;
            return pos;
        }

        static void ValidateNestAppleDistance(Vector3 hivePos, Vector3 applePos, string label)
        {
            float dist = Vector3.Distance(
                new Vector3(hivePos.x, 0f, hivePos.z),
                new Vector3(applePos.x, 0f, applePos.z));
            if (dist > RecommendedMaxNestToAppleDistance)
                Debug.LogWarning($"[MapDirector] {label} nest-to-apple distance ({dist:F0}) exceeds recommended max ({RecommendedMaxNestToAppleDistance}). Workers may struggle to gather efficiently.");
            if (dist < MinNestToAppleDistance)
                Debug.LogWarning($"[MapDirector] {label} nest-to-apple distance ({dist:F0}) is very short. Units may clip into structures.");
        }

        IEnumerator Start()
        {
            // Guarantee the game isn't accidentally paused from a previous session
            // (static IsPaused can persist when domain reload is disabled).
            Time.timeScale = 1f;
            PauseController.ForceUnpause();

            GameSession.LoadPrefs();
            EnemyResources.Reset();
            BuildWorldPreview();

            var allFruit = FindObjectsByType<RottingFruitNode>(FindObjectsSortMode.None);
            var playerApple = FindNearestFruit(allFruit, _applePos);
            var enemyApple  = FindNearestFruit(allFruit, _enemyApplePos);

            // Spawn all units first — but DO NOT issue orders yet.
            // NavMeshAgent components need one full frame to bind to the baked NavMesh.
            var playerWorkers = new System.Collections.Generic.List<InsectUnit>();
            for (int i = 0; i < 5; i++)
            {
                float angle = i * (360f / 5f);
                var pos = SpawnPositionAroundBuilding(_playerHive, WorkerSpawnRadius, angle);
                var worker = SpawnUnit(pos, Team.Player, UnitArchetype.Worker);
                if (worker != null) playerWorkers.Add(worker);
            }

            var playerCombatArchs = new[] { UnitArchetype.BasicFighter, UnitArchetype.BasicFighter, UnitArchetype.BasicRanged, UnitArchetype.BasicRanged };
            for (int i = 0; i < playerCombatArchs.Length; i++)
            {
                float angle = i * (360f / playerCombatArchs.Length);
                SpawnUnit(SpawnPositionAroundBuilding(_playerHive, CombatSpawnRadius, angle), Team.Player, playerCombatArchs[i]);
            }

            var enemyWorkers = new System.Collections.Generic.List<InsectUnit>();
            var nWorkers = Mathf.RoundToInt(4f * GameSession.DifficultyEnemySpawnMultiplier);
            for (int i = 0; i < nWorkers; i++)
            {
                float angle = i * (360f / Mathf.Max(nWorkers, 1));
                var pos = SpawnPositionAroundBuilding(_enemyHive, WorkerSpawnRadius, angle);
                var worker = SpawnUnit(pos, Team.Enemy, UnitArchetype.Worker);
                if (worker != null) enemyWorkers.Add(worker);
            }

            var nCombat = Mathf.Clamp(Mathf.RoundToInt(3f * GameSession.DifficultyEnemySpawnMultiplier), 1, 8);
            var archCycle = new[] { UnitArchetype.BasicFighter, UnitArchetype.BasicFighter, UnitArchetype.BasicRanged };
            for (int i = 0; i < nCombat; i++)
            {
                float angle = i * (360f / nCombat);
                var arch = archCycle[Mathf.Min(i, archCycle.Length - 1)];
                SpawnUnit(SpawnPositionAroundBuilding(_enemyHive, CombatSpawnRadius, angle), Team.Enemy, arch);
            }

            SetStartingRallyPoints();
            RegisterBuildZones();

            var camCtrl = FindFirstObjectByType<RTSCameraController>();
            if (camCtrl != null)
                camCtrl.FocusWorldPosition(new Vector3(_playerHive.x, 0f, _playerHive.z));

            // Wait several frames so NavMeshAgent components can fully bind to the baked mesh.
            // Two frames covers Start() + first Update(); extra frames handle slower machines
            // or scenes where baking completes slightly after the first render tick.
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            yield return null;

            // Issue initial gather orders. Workers whose agents didn't bind yet will
            // still be set to Gather state and will start moving as soon as binding succeeds
            // (SafeSetDestination warps automatically; TickIdleAutoGather acts as fallback).
            foreach (var w in playerWorkers)
            {
                if (w == null) continue;
                if (playerApple != null && !playerApple.Depleted)
                    w.OrderGather(playerApple);
            }

            foreach (var w in enemyWorkers)
            {
                if (w == null) continue;
                if (enemyApple != null && !enemyApple.Depleted)
                    w.OrderGather(enemyApple);
            }
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

            SpawnBuildZoneRings();
        }

        void SpawnBuildZoneRings()
        {
            var world = GameObject.Find("WorldRoot");
            if (world == null) return;

            for (int i = 0; i < BuildZoneRegistry.Count; i++)
            {
                BuildZoneRegistry.GetZone(i, out var center, out var radius);
                var ring = new GameObject($"BuildZoneRing_{i}");
                ring.transform.SetParent(world.transform, false);

                float h = GetHeight(center);
                ring.transform.position = new Vector3(center.x, h + 0.06f, center.z);

                const int segments = 64;
                var lr = ring.AddComponent<LineRenderer>();
                lr.useWorldSpace = false;
                lr.loop = true;
                lr.positionCount = segments;
                lr.startWidth = 0.25f;
                lr.endWidth = 0.25f;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lr.receiveShadows = false;

                var sh = Shader.Find("Sprites/Default");
                if (sh == null) sh = Shader.Find("Universal Render Pipeline/Unlit");
                var mat = new Material(sh);
                var c = new Color(0.3f, 0.85f, 0.4f, 0.22f);
                if (mat.HasProperty("_Color")) mat.color = c;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
                lr.sharedMaterial = mat;

                for (int s = 0; s < segments; s++)
                {
                    float a = s * Mathf.PI * 2f / segments;
                    lr.SetPosition(s, new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
                }
            }
        }

        void SetStartingRallyPoints()
        {
            var allFruit = FindObjectsByType<RottingFruitNode>(FindObjectsSortMode.None);

            if (HiveDeposit.PlayerHive != null)
            {
                var apple = FindNearestFruit(allFruit, _applePos);
                if (apple != null)
                {
                    HiveDeposit.PlayerHive.SetRallyGather(_applePos, apple);
                    var prod = HiveDeposit.PlayerHive.GetComponent<ProductionBuilding>();
                    if (prod != null) prod.SetRallyGather(_applePos, apple);
                }
            }

            if (HiveDeposit.EnemyHive != null)
            {
                var apple = FindNearestFruit(allFruit, _enemyApplePos);
                if (apple != null)
                {
                    HiveDeposit.EnemyHive.SetRallyGather(_enemyApplePos, apple);
                    var prod = HiveDeposit.EnemyHive.GetComponent<ProductionBuilding>();
                    if (prod != null) prod.SetRallyGather(_enemyApplePos, apple);
                }
            }
        }

        static RottingFruitNode FindNearestFruit(RottingFruitNode[] fruits, Vector3 target)
        {
            RottingFruitNode best = null;
            float bestDist = float.MaxValue;
            foreach (var f in fruits)
            {
                if (f == null) continue;
                float d = (f.transform.position - target).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = f; }
            }
            return best;
        }

        public void BuildWorldPreview()
        {
            ApplyMapLayout();
            RtsSimRegistry.Clear();
            PlayArea.Configure(_mapHalfExtent, _mapHalfExtent);
            ActiveVisualLibrary = visualLibrary;

            EnsureLitShader();
            var world = GameObject.Find("WorldRoot");
            DestroyObject(world);

            world = new GameObject("WorldRoot");
            var surface = world.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.ignoreNavMeshObstacle = true;

            BuildTerrain(world.transform, _mapHalfExtent);
            AddMapBounds(world.transform, _mapHalfExtent, 0.45f);

            foreach (var c in _clayLayout)
                AddClay(world.transform, c.position, c.scale, _clayWallOverride);

            var exclusions = BuildExclusionZones();
            PassiveScatter.Scatter(world.transform, _mapHalfExtent, _scatterSeed, exclusions);

            // Force physics to register the terrain collider before baking.
            Physics.SyncTransforms();
            surface.BuildNavMesh();

            // Placed after NavMesh bake — these use NavMeshObstacle for dynamic carving
            BuildHive(world.transform, _playerHive, Team.Player, "PlayerHive");
            BuildHive(world.transform, _enemyHive, Team.Enemy, "EnemyHive");

            AddRottingApple(world.transform, _applePos);
            AddRottingApple(world.transform, _enemyApplePos);

            foreach (var f in _fruitLayout)
                AddFruit(world.transform, f);

            foreach (var tf in _terrainFeatureLayout)
                AddTerrainFeature(world.transform, tf);

            PlaceStarterPlayerBuildings(world.transform);

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                var ph = new Vector3(_playerHive.x, 0f, _playerHive.z);
                var eh = new Vector3(_enemyHive.x, 0f, _enemyHive.z);
                SpawnUnit(ph + Vector3.forward * WorkerSpawnRadius,  Team.Player, UnitArchetype.Worker).transform.SetParent(world.transform);
                SpawnUnit(ph + Vector3.right * CombatSpawnRadius,    Team.Player, UnitArchetype.BasicFighter).transform.SetParent(world.transform);
                SpawnUnit(ph + Vector3.left * CombatSpawnRadius,     Team.Player, UnitArchetype.BasicRanged).transform.SetParent(world.transform);
                SpawnUnit(eh + Vector3.back * CombatSpawnRadius,     Team.Enemy, UnitArchetype.BasicFighter).transform.SetParent(world.transform);
                SpawnUnit(eh + Vector3.forward * WorkerSpawnRadius,  Team.Enemy, UnitArchetype.Worker).transform.SetParent(world.transform);
            }
#endif

            var systems = GameObject.Find("Systems");
            DestroyObject(systems);
            systems = new GameObject("Systems");
            systems.AddComponent<PlayerResources>();
            systems.AddComponent<SelectionController>();
            systems.AddComponent<CommandController>();
            systems.AddComponent<GameHUD>();
            systems.AddComponent<BottomBar>();
            systems.AddComponent<Minimap>();
            systems.AddComponent<FogOfWarSystem>();
            systems.AddComponent<MatchDirector>();
            systems.AddComponent<EnemyCommander>();
            systems.AddComponent<PauseController>();
            systems.AddComponent<GameAudio>();
        }

        void PlaceStarterPlayerBuildings(Transform worldRoot) { }

        List<PassiveScatter.ExclusionZone> BuildExclusionZones()
        {
            var z = new List<PassiveScatter.ExclusionZone>
            {
                new(new Vector2(_playerHive.x, _playerHive.z), 28f),
                new(new Vector2(_enemyHive.x, _enemyHive.z), 28f),
                new(new Vector2(_applePos.x, _applePos.z), 12f),
                new(new Vector2(_enemyApplePos.x, _enemyApplePos.z), 12f)
            };
            foreach (var f in _fruitLayout)
                z.Add(new PassiveScatter.ExclusionZone(new Vector2(f.position.x, f.position.z), 8f));
            foreach (var c in _clayLayout)
            {
                z.Add(new PassiveScatter.ExclusionZone(new Vector2(c.position.x, c.position.z), Mathf.Max(c.scale.x, c.scale.z) + 4f));
            }
            return z;
        }

        static float GetHeight(Vector3 pos)
        {
            if (Terrain.activeTerrain != null) return Terrain.activeTerrain.SampleHeight(pos);
            return 0f;
        }

        static void SafeSetLayer(GameObject go, string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer >= 0) go.layer = layer;
        }

        static void SafeSetLayerRecursive(GameObject go, string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer < 0) return;
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = layer;
        }

        void BuildTerrain(Transform parent, float halfExtent)
        {
            float terrainSize = halfExtent * 2f;
            var terrainData = new TerrainData
            {
                heightmapResolution = 257,
                size = new Vector3(terrainSize, 20f, terrainSize)
            };

            var lib = ActiveVisualLibrary;
            if (lib != null && lib.baseSoilLayer != null)
            {
                var layers = lib.drySoilLayer != null
                    ? new[] { lib.baseSoilLayer, lib.drySoilLayer }
                    : new[] { lib.baseSoilLayer };
                terrainData.terrainLayers = layers;

                if (lib.drySoilLayer != null)
                {
                    int res = terrainData.alphamapResolution;
                    float[,,] maps = new float[res, res, 2];
                    for (int y = 0; y < res; y++)
                    for (int x = 0; x < res; x++)
                    {
                        float nx = (float)x / res;
                        float ny = (float)y / res;
                        float noise = Mathf.PerlinNoise(nx * 4f + 0.5f, ny * 4f + 0.5f);
                        float edge = Mathf.Min(nx, ny, 1f - nx, 1f - ny) * 4f;
                        edge = Mathf.Clamp01(edge);
                        float dryWeight = Mathf.Clamp01(noise * 0.4f + (1f - edge) * 0.3f);
                        maps[y, x, 0] = 1f - dryWeight;
                        maps[y, x, 1] = dryWeight;
                    }
                    terrainData.SetAlphamaps(0, 0, maps);
                }
            }

            var terrainGo = Terrain.CreateTerrainGameObject(terrainData);
            terrainGo.name = "Terrain";
            terrainGo.transform.SetParent(parent, false);
            terrainGo.transform.position = new Vector3(-halfExtent, 0f, -halfExtent);
            SafeSetLayer(terrainGo, "Ground");
        }

        void AddMapBounds(Transform parent, float halfExtent, float thickness) { }

        void AddClay(Transform parent, Vector3 pos, Vector3 scale, GameObject overridePrefab)
        {
            var clayPrefab = overridePrefab;
            if (clayPrefab == null && ActiveVisualLibrary != null)
                clayPrefab = ActiveVisualLibrary.clayWallPrefab;

            GameObject go = clayPrefab != null ? Instantiate(clayPrefab) : GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "ClayWall";
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            SafeSetLayer(go, "Environment");
        }

        void BuildHive(Transform parent, Vector3 pos, Team team, string name)
        {
            // Save existing hive refs — AddComponent<HiveDeposit> fires Awake+OnEnable with
            // the default team (Player) and can clobber whichever static ref was already set.
            // We restore the correct one after Configure() runs.
            var savedPlayerHive = HiveDeposit.PlayerHive;
            var savedEnemyHive  = HiveDeposit.EnemyHive;

            var lib = ActiveVisualLibrary;
            GameObject go;

            if (lib != null && lib.hivePrefab != null)
            {
                go = Instantiate(lib.hivePrefab);
                go.name = name;
                go.transform.localScale *= 2f; // Correctly double the existing scale

                var existingHive = go.GetComponent<HiveVisual>();
                if (existingHive != null) Destroy(existingHive);
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = name;
                go.transform.localScale = new Vector3(7f, 4f, 7f); // Original was 3.5, 2, 3.5
            }

            go.transform.SetParent(parent, false);
            float groundY = GetHeight(pos);
            go.transform.position = new Vector3(pos.x, groundY, pos.z);

            if (go.GetComponent<Collider>() == null)
            {
                var col = go.AddComponent<BoxCollider>();
                col.center = new Vector3(0f, 2f, 0f); // Adjust center for double height
                col.size = new Vector3(6f, 4f, 6f); // Original was 3, 2, 3
            }
            if (go.GetComponent<NavMeshObstacle>() == null)
            {
                var obs = go.AddComponent<NavMeshObstacle>();
                obs.carving = true;
                obs.shape = NavMeshObstacleShape.Box;
                obs.size = new Vector3(6f, 4f, 6f); // Original was 3, 2, 3
                obs.center = new Vector3(0f, 2f, 0f);
            }

            SitOnGround(go);

            var hive = go.AddComponent<HiveDeposit>();
            hive.Configure(team);

            // Restore whichever opposite-team ref was clobbered by the temporary default
            // Player-team registration that fires in AddComponent's Awake/OnEnable.
            if (team == Team.Player)
                HiveDeposit.RestoreEnemyHiveReference(savedEnemyHive);
            else
                HiveDeposit.RestorePlayerHiveReference(savedPlayerHive);

            var prod = go.AddComponent<ProductionBuilding>();
            prod.Initialize(BuildingType.AntNest, team, startActive: true);
        }

        static void SitOnGround(GameObject go)
        {
            var renderer = go.GetComponentInChildren<Renderer>();
            if (renderer == null) return;
            float bottomY = renderer.bounds.min.y;
            float groundY = GetHeight(go.transform.position);
            float offset = groundY - bottomY;
            if (Mathf.Abs(offset) > 0.01f)
                go.transform.position += new Vector3(0f, offset, 0f);
        }

        void AddRottingApple(Transform parent, Vector3 pos)
        {
            var lib = ActiveVisualLibrary;
            GameObject go;

            if (lib != null && lib.rottingApplePrefab != null)
            {
                go = Instantiate(lib.rottingApplePrefab);
                go.name = "RottingApple";
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = "RottingApple";
                go.transform.localScale = new Vector3(2f, 2f, 2f);
            }

            go.transform.SetParent(parent, false);
            float appleY = GetHeight(pos);
            go.transform.position = new Vector3(pos.x, appleY, pos.z);
            SitOnGround(go);
            if (go.GetComponentInChildren<Collider>() == null)
                go.AddComponent<SphereCollider>();
            SafeSetLayerRecursive(go, "Resources");
            var node = go.GetComponent<RottingFruitNode>();
            if (node == null) node = go.AddComponent<RottingFruitNode>();
            node.Configure(1000, 10, 5f);
        }

        void AddFruit(Transform parent, FruitPlaced f)
        {
            var lib = ActiveVisualLibrary;
            GameObject go;

            if (lib != null && lib.calorieChunkPrefab != null)
            {
                go = Instantiate(lib.calorieChunkPrefab);
                go.name = "Fruit";
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = "Fruit";
                go.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            }

            go.transform.SetParent(parent, false);
            go.transform.position = f.position;
            if (go.GetComponentInChildren<Collider>() == null)
                go.AddComponent<SphereCollider>();
            SafeSetLayerRecursive(go, "Resources");
            var node = go.GetComponent<RottingFruitNode>();
            if (node == null) node = go.AddComponent<RottingFruitNode>();
            node.Configure(f.calories, f.gatherPerTick, f.gatherSeconds);
        }

        void AddTerrainFeature(Transform parent, TerrainFeaturePlaced tf) { }

        public static InsectUnit SpawnUnit(Vector3 pos, Team team, UnitArchetype arch)
        {
            GameObject prefab = null;
            if (ActiveVisualLibrary != null)
                prefab = ActiveVisualLibrary.GetUnitPrefab(arch);

            GameObject go;
            InsectUnit unit;

            if (prefab != null)
            {
                // Instantiate at pos to ensure NavMeshAgent wakes up near a valid NavMesh.
                // We use Quaternion.identity to avoid any rotation issues.
                go = Instantiate(prefab, pos, Quaternion.identity);
                unit = go.GetComponent<InsectUnit>();
                if (unit == null) unit = go.AddComponent<InsectUnit>();
            }
            else
            {
                go = new GameObject(arch.ToString());
                go.transform.position = pos;
                unit = go.AddComponent<InsectUnit>();
            }
            
            unit.Configure(team, null, arch);
            unit.enabled = true; 
            
            var agent = go.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                // Ensure agent radius is compatible with default NavMesh baking settings (0.5 radius)
                if (agent.radius > 0.45f) agent.radius = 0.45f;
                
                agent.enabled = true;
                if (!agent.isOnNavMesh)
                {
                    // Warp is critical for NavMeshAgent to bind to the baked data.
                    if (!agent.Warp(pos))
                    {
                        // Fallback: search a wider area for a valid binding point.
                        if (NavMesh.SamplePosition(pos, out var hit, 20f, NavMesh.AllAreas))
                        {
                            agent.Warp(hit.position);
                        }
                    }
                }
            }

            // Put unit on "Units" layer so OverlapSphere combat scans can find it.
            SafeSetLayerRecursive(go, "Units");

            if (team == Team.Enemy && go.GetComponent<SimpleEnemyAi>() == null)
                go.AddComponent<SimpleEnemyAi>();
            return unit;
        }

        void EnsureLitShader()
        {
            if (s_lit == null) s_lit = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        }
    }
}
