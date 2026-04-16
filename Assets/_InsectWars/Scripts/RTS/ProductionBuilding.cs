using System.Collections.Generic;
using System.Linq;
using InsectWars.Core;
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
        InsectUnit _rallyUnitTarget;
        GameObject _rallyFlag;
        BuildingState _state = BuildingState.Active;
        float _maxHealth;
        float _currentHealth;
        float _buildTimeTotal;
        float _buildTimeElapsed;
        float _regenTimer;

        Vector3 _originalScale;
        Vector3 _originalPosition;
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
        public InsectUnit RallyUnitTarget => _rallyUnitTarget;
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

        public int GetQueuedCCCost()
        {
            int cost = 0;
            for (int i = 0; i < _queue.Count; i++)
                cost += ColonyCapacity.GetUnitCCCost(_queue[i].archetype);
            return cost;
        }

        public string DisplayName => _type switch
        {
            BuildingType.Underground => "Underground",
            BuildingType.AntNest => "Ant's\nNest",
            BuildingType.SkyTower => "Sky Tower",
            _ => _type.ToString()
        };

        public UnitArchetype[] ProducibleUnits => _type switch
        {
            BuildingType.Underground => new[] { UnitArchetype.BasicFighter, UnitArchetype.BasicRanged, UnitArchetype.GiantStagBeetle },
            BuildingType.AntNest => new[] { UnitArchetype.Worker },
            BuildingType.SkyTower => new[] { UnitArchetype.BlackWidow, UnitArchetype.StickSpy },
            _ => System.Array.Empty<UnitArchetype>()
        };

        public static int GetUnitCost(UnitArchetype arch) => arch switch
        {
            UnitArchetype.BasicFighter => 100,
            UnitArchetype.BasicRanged => 100,
            UnitArchetype.BlackWidow => 250,
            UnitArchetype.StickSpy => 200,
            UnitArchetype.GiantStagBeetle => 350,
            UnitArchetype.Worker => 50,
            _ => 50
        };

        public static string GetUnitName(UnitArchetype arch) => arch switch
        {
            UnitArchetype.BasicFighter => "Mantis",
            UnitArchetype.BasicRanged => "Beetle",
            UnitArchetype.BlackWidow => "Black Widow",
            UnitArchetype.StickSpy => "Stick",
            UnitArchetype.GiantStagBeetle => "Stag Beetle",
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
            UnitArchetype.BlackWidow => 35f,
            UnitArchetype.StickSpy => 20f,
            UnitArchetype.GiantStagBeetle => 40f,
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

        public static string GetDisplayName(BuildingType type) => type switch
        {
            BuildingType.Underground => "Underground",
            BuildingType.AntNest => "Ant's Nest",
            BuildingType.SkyTower => "Sky Tower",
            BuildingType.RootCellar => "Root Cellar",
            _ => type.ToString()
        };

        public static string GetDescription(BuildingType type) => type switch
        {
            BuildingType.Underground => "Military barracks dug beneath the surface. Produces Mantis fighters and Beetle ranged units.",
            BuildingType.AntNest => "Expands the colony. Produces Worker ants and provides supply capacity.",
            BuildingType.SkyTower => "Elevated hive tower. Produces Black Widows — elite assassins — and Sticks — invisible spy units that can climb high ground.",
            BuildingType.RootCellar => "Storage burrow. Provides additional supply capacity for the colony.",
            _ => ""
        };

        public static float GetFootprintRadius(BuildingType type) => type switch
        {
            BuildingType.AntNest => 5f,
            BuildingType.Underground => 4f,
            BuildingType.SkyTower => 4f,
            BuildingType.RootCellar => 1.75f,
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
            ColonyCapacity.NotifyChanged();
            Destroy(gameObject, 0.1f);
        }

        public void TakeDamage(float dmg)
        {
            if (!IsAlive) return;
            _currentHealth = Mathf.Max(0f, _currentHealth - dmg);
            LastDamageTime = Time.time;
            if (_currentHealth <= 0f)
            {
                _state = BuildingState.Destroyed;
                s_all.Remove(this);
                ColonyCapacity.NotifyChanged();
                Destroy(gameObject, 0.3f);
            }
        }

        public float LastDamageTime { get; private set; } = -1f;

        void TickPassiveRegen()
        {
            if (!IsAlive || _currentHealth >= _maxHealth) return;
            _regenTimer += Time.deltaTime;
            if (_regenTimer >= 10f)
            {
                _regenTimer -= 10f;
                _currentHealth = Mathf.Min(_currentHealth + 1f, _maxHealth);
            }
        }

        public void Initialize(BuildingType type, Team team = Team.Player, bool startBuilt = true)
        {
            _type = type;
            _team = team;
            _maxHealth = GetMaxHealth(type);
            _buildTimeTotal = GetConstructionTime(type);

            if (GetComponent<BuildingHealthBar>() == null)
                gameObject.AddComponent<BuildingHealthBar>();

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
                _originalPosition = transform.position;
                foreach (var r in GetComponentsInChildren<Renderer>(true))
                {
                    if (r.material == null) continue;
                    
                    // Ensure the material is transparent for the construction effect
                    if (r.material.HasProperty("_Surface")) r.material.SetFloat("_Surface", 1); // Transparent
                    if (r.material.HasProperty("_Blend")) r.material.SetFloat("_Blend", 0); // Alpha
                    if (r.material.HasProperty("_ZWrite")) r.material.SetFloat("_ZWrite", 0);
                    r.material.renderQueue = 3000; // Transparent queue

                    _originalColors[r] = r.material.HasProperty("_BaseColor")
                        ? r.material.GetColor("_BaseColor")
                        : r.material.color;
                }
                UpdateConstructionVisual();
            }

            if (!s_all.Contains(this))
                s_all.Add(this);

            ColonyCapacity.NotifyChanged();
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
                PlayCompletionEffect();
                SwapToFinalVisual();
                ColonyCapacity.NotifyChanged();
            }
        }

        void UpdateConstructionVisual()
        {
            float progress = ConstructionProgress;

            float scaleY = Mathf.Lerp(0.35f, 1f, progress);
            transform.localScale = new Vector3(
                _originalScale.x,
                _originalScale.y * scaleY,
                _originalScale.z);

            bool hasCustomShader = false;
            foreach (var kvp in _originalColors)
            {
                if (kvp.Key == null || kvp.Key.material == null) continue;
                if (kvp.Key.material.HasProperty("_ConstructionProgress"))
                {
                    kvp.Key.material.SetFloat("_ConstructionProgress", progress);
                    hasCustomShader = true;
                }
            }

            if (!hasCustomShader)
            {
                float alpha = progress < 0.2f
                    ? Mathf.Lerp(0.45f, 0.65f, progress / 0.2f)
                    : Mathf.Lerp(0.65f, 1.0f, (progress - 0.2f) / 0.8f);
                float darkening = Mathf.Lerp(0.5f, 1f, progress);
                Color constructionTint = TeamPalette.GetShellColor(_team);

                foreach (var kvp in _originalColors)
                {
                    if (kvp.Key == null || kvp.Key.material == null) continue;
                    var orig = kvp.Value;
                    // Mix the team color with the prefab's original color (mostly white) for the construction phase
                    Color targetBase = Color.Lerp(constructionTint * darkening, orig * constructionTint, Mathf.Clamp01((progress - 0.4f) / 0.6f));
                    Color finalColor = new Color(targetBase.r, targetBase.g, targetBase.b, alpha);

                    if (kvp.Key.material.HasProperty("_BaseColor"))
                        kvp.Key.material.SetColor("_BaseColor", finalColor);
                    else if (kvp.Key.material.HasProperty("_Color"))
                        kvp.Key.material.color = finalColor;
                }
            }

            if (_assignedBuilders > 0 && progress < 1f)
                SpawnConstructionParticles(progress);
        }

        void SpawnConstructionParticles(float progress)
        {
            if (Random.value > 0.32f) return;

            var rends = GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) return;
            var bounds = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) bounds.Encapsulate(rends[i].bounds);

            Vector3 topCenter = new Vector3(transform.position.x, bounds.max.y, transform.position.z);

            int count = Random.Range(1, 3);
            for (int i = 0; i < count; i++)
            {
                Vector3 randomOffset = new Vector3(
                    Random.Range(-2.5f, 2.5f), Random.Range(-0.5f, 0.5f), Random.Range(-2.5f, 2.5f));

                var dust = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                dust.transform.position = topCenter + randomOffset;
                dust.transform.localScale = Vector3.one * Random.Range(0.4f, 0.8f);
                Destroy(dust.GetComponent<Collider>());

                var sh = Shader.Find("InsectWars/SoftDust");
                if (sh == null) sh = Shader.Find("Universal Render Pipeline/Unlit");

                var mat = new Material(sh);
                Color dustColor = new Color(0.85f, 0.85f, 0.85f, 0.5f);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", dustColor);
                else mat.color = dustColor;

                dust.GetComponent<Renderer>().sharedMaterial = mat;

                var behavior = dust.AddComponent<ConstructionDust>();
                behavior.lifetime = Random.Range(1.0f, 1.8f);
                behavior.maxScale = Random.Range(2.5f, 4.0f);
            }
        }

        void PlayCompletionEffect()
        {
            if (GameAudio.Instance != null && GameAudio.Instance.constructionComplete != null)
                GameAudio.PlayWorld(GameAudio.Instance.constructionComplete, transform.position);
        }

        void SwapToFinalVisual()
        {
            var position = transform.position;
            var savedRally = _rallyPoint;
            var savedRallyTarget = _rallyGatherTarget;
            var savedRallyUnit = _rallyUnitTarget;
            bool wasSelected = SelectionController.Instance != null &&
                               SelectionController.Instance.SelectedBuildings.Contains(this);

            s_all.Remove(this);

            var newBuilding = Place(position, _type, _team, startBuilt: true);

            if (savedRallyUnit != null && savedRallyUnit.IsAlive)
                newBuilding.SetRallyUnit(savedRallyUnit);
            else if (savedRallyTarget != null && savedRally.HasValue)
                newBuilding.SetRallyGather(savedRally.Value, savedRallyTarget);
            else if (savedRally.HasValue)
                newBuilding.SetRallyPoint(savedRally.Value);

            if (wasSelected && SelectionController.Instance != null)
                SelectionController.Instance.SelectBuilding(newBuilding);

            _rallyFlag = null;
            Destroy(gameObject);
        }

        public InsectUnit ProduceUnit(UnitArchetype archetype)
        {
            if (!ColonyCapacity.CanAfford(_team, archetype))
                return null;
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
            if (!ColonyCapacity.CanAfford(_team, archetype))
                return false;
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
            TickRallyUnitTarget();
            TickPassiveRegen();
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
            if (_rallyUnitTarget != null && _rallyUnitTarget.IsAlive)
            {
                unit.OrderMove(_rallyUnitTarget.transform.position);
                return;
            }
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
            _rallyUnitTarget = null;
            SyncRallyFlag();
        }

        public void SetRallyGather(Vector3 pos, RottingFruitNode node)
        {
            _rallyPoint = pos;
            _rallyGatherTarget = node;
            _rallyUnitTarget = null;
            SyncRallyFlag();
        }

        public void SetRallyUnit(InsectUnit unit)
        {
            _rallyPoint = unit.transform.position;
            _rallyGatherTarget = null;
            _rallyUnitTarget = unit;
            SyncRallyFlag();
        }

        public void ClearRally()
        {
            _rallyPoint = null;
            _rallyGatherTarget = null;
            _rallyUnitTarget = null;
            SyncRallyFlag();
        }

        void TickRallyUnitTarget()
        {
            if (_rallyUnitTarget == null) return;
            if (!_rallyUnitTarget.IsAlive)
            {
                _rallyUnitTarget = null;
                _rallyPoint = null;
                SyncRallyFlag();
                return;
            }
            _rallyPoint = _rallyUnitTarget.transform.position;
            if (_rallyFlag != null && _rallyFlag.activeSelf)
                _rallyFlag.transform.position = _rallyPoint.Value;
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

            if (startBuilt)
            {
                if (type == BuildingType.AntNest && lib != null && lib.hivePrefab != null)
                    return PlaceAntNestFromPrefab(position, lib.hivePrefab, team, true);

                if (lib != null)
                {
                    var prefab = lib.GetBuildingPrefab(type);
                    if (prefab != null)
                        return PlaceFromPrefab(position, prefab, type, team, true);
                }
            }

            Color buildingColor;
            Vector3 scale;
            switch (type)
            {
                case BuildingType.AntNest:
                    buildingColor = new Color(0.5f, 0.35f, 0.2f);
                    scale = new Vector3(3.5f, 2f, 3.5f);
                    break;
                case BuildingType.Underground:
                    buildingColor = new Color(0.35f, 0.25f, 0.45f);
                    scale = new Vector3(4f, 2f, 4f);
                    break;
                case BuildingType.SkyTower:
                    buildingColor = new Color(0.3f, 0.5f, 0.6f);
                    scale = new Vector3(4f, 2f, 4f);
                    break;
                case BuildingType.RootCellar:
                    buildingColor = new Color(0.4f, 0.3f, 0.2f);
                    scale = new Vector3(1.75f, 1.25f, 1.75f);
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

            GameObject visual;
            if (lib != null && lib.constructionPrefab != null)
            {
                visual = Instantiate(lib.constructionPrefab, go.transform);
                visual.name = "Visual";
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localScale = Vector3.one;
                PlaceOnGround(visual, groundY);
                var lp = visual.transform.localPosition;
                if (float.IsNaN(lp.x) || float.IsNaN(lp.y) || float.IsNaN(lp.z))
                    visual.transform.localPosition = Vector3.zero;
                var ls = visual.transform.localScale;
                if (float.IsNaN(ls.x) || float.IsNaN(ls.y) || float.IsNaN(ls.z))
                    visual.transform.localScale = Vector3.one;
            }
            else
            {
                visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visual.name = "Visual";
                visual.transform.SetParent(go.transform, false);
                visual.transform.localPosition = new Vector3(0f, 0.5f, 0f);
                visual.transform.localScale = Vector3.one;
                Destroy(visual.GetComponent<Collider>());
                ApplyMat(visual, buildingColor);
            }
            
            // Straps for buildings — only for primitive fallback
            if (lib == null || lib.constructionPrefab == null)
            {
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
            }

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

        static void StripAllBehaviours(GameObject go)
        {
            foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null) continue;
                Destroy(mb);
            }
        }

        static ProductionBuilding PlaceAntNestFromPrefab(Vector3 position, GameObject hivePrefab, Team team = Team.Player, bool startBuilt = false)
        {
            var savedPlayerHive = HiveDeposit.PlayerHive;
            var savedEnemyHive = HiveDeposit.EnemyHive;

            var go = Object.Instantiate(hivePrefab);
            go.name = "Building_AntNest";
            go.tag = "Untagged";

            StripAllBehaviours(go);

            HiveDeposit.SetMainPlayerHive(savedPlayerHive);
            HiveDeposit.SetMainEnemyHive(savedEnemyHive);

            go.transform.localScale *= 2.7f;
            float groundY = SampleMaxTerrainHeight(position, 6f);
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

            SizeObstacleFromBounds(go);

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
                BuildingType.SkyTower => new Vector3(1.0f, 1.3f, 1.0f),
                BuildingType.RootCellar => new Vector3(0.5f, 0.5f, 0.5f),
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

            SizeObstacleFromBounds(go);

            var building = go.AddComponent<ProductionBuilding>();
            building.Initialize(type, team, startBuilt);
            return building;
        }

        static void SizeObstacleFromBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;

            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            var s = go.transform.localScale;
            var worldObsSize = new Vector3(bounds.size.x * 0.55f, bounds.size.y, bounds.size.z * 0.55f);
            var worldCenter = bounds.center - go.transform.position;

            var localSize = new Vector3(worldObsSize.x / s.x, worldObsSize.y / s.y, worldObsSize.z / s.z);
            var localCenter = new Vector3(worldCenter.x / s.x, worldCenter.y / s.y, worldCenter.z / s.z);

            var existing = go.GetComponent<NavMeshObstacle>();
            if (existing != null) Destroy(existing);

            var col = go.GetComponent<BoxCollider>();
            if (col == null) col = go.AddComponent<BoxCollider>();
            col.size = localSize;
            col.center = localCenter;

            var obs = go.AddComponent<NavMeshObstacle>();
            obs.carving = true;
            obs.shape = NavMeshObstacleShape.Box;
            obs.size = localSize;
            obs.center = localCenter;
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
