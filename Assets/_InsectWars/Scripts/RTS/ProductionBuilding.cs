using System.Collections.Generic;
using InsectWars.Data;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

namespace InsectWars.RTS
{
    public enum BuildingType
    {
        Underground,
        AntNest,
        SkyTower,
        RootCellar
    }

    public class ProductionBuilding : MonoBehaviour
    {
        static readonly List<ProductionBuilding> s_all = new();
        public static IReadOnlyList<ProductionBuilding> All => s_all;

        struct QueueEntry
        {
            public UnitArchetype archetype;
            public float buildTime;
            public float elapsed;
        }

        const int MaxQueueSize = 5;
        readonly List<QueueEntry> _queue = new();

        BuildingType _type;
        Team _team = Team.Player;
        Vector3? _rallyPoint;
        RottingFruitNode _rallyGatherTarget;
        GameObject _rallyFlag;

        public BuildingType Type => _type;
        public Team Team => _team;
        public Vector3? RallyPoint => _rallyPoint;
        public RottingFruitNode RallyGatherTarget => _rallyGatherTarget;
        public bool IsProducing => _queue.Count > 0;
        public int QueueCount => _queue.Count;
        public float ProductionProgress => _queue.Count > 0 ? Mathf.Clamp01(_queue[0].elapsed / _queue[0].buildTime) : 0f;
        public UnitArchetype? CurrentProducing => _queue.Count > 0 ? _queue[0].archetype : null;

        public string DisplayName => _type switch
        {
            BuildingType.Underground => "Underground",
            BuildingType.AntNest => "Ant's\nNest",
            BuildingType.SkyTower => "Sky Tower",
            BuildingType.RootCellar => "Root\nCellar",
            _ => _type.ToString()
        };

        public UnitArchetype[] ProducibleUnits => _type switch
        {
            BuildingType.Underground => new[] { UnitArchetype.BasicFighter, UnitArchetype.BasicRanged },
            BuildingType.AntNest => new[] { UnitArchetype.Worker },
            BuildingType.SkyTower => System.Array.Empty<UnitArchetype>(),
            BuildingType.RootCellar => System.Array.Empty<UnitArchetype>(),
            _ => new[] { UnitArchetype.Worker }
        };

        public static int GetUnitCost(UnitArchetype arch) => arch switch
        {
            UnitArchetype.BasicFighter => 100,
            UnitArchetype.BasicRanged => 100,
            UnitArchetype.Worker => 50,
            _ => 50
        };

        public static string GetUnitName(UnitArchetype arch) => arch switch
        {
            UnitArchetype.BasicFighter => "Mantis",
            UnitArchetype.BasicRanged => "Beetle",
            UnitArchetype.Worker => "Worker",
            _ => "Unit"
        };

        public static float GetBuildTime(UnitArchetype arch) => arch switch
        {
            UnitArchetype.Worker => 10f,
            UnitArchetype.BasicFighter => 18f,
            UnitArchetype.BasicRanged => 22f,
            _ => 15f
        };

        public static int GetBuildCost(BuildingType type) => type switch
        {
            BuildingType.Underground => 200,
            BuildingType.AntNest => 400,
            BuildingType.SkyTower => 300,
            BuildingType.RootCellar => 150,
            _ => 100
        };

        void OnDestroy()
        {
            s_all.Remove(this);
            if (_rallyFlag != null) Destroy(_rallyFlag);
        }

        public void Initialize(BuildingType type, Team team = Team.Player)
        {
            _type = type;
            _team = team;
            s_all.Add(this);
        }

        void Update()
        {
            if (_queue.Count == 0) return;
            var entry = _queue[0];
            entry.elapsed += Time.deltaTime;
            _queue[0] = entry;
            if (entry.elapsed >= entry.buildTime)
            {
                _queue.RemoveAt(0);
                SpawnFinishedUnit(entry.archetype);
            }
        }

