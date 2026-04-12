using InsectWars.Core;
using InsectWars.Data;
using UnityEngine;
using UnityEngine.AI;

namespace InsectWars.RTS
{
    public enum UnitOrder
    {
        Idle,
        Move,
        Attack,
        Gather,
        ReturnDeposit,
        Patrol,
        Build,
        AttackBuilding,
        AttackHive
    }

    public enum CargoType { Calories }

    [RequireComponent(typeof(NavMeshAgent))]
    public class InsectUnit : MonoBehaviour
    {
        [SerializeField] UnitDefinition definition;
        [SerializeField] Team team = Team.Player;

        NavMeshAgent _agent;
        float _health;
        float _attackCooldown;
        UnitOrder _order;

        // ──────────── Targets ────────────
        Transform _attackTarget;
        ProductionBuilding _buildingAttackTarget;
        HiveDeposit _hiveAttackTarget;
        ProductionBuilding _buildTarget;
        RottingFruitNode _gatherTarget;
        RottingFruitNode _lastGatherTarget;

        // ──────────── Timers ────────────
        float _gatherTimer;
        float _buildTimer;
        float _idleScanTimer;
        float _terrainSpeedTimer;
        float _terrainDmgAccum;

        // ──────────── Flags ────────────
        bool _holdPosition;
        bool _wantsAttackMove;
        Vector3 _attackMoveDest;
        bool _patrolActive;
        Vector3 _patrolA;
        Vector3 _patrolB;
        bool _patrolToB = true;

        static int s_unitsLayer = -1;

        // ──────────── Public API ────────────
        public Team Team => team;
        public UnitDefinition Definition => definition;
        public UnitArchetype Archetype => definition != null ? definition.archetype : UnitArchetype.Worker;
        public bool IsAlive => _health > 0;
        public float CurrentHealth => _health;
        public float MaxHealth => definition != null ? definition.maxHealth : 40f;
        public NavMeshAgent Agent => _agent;
        public bool IsSelected { get; set; }
        public UnitOrder CurrentOrder => _order;
        public Transform AttackTarget => _attackTarget;
        public float LastDamageTime { get; private set; } = -1f;

        // ──────────── Selection Ring ────────────
        GameObject _selectionRing;
        static Mesh s_sharedRingMesh;
        static Material s_sharedRingMaterial;

        bool AgentReady => _agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh;

        // ──────────── Lifecycle ────────────

        void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            if (s_unitsLayer < 0)
                s_unitsLayer = LayerMask.NameToLayer("Units");
        }

        void OnEnable() => RtsSimRegistry.Register(this);
        void OnDisable() => RtsSimRegistry.Unregister(this);

        void Start()
        {
            EnsureDefinition(Archetype);
            ApplyDefinition();

            if (_health <= 0.001f)
            {
                _health = definition.maxHealth;
                if (team == Team.Enemy)
                    _health *= GameSession.DifficultyEnemyHpMultiplier;
            }

            if (GetComponent<UnitHealthBar>() == null)
                gameObject.AddComponent<UnitHealthBar>();
        }

        public void Configure(Team t, UnitDefinition def, UnitArchetype archetype = UnitArchetype.Worker)
        {
            team = t;
            if (def != null) definition = def;
            EnsureDefinition(archetype);
            ApplyDefinition();

            _health = definition.maxHealth;
            if (team == Team.Enemy)
                _health *= GameSession.DifficultyEnemyHpMultiplier;

            if (GetComponent<UnitHealthBar>() == null)
                gameObject.AddComponent<UnitHealthBar>();
        }

        void EnsureDefinition(UnitArchetype archetype)
        {
            if (definition != null) return;
            definition = UnitDefinition.CreateRuntimeDefault(archetype,
                TeamPalette.UnitBody(team, archetype));
        }

