using System.Collections.Generic;
using InsectWars.Data;
using UnityEngine;
using UnityEngine.AI;

namespace InsectWars.RTS
{
    public class HiveDeposit : MonoBehaviour
    {
        public static HiveDeposit PlayerHive { get; private set; }
        public static HiveDeposit EnemyHive { get; private set; }

        const float HiveMaxHealth = 600f;

        [SerializeField] Team team = Team.Player;
        Vector3? _rallyPoint;
        RottingFruitNode _rallyGatherTarget;
        InsectUnit _rallyUnitTarget;
        GameObject _rallyFlag;

        float _currentHealth = HiveMaxHealth;
        float _regenTimer;

        struct WorkerQueueEntry
        {
            public float buildTime;
            public float elapsed;
        }

        const int MaxQueueSize = 5;
        readonly List<WorkerQueueEntry> _workerQueue = new();

        public Team Team => team;
        public Vector3? RallyPoint => _rallyPoint;
        public RottingFruitNode RallyGatherTarget => _rallyGatherTarget;
        public InsectUnit RallyUnitTarget => _rallyUnitTarget;
        public bool IsProducing => _workerQueue.Count > 0;
        public int QueueCount => _workerQueue.Count;
        public float ProductionProgress => _workerQueue.Count > 0
            ? Mathf.Clamp01(_workerQueue[0].elapsed / _workerQueue[0].buildTime) : 0f;
        public float CurrentHealth => _currentHealth;
        public float MaxHealth => HiveMaxHealth;
        public bool IsAlive => _currentHealth > 0f;

        void Awake()
        {
            RegisterHive();
            if (GetComponent<BuildingHealthBar>() == null)
                gameObject.AddComponent<BuildingHealthBar>();
        }

        void OnEnable()
        {
            RegisterHive();
        }

        void OnDisable()
        {
            if (PlayerHive == this) PlayerHive = null;
            if (EnemyHive == this) EnemyHive = null;
        }

        void OnDestroy()
        {
            if (PlayerHive == this) PlayerHive = null;
            if (EnemyHive == this) EnemyHive = null;
        }

        public void Configure(Team t)
        {
            if (PlayerHive == this) PlayerHive = null;
            if (EnemyHive == this) EnemyHive = null;
            team = t;
            RegisterHive();
        }

        void RegisterHive()
        {
            if (team == Team.Player)
            {
                if (PlayerHive == null) PlayerHive = this;
            }
            else if (team == Team.Enemy)
            {
                if (EnemyHive == null) EnemyHive = this;
            }
        }

        void Update()
        {
            TickPassiveRegen();
            if (_workerQueue.Count == 0) return;
            var entry = _workerQueue[0];
            entry.elapsed += Time.deltaTime;
            _workerQueue[0] = entry;
            if (entry.elapsed >= entry.buildTime)
            {
                _workerQueue.RemoveAt(0);
                SpawnWorker();
            }
        }

        void LateUpdate()
        {
            TickRallyUnitTarget();
            if (_rallyFlag != null)
            {
                bool show = _rallyPoint.HasValue
                    && SelectionController.Instance != null
                    && SelectionController.Instance.SelectedHive == this;
                _rallyFlag.SetActive(show);
            }
        }

        public bool QueueWorker()
        {
            if (_workerQueue.Count >= MaxQueueSize)
            {
                if (team == Team.Player)
                    WarningSystem.ReportWarning(WarningType.QueueFull);
                return false;
            }
            if (!ColonyCapacity.CanAfford(team, UnitArchetype.Worker))
            {
                if (team == Team.Player)
                    WarningSystem.ReportWarning(WarningType.ColonyCapFull);
                return false;
            }
            int cost = ProductionBuilding.GetUnitCost(UnitArchetype.Worker);
            if (team == Team.Player && PlayerResources.Instance != null && !PlayerResources.Instance.TrySpend(cost))
            {
                WarningSystem.ReportWarning(WarningType.NotEnoughCalories);
                return false;
            }
            if (team == Team.Enemy && !EnemyResources.TrySpend(cost))
                return false;
            _workerQueue.Add(new WorkerQueueEntry
            {
                buildTime = ProductionBuilding.GetBuildTime(UnitArchetype.Worker),
                elapsed = 0f
            });
            return true;
        }

        public void CancelLast()
        {
            if (_workerQueue.Count == 0) return;
            _workerQueue.RemoveAt(_workerQueue.Count - 1);
            int refund = ProductionBuilding.GetUnitCost(UnitArchetype.Worker);
            if (team == Team.Player && PlayerResources.Instance != null)
                PlayerResources.Instance.AddCalories(refund);
            else if (team == Team.Enemy)
                EnemyResources.AddCalories(refund);
            ColonyCapacity.NotifyChanged();
        }

        public void CancelAtIndex(int index)
        {
            if (index < 0 || index >= _workerQueue.Count) return;
            _workerQueue.RemoveAt(index);
            int refund = ProductionBuilding.GetUnitCost(UnitArchetype.Worker);
            if (team == Team.Player && PlayerResources.Instance != null)
                PlayerResources.Instance.AddCalories(refund);
            else if (team == Team.Enemy)
                EnemyResources.AddCalories(refund);
            ColonyCapacity.NotifyChanged();
        }

