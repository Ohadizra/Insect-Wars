using System.Collections.Generic;
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
        const float MapHalfExtent = 88f;
        const float MinimapOrtho = MapHalfExtent;

        [SerializeField] UnitVisualLibrary visualLibrary;

        /// <summary>Used by units for projectile settings when not using a prefab-only pipeline.</summary>
        public static UnitVisualLibrary ActiveVisualLibrary { get; private set; }

        static Material s_lit;

        void OnDestroy()
        {
            if (ActiveVisualLibrary == visualLibrary)
                ActiveVisualLibrary = null;
            SkirmishPlayArea.Clear();
        }

        void Start()
        {
            BuildWorldPreview();
            
            var playerStart = new Vector3(-54f, 0f, -44f);
            for (int i = 0; i < 5; i++)
            {
                var angle = i * 72f * Mathf.Deg2Rad;
                var offset = new Vector3(Mathf.Cos(angle) * 2.5f, 0f, Mathf.Sin(angle) * 2.5f);
                SpawnUnit(playerStart + offset, Team.Player, UnitArchetype.Worker);
            }
            SpawnUnit(playerStart + new Vector3(1f, 0, 1f), Team.Player, UnitArchetype.BasicFighter);
            SpawnUnit(playerStart + new Vector3(3.5f, 0, 0f), Team.Player, UnitArchetype.BasicFighter);
            SpawnUnit(playerStart + new Vector3(-1f, 0, -1.5f), Team.Player, UnitArchetype.BasicRanged);
            SpawnUnit(playerStart + new Vector3(2f, 0, -2f), Team.Player, UnitArchetype.BasicRanged);

            var enemyStart = new Vector3(62f, 0f, 52f);
            SpawnUnit(enemyStart + new Vector3(0f, 0, 1f), Team.Enemy, UnitArchetype.BasicFighter);
            SpawnUnit(enemyStart + new Vector3(2.5f, 0, 0f), Team.Enemy, UnitArchetype.BasicFighter);
            SpawnUnit(enemyStart + new Vector3(-2f, 0, 2f), Team.Enemy, UnitArchetype.BasicRanged);

            var camCtrl = FindFirstObjectByType<RTSCameraController>();
            if (camCtrl != null)
                camCtrl.FocusWorldPosition(new Vector3(-48f, 0f, -38f));
        }

        public void BuildWorldPreview()
        {
            SkirmishPlayArea.Configure(MapHalfExtent, MinimapOrtho);
            ActiveVisualLibrary = visualLibrary;

            EnsureLitShader();
            var world = GameObject.Find("WorldRoot");
            if (world != null) DestroyImmediate(world);
            
            world = new GameObject("WorldRoot");
            var surface = world.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
            surface.ignoreNavMeshObstacle = false;

            BuildTerrain(world.transform);
            AddMapBounds(world.transform, MapHalfExtent, 0.45f);

            AddClay(world.transform, new Vector3(-8f, 0f, 22f), new Vector3(6f, 2.2f, 2f));
            AddClay(world.transform, new Vector3(12f, 0f, -18f), new Vector3(3f, 2.8f, 9f));
            AddClay(world.transform, new Vector3(28f, 0f, 35f), new Vector3(4f, 2f, 5f));
            AddClay(world.transform, new Vector3(-35f, 0f, 8f), new Vector3(5f, 2.5f, 3f));
            AddClay(world.transform, new Vector3(5f, 0f, -42f), new Vector3(8f, 2f, 2.5f));
            AddClay(world.transform, new Vector3(-22f, 0f, -55f), new Vector3(3.5f, 2f, 7f));
            AddClay(world.transform, new Vector3(48f, 0f, -12f), new Vector3(2.5f, 3f, 6f));
            AddClay(world.transform, new Vector3(-48f, 0f, 48f), new Vector3(6f, 2f, 4f));

            GameObject hive;
            if (visualLibrary != null && visualLibrary.hivePrefab != null)
            {
                hive = Instantiate(visualLibrary.hivePrefab, world.transform);
                hive.name = "PlayerHive";
                if (!hive.CompareTag("Hive")) hive.tag = "Hive";
                hive.transform.position = new Vector3(-62f, 1f, -52f);
                if (hive.GetComponent<HiveDeposit>() == null) hive.AddComponent<HiveDeposit>();
                if (hive.GetComponent<HiveVisual>() == null) hive.AddComponent<HiveVisual>();
            }
            else
            {
                hive = GameObject.CreatePrimitive(PrimitiveType.Cube);
                hive.name = "PlayerHive";
                hive.tag = "Hive";
                hive.transform.SetParent(world.transform);
                hive.transform.position = new Vector3(-62f, 1f, -52f);
                hive.transform.localScale = new Vector3(4f, 2f, 4f);
                ApplyMat(hive, new Color(0.32f, 0.52f, 0.88f));
                var hiveMod = hive.AddComponent<NavMeshModifier>();
                hiveMod.ignoreFromBuild = true;
                hive.AddComponent<HiveDeposit>();
                hive.AddComponent<HiveVisual>();
            }

            AddRottingApple(world.transform, new Vector3(-50f, 1.5f, -42f));

            AddFruit(world.transform, new Vector3(-48f, 0.6f, -28f));
            AddFruit(world.transform, new Vector3(-72f, 0.6f, -15f));
            AddFruit(world.transform, new Vector3(-25f, 0.6f, 8f));
            AddFruit(world.transform, new Vector3(8f, 0.6f, 12f));
            AddFruit(world.transform, new Vector3(42f, 0.6f, 58f));
            AddFruit(world.transform, new Vector3(68f, 0.6f, 28f));
            AddFruit(world.transform, new Vector3(22f, 0.6f, -58f));
            AddFruit(world.transform, new Vector3(-15f, 0.6f, 62f));

            var exclusions = BuildExclusionZones();
            SkirmishPassiveScatter.Scatter(world.transform, MapHalfExtent, 18427, exclusions);

            surface.BuildNavMesh();

            var systems = GameObject.Find("Systems");
            if (systems != null) DestroyImmediate(systems);
            systems = new GameObject("Systems");
            systems.AddComponent<PlayerResources>();
            systems.AddComponent<SelectionController>();
            systems.AddComponent<CommandController>();
            systems.AddComponent<GameHUD>();
            systems.AddComponent<Sc2BottomBar>();
            systems.AddComponent<SkirmishMinimap>();
            systems.AddComponent<FogOfWarSystem>();
        }


        static List<SkirmishPassiveScatter.ExclusionZone> BuildExclusionZones()
        {
            var z = new List<SkirmishPassiveScatter.ExclusionZone>
            {
                new(new Vector2(-54f, -44f), 26f),
                new(new Vector2(62f, 52f), 26f),
                new(new Vector2(-62f, -52f), 18f),
                new(new Vector2(-50f, -42f), 12f)
            };
            void Fruit(float x, float fz) => z.Add(new SkirmishPassiveScatter.ExclusionZone(new Vector2(x, fz), 8f));
            Fruit(-48f, -28f);
            Fruit(-72f, -15f);
            Fruit(-25f, 8f);
            Fruit(8f, 12f);
            Fruit(42f, 58f);
            Fruit(68f, 28f);
            Fruit(22f, -58f);
            Fruit(-15f, 62f);
            void Clay(float x, float fz, float spread) => z.Add(new SkirmishPassiveScatter.ExclusionZone(new Vector2(x, fz), spread));
            Clay(-8f, 22f, 9f);
            Clay(12f, -18f, 11f);
            Clay(28f, 35f, 8f);
            Clay(-35f, 8f, 9f);
            Clay(5f, -42f, 12f);
            Clay(-22f, -55f, 10f);
            Clay(48f, -12f, 9f);
            Clay(-48f, 48f, 11f);
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

        static void ApplyMat(GameObject go, Color c)
        {
            EnsureLitShader();
            var m = new Material(s_lit);
            if (m.HasProperty("_BaseColor"))
                m.SetColor("_BaseColor", c);
            else if (m.HasProperty("_Color"))
                m.SetColor("_Color", c);
            var r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = m;
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

        static void BuildWorkerVisual(Transform root, Color body)
        {
            AddPrimitivePart(root, PrimitiveType.Cylinder, new Vector3(0f, 0.28f, 0f), new Vector3(0.52f, 0.24f, 0.52f),
                Quaternion.identity, body);
            AddPrimitivePart(root, PrimitiveType.Sphere, new Vector3(0f, 0.58f, 0f), Vector3.one * 0.3f,
                Quaternion.identity, Color.Lerp(body, Color.white, 0.12f));
        }

        static void BuildFighterVisual(Transform root, Color body)
        {
            AddPrimitivePart(root, PrimitiveType.Capsule, new Vector3(0f, 0.22f, 0f), new Vector3(1.05f, 0.36f, 0.52f),
                Quaternion.Euler(0f, 0f, 90f), body);
            AddPrimitivePart(root, PrimitiveType.Sphere, new Vector3(-0.38f, 0.16f, 0.12f), Vector3.one * 0.2f,
                Quaternion.identity, Color.Lerp(body, Color.black, 0.15f));
            AddPrimitivePart(root, PrimitiveType.Sphere, new Vector3(0.38f, 0.16f, 0.12f), Vector3.one * 0.2f,
                Quaternion.identity, Color.Lerp(body, Color.black, 0.15f));
            AddPrimitivePart(root, PrimitiveType.Sphere, new Vector3(0f, 0.18f, 0.48f), Vector3.one * 0.17f,
                Quaternion.identity, Color.Lerp(body, Color.black, 0.08f));
        }

        static void BuildRangedVisual(Transform root, Color body, Team team)
        {
            AddPrimitivePart(root, PrimitiveType.Capsule, new Vector3(0f, 0.52f, 0f), new Vector3(0.42f, 0.5f, 0.42f),
                Quaternion.identity, body);
            AddPrimitivePart(root, PrimitiveType.Sphere, new Vector3(0f, 1.02f, 0f), Vector3.one * 0.3f,
                Quaternion.identity, Color.Lerp(body, Color.white, 0.1f));
            AddPrimitivePart(root, PrimitiveType.Cube, new Vector3(0.2f, 0.62f, 0.38f), new Vector3(0.1f, 0.08f, 0.48f),
                Quaternion.identity, TeamPalette.WeaponAccent(team));
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

        void BuildTerrain(Transform parent)
        {
            float size = MapHalfExtent * 2f;
            int resolution = 128; // heightmap res
            
            TerrainData terrainData = new TerrainData();
            terrainData.heightmapResolution = resolution + 1;
            terrainData.size = new Vector3(size, 20f, size);
            terrainData.alphamapResolution = 128;

            // Setup Layers
            if (visualLibrary != null && visualLibrary.baseSoilLayer != null && visualLibrary.drySoilLayer != null)
            {
                terrainData.terrainLayers = new[] { visualLibrary.baseSoilLayer, visualLibrary.drySoilLayer };
            }

            // Generate Heightmap (Plateaus with sharper edges)
            float[,] heights = new float[resolution + 1, resolution + 1];
            Vector2[] highGrounds = { new Vector2(0.25f, 0.25f), new Vector2(0.75f, 0.75f), new Vector2(0.5f, 0.5f) };
            float highLevel = 0.15f; // 15% of 20f = 3m high
            
            for (int x = 0; x <= resolution; x++)
            {
                for (int y = 0; y <= resolution; y++)
                {
                    float u = (float)x / resolution;
                    float v = (float)y / resolution;
                    float maxH = 0f;
                    foreach (var hg in highGrounds)
                    {
                        float dist = Vector2.Distance(new Vector2(u, v), hg);
                        if (dist < 0.13f) maxH = highLevel;
                        else if (dist < 0.15f) maxH = Mathf.Lerp(highLevel, 0f, (dist - 0.13f) / 0.02f); // sharp ramp
                    }
                    heights[y, x] = maxH;
                }
            }
            terrainData.SetHeights(0, 0, heights);

            // Generate Alphamap (DrySoil on High Ground, BaseSoil on Low Ground + Noise)
            float[,,] alpha = new float[128, 128, 2];
            for (int x = 0; x < 128; x++)
            {
                for (int y = 0; y < 128; y++)
                {
                    float u = (float)x / 128f;
                    float v = (float)y / 128f;
                    
                    float h = 0f;
                    foreach (var hg in highGrounds)
                    {
                        float dist = Vector2.Distance(new Vector2(u, v), hg);
                        if (dist < 0.13f) h = 1f;
                        else if (dist < 0.15f) h = Mathf.InverseLerp(0.15f, 0.13f, dist);
                    }
                    
                    float noise = Mathf.PerlinNoise(x * 0.1f, y * 0.1f);
                    float baseWeight = Mathf.Clamp01(1f - h + (noise - 0.5f) * 0.3f);
                    
                    alpha[y, x, 0] = baseWeight;     // BaseSoil
                    alpha[y, x, 1] = 1f - baseWeight; // DrySoil
                }
            }
            terrainData.SetAlphamaps(0, 0, alpha);

            GameObject terrainObj = Terrain.CreateTerrainGameObject(terrainData);
            terrainObj.name = "Ground";
            terrainObj.transform.SetParent(parent);
            terrainObj.transform.position = new Vector3(-MapHalfExtent, 0f, -MapHalfExtent);
            
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
            var apple = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            apple.name = "RottingApple";
            apple.tag = "Fruit";
            int layer = LayerMask.NameToLayer("Resources");
            if (layer >= 0) apple.layer = layer;
            apple.transform.SetParent(parent);
            float h = GetHeight(pos);
            apple.transform.position = new Vector3(pos.x, h + pos.y, pos.z);
            apple.transform.localScale = new Vector3(4f, 3f, 4f);
            EnsureLitShader();
            var m = new Material(s_lit);
            if (m.HasProperty("_BaseColor"))
                m.SetColor("_BaseColor", new Color(0.85f, 0.68f, 0.15f));
            else if (m.HasProperty("_Color"))
                m.SetColor("_Color", new Color(0.85f, 0.68f, 0.15f));
            if (m.HasProperty("_Smoothness"))
                m.SetFloat("_Smoothness", 0.15f);
apple.GetComponent<Renderer>().sharedMaterial = m;
            var modifier = apple.AddComponent<NavMeshModifier>();
            modifier.ignoreFromBuild = true;
            apple.AddComponent<RottingFruitNode>();
        }

        static void AddFruit(Transform parent, Vector3 pos)
        {
            var fruit = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fruit.name = "RottingFruit";
            fruit.tag = "Fruit";
            int layer = LayerMask.NameToLayer("Resources");
            if (layer >= 0) fruit.layer = layer;
            fruit.transform.SetParent(parent);
            float h = GetHeight(pos);
            fruit.transform.position = new Vector3(pos.x, h + pos.y, pos.z);
            fruit.transform.localScale = Vector3.one * 1.8f;
            ApplyMat(fruit, new Color(0.65f, 0.2f, 0.55f));
            var modifier = fruit.AddComponent<NavMeshModifier>();
            modifier.ignoreFromBuild = true;
            var node = fruit.AddComponent<RottingFruitNode>();
            node.Configure(10000, 10, 5f);
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
                Color body = TeamPalette.UnitBody(team, arch);
                var def = UnitDefinition.CreateRuntimeDefault(arch, body);
                unit.Configure(team, def);
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
                    BuildWorkerVisual(visualRoot.transform, body2);
                    break;
                case UnitArchetype.BasicFighter:
                    BuildFighterVisual(visualRoot.transform, body2);
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