        void ApplyDefinition()
        {
            if (definition == null) return;
            if (_agent == null) _agent = GetComponent<NavMeshAgent>();
            if (_agent == null) return;

            _agent.speed = definition.moveSpeed;
            if (_agent.radius > 0.45f) _agent.radius = 0.45f;
            _agent.stoppingDistance = definition.archetype == UnitArchetype.BasicRanged
                ? definition.attackRange * 0.85f
                : 0.5f;
            _agent.autoRepath = true;
            _agent.autoBraking = true;

            var animator = GetComponentInChildren<Animator>();
            if (animator != null)
                animator.applyRootMotion = false;

            TeamPalette.ApplyToGameObject(team, gameObject, _selectionRing);
        }

        // ──────────── NavMesh Helpers ────────────

        void SafeSetDestination(Vector3 dest)
        {
            if (AgentReady)
            {
                _agent.isStopped = false;
                _agent.SetDestination(dest);
            }
        }

        void SafeStopAgent()
        {
            if (AgentReady)
            {
                _agent.isStopped = true;
                _agent.ResetPath();
            }
        }

        void RestoreAgentControl()
        {
            if (_agent != null)
            {
                _agent.updatePosition = true;
                _agent.updateRotation = true;
            }
        }

        // ──────────── Order Commands ────────────

        public void OrderMove(Vector3 world)
        {
            if (!IsAlive) return;
            ClearTargets();
            _wantsAttackMove = false;
            _holdPosition = false;
            _patrolActive = false;
            _order = UnitOrder.Move;
            SafeSetDestination(world);
        }

        public void OrderAttackMove(Vector3 world)
        {
            if (!IsAlive) return;
            ClearTargets();
            _wantsAttackMove = true;
            _attackMoveDest = world;
            _holdPosition = false;
            _patrolActive = false;
            _order = UnitOrder.Move;
            SafeSetDestination(world);
        }

        public void OrderAttack(InsectUnit target, bool keepAttackMoveIntent = false)
        {
            if (!IsAlive || target == null || !target.IsAlive || target.team == team) return;
            bool keepAm = keepAttackMoveIntent && _wantsAttackMove;
            Vector3 keepDest = _attackMoveDest;
            ClearTargets();
            _wantsAttackMove = keepAm;
            _attackMoveDest = keepDest;
            _holdPosition = false;
            _patrolActive = false;
            _attackTarget = target.transform;
            _order = UnitOrder.Attack;
            SafeSetDestination(target.transform.position);
        }

        public void OrderStop()
        {
            if (!IsAlive) return;
            ClearTargets();
            _wantsAttackMove = false;
            _holdPosition = false;
            _patrolActive = false;
            _order = UnitOrder.Idle;
            SafeStopAgent();
        }

        public void OrderHoldPosition()
        {
            if (!IsAlive) return;
            ClearTargets();
            _wantsAttackMove = false;
            _patrolActive = false;
            _holdPosition = true;
            _order = UnitOrder.Idle;
            SafeStopAgent();
        }

        public void OrderPatrol(Vector3 a, Vector3 b)
        {
            if (!IsAlive) return;
            ClearTargets();
            _wantsAttackMove = false;
            _holdPosition = false;
            _patrolActive = true;
            _patrolA = a;
            _patrolB = b;
            _patrolToB = true;
            _order = UnitOrder.Patrol;
            SafeSetDestination(_patrolB);
        }

        public void OrderGather(RottingFruitNode node)
        {
            if (!IsAlive || node == null || node.Depleted) return;
            if (definition == null || !definition.canGather) return;

            var inv = GetComponent<WorkerInventory>();
            if (inv != null && inv.Carrying > 0)
            {
                var depositDest = FindNearestDepositPoint();
                if (depositDest.HasValue)
                {
                    _lastGatherTarget = node;
                    _gatherTarget = null;
                    _wantsAttackMove = false;
                    _holdPosition = false;
                    _patrolActive = false;
                    _order = UnitOrder.ReturnDeposit;
                    SafeSetDestination(depositDest.Value);
                    return;
                }
            }

            ClearTargets();
            _wantsAttackMove = false;
            _holdPosition = false;
            _patrolActive = false;
            _gatherTarget = node;
            _lastGatherTarget = node;
            _order = UnitOrder.Gather;
            SafeSetDestination(node.GetGatherPoint(transform.position));
        }