        void SpawnWorker()
        {
            var center = new Vector3(transform.position.x, 0f, transform.position.z);
            var rend = GetComponentInChildren<Renderer>();
            float edge = rend != null
                ? Mathf.Max(rend.bounds.extents.x, rend.bounds.extents.z) + 1.5f
                : transform.localScale.x * 0.5f + 1.5f;

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

            var spawnPos = new Vector3(
                center.x + Mathf.Cos(angle) * edge, 0f,
                center.z + Mathf.Sin(angle) * edge);
            if (NavMesh.SamplePosition(spawnPos, out var hit, 4f, NavMesh.AllAreas))
                spawnPos = hit.position;

            var unit = SkirmishDirector.SpawnUnit(spawnPos, team, UnitArchetype.Worker);
            if (unit == null) return;
            ColonyCapacity.NotifyChanged();

            if (_rallyUnitTarget != null && _rallyUnitTarget.IsAlive)
                unit.OrderMove(_rallyUnitTarget.transform.position);
            else if (_rallyGatherTarget != null && !_rallyGatherTarget.Depleted &&
                unit.Definition != null && unit.Definition.canGather)
                unit.OrderGather(_rallyGatherTarget);
            else if (_rallyPoint.HasValue)
                unit.OrderMove(_rallyPoint.Value);
        }

        public float LastDamageTime { get; private set; } = -1f;

        public void TakeDamage(float dmg)
        {
            _currentHealth = Mathf.Max(0f, _currentHealth - dmg);
            LastDamageTime = Time.time;
            if (this == PlayerHive)
                WarningSystem.ReportWarning(WarningType.BaseUnderAttack, transform.position);
        }

        void TickPassiveRegen()
        {
            if (!IsAlive || _currentHealth >= HiveMaxHealth) return;
            _regenTimer += Time.deltaTime;
            if (_regenTimer >= 10f)
            {
                _regenTimer -= 10f;
                _currentHealth = Mathf.Min(_currentHealth + 1f, HiveMaxHealth);
            }
        }

        /// <summary>
        /// Forcefully sets the main player hive reference. Used when the primary hive
        /// is definitively known (e.g. at map build) or needs to be restored after ghost stripping.
        /// </summary>
        public static void SetMainPlayerHive(HiveDeposit hive)
        {
            PlayerHive = hive;
        }

        public static void SetMainEnemyHive(HiveDeposit hive)
        {
            EnemyHive = hive;
        }

        /// <summary>
        /// Ant Nest uses the hive prefab but strips <see cref="HiveDeposit"/>. Instantiate still runs Awake on the
        /// duplicate component and overwrites static refs; restore the real hive after <c>Destroy(hd)</c>.
        /// </summary>
        public static void RestorePlayerHiveReference(HiveDeposit mainPlayerHive)
        {
            if (mainPlayerHive != null)
                PlayerHive = mainPlayerHive;
        }

        public static void RestoreEnemyHiveReference(HiveDeposit mainEnemyHive)
        {
            if (mainEnemyHive != null)
                EnemyHive = mainEnemyHive;
        }

        public Vector3 DepositPoint
        {
            get
            {
                var rend = GetComponentInChildren<Renderer>();
                float edgeDist = rend != null
                    ? Mathf.Max(rend.bounds.extents.x, rend.bounds.extents.z) + 0.8f
                    : transform.localScale.x * 0.5f + 0.8f;
                var ground = new Vector3(transform.position.x, 0f, transform.position.z);
                var edgePoint = ground + Vector3.forward * edgeDist;
                if (NavMesh.SamplePosition(edgePoint, out var hit, 6f, NavMesh.AllAreas))
                    return hit.position;
                if (NavMesh.SamplePosition(ground, out hit, 12f, NavMesh.AllAreas))
                    return hit.position;
                return ground;
            }
        }

        public Vector3 GetDepositPoint(Vector3 fromPosition)
        {
            var rend = GetComponentInChildren<Renderer>();
            float edgeDist = rend != null
                ? Mathf.Max(rend.bounds.extents.x, rend.bounds.extents.z) + 0.8f
                : transform.localScale.x * 0.5f + 0.8f;
            var dir = fromPosition - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) dir = Vector3.forward;
            var edgePoint = new Vector3(transform.position.x, 0f, transform.position.z)
                            + dir.normalized * edgeDist;
            if (NavMesh.SamplePosition(edgePoint, out var hit, 6f, NavMesh.AllAreas))
                return hit.position;
            return DepositPoint;
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
            if (_rallyFlag != null)
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
                Object.Destroy(pole.GetComponent<Collider>());
                ApplyFlagMat(pole, new Color(0.9f, 0.9f, 0.7f));

                var banner = GameObject.CreatePrimitive(PrimitiveType.Cube);
                banner.name = "Banner";
                banner.transform.SetParent(_rallyFlag.transform, false);
                banner.transform.localPosition = new Vector3(0.2f, 1.05f, 0f);
                banner.transform.localScale = new Vector3(0.35f, 0.22f, 0.05f);
                Object.Destroy(banner.GetComponent<Collider>());
                ApplyFlagMat(banner, new Color(0.3f, 1f, 0.45f, 0.9f));
            }

            _rallyFlag.SetActive(true);
            _rallyFlag.transform.position = _rallyPoint.Value;
        }

        static void ApplyFlagMat(GameObject go, Color c)
        {
            var sh = Shader.Find("Sprites/Default");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Unlit");
            var m = new Material(sh);
            if (m.HasProperty("_Color")) m.color = c;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            var r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = m;
        }
    }
}