        public bool QueueUnit(UnitArchetype archetype)
        {
            if (_queue.Count >= MaxQueueSize) return false;

            int cost = GetUnitCost(archetype);
            if (_team == Team.Player && PlayerResources.Instance != null && !PlayerResources.Instance.TrySpend(cost))
                return false;
            if (_team == Team.Enemy && !EnemyResources.TrySpend(cost))
                return false;

            _queue.Add(new QueueEntry
            {
                archetype = archetype,
                buildTime = GetBuildTime(archetype),
                elapsed = 0f
            });
            return true;
        }

        public void CancelLast()
        {
            if (_queue.Count == 0) return;
            var last = _queue[_queue.Count - 1];
            _queue.RemoveAt(_queue.Count - 1);
            int refund = GetUnitCost(last.archetype);
            if (_team == Team.Player && PlayerResources.Instance != null)
                PlayerResources.Instance.AddCalories(refund);
            else if (_team == Team.Enemy)
                EnemyResources.AddCalories(refund);
        }

        public int QueuedCountOf(UnitArchetype arch)
        {
            int count = 0;
            foreach (var e in _queue)
                if (e.archetype == arch) count++;
            return count;
        }

        void SpawnFinishedUnit(UnitArchetype archetype)
        {
            var center = transform.position;
            var extent = transform.localScale.x * 0.5f + 1.5f;

            Vector3 spawnDir;
            if (_rallyPoint.HasValue)
            {
                spawnDir = _rallyPoint.Value - center;
                spawnDir.y = 0f;
            }
            else
            {
                spawnDir = transform.forward;
                spawnDir.y = 0f;
            }
            if (spawnDir.sqrMagnitude < 0.01f) spawnDir = Vector3.forward;

            float baseAngle = Mathf.Atan2(spawnDir.z, spawnDir.x);
            var angle = baseAngle + Random.Range(-25f, 25f) * Mathf.Deg2Rad;
            var offset = new Vector3(Mathf.Cos(angle) * extent, 0f, Mathf.Sin(angle) * extent);
            var spawnPos = center + offset;
            if (NavMesh.SamplePosition(spawnPos, out var hit, 4f, NavMesh.AllAreas))
                spawnPos = hit.position;

            var unit = SkirmishDirector.SpawnUnit(spawnPos, _team, archetype);
            if (unit == null) return;

            if (_rallyGatherTarget != null && !_rallyGatherTarget.Depleted &&
                unit.Definition != null && unit.Definition.canGather)
                unit.OrderGather(_rallyGatherTarget);
            else if (_rallyPoint.HasValue)
                unit.OrderMove(_rallyPoint.Value);
        }

        public void SetRallyPoint(Vector3 pos)
        {
            _rallyPoint = pos;
            _rallyGatherTarget = null;
            SyncRallyFlag();
        }

        public void SetRallyGather(Vector3 pos, RottingFruitNode node)
        {
            _rallyPoint = pos;
            _rallyGatherTarget = node;
            SyncRallyFlag();
        }

        public void ClearRally()
        {
            _rallyPoint = null;
            _rallyGatherTarget = null;
            SyncRallyFlag();
        }

        void SyncRallyFlag()
        {
            if (_rallyPoint == null)
            {
                if (_rallyFlag != null) _rallyFlag.SetActive(false);
                return;
            }

            if (_rallyFlag == null)
            {
                _rallyFlag = new GameObject("RallyFlag");

                var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pole.name = "Pole";
                pole.transform.SetParent(_rallyFlag.transform, false);
                pole.transform.localPosition = new Vector3(0f, 0.6f, 0f);
                pole.transform.localScale = new Vector3(0.08f, 0.6f, 0.08f);
                Destroy(pole.GetComponent<Collider>());
                ApplyMat(pole, new Color(0.9f, 0.9f, 0.7f));

                var banner = GameObject.CreatePrimitive(PrimitiveType.Cube);
                banner.name = "Banner";
                banner.transform.SetParent(_rallyFlag.transform, false);
                banner.transform.localPosition = new Vector3(0.2f, 1.05f, 0f);
                banner.transform.localScale = new Vector3(0.35f, 0.22f, 0.05f);
                Destroy(banner.GetComponent<Collider>());
                ApplyMat(banner, TeamPalette.GetTeamColor(_team)); // Team color banner
            }

            _rallyFlag.SetActive(true);
            _rallyFlag.transform.position = _rallyPoint.Value;
        }