        public void OrderReturnToHive()
        {
            if (!IsAlive) return;
            var dest = FindNearestDepositPoint();
            if (!dest.HasValue) return;
            ClearTargets();
            _wantsAttackMove = false;
            _holdPosition = false;
            _patrolActive = false;
            _order = UnitOrder.ReturnDeposit;
            SafeSetDestination(dest.Value);
        }

        public void OrderBuild(ProductionBuilding building)
        {
            if (!IsAlive || building == null) return;
            if (building.State != BuildingState.UnderConstruction) return;
            if (definition == null || !definition.canGather) return;
            ClearTargets();
            _wantsAttackMove = false;
            _holdPosition = false;
            _patrolActive = false;
            _buildTarget = building;
            _buildTarget.AssignBuilder();
            _order = UnitOrder.Build;
            SafeSetDestination(building.transform.position);
        }

        public void OrderAttackBuilding(ProductionBuilding building)
        {
            if (!IsAlive || building == null || !building.IsAlive) return;
            if (building.Team == team) return;
            ClearTargets();
            _wantsAttackMove = false;
            _holdPosition = false;
            _patrolActive = false;
            _buildingAttackTarget = building;
            _order = UnitOrder.AttackBuilding;
            SafeSetDestination(building.transform.position);
        }

        public void OrderAttackHive(HiveDeposit hive)
        {
            if (!IsAlive || hive == null || !hive.IsAlive) return;
            if (hive.Team == team) return;
            ClearTargets();
            _wantsAttackMove = false;
            _holdPosition = false;
            _patrolActive = false;
            _hiveAttackTarget = hive;
            _order = UnitOrder.AttackHive;
            SafeSetDestination(hive.transform.position);
        }

        void ClearTargets()
        {
            if (_buildTarget != null)
            {
                _buildTarget.UnassignBuilder();
                _buildTarget = null;
            }
            _buildingAttackTarget = null;
            _hiveAttackTarget = null;
            _attackTarget = null;
            _gatherTarget = null;
            _lastGatherTarget = null;
            _gatherTimer = 0f;
            _buildTimer = 0f;
            RestoreAgentControl();
        }

        // ──────────── Update Dispatch ────────────

        void Update()
        {
            if (!IsAlive) return;

            _attackCooldown -= Time.deltaTime;
            TickTerrainEffects();

            switch (_order)
            {
                case UnitOrder.Idle:          TickIdle();          break;
                case UnitOrder.Move:          TickMove();          break;
                case UnitOrder.Attack:        TickAttack();        break;
                case UnitOrder.Gather:        TickGather();        break;
                case UnitOrder.ReturnDeposit: TickReturn();        break;
                case UnitOrder.Patrol:        TickPatrol();        break;
                case UnitOrder.Build:         TickBuild();         break;
                case UnitOrder.AttackBuilding:
                case UnitOrder.AttackHive:    TickAttackStructure(); break;
            }
        }

        void LateUpdate() => SyncSelectionRing();

        // ──────────── Tick: Idle ────────────

        void TickIdle()
        {
            if (definition == null || !definition.canGather) return;
            if (_holdPosition) return;

            _idleScanTimer -= Time.deltaTime;
            if (_idleScanTimer > 0f) return;
            _idleScanTimer = 0.5f;

            RottingFruitNode best = null;
            float bestDist = float.MaxValue;
            foreach (var node in RtsSimRegistry.FruitNodes)
            {
                if (node == null || node.Depleted) continue;
                float d = Vector3.Distance(transform.position, node.transform.position);
                if (d < bestDist) { bestDist = d; best = node; }
            }
            if (best != null) OrderGather(best);
        }

        // ──────────── Tick: Move ────────────

