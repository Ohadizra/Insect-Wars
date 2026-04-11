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

        const float WorkerSpawnRadius = 5f;
        const float CombatSpawnRadius = 10f;
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
                Debug.LogWarning($"[SkirmishDirector] {label} nest-to-apple distance ({dist:F0}) exceeds recommended max ({RecommendedMaxNestToAppleDistance}). Workers may struggle to gather efficiently.");
            if (dist < MinNestToAppleDistance)
                Debug.LogWarning($"[SkirmishDirector] {label} nest-to-apple distance ({dist:F0}) is very short. Units may clip into structures.");
        }

        void Start()
        {
            GameSession.LoadPrefs();
            EnemyResources.Reset();
            BuildWorldPreview();

            var allFruit = FindObjectsByType<RottingFruitNode>(FindObjectsSortMode.None);
            var playerApple = FindNearestFruit(allFruit, _applePos);
            var enemyApple  = FindNearestFruit(allFruit, _enemyApplePos);

            for (int i = 0; i < 5; i++)
            {
                float angle = i * (360f / 5f);
                var pos = SpawnPositionAroundBuilding(_playerHive, WorkerSpawnRadius, angle);
                var worker = SpawnUnit(pos, Team.Player, UnitArchetype.Worker);
                if (worker != null && playerApple != null && !playerApple.Depleted)
                    worker.OrderGather(playerApple);
            }

            var playerCombatArchs = new[] { UnitArchetype.BasicFighter, UnitArchetype.BasicFighter, UnitArchetype.BasicRanged, UnitArchetype.BasicRanged };
            for (int i = 0; i < playerCombatArchs.Length; i++)
            {
                float angle = i * (360f / playerCombatArchs.Length);
                SpawnUnit(SpawnPositionAroundBuilding(_playerHive, CombatSpawnRadius, angle), Team.Player, playerCombatArchs[i]);
            }

            var nWorkers = Mathf.RoundToInt(4f * GameSession.DifficultyEnemySpawnMultiplier);
            for (int i = 0; i < nWorkers; i++)
            {
                float angle = i * (360f / Mathf.Max(nWorkers, 1));
                var pos = SpawnPositionAroundBuilding(_enemyHive, WorkerSpawnRadius, angle);
                var worker = SpawnUnit(pos, Team.Enemy, UnitArchetype.Worker);
                if (worker != null && enemyApple != null && !enemyApple.Depleted)
                    worker.OrderGather(enemyApple);
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
                AddClay(world.transform, c.position, c.scale, _clayWallOverride);

            BuildHive(world.transform, _playerHive, Team.Player, "PlayerHive");
            BuildHive(world.transform, _enemyHive, Team.Enemy, "EnemyHive");

            AddRottingApple(world.transform, _applePos);
            AddRottingApple(world.transform, _enemyApplePos);

            foreach (var f in _fruitLayout)
                AddFruit(world.transform, f);

            foreach (var tf in _terrainFeatureLayout)
                AddTerrainFeature(world.transform, tf);

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
            systems.AddComponent<BottomBar>();
            systems.AddComponent<SkirmishMinimap>();
            systems.AddComponent<FogOfWarSystem>();
            systems.AddComponent<MatchDirector>();
            systems.AddComponent<EnemyCommander>();
            systems.AddComponent<PauseController>();
            systems.AddComponent<GameAudio>();
        }

        void PlaceStarterPlayerBuildings(Transform worldRoot) { }

        List<SkirmishPassiveScatter.ExclusionZone> BuildExclusionZones()
        {
            var z = new List<SkirmishPassiveScatter.ExclusionZone>
            {
                new(new Vector2(_playerHive.x, _playerHive.z), 28f),
                new(new Vector2(_enemyHive.x, _enemyHive.z), 28f),
                new(new Vector2(_applePos.x, _applePos.z), 12f),
                new(new Vector2(_enemyApplePos.x, _enemyApplePos.z), 12f)
            };
            foreach (var f in _fruitLayout)
                z.Add(new SkirmishPassiveScatter.ExclusionZone(new Vector2(f.position.x, f.position.z), 8f));
            foreach (var c in _clayLayout)
            {
                var rect = new Rect(c.position.x - c.scale.x * 0.5f, c.position.z - c.scale.z * 0.5f, c.scale.x, c.scale.z);
                z.Add(new SkirmishPassiveScatter.ExclusionZone(new Vector2(c.position.x, c.position.z), Mathf.Max(c.scale.x, c.scale.z) + 4f));
            }
            return z;
        }

        static float GetHeight(Vector3 pos)
        {
            if (Terrain.activeTerrain != null) return Terrain.activeTerrain.SampleHeight(pos);
            return 0f;
        }

        void BuildTerrain(Transform parent, float halfExtent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
            go.name = "GroundPlane";
            go.transform.SetParent(parent, false);
            go.transform.localScale = new Vector3(halfExtent * 0.2f, 1f, halfExtent * 0.2f);
            go.transform.position = Vector3.zero;
            go.layer = LayerMask.NameToLayer("Ground");
        }

        void AddMapBounds(Transform parent, float halfExtent, float thickness) { }

        void AddClay(Transform parent, Vector3 pos, Vector3 scale, GameObject overridePrefab)
        {
            GameObject go = overridePrefab != null ? Instantiate(overridePrefab) : GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "ClayWall";
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.layer = LayerMask.NameToLayer("Environment");
        }

        void BuildHive(Transform parent, Vector3 pos, Team team, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var hive = go.AddComponent<HiveDeposit>();
            hive.Init(team);
            var prod = go.AddComponent<ProductionBuilding>();
            prod.Init(team, BuildingType.AntNest);
        }

        void AddRottingApple(Transform parent, Vector3 pos)
        {
            var go = new GameObject("RottingApple");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var node = go.AddComponent<RottingFruitNode>();
            node.Init(1000);
        }

        void AddFruit(Transform parent, FruitPlaced f)
        {
            var go = new GameObject("Fruit");
            go.transform.SetParent(parent, false);
            go.transform.position = f.position;
            var node = go.AddComponent<RottingFruitNode>();
            node.Init(500);
        }

        void AddTerrainFeature(Transform parent, TerrainFeaturePlaced tf) { }

        InsectUnit SpawnUnit(Vector3 pos, Team team, UnitArchetype arch)
        {
            var go = new GameObject(arch.ToString());
            go.transform.position = pos;
            var unit = go.AddComponent<InsectUnit>();
            unit.Init(team, arch);
            if (team == Team.Enemy) go.AddComponent<SimpleEnemyAi>();
            return unit;
        }

        void EnsureLitShader()
        {
            if (s_lit == null) s_lit = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        }
    }
}
