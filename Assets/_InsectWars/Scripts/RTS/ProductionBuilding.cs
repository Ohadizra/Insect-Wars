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

    public enum BuildingState
    {
        UnderConstruction,
        Active,
        Destroyed
    }

    public class ProductionBuilding : MonoBehaviour
    {
        static readonly List<ProductionBuilding> s_all = new();
        public static IReadOnlyList<ProductionBuilding> All => s_all;

        BuildingType _type;
        Team _team = Team.Player;
        Vector3? _rallyPoint;
        RottingFruitNode _rallyGatherTarget;
        GameObject _rallyFlag;
        BuildingState _state = BuildingState.Active;
        float _maxHealth;
        float _currentHealth;
        float _buildTimeTotal;
        float _buildTimeElapsed;

        Vector3 _originalScale;
        readonly Dictionary<Renderer, Color> _originalColors = new();

        struct QueueEntry
        {
            public UnitArchetype archetype;
            public float buildTime;
            public float elapsed;
        }
        const int MaxQueueSize = 5;
        readonly List<QueueEntry> _queue = new();

        public BuildingType Type => _type;
        public Team Team => _team;
        public Vector3? RallyPoint => _rallyPoint;
        public RottingFruitNode RallyGatherTarget => _rallyGatherTarget;
        public BuildingState State => _state;
        public bool IsOperational => _state == BuildingState.Active;
        public float ConstructionProgress => _buildTimeTotal > 0f
            ? Mathf.Clamp01(_buildTimeElapsed / _buildTimeTotal) : 1f;
        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _maxHealth;
        public bool IsAlive => _state != BuildingState.Destroyed;
        public bool IsUnderConstruction => _state == BuildingState.UnderConstruction;

        int _assignedBuilders;
        public int AssignedBuilders => _assignedBuilders;
        public bool IsProducing => _queue.Count > 0 && IsOperational;
        public int QueueCount => _queue.Count;
        public float ProductionProgress => _queue.Count > 0
            ? Mathf.Clamp01(_queue[0].elapsed / _queue[0].buildTime) : 0f;
        public UnitArchetype? CurrentProducing => _queue.Count > 0 ? _queue[0].archetype : null;

        public string DisplayName => _type switch
        {
            BuildingType.Underground => "Underground",
            BuildingType.AntNest => "Ant's\nNest",
            BuildingType.SkyTower => "Sky Tower",
            _ => _type.ToString()
        };

        public UnitArchetype[] ProducibleUnits => _type switch
        {
            BuildingType.Underground => new[] { UnitArchetype.BasicFighter, UnitArchetype.BasicRanged },
            BuildingType.AntNest => new[] { UnitArchetype.Worker },
            BuildingType.SkyTower => System.Array.Empty<UnitArchetype>(),
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

        public static int GetBuildCost(BuildingType type) => type switch
        {
            BuildingType.Underground => 200,
            BuildingType.AntNest => 400,
            BuildingType.SkyTower => 300,
            BuildingType.RootCellar => 150,
            _ => 100
        };

        public static float GetBuildTime(UnitArchetype arch) => arch switch
        {
            UnitArchetype.Worker => 10f,
            UnitArchetype.BasicFighter => 18f,
            UnitArchetype.BasicRanged => 22f,
            _ => 15f
        };

        public static float GetMaxHealth(BuildingType type) => type switch
        {
            BuildingType.Underground => 400f,
            BuildingType.AntNest => 500f,
            BuildingType.SkyTower => 350f,
            BuildingType.RootCellar => 300f,
            _ => 300f
        };

        public static float GetConstructionTime(BuildingType type) => type switch
        {
            BuildingType.Underground => 25f,
            BuildingType.AntNest => 35f,
            BuildingType.SkyTower => 30f,
            BuildingType.RootCellar => 20f,
            _ => 20f
        };

        public static float GetFootprintRadius(BuildingType type) => type switch
        {
            BuildingType.AntNest => 5f,
            BuildingType.Underground => 4f,
            BuildingType.SkyTower => 3f,
            BuildingType.RootCellar => 3.5f,
            _ => 4f
        };

        void OnDestroy()
        {
            s_all.Remove(this);
            if (_rallyFlag != null) Destroy(_rallyFlag);
        }

        public void CancelConstruction()
        {
            if (_state != BuildingState.UnderConstruction) return;
            int cost = GetBuildCost(_type);
            int refund = Mathf.RoundToInt(cost * 0.8f);
            if (_team == Team.Player && PlayerResources.Instance != null)
                PlayerResources.Instance.AddCalories(refund);
            _state = BuildingState.Destroyed;
            s_all.Remove(this);
            Destroy(gameObject, 0.1f);
        }

        public void TakeDamage(float dmg)
        {
            if (!IsAlive) return;
            _currentHealth = Mathf.Max(0f, _currentHealth - dmg);
            if (_currentHealth <= 0f)
            {
                _state = BuildingState.Destroyed;
                s_all.Remove(this);
                Destroy(gameObject, 0.3f);
            }
        }

        public void Initialize(BuildingType type, Team team = Team.Player, bool startBuilt = true)
        {
            _type = type;
            _team = team;
            _maxHealth = GetMaxHealth(type);
            _buildTimeTotal = GetConstructionTime(type);

            if (startBuilt)
            {
                _currentHealth = _maxHealth;
                _buildTimeElapsed = _buildTimeTotal;
                _state = BuildingState.Active;
            }
            else
            {
                _currentHealth = _maxHealth * 0.1f;
                _buildTimeElapsed = 0f;
                _state = BuildingState.UnderConstruction;
                _originalScale = transform.localScale;
                foreach (var r in GetComponentsInChildren<Renderer>(true))
                {
                    if (r.material != null)
                        _originalColors[r] = r.material.color;
                }
                UpdateConstructionVisual();
            }

            if (!s_all.Contains(this))
                s_all.Add(this);
        }

        public void RegisterBuilder() => _assignedBuilders++;
        public void UnregisterBuilder() => _assignedBuilders = Mathf.Max(0, _assignedBuilders - 1);

        public void TickConstruction(float dt)
        {
            if (_state != BuildingState.UnderConstruction) return;
            _buildTimeElapsed += dt;
            float progress = Mathf.Clamp01(_buildTimeElapsed / _buildTimeTotal);
            _currentHealth = Mathf.Lerp(_maxHealth * 0.1f, _maxHealth, progress);

            UpdateConstructionVisual();

            if (_buildTimeElapsed >= _buildTimeTotal)
            {
                _state = BuildingState.Active;
                _currentHealth = _maxHealth;
                _buildTimeElapsed = _buildTimeTotal;
                transform.localScale = _originalScale;
                foreach (var kvp in _originalColors)
                {
                    if (kvp.Key != null && kvp.Key.material != null)
                        kvp.Key.material.color = kvp.Value;
                }
            }
        }

        void UpdateConstructionVisual()
        {
            float progress = ConstructionProgress;
            float alpha = Mathf.Lerp(0.3f, 1f, progress);
            float scaleY = Mathf.Lerp(0.6f, 1f, progress);

            transform.localScale = new Vector3(
                _originalScale.x,
                _originalScale.y * scaleY,
                _originalScale.z);

            foreach (var kvp in _originalColors)
            {
                if (kvp.Key == null || kvp.Key.material == null) continue;
                var orig = kvp.Value;
                kvp.Key.material.color = new Color(orig.r, orig.g, orig.b, alpha);
            }
        }

        public InsectUnit ProduceUnit(UnitArchetype archetype)
        {
            int cost = GetUnitCost(archetype);
            if (_team == Team.Player && PlayerResources.Instance != null && !PlayerResources.Instance.TrySpend(cost))
                return null;
            if (_team == Team.Enemy && !EnemyResources.TrySpend(cost))
                return null;

            var spawnPos = GetSpawnPosition();
            var unit = SkirmishDirector.SpawnUnit(spawnPos, _team, archetype);
            if (unit == null) return null;

            SendToRally(unit);
            return unit;
        }

        public bool QueueUnit(UnitArchetype archetype)
        {
            if (!IsOperational || _queue.Count >= MaxQueueSize) return false;
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

        void Update()
        {
            if (_queue.Count == 0 || !IsOperational) return;
            var entry = _queue[0];
            entry.elapsed += Time.deltaTime;
            _queue[0] = entry;
            if (entry.elapsed >= entry.buildTime)
            {
                _queue.RemoveAt(0);
                ProduceUnitImmediate(entry.archetype);
            }
        }

        void ProduceUnitImmediate(UnitArchetype archetype)
        {
            var spawnPos = GetSpawnPosition();
            var unit = SkirmishDirector.SpawnUnit(spawnPos, _team, archetype);
            if (unit == null) return;
            SendToRally(unit);
        }

        Vector3 GetSpawnPosition()
        {
            var center = new Vector3(transform.position.x, 0f, transform.position.z);

            var rend = GetComponentInChildren<Renderer>();
            float edge = transform.localScale.x * 0.5f + 1.5f;
            if (rend != null)
                edge = Mathf.Max(rend.bounds.extents.x, rend.bounds.extents.z) + 1.5f;

            float angle;
            if (_rallyPoint.HasValue)
            {
                var dir = _rallyPoint.Value - center;
                dir.y = 0f;
                angle = Mathf.Atan2(dir.z, dir.x);
                angle += Random.Range(-0.3f, 0.3f);
            }
            else
            {
                angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            }

            var offset = new Vector3(Mathf.Cos(angle) * edge, 0f, Mathf.Sin(angle) * edge);
            var spawnPos = center + offset;
            if (NavMesh.SamplePosition(spawnPos, out var hit, 4f, NavMesh.AllAreas))
                spawnPos = hit.position;
            return spawnPos;
        }

        void SendToRally(InsectUnit unit)
        {
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

        public static ProductionBuilding Place(Vector3 position, BuildingType type, Team team = Team.Player, bool startBuilt = false)
        {
            var lib = SkirmishDirector.ActiveVisualLibrary;

            if (type == BuildingType.AntNest && lib != null && lib.hivePrefab != null)
                return PlaceAntNestFromPrefab(position, lib.hivePrefab, team, startBuilt);

            if (lib != null)
            {
                var prefab = lib.GetBuildingPrefab(type);
                if (prefab != null)
                    return PlaceFromPrefab(position, prefab, type, team, startBuilt);
            }

            Color buildingColor;
            Vector3 scale;
            switch (type)
            {
                case BuildingType.AntNest:
                    buildingColor = new Color(0.5f, 0.35f, 0.2f);
                    scale = new Vector3(6f, 3.5f, 6f);
                    break;
                case BuildingType.Underground:
                    buildingColor = new Color(0.35f, 0.25f, 0.45f);
                    scale = new Vector3(4f, 2f, 4f);
                    break;
                case BuildingType.SkyTower:
                    buildingColor = new Color(0.3f, 0.5f, 0.6f);
                    scale = new Vector3(3f, 5f, 3f);
                    break;
                case BuildingType.RootCellar:
                    buildingColor = new Color(0.4f, 0.3f, 0.2f);
                    scale = new Vector3(3.5f, 2.5f, 3.5f);
                    break;
                default:
                    buildingColor = Color.gray;
                    scale = new Vector3(3.5f, 2.5f, 3.5f);
                    break;
            }

            float footprint = Mathf.Max(scale.x, scale.z);
            float groundY = SampleMaxTerrainHeight(position, footprint);
            var go = new GameObject($"Building_{type}");
            go.transform.position = new Vector3(position.x, groundY, position.z);
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
            building.Initialize(type, team, startBuilt);

            return building;
        }

        static ProductionBuilding PlaceAntNestFromPrefab(Vector3 position, GameObject hivePrefab, Team team = Team.Player, bool startBuilt = false)
        {
            var savedPlayerHive = HiveDeposit.PlayerHive;
            var savedEnemyHive = HiveDeposit.EnemyHive;

            var go = Object.Instantiate(hivePrefab);
            go.name = "Building_AntNest";
            go.transform.localScale *= 1.68f;
            float groundY = SampleMaxTerrainHeight(position, 6f);
            go.transform.position = new Vector3(position.x, 0f, position.z);
            PlaceOnGround(go, groundY);
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
            
            // Apply skin color to prefab renderers
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
            building.Initialize(BuildingType.AntNest, team, startBuilt);
            return building;
        }

        static ProductionBuilding PlaceFromPrefab(Vector3 position, GameObject prefab, BuildingType type, Team team, bool startBuilt = false)
        {
            var go = Object.Instantiate(prefab);
            go.name = $"Building_{type}";

            Vector3 scale = type switch
            {
                BuildingType.Underground => new Vector3(0.845f, 0.676f, 0.845f),
                BuildingType.SkyTower => new Vector3(1f, 1.3f, 1f),
                _ => Vector3.one
            };
            go.transform.localScale = scale;

            float footprint = Mathf.Max(scale.x, scale.z) * 5f;
            float groundY = SampleMaxTerrainHeight(position, footprint);
            go.transform.position = new Vector3(position.x, 0f, position.z);
            PlaceOnGround(go, groundY);

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
            building.Initialize(type, team, startBuilt);
            return building;
        }

        static float SampleMaxTerrainHeight(Vector3 center, float footprint)
        {
            var terrain = Terrain.activeTerrain;
            if (terrain == null) return center.y;
            float best = terrain.SampleHeight(center);
            float half = footprint * 0.5f;
            best = Mathf.Max(best, terrain.SampleHeight(center + new Vector3(half, 0f, half)));
            best = Mathf.Max(best, terrain.SampleHeight(center + new Vector3(-half, 0f, half)));
            best = Mathf.Max(best, terrain.SampleHeight(center + new Vector3(half, 0f, -half)));
            best = Mathf.Max(best, terrain.SampleHeight(center + new Vector3(-half, 0f, -half)));
            return best;
        }

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