        void TickMove()
        {
            if (_wantsAttackMove) ScanForEnemies();
            if (!AgentReady) return;
            if (_agent.pathPending) return;

            if (_agent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                _order = UnitOrder.Idle;
                return;
            }

            if (!_wantsAttackMove && _agent.hasPath &&
                _agent.remainingDistance <= _agent.stoppingDistance + 0.2f &&
                _agent.velocity.sqrMagnitude < 0.01f)
            {
                _order = UnitOrder.Idle;
            }
        }

        void ScanForEnemies()
        {
            if (_order != UnitOrder.Move || s_unitsLayer < 0) return;
            float scan = definition != null ? definition.visionRadius : 12f;
            var cols = Physics.OverlapSphere(transform.position, scan, 1 << s_unitsLayer, QueryTriggerInteraction.Ignore);
            InsectUnit best = null;
            float bestD = scan;
            foreach (var c in cols)
            {
                var u = c.GetComponentInParent<InsectUnit>();
                if (u == null || !u.IsAlive || u.team == team) continue;
                float d = Vector3.Distance(transform.position, u.transform.position);
                if (d < bestD) { bestD = d; best = u; }
            }
            if (best != null) OrderAttack(best, true);
        }

        // ──────────── Tick: Attack ────────────

        void TickAttack()
        {
            if (_attackTarget == null)
            {
                ResumeAfterCombat();
                return;
            }
            var targetUnit = _attackTarget.GetComponent<InsectUnit>();
            if (targetUnit != null && !targetUnit.IsAlive)
            {
                ResumeAfterCombat();
                return;
            }

            float dist = Vector3.Distance(transform.position, _attackTarget.position);
            float range = GetEffectiveRange();
            bool isRanged = definition.archetype == UnitArchetype.BasicRanged;

            if (dist > range)
            {
                RestoreAgentControl();
                SafeSetDestination(_attackTarget.position);
                return;
            }

            if (AgentReady)
            {
                _agent.isStopped = true;
                _agent.ResetPath();
                _agent.velocity = Vector3.zero;
                if (!isRanged)
                {
                    _agent.updatePosition = false;
                    _agent.updateRotation = false;
                }
            }

            if (_attackCooldown > 0f) return;
            _attackCooldown = definition.attackCooldown;

            var animDriver = GetComponent<UnitAnimationDriver>();
            if (isRanged)
            {
                var lookDir = _attackTarget.position - transform.position;
                lookDir.y = 0f;
                var origin = animDriver != null ? animDriver.GetSprayOrigin() : transform.position + Vector3.up * 0.45f;
                SprayAttack.Fire(origin, lookDir.normalized, definition.attackRange, team, definition.attackDamage, this);
            }
            else if (targetUnit != null)
            {
                targetUnit.ApplyDamage(definition.attackDamage, this);
            }
            animDriver?.NotifyAttack();
        }

        float GetEffectiveRange()
        {
            if (definition == null) return 1.5f;
            if (_attackTarget == null) return definition.attackRange;

            float selfR = _agent != null ? _agent.radius : 0.4f;
            float targetR = 0.4f;
            var targetAgent = _attackTarget.GetComponent<NavMeshAgent>();
            if (targetAgent != null) targetR = targetAgent.radius;

            float surfaceRange = Mathf.Max(0.1f, definition.attackRange - 0.8f);
            return surfaceRange + selfR + targetR;
        }

        // ──────────── Tick: Gather ────────────

