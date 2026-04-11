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

        /// <summary>
        /// Compute a spawn position at a given angle and radius around a building center,
        /// snapped to the NavMesh for walkability. Uses terrain height so sampling works
        /// correctly on elevated plateaus.
        /// </summary>
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

            // ── Player starting workers — close ring around the hive ──
            for (int i = 0; i < 5; i++)
            {
                float angle = i * (360f / 5f);
                var pos = SpawnPositionAroundBuilding(_playerHive, WorkerSpawnRadius, angle);
                var worker = SpawnUnit(pos, Team.Player, UnitArchetype.Worker);
                if (worker != null && playerApple != null && !playerApple.Depleted)
                    worker.OrderGather(playerApple);
            }

            // ── Player starting combat — outer ring, away from the nest ──
            var playerCombatArchs = new[] { UnitArchetype.BasicFighter, UnitArchetype.BasicFighter, UnitArchetype.BasicRanged, UnitArchetype.BasicRanged };
            for (int i = 0; i < playerCombatArchs.Length; i++)
            {
                float angle = i * (360f / playerCombatArchs.Length);
                SpawnUnit(SpawnPositionAroundBuilding(_playerHive, CombatSpawnRadius, angle), Team.Player, playerCombatArchs[i]);
            }

            // ── Enemy starting workers — close ring around the hive ──
            var nWorkers = Mathf.RoundToInt(4f * GameSession.DifficultyEnemySpawnMultiplier);
            for (int i = 0; i < nWorkers; i++)
            {
                float angle = i * (360f / Mathf.Max(nWorkers, 1));
                var pos = SpawnPositionAroundBuilding(_enemyHive, WorkerSpawnRadius, angle);
                var worker = SpawnUnit(pos, Team.Enemy, UnitArchetype.Worker);
                if (worker != null && enemyApple != null && !enemyApple.Depleted)
                    worker.OrderGather(enemyApple);
            }

            // ── Enemy starting combat — outer ring, away from the nest ──
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
                new(new Vector2(_playerHive.x, _playerHive.z), 28f),
                new(new Vector2(_enemyHive.x, _enemyHive.z), 28f),
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

        static void SitOnGround(GameObject go)
        {
            var renderer = go.GetComponentInChildren<Renderer>();
            if (renderer == null) return;
            float bottomY = renderer.bounds.min.y;
            float groundY = GetHeight(go.transform.position);
            float offset = groundY - bottomY;
            if (offset > 0.01f || offset < -0.01f)
                go.transform.position += new Vector3(0f, offset, 0f);
        }

        static void AddClay(Transform parent, Vector3 pos, Vector3 scale, GameObject prefabOverride = null)
        {
            GameObject clay;
            var lib = ActiveVisualLibrary;
            float h = GetHeight(pos);
            Vector3 finalPos = new Vector3(pos.x, h + (scale.y * 0.5f), pos.z);

            var prefab = prefabOverride != null ? prefabOverride
                       : (lib != null ? lib.clayWallPrefab : null);

            if (prefab != null)
            {
                clay = Object.Instantiate(prefab, parent);
                clay.name = "Clay";
                clay.tag = "Clay";
                clay.transform.position = finalPos;
                clay.transform.localScale = scale;
            }
            else
            {
                clay = GameObject.CreatePrimitive(PrimitiveType.Cube);
                clay.name = "Clay";
                clay.tag = "Clay";
                clay.transform.SetParent(parent);
                clay.transform.position = finalPos;
                clay.transform.localScale = scale;
                ApplyMat(clay, new Color(0.45f, 0.32f, 0.22f));
            }

            var obs = clay.GetComponent<NavMeshObstacle>();
            if (obs == null) obs = clay.AddComponent<NavMeshObstacle>();
            obs.carving = true;
            obs.shape = NavMeshObstacleShape.Box;
            obs.size = Vector3.one;
            obs.center = Vector3.zero;
        }

        void BuildHive(Transform parent, Vector3 worldPos, Team team, string name)
        {
            float h = GetHeight(worldPos);
            var placedPos = new Vector3(worldPos.x, h + worldPos.y, worldPos.z);

            const float hiveScale = 3.5f;

            GameObject hive;
            if (visualLibrary != null && visualLibrary.hivePrefab != null)
            {
                hive = Instantiate(visualLibrary.hivePrefab, parent);
                hive.name = name;
                if (!hive.CompareTag("Hive")) hive.tag = "Hive";
                hive.transform.position = placedPos;
                hive.transform.localScale *= hiveScale;
                var deposit = hive.GetComponent<HiveDeposit>();
                if (deposit == null) deposit = hive.AddComponent<HiveDeposit>();
                deposit.Configure(team);
                if (hive.GetComponent<HiveVisual>() == null) hive.AddComponent<HiveVisual>();

                if (hive.GetComponent<Collider>() == null)
                {
                    var col = hive.AddComponent<BoxCollider>();
                    col.isTrigger = true;
                    col.center = new Vector3(0f, 1.5f, 0f);
                    col.size = new Vector3(5f, 3f, 5f);
                }

                if (visualLibrary.hiveMaterial != null)
                {
                    foreach (var renderer in hive.GetComponentsInChildren<Renderer>(true))
                        renderer.sharedMaterial = visualLibrary.hiveMaterial;
                }

                var skinColor = TeamPalette.GetShellColor(team);
                var block = new MaterialPropertyBlock();
                foreach (var renderer in hive.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.GetPropertyBlock(block);
                    if (renderer.sharedMaterial != null)
                    {
                        if (renderer.sharedMaterial.HasProperty("_BaseColor"))
                            block.SetColor("_BaseColor", skinColor);
                        else if (renderer.sharedMaterial.HasProperty("_Color"))
                            block.SetColor("_Color", skinColor);
                    }
                    renderer.SetPropertyBlock(block);
                }

                var prod = hive.AddComponent<ProductionBuilding>();
                prod.Initialize(BuildingType.AntNest, team);

                SitOnGround(hive);
            }
            else
            {
                hive = GameObject.CreatePrimitive(PrimitiveType.Cube);
                hive.name = name;
                hive.tag = "Hive";
                hive.transform.SetParent(parent);
                hive.transform.position = placedPos;
                hive.transform.localScale = new Vector3(4f, 2f, 4f) * hiveScale;
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
                        float dist;
                        float baseRad;
                        if (hg.radius > 0.001f)
                        {
                            dist = Vector2.Distance(new Vector2(u, v), hg.uv);
                            baseRad = hg.radius;
                        }
                        else
                        {
                            // Rotated box SDF
                            Vector2 uvRel = new Vector2(u, v) - hg.uv;
                            float rad = hg.rotation * Mathf.Deg2Rad;
                            float cos = Mathf.Cos(rad);
                            float sin = Mathf.Sin(rad);
                            Vector2 local = new Vector2(uvRel.x * cos + uvRel.y * sin, -uvRel.x * sin + uvRel.y * cos);
                            Vector2 d = new Vector2(Mathf.Abs(local.x) - hg.boxSize.x * 0.5f, Mathf.Abs(local.y) - hg.boxSize.y * 0.5f);
                            dist = Mathf.Min(Mathf.Max(d.x, d.y), 0f) + Vector2.Max(d, Vector2.zero).magnitude;
                            baseRad = 0f; // Inside box dist <= 0
                        }

                        float rw = Mathf.Max(hg.rampWidth, 0.01f);
                        if (dist <= baseRad) maxH = Mathf.Max(maxH, hg.heightFraction);
                        else if (dist < baseRad + rw) maxH = Mathf.Max(maxH, Mathf.Lerp(hg.heightFraction, 0f, (dist - baseRad) / rw));
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
                        float dist;
                        float baseRad;
                        if (hg.radius > 0.001f)
                        {
                            dist = Vector2.Distance(new Vector2(u, v), hg.uv);
                            baseRad = hg.radius;
                        }
                        else
                        {
                            Vector2 uvRel = new Vector2(u, v) - hg.uv;
                            float rad = hg.rotation * Mathf.Deg2Rad;
                            float cos = Mathf.Cos(rad);
                            float sin = Mathf.Sin(rad);
                            Vector2 local = new Vector2(uvRel.x * cos + uvRel.y * sin, -uvRel.x * sin + uvRel.y * cos);
                            Vector2 d = new Vector2(Mathf.Abs(local.x) - hg.boxSize.x * 0.5f, Mathf.Abs(local.y) - hg.boxSize.y * 0.5f);
                            dist = Mathf.Min(Mathf.Max(d.x, d.y), 0f) + Vector2.Max(d, Vector2.zero).magnitude;
                            baseRad = 0f;
                        }

                        float rw = Mathf.Max(hg.rampWidth, 0.01f);
                        if (dist <= baseRad) h = 1f;
                        else if (dist < baseRad + rw) h = Mathf.Max(h, Mathf.InverseLerp(baseRad + rw, baseRad, dist));
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

            if (lib != null && lib.rottingApplePrefab != null)
            {
                apple = Object.Instantiate(lib.rottingApplePrefab, parent);
                apple.name = "RottingApple";
                apple.tag = "Fruit";
                int layer = LayerMask.NameToLayer("Resources");
                if (layer >= 0) apple.layer = layer;
                apple.transform.position = new Vector3(pos.x, h, pos.z);
                apple.transform.localScale = new Vector3(4f, appleHeight, 4f);
                SitOnGround(apple);
            }
            else
            {
                apple = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                apple.name = "RottingApple";
                apple.tag = "Fruit";
                int layer = LayerMask.NameToLayer("Resources");
                if (layer >= 0) apple.layer = layer;
                apple.transform.SetParent(parent);
                apple.transform.position = new Vector3(pos.x, h, pos.z);
                apple.transform.localScale = new Vector3(4f, appleHeight, 4f);
                SitOnGround(apple);
                
                var r = apple.GetComponent<Renderer>();
                if (r != null)
                {
                    if (lib != null && lib.bigAppleMaterial != null)
                        r.sharedMaterial = lib.bigAppleMaterial;
                    else
                    {
                        var col = new Color(0.85f, 0.68f, 0.15f);
                        r.sharedMaterial = GetSharedTinted(col);
                    }
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
                obs.size = b.size * 0.85f;
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
            if (NavMesh.SamplePosition(finalPos, out var navHit, 8f, NavMesh.AllAreas))
                finalPos = navHit.position;

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
