using System.Collections.Generic;
using InsectWars.Core;
using InsectWars.Data;
using UnityEngine;
using UnityEngine.AI;

namespace InsectWars.RTS
{
    public class HiveDeposit : MonoBehaviour
    {
        public static HiveDeposit PlayerHive { get; private set; }
        public static HiveDeposit EnemyHive { get; private set; }

        public static System.Action<HiveDeposit> OnDestroyed;

        const float HiveMaxHealth = 600f;

        [SerializeField] Team team = Team.Player;
        Vector3? _rallyPoint;
        RottingFruitNode _rallyGatherTarget;
        GameObject _rallyFlag;

        float _currentHealth = HiveMaxHealth;

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
        public bool IsProducing => _workerQueue.Count > 0;
        public int QueueCount => _workerQueue.Count;
        public float ProductionProgress => _workerQueue.Count > 0 ? Mathf.Clamp01(_workerQueue[0].elapsed / _workerQueue[0].buildTime) : 0f;
        public float CurrentHealth => _currentHealth;
        public float MaxHealth => HiveMaxHealth;
        public bool IsAlive => _currentHealth > 0f;

        void Awake()
        {
            RegisterHive();
        }

        void OnDestroy()
        {
            if (PlayerHive == this) PlayerHive = null;
            if (EnemyHive == this) EnemyHive = null;
        }

        void Update()
        {
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

        public bool QueueWorker()
        {
            if (_workerQueue.Count >= MaxQueueSize) return false;
            int cost = ProductionBuilding.GetUnitCost(UnitArchetype.Worker);
            if (team == Team.Player && PlayerResources.Instance != null && !PlayerResources.Instance.TrySpend(cost))
                return false;
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
        }

        void SpawnWorker()
        {
            var center = transform.position;
            var rend = GetComponentInChildren<Renderer>();
            float hiveExtent = rend != null
                ? Mathf.Max(rend.bounds.extents.x, rend.bounds.extents.z) + 1.2f
                : transform.localScale.x * 0.5f + 1.2f;

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
            var offset = new Vector3(Mathf.Cos(angle) * hiveExtent, 0f, Mathf.Sin(angle) * hiveExtent);
            var spawnPos = center + offset;
            if (NavMesh.SamplePosition(spawnPos, out var hit, 12f, NavMesh.AllAreas))
                spawnPos = hit.position;
            var unit = MapDirector.SpawnUnit(spawnPos, team, UnitArchetype.Worker);
            if (unit == null) return;

            if (_rallyGatherTarget != null && !_rallyGatherTarget.Depleted)
                unit.OrderGather(_rallyGatherTarget);
            else if (_rallyPoint.HasValue)
                unit.OrderMove(_rallyPoint.Value);
        }

        public void ApplyDamage(float dmg)
        {
            if (_currentHealth <= 0f) return;
            _currentHealth -= dmg;
            GameAudio.PlayCombatHit(transform.position);
            if (_currentHealth <= 0f)
            {
                _currentHealth = 0f;
                DestroyHive();
            }
        }

        void DestroyHive()
        {
            if (PlayerHive == this) PlayerHive = null;
            if (EnemyHive == this) EnemyHive = null;
            OnDestroyed?.Invoke(this);
            Destroy(gameObject, 0.1f);
        }

        public void Configure(Team t)
        {
            if (PlayerHive == this) PlayerHive = null;
            if (EnemyHive == this) EnemyHive = null;
            team = t;
            _currentHealth = HiveMaxHealth;
            RegisterHive();
            ApplyTeamColor();
        }

        void RegisterHive()
        {
            if (team == Team.Player)
                PlayerHive = this;
            else if (team == Team.Enemy)
                EnemyHive = this;
        }

        void ApplyTeamColor() => TeamPalette.ApplyToGameObject(team, gameObject);

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