        void TickGather()
        {
            if (_gatherTarget == null || _gatherTarget.Depleted)
            {
                _gatherTarget = null;
                _order = UnitOrder.Idle;
                return;
            }
            if (!AgentReady) return;

            var diff = transform.position - _gatherTarget.transform.position;
            diff.y = 0f;
            if (diff.magnitude > _gatherTarget.GatherRange)
            {
                if (!_agent.pathPending && _agent.pathStatus == NavMeshPathStatus.PathInvalid)
                {
                    _gatherTarget = null;
                    _lastGatherTarget = null;
                    _order = UnitOrder.Idle;
                    return;
                }
                if (!_agent.pathPending && !_agent.hasPath)
                    SafeSetDestination(_gatherTarget.GetGatherPoint(transform.position));
                return;
            }

            SafeStopAgent();
            _gatherTimer += Time.deltaTime;
            if (_gatherTimer < _gatherTarget.GatherTickSeconds) return;
            _gatherTimer = 0f;

            if (!_gatherTarget.TryHarvest(out int amt)) return;
            var inv = GetComponent<WorkerInventory>();
            if (inv == null) inv = gameObject.AddComponent<WorkerInventory>();
            inv.Carrying += amt;

            _gatherTarget = null;
            _order = UnitOrder.ReturnDeposit;
            var returnDest = FindNearestDepositPoint();
            if (returnDest.HasValue) SafeSetDestination(returnDest.Value);
        }

        // ──────────── Tick: Return Deposit ────────────

        void TickReturn()
        {
            var inv = GetComponent<WorkerInventory>();
            var depositDest = FindNearestDepositPoint();
            if (!depositDest.HasValue)
            {
                _order = UnitOrder.Idle;
                return;
            }
            if (!AgentReady) return;

            var nearestHive = FindNearestDepositTransform();
            float arrivalDist = nearestHive != null ? GetStructureRadius(nearestHive) + 1.5f : 3.5f;
            Vector3 hivePos = nearestHive != null ? nearestHive.position : depositDest.Value;
            var diff = transform.position - hivePos;
            diff.y = 0f;

            if (diff.magnitude > arrivalDist)
            {
                if (!_agent.pathPending && _agent.pathStatus == NavMeshPathStatus.PathInvalid)
                {
                    if (inv != null) inv.Carrying = 0;
                    _lastGatherTarget = null;
                    _order = UnitOrder.Idle;
                    return;
                }
                if (!_agent.pathPending && !_agent.hasPath)
                    SafeSetDestination(depositDest.Value);
                return;
            }

            SafeStopAgent();
            if (inv != null && inv.Carrying > 0)
            {
                if (team == Team.Player && PlayerResources.Instance != null)
                    PlayerResources.Instance.AddCalories(inv.Carrying);
                else if (team == Team.Enemy)
                    EnemyResources.AddCalories(inv.Carrying);
                inv.Carrying = 0;
            }

            if (_lastGatherTarget != null && !_lastGatherTarget.Depleted)
            {
                OrderGather(_lastGatherTarget);
                return;
            }
            _lastGatherTarget = null;
            _order = UnitOrder.Idle;
        }

        // ──────────── Tick: Patrol ────────────

        void TickPatrol()
        {
            if (!_patrolActive || !AgentReady) return;
            var dest = _patrolToB ? _patrolB : _patrolA;
            _agent.SetDestination(dest);
            if (_agent.pathPending) return;
            if (_agent.hasPath && _agent.remainingDistance <= _agent.stoppingDistance + 0.35f)
                _patrolToB = !_patrolToB;
        }

        // ──────────── Tick: Build ────────────

        void TickBuild()
        {
            if (_buildTarget == null || _buildTarget.State == BuildingState.Destroyed)
            {
                ClearTargets();
                _order = UnitOrder.Idle;
                return;
            }
            if (_buildTarget.State == BuildingState.Active)
            {
                ClearTargets();
                _order = UnitOrder.Idle;
                return;
            }
            if (!AgentReady) return;

            var diff = transform.position - _buildTarget.transform.position;
            diff.y = 0f;
            if (diff.magnitude > _buildTarget.BuildRange)
            {
                if (!_agent.pathPending && !_agent.hasPath)
                    SafeSetDestination(_buildTarget.transform.position);
                return;
            }
            SafeStopAgent();
            _buildTarget.ContributeConstruction(Time.deltaTime);
        }

        // ──────────── Tick: Attack Structure ────────────