        public static ProductionBuilding Place(Vector3 position, BuildingType type, Team team = Team.Player)
        {
            var lib = SkirmishDirector.ActiveVisualLibrary;

            if (type == BuildingType.AntNest && lib != null && lib.hivePrefab != null)
                return PlaceAntNestFromPrefab(position, lib.hivePrefab, team);

            if (lib != null)
            {
                var prefab = lib.GetBuildingPrefab(type);
                if (prefab != null)
                    return PlaceFromPrefab(position, prefab, type, team);
            }

            var go = new GameObject($"Building_{type}");
            {
                var terrain = Terrain.activeTerrain;
                float terrainY = terrain != null ? terrain.SampleHeight(position) : position.y;
                go.transform.position = new Vector3(position.x, terrainY, position.z);
            }

            Color buildingColor;
            Vector3 scale;
            switch (type)
            {
                case BuildingType.Underground:
                    buildingColor = new Color(0.35f, 0.25f, 0.45f);
                    scale = new Vector3(4f, 1.5f, 4f);
                    break;
                case BuildingType.AntNest:
                    buildingColor = new Color(0.5f, 0.35f, 0.2f);
                    scale = new Vector3(3.5f, 2f, 3.5f);
                    break;
                case BuildingType.SkyTower:
                    buildingColor = new Color(0.3f, 0.5f, 0.6f);
                    scale = new Vector3(2.5f, 5f, 2.5f);
                    break;
                case BuildingType.RootCellar:
                    buildingColor = new Color(0.5f, 0.35f, 0.2f);
                    scale = new Vector3(3.5f, 2f, 3.5f);
                    break;
                default:
                    buildingColor = Color.gray;
                    scale = new Vector3(3f, 2f, 3f);
                    break;
            }

            go.transform.localScale = scale;

            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Visual";
            visual.transform.SetParent(go.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            visual.transform.localScale = Vector3.one;
            Destroy(visual.GetComponent<Collider>());
            ApplyMat(visual, buildingColor);
            
            // Straps for buildings
            var strapColor = TeamPalette.GetTeamColor(team);
            var strap1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            strap1.name = "Strap_Top";
            strap1.transform.SetParent(go.transform, false);
            strap1.transform.localPosition = new Vector3(0f, 1.02f, 0f);
            strap1.transform.localScale = new Vector3(0.5f, 0.05f, 0.5f);
            Destroy(strap1.GetComponent<Collider>());
            ApplyMat(strap1, strapColor);

            var strap2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            strap2.name = "Strap_Side";
            strap2.transform.SetParent(go.transform, false);
            strap2.transform.localPosition = new Vector3(0.52f, 0.5f, 0f);
            strap2.transform.localScale = new Vector3(0.05f, 0.4f, 0.15f);
            Destroy(strap2.GetComponent<Collider>());
            ApplyMat(strap2, strapColor);

            var col = go.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.5f, 0f);
            col.size = Vector3.one;

            var obs = go.AddComponent<NavMeshObstacle>();
            obs.carving = true;
            obs.shape = NavMeshObstacleShape.Box;
            obs.size = new Vector3(1f, 1f, 1f);
            obs.center = new Vector3(0f, 0.5f, 0f);

            var building = go.AddComponent<ProductionBuilding>();
            building.Initialize(type, team);

            SitOnGround(go);
            return building;
        }