        void TickAttackStructure()
        {
            Transform target = null;
            bool alive = false;

            if (_buildingAttackTarget != null)
            {
                target = _buildingAttackTarget.transform;
                alive = _buildingAttackTarget.IsAlive;
            }
            else if (_hiveAttackTarget != null)
            {
                target = _hiveAttackTarget.transform;
                alive = _hiveAttackTarget.IsAlive;
            }

            if (target == null || !alive)
            {
                ResumeAfterCombat();
                return;
            }
            if (!AgentReady) return;

            float dist = Vector3.Distance(transform.position, target.position);
            float range = definition != null ? definition.attackRange + 2f : 4f;

            if (dist > range)
            {
                RestoreAgentControl();
                if (!_agent.pathPending && !_agent.hasPath)
                    SafeSetDestination(target.position);
                return;
            }

            if (AgentReady)
            {
                _agent.isStopped = true;
                _agent.ResetPath();
                _agent.velocity = Vector3.zero;
                if (definition == null || definition.archetype != UnitArchetype.BasicRanged)
                {
                    _agent.updatePosition = false;
                    _agent.updateRotation = false;
                }
            }

            if (_attackCooldown > 0f) return;
            _attackCooldown = definition != null ? definition.attackCooldown : 1f;

            var animDriver = GetComponent<UnitAnimationDriver>();
            if (definition != null && definition.archetype == UnitArchetype.BasicRanged)
            {
                var lookDir = target.position - transform.position;
                lookDir.y = 0f;
                var origin = animDriver != null ? animDriver.GetSprayOrigin() : transform.position + Vector3.up * 0.45f;
                SprayAttack.Fire(origin, lookDir.normalized, definition.attackRange, team, definition.attackDamage, null);
            }
            animDriver?.NotifyAttack();

            float dmg = definition != null ? definition.attackDamage : 10f;
            if (_buildingAttackTarget != null)
                _buildingAttackTarget.ApplyDamage(dmg);
            else if (_hiveAttackTarget != null)
                _hiveAttackTarget.ApplyDamage(dmg);
        }

        void ResumeAfterCombat()
        {
            _attackTarget = null;
            _buildingAttackTarget = null;
            _hiveAttackTarget = null;
            RestoreAgentControl();
            if (_wantsAttackMove)
            {
                _order = UnitOrder.Move;
                SafeSetDestination(_attackMoveDest);
                return;
            }
            _order = UnitOrder.Idle;
        }

        // ──────────── Terrain Effects ────────────

        void TickTerrainEffects()
        {
            _terrainSpeedTimer -= Time.deltaTime;
            if (_terrainSpeedTimer <= 0f)
            {
                _terrainSpeedTimer = 0.15f;
                float baseSpeed = definition != null ? definition.moveSpeed : 4.5f;
                float mult = TerrainFeatureRegistry.GetSpeedMultiplier(transform.position);
                if (_agent != null) _agent.speed = baseSpeed * mult;
            }

            float dps = TerrainFeatureRegistry.GetDamagePerSecond(transform.position);
            if (dps > 0f)
            {
                _terrainDmgAccum += dps * Time.deltaTime;
                if (_terrainDmgAccum >= 1f)
                {
                    float dmg = Mathf.Floor(_terrainDmgAccum);
                    _terrainDmgAccum -= dmg;
                    ApplyEnvironmentDamage(dmg);
                }
            }
        }

        // ──────────── Damage and Death ────────────

        public void ApplyDamage(float dmg) => ApplyDamage(dmg, null);

        public void ApplyDamage(float dmg, InsectUnit attacker)
        {
            if (_health <= 0f) return;
            _health -= dmg;
            LastDamageTime = Time.time;
            GameAudio.PlayCombatHit(transform.position);
            if (_health <= 0f)
            {
                _health = 0f;
                Die();
                return;
            }

            if (attacker != null && attacker.IsAlive
                && Archetype != UnitArchetype.Worker
                && (_order == UnitOrder.Idle || _order == UnitOrder.Patrol)
                && !_wantsAttackMove)
            {
                OrderAttack(attacker);
            }
        }

        public void ApplyEnvironmentDamage(float dmg)
        {
            if (_health <= 0f) return;
            _health -= dmg;
            if (_health <= 0f)
            {
                _health = 0f;
                Die();
            }
        }

        void Die()
        {
            RestoreAgentControl();
            SafeStopAgent();
            SelectionController.Instance?.Deselect(this);
            var drv = GetComponent<UnitAnimationDriver>();
            if (drv != null)
                drv.NotifyDeath(0.48f);
            else
                Destroy(gameObject, 0.15f);
        }

        // ──────────── Deposit Helpers ────────────

        Vector3? FindNearestDepositPoint()
        {
            float bestDist = float.MaxValue;
            Vector3? bestPoint = null;

            var teamHive = team == Team.Player ? HiveDeposit.PlayerHive : HiveDeposit.EnemyHive;
            if (teamHive != null)
            {
                float d = Vector3.Distance(transform.position, teamHive.transform.position);
                if (d < bestDist) { bestDist = d; bestPoint = teamHive.GetDepositPoint(transform.position); }
            }

            foreach (var bld in ProductionBuilding.All)
            {
                if (bld == null || bld.Team != team) continue;
                if (bld.Type != BuildingType.AntNest && bld.Type != BuildingType.RootCellar) continue;
                if (!bld.IsOperational) continue;
                float d = Vector3.Distance(transform.position, bld.transform.position);
                if (d < bestDist) { bestDist = d; bestPoint = bld.transform.position; }
            }
            return bestPoint;
        }

        Transform FindNearestDepositTransform()
        {
            float bestDist = float.MaxValue;
            Transform best = null;

            var teamHive = team == Team.Player ? HiveDeposit.PlayerHive : HiveDeposit.EnemyHive;
            if (teamHive != null)
            {
                float d = Vector3.Distance(transform.position, teamHive.transform.position);
                if (d < bestDist) { bestDist = d; best = teamHive.transform; }
            }

            foreach (var bld in ProductionBuilding.All)
            {
                if (bld == null || bld.Team != team) continue;
                if (bld.Type != BuildingType.AntNest && bld.Type != BuildingType.RootCellar) continue;
                if (!bld.IsOperational) continue;
                float d = Vector3.Distance(transform.position, bld.transform.position);
                if (d < bestDist) { bestDist = d; best = bld.transform; }
            }
            return best;
        }

        static float GetStructureRadius(Transform structure)
        {
            var rend = structure.GetComponentInChildren<Renderer>();
            if (rend != null)
                return Mathf.Max(rend.bounds.extents.x, rend.bounds.extents.z);
            return Mathf.Max(structure.localScale.x, structure.localScale.z) * 0.5f;
        }

        // ──────────── Selection Ring ────────────

        static void EnsureSharedSelectionRing()
        {
            if (s_sharedRingMesh != null) return;
            const int segments = 48;
            const float inner = 0.55f;
            const float outer = 0.65f;
            var verts = new Vector3[segments * 2];
            var tris = new int[segments * 6];
            for (int i = 0; i < segments; i++)
            {
                float a = Mathf.PI * 2f * i / segments;
                float c = Mathf.Cos(a), sn = Mathf.Sin(a);
                verts[i * 2] = new Vector3(c * inner, sn * inner, 0f);
                verts[i * 2 + 1] = new Vector3(c * outer, sn * outer, 0f);
                int next = (i + 1) % segments;
                int ti = i * 6;
                tris[ti] = i * 2;
                tris[ti + 1] = next * 2;
                tris[ti + 2] = i * 2 + 1;
                tris[ti + 3] = next * 2;
                tris[ti + 4] = next * 2 + 1;
                tris[ti + 5] = i * 2 + 1;
            }
            s_sharedRingMesh = new Mesh { name = "IW_SharedSelectionRing" };
            s_sharedRingMesh.vertices = verts;
            s_sharedRingMesh.triangles = tris;
            s_sharedRingMesh.RecalculateNormals();

            var sh = Shader.Find("Sprites/Default");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Unlit");
            s_sharedRingMaterial = new Material(sh);
            var col = new Color(0.3f, 1f, 0.5f, 0.7f);
            if (s_sharedRingMaterial.HasProperty("_Color")) s_sharedRingMaterial.color = col;
            if (s_sharedRingMaterial.HasProperty("_BaseColor")) s_sharedRingMaterial.SetColor("_BaseColor", col);
        }