        static ProductionBuilding PlaceAntNestFromPrefab(Vector3 position, GameObject hivePrefab, Team team = Team.Player)
        {
            var savedPlayerHive = HiveDeposit.PlayerHive;
            var savedEnemyHive = HiveDeposit.EnemyHive;

            var go = Object.Instantiate(hivePrefab);
            go.name = "Building_AntNest";
            var terrain = Terrain.activeTerrain;
            float terrainY = terrain != null ? terrain.SampleHeight(position) : position.y;
            go.transform.position = new Vector3(position.x, terrainY, position.z);
            go.tag = "Untagged";

            var hd = go.GetComponent<HiveDeposit>();
            if (hd != null) Destroy(hd);
            var hv = go.GetComponent<HiveVisual>();
            if (hv != null) Destroy(hv);

            if (team == Team.Player)
                HiveDeposit.RestorePlayerHiveReference(savedPlayerHive);
            else if (team == Team.Enemy)
                HiveDeposit.RestoreEnemyHiveReference(savedEnemyHive);

            if (go.GetComponent<Collider>() == null)
            {
                var col = go.AddComponent<BoxCollider>();
                col.center = new Vector3(0f, 0.5f, 0f);
                col.size = new Vector3(2f, 2f, 2f);
            }

            if (go.GetComponent<NavMeshObstacle>() == null)
            {
                var obs = go.AddComponent<NavMeshObstacle>();
                obs.carving = true;
                obs.shape = NavMeshObstacleShape.Box;
                obs.size = new Vector3(2f, 2f, 2f);
                obs.center = new Vector3(0f, 0.5f, 0f);
            }
            
            var lib = SkirmishDirector.ActiveVisualLibrary;
            if (lib != null && lib.hiveMaterial != null)
            {
                foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
                    renderer.sharedMaterial = lib.hiveMaterial;
            }

            var skinColor = TeamPalette.GetShellColor(team);
            foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
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

            var building = go.AddComponent<ProductionBuilding>();
            building.Initialize(BuildingType.AntNest, team);

            SitOnGround(go);
            return building;
        }

        static void SitOnGround(GameObject go)
        {
            var renderer = go.GetComponentInChildren<Renderer>();
            if (renderer == null) return;
            float bottomY = renderer.bounds.min.y;
            var terrain = Terrain.activeTerrain;
            float groundY = terrain != null ? terrain.SampleHeight(go.transform.position) : 0f;
            float offset = groundY - bottomY;
            if (offset > 0.01f || offset < -0.01f)
                go.transform.position += new Vector3(0f, offset, 0f);
        }

        static ProductionBuilding PlaceFromPrefab(Vector3 position, GameObject prefab, BuildingType type, Team team)
        {
            var go = Object.Instantiate(prefab);
            go.name = $"Building_{type}";
            var terrain = Terrain.activeTerrain;
            float terrainY = terrain != null ? terrain.SampleHeight(position) : position.y;
            go.transform.position = new Vector3(position.x, terrainY, position.z);

            Vector3 scale = type switch
            {
                BuildingType.Underground => new Vector3(1f, 0.7f, 1f),
                BuildingType.SkyTower => new Vector3(1f, 1.2f, 1f),
                BuildingType.RootCellar => Vector3.one,
                _ => Vector3.one
            };
            go.transform.localScale = scale;

            var skinColor = TeamPalette.GetShellColor(team);
            foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
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

            if (go.GetComponent<Collider>() == null)
            {
                var col = go.AddComponent<BoxCollider>();
                col.center = new Vector3(0f, 1.5f, 0f);
                col.size = new Vector3(5f, 3f, 5f);
            }

            if (go.GetComponent<NavMeshObstacle>() == null)
            {
                var obs = go.AddComponent<NavMeshObstacle>();
                obs.carving = true;
                obs.shape = NavMeshObstacleShape.Box;
                obs.size = new Vector3(4f, 3f, 4f);
                obs.center = new Vector3(0f, 1.5f, 0f);
            }

            var building = go.AddComponent<ProductionBuilding>();
            building.Initialize(type, team);

            SitOnGround(go);
            return building;
        }

        static void ApplyMat(GameObject go, Color c)
        {
            var sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            var m = new Material(sh);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.color = c;
            var r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = m;
        }
    }
}