        void SyncSelectionRing()
        {
            if (team != Team.Player) return;
            if (_selectionRing == null)
            {
                EnsureSharedSelectionRing();
                _selectionRing = new GameObject("SelectionRing");
                _selectionRing.transform.SetParent(transform, false);
                _selectionRing.transform.localPosition = new Vector3(0f, 0.07f, 0f);
                _selectionRing.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                var mf = _selectionRing.AddComponent<MeshFilter>();
                mf.sharedMesh = s_sharedRingMesh;
                var mr = _selectionRing.AddComponent<MeshRenderer>();
                mr.sharedMaterial = s_sharedRingMaterial;
                _selectionRing.SetActive(false);
            }
            bool show = IsSelected && IsAlive;
            if (_selectionRing.activeSelf != show)
                _selectionRing.SetActive(show);
        }
    }

    // ──────────── Worker Inventory ────────────

    public class WorkerInventory : MonoBehaviour
    {
        public int Carrying;
        public CargoType Cargo;
        GameObject _cargoVisual;
        CargoType _visualCargo;
        Transform _headBone;
        bool _boneSearched;

        Transform FindBoneRecursive(Transform parent, string boneName)
        {
            if (parent.name.IndexOf(boneName, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return parent;
            foreach (Transform child in parent)
            {
                var found = FindBoneRecursive(child, boneName);
                if (found != null) return found;
            }
            return null;
        }

        Transform FindHeadBone()
        {
            if (_boneSearched) return _headBone;
            _boneSearched = true;
            var visual = transform.Find("Visual");
            if (visual != null)
                _headBone = FindBoneRecursive(visual, "headend")
                         ?? FindBoneRecursive(visual, "head");
            return _headBone;
        }

        void LateUpdate()
        {
            bool show = Carrying > 0;
            if (show && (_cargoVisual == null || _visualCargo != Cargo))
            {
                if (_cargoVisual != null) Destroy(_cargoVisual);

                var lib = MapDirector.ActiveVisualLibrary;
                if (lib != null && lib.calorieChunkPrefab != null)
                {
                    _cargoVisual = Instantiate(lib.calorieChunkPrefab, transform);
                }
                else
                {
                    _cargoVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Destroy(_cargoVisual.GetComponent<Collider>());
                    _cargoVisual.transform.SetParent(transform, false);
                    var r = _cargoVisual.GetComponent<Renderer>();
                    var sh = Shader.Find("Universal Render Pipeline/Lit");
                    if (sh == null) sh = Shader.Find("Standard");
                    var m = new Material(sh);
                    var col = new Color(0.95f, 0.85f, 0.15f);
                    m.color = col;
                    if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
                    r.sharedMaterial = m;
                }
                _cargoVisual.name = "CargoVisual";
                _cargoVisual.transform.localScale = Vector3.one * 0.15f;
                _visualCargo = Cargo;
            }

            if (_cargoVisual != null)
            {
                _cargoVisual.SetActive(show);
                if (show)
                {
                    var head = FindHeadBone();
                    if (head != null)
                        _cargoVisual.transform.position = head.position + transform.forward * 0.08f + Vector3.down * 0.02f;
                    else
                        _cargoVisual.transform.localPosition = new Vector3(0f, 0.35f, 0.55f);
                }
            }
        }
    }
}
