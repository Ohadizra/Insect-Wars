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
        Patrol
    }

    public enum CargoType
    {
        Calories
    }

    [RequireComponent(typeof(NavMeshAgent))]
    public class InsectUnit : MonoBehaviour
    {
        [SerializeField] UnitDefinition definition;
        [SerializeField] Team team = Team.Player;

        NavMeshAgent _agent;
        float _health;
        float _attackCooldown;
        UnitOrder _order;
        Transform _attackTarget;
        RottingFruitNode _gatherTarget;
        RottingFruitNode _lastGatherTarget;
        float _gatherTimer;
        float _idleScanTimer;
        bool _holdPosition;
        bool _patrolActive;
        Vector3 _patrolA;
        Vector3 _patrolB;
        bool _patrolToB = true;
        bool _wantsAttackMove;
        Vector3 _attackMoveDest;
        Vector3? _meleeLockedPos;
        static int s_unitsLayer = -1;

        float _terrainSpeedTimer;
        float _terrainDmgAccum;

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

        GameObject _selectionRing;
        static Mesh s_sharedRingMesh;
        static Material s_sharedRingMaterial;

        void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            if (s_unitsLayer < 0)
                s_unitsLayer = LayerMask.NameToLayer("Units");
        }

        bool AgentActiveOnNavMesh => _agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh;

        void SafeSetDestination(Vector3 dest)
        {
            if (_agent == null) _agent = GetComponent<NavMeshAgent>();
            if (_agent == null) return;

            if (!_agent.enabled) _agent.enabled = true;

            // Move commands should always have small stopping distance to feel responsive
            if (_order == UnitOrder.Move || _order == UnitOrder.Idle)
                _agent.stoppingDistance = 0.5f; // Increased slightly for stability
            else if (_order == UnitOrder.Attack && definition != null)
                _agent.stoppingDistance = definition.archetype == UnitArchetype.BasicRanged ? definition.attackRange * 0.85f : 0.5f;

            if (!_agent.isOnNavMesh)
            {
                if (NavMesh.SamplePosition(transform.position, out var hit, 5f, NavMesh.AllAreas))
                    _agent.Warp(hit.position);
            }

            if (_agent.isOnNavMesh)
            {
                _agent.isStopped = false;
                _agent.SetDestination(dest);
            }
        }

        void SafeStopAgent()
        {
            if (_agent == null) _agent = GetComponent<NavMeshAgent>();
            if (_agent == null) return;

            if (!_agent.enabled) _agent.enabled = true;

            if (AgentActiveOnNavMesh)
            {
                _agent.isStopped = true;
                _agent.ResetPath();
            }
        }

        void OnEnable()
        {
            RtsSimRegistry.Register(this);
        }

        void OnDisable()
        {
            RtsSimRegistry.Unregister(this);
            UnlockMelee();
        }

        void LateUpdate()
        {
            SyncSelectionRing();
        }

        static void EnsureSharedSelectionRing()
        {
            if (s_sharedRingMesh != null) return;
            const int segments = 48;
            const float inner = 0.55f;
            const float outer = 0.65f;
            var verts = new Vector3[segments * 2];
            var tris = new int[segments * 6];
            for (var i = 0; i < segments; i++)
            {
                float a = Mathf.PI * 2f * i / segments;
                float c = Mathf.Cos(a), sn = Mathf.Sin(a);
                verts[i * 2] = new Vector3(c * inner, sn * inner, 0f);
                verts[i * 2 + 1] = new Vector3(c * outer, sn * outer, 0f);
                var next = (i + 1) % segments;
                var ti = i * 6;
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
            var show = IsSelected && IsAlive;
            if (_selectionRing.activeSelf != show)
                _selectionRing.SetActive(show);
        }

        void Start()
        {
            EnsureDefinition(Archetype);
            ApplyDefinition();
            // Health is already set in Configure() at spawn time, but let's ensure it's set if not already
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
            if (def != null)
                definition = def;
            else
                definition = UnitDefinition.CreateRuntimeDefault(archetype,
                    TeamPalette.UnitBody(team, archetype));
            
            if (_agent == null) _agent = GetComponent<NavMeshAgent>();
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
            if (_agent != null)
            {
                _agent.speed = definition.moveSpeed;
                // Units with radius > 0.45 fail to bind to default NavMesh (baked for 0.5 radius agents)
                if (_agent.radius > 0.45f) _agent.radius = 0.45f;
                // Melee units should try to get close enough for their radius. 
                _agent.stoppingDistance = definition.archetype == UnitArchetype.BasicRanged ? definition.attackRange * 0.85f : 0.5f;
            }
        }

        float GetEffectiveAttackRange()
        {
            if (definition == null) return 1.5f;
            if (_attackTarget == null) return definition.attackRange;

            float selfRadius = _agent != null ? _agent.radius : 0.4f;
            float targetRadius = 0.4f;

            var targetUnit = _attackTarget.GetComponent<InsectUnit>();
            if (targetUnit != null)
            {
                var targetAgent = targetUnit.GetComponent<NavMeshAgent>();
                if (targetAgent != null) targetRadius = targetAgent.radius;
                else
                {
                    var targetCol = targetUnit.GetComponent<CapsuleCollider>();
                    if (targetCol != null) targetRadius = targetCol.radius;
                }
            }
            else
            {
                var targetCol = _attackTarget.GetComponent<Collider>();
                if (targetCol != null)
                {
                    if (targetCol is CapsuleCollider cc) targetRadius = cc.radius;
                    else if (targetCol is BoxCollider bc) targetRadius = Mathf.Max(bc.size.x, bc.size.z) * 0.5f;
                    else if (targetCol is SphereCollider sc) targetRadius = sc.radius;
                }
            }

            // The attackRange in definition (e.g. 1.55) is assumed to be tuned for center-to-center 
            // with standard units (~0.4 radius). We convert it to a surface-to-surface range 
            // and then add the actual radii.
            float surfaceRange = Mathf.Max(0.1f, definition.attackRange - 0.8f);
            return surfaceRange + selfRadius + targetRadius;
        }

        void Update()
        {
            if (!IsAlive) return;
            _attackCooldown -= Time.deltaTime;
            TickTerrainEffects();
            switch (_order)
            {
                case UnitOrder.Gather:
                    TickGather();
                    break;
                case UnitOrder.ReturnDeposit:
                    TickReturn();
                    break;
                case UnitOrder.Attack:
                    TickAttack();
                    break;
                case UnitOrder.Move:
                    TickMove();
                    break;
                case UnitOrder.Patrol:
                    TickPatrol();
                    break;
                case UnitOrder.Idle:
                    TickIdleAutoGather();
                    break;
            }
        }

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
            var keepAm = keepAttackMoveIntent && _wantsAttackMove;
            var keepDest = _attackMoveDest;
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
            if (!IsAlive || node == null || node.Depleted || definition == null || !definition.canGather) return;

            var inv = GetComponent<WorkerInventory>();
            var depositDest = FindNearestTeamDepositPoint();
            if (inv != null && inv.Carrying > 0 && depositDest.HasValue)
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
            var dest = FindNearestTeamDepositPoint();
            if (!dest.HasValue) return;
            ClearTargets();
            _wantsAttackMove = false;
            _holdPosition = false;
            _patrolActive = false;
            _order = UnitOrder.ReturnDeposit;
            SafeSetDestination(dest.Value);
        }

        void ClearTargets()
        {
            _attackTarget = null;
            _gatherTarget = null;
            _lastGatherTarget = null;
            _gatherTimer = 0f;
            UnlockMelee();
        }

        void UnlockMelee()
        {
            if (_meleeLockedPos == null) return;
            _meleeLockedPos = null;
            if (_agent != null)
            {
                _agent.updatePosition = true;
                _agent.updateRotation = true;
            }
        }

        void TickTerrainEffects()
        {
            _terrainSpeedTimer -= Time.deltaTime;
            if (_terrainSpeedTimer <= 0f)
            {
                _terrainSpeedTimer = 0.15f;
                float baseSpeed = definition != null ? definition.moveSpeed : 4.5f;
                float mult = TerrainFeatureRegistry.GetSpeedMultiplier(transform.position);
                if (_agent != null)
                    _agent.speed = baseSpeed * mult;
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

        public void ApplyEnvironmentDamage(float dmg)
        {
            if (_health <= 0f) return;
            _health -= dmg;
            if (_health <= 0)
            {
                _health = 0;
                UnlockMelee();
                SafeStopAgent();
                SelectionController.Instance?.Deselect(this);
                var drv = GetComponent<UnitAnimationDriver>();
                if (drv != null)
                    drv.NotifyDeath(0.48f);
                else
                    Destroy(gameObject, 0.15f);
            }
        }

        void TickPatrol()
        {
            if (!_patrolActive || !AgentActiveOnNavMesh) return;
            var dest = _patrolToB ? _patrolB : _patrolA;
            _agent.SetDestination(dest);
            if (_agent.pathPending) return;
            if (_agent.hasPath && _agent.remainingDistance <= _agent.stoppingDistance + 0.35f)
                _patrolToB = !_patrolToB;
        }

        void TickMove()
        {
            if (_wantsAttackMove)
                TickAttackMoveScan();

            if (!AgentActiveOnNavMesh || _agent.pathPending) return;
            if (!_wantsAttackMove && _agent.hasPath && _agent.remainingDistance <= _agent.stoppingDistance + 0.2f)
                _order = UnitOrder.Idle;
        }

        void TickAttackMoveScan()
        {
            if (!_wantsAttackMove || _order != UnitOrder.Move) return;
            if (s_unitsLayer < 0) return;
            var mask = 1 << s_unitsLayer;
            var scan = definition != null ? definition.visionRadius : 12f;
            var cols = Physics.OverlapSphere(transform.position, scan, mask, QueryTriggerInteraction.Ignore);
            InsectUnit best = null;
            var bestD = scan;
            foreach (var c in cols)
            {
                var u = c.GetComponentInParent<InsectUnit>();
                if (u == null || !u.IsAlive || u.team == team) continue;
                var d = Vector3.Distance(transform.position, u.transform.position);
                if (d < bestD)
                {
                    bestD = d;
                    best = u;
                }
            }
            if (best != null)
                OrderAttack(best, true);
        }

        void TickGather()
        {
            if (_gatherTarget == null || _gatherTarget.Depleted)
            {
                _order = UnitOrder.Idle;
                return;
            }
            if (!AgentActiveOnNavMesh) return;
            var diff = transform.position - _gatherTarget.transform.position;
            diff.y = 0f;
            if (diff.magnitude > _gatherTarget.GatherRange)
            {
                SafeSetDestination(_gatherTarget.GetGatherPoint(transform.position));
                return;
            }
            SafeStopAgent();
            _gatherTimer += Time.deltaTime;
            if (_gatherTimer < _gatherTarget.GatherTickSeconds) return;
            _gatherTimer = 0f;
            if (!_gatherTarget.TryHarvest(out var amt)) return;
            var inv = GetComponent<WorkerInventory>();
            if (inv == null) inv = gameObject.AddComponent<WorkerInventory>();
            inv.Carrying += amt;
            _gatherTarget = null;
            _order = UnitOrder.ReturnDeposit;
            var returnDest = FindNearestTeamDepositPoint();
            if (returnDest.HasValue)
                SafeSetDestination(returnDest.Value);
        }

        void TickReturn()
        {
            var inv = GetComponent<WorkerInventory>();
            var depositDest = FindNearestTeamDepositPoint();
            if (!depositDest.HasValue)
            {
                _order = UnitOrder.Idle;
                return;
            }

            if (!AgentActiveOnNavMesh) return;

            var nearestHive = FindNearestTeamHive();
            float arrivalDist = nearestHive != null ? GetHiveArrivalRadius(nearestHive) : 3.5f;
            var hivePos = nearestHive != null ? nearestHive.position : depositDest.Value;
            var diff = transform.position - hivePos;
            diff.y = 0f;
            if (diff.magnitude > arrivalDist)
            {
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


        HiveDeposit GetTeamHive()
        {
            return team == Team.Player ? HiveDeposit.PlayerHive : HiveDeposit.EnemyHive;
        }

        Vector3? FindNearestTeamDepositPoint()
        {
            float bestDist = float.MaxValue;
            Vector3? bestPoint = null;

            HiveDeposit teamHive = team == Team.Player ? HiveDeposit.PlayerHive : HiveDeposit.EnemyHive;
            if (teamHive != null)
            {
                float d = Vector3.Distance(transform.position, teamHive.transform.position);
                if (d < bestDist) { bestDist = d; bestPoint = teamHive.GetDepositPoint(transform.position); }
            }

            foreach (var bld in ProductionBuilding.All)
            {
                if (bld == null || bld.Team != team) continue;
                if (bld.Type != BuildingType.AntNest && bld.Type != BuildingType.RootCellar) continue;
                float d = Vector3.Distance(transform.position, bld.transform.position);
                if (d < bestDist) { bestDist = d; bestPoint = bld.transform.position; }
            }

            return bestPoint;
        }

        Transform FindNearestTeamHive()
        {
            float bestDist = float.MaxValue;
            Transform best = null;

            HiveDeposit teamHive = team == Team.Player ? HiveDeposit.PlayerHive : HiveDeposit.EnemyHive;
            if (teamHive != null)
            {
                float d = Vector3.Distance(transform.position, teamHive.transform.position);
                if (d < bestDist) { bestDist = d; best = teamHive.transform; }
            }

            foreach (var bld in ProductionBuilding.All)
            {
                if (bld == null || bld.Team != team) continue;
                if (bld.Type != BuildingType.AntNest && bld.Type != BuildingType.RootCellar) continue;
                float d = Vector3.Distance(transform.position, bld.transform.position);
                if (d < bestDist) { bestDist = d; best = bld.transform; }
            }

            return best;
        }

        float GetHiveArrivalRadius(Transform hive)
        {
            var rend = hive.GetComponentInChildren<Renderer>();
            if (rend != null)
                return Mathf.Max(rend.bounds.extents.x, rend.bounds.extents.z) + 1.5f;
            return Mathf.Max(hive.localScale.x, hive.localScale.z) * 0.5f + 1.5f;
        }

        void TickIdleAutoGather()
        {
            if (definition == null || !definition.canGather) return;
            if (!AgentActiveOnNavMesh) return;
            _idleScanTimer -= Time.deltaTime;
            if (_idleScanTimer > 0f) return;
            _idleScanTimer = 0.5f;

            RottingFruitNode bestFruit = null;
            float bestFruitDist = float.MaxValue;

            foreach (var node in RtsSimRegistry.FruitNodes)
            {
                if (node == null || node.Depleted) continue;
                var d = Vector3.Distance(transform.position, node.transform.position);
                if (d < bestFruitDist) { bestFruitDist = d; bestFruit = node; }
            }

            if (bestFruit != null)
                OrderGather(bestFruit);
        }

        void TickAttack()
        {
            if (_attackTarget == null)
            {
                ResumeAfterAttack();
                return;
            }
            var targetUnit = _attackTarget.GetComponent<InsectUnit>();
            // Targets can be units or buildings (which might not have InsectUnit)
            bool targetAlive = targetUnit != null ? targetUnit.IsAlive : true; 
            if (!targetAlive)
            {
                ResumeAfterAttack();
                return;
            }

            var dist = Vector3.Distance(transform.position, _attackTarget.position);
            var range = GetEffectiveAttackRange();
            bool isMelee = definition.archetype != UnitArchetype.BasicRanged;

            if (isMelee)
            {
                if (dist <= range)
                {
                    _meleeLockedPos = transform.position;
                    if (AgentActiveOnNavMesh)
                    {
                        _agent.isStopped = true;
                        _agent.ResetPath();
                        _agent.velocity = Vector3.zero;
                        _agent.updatePosition = false;
                        _agent.updateRotation = false;
                    }

                    if (_attackCooldown <= 0f)
                    {
                        if (targetUnit != null)
                        {
                            targetUnit.ApplyDamage(definition.attackDamage, this);
                        }
                        GetComponent<UnitAnimationDriver>()?.NotifyAttack();
                        _attackCooldown = definition.attackCooldown;
                    }
                    return;
                }

                if (_meleeLockedPos.HasValue)
                    UnlockMelee();

                SafeSetDestination(_attackTarget.position);
                return;
            }

            if (dist > range)
            {
                SafeSetDestination(_attackTarget.position);
                return;
            }

            if (AgentActiveOnNavMesh)
            {
                _agent.isStopped = true;
                _agent.ResetPath();
                _agent.velocity = Vector3.zero;
            }

            var animDriver = GetComponent<UnitAnimationDriver>();

            if (_attackCooldown > 0) return;

            var lookDir = _attackTarget.position - transform.position;
            lookDir.y = 0f;
            var sprayOrigin = animDriver != null
                ? animDriver.GetSprayOrigin()
                : transform.position + Vector3.up * 0.45f;
            var sprayDir = lookDir.normalized;
            SprayAttack.Fire(sprayOrigin, sprayDir, definition.attackRange,
                team, definition.attackDamage, this);
            animDriver?.NotifyAttack();
            _attackCooldown = definition.attackCooldown;
        }

        void ResumeAfterAttack()
        {
            _attackTarget = null;
            UnlockMelee();
            if (_wantsAttackMove)
            {
                _order = UnitOrder.Move;
                SafeSetDestination(_attackMoveDest);
                return;
            }
            _order = UnitOrder.Idle;
        }

        public void ApplyDamage(float dmg) => ApplyDamage(dmg, null);

        public void ApplyDamage(float dmg, InsectUnit attacker)
        {
            if (_health <= 0f) return;
            _health -= dmg;
            LastDamageTime = Time.time;
            GameAudio.PlayCombatHit(transform.position);
            if (_health <= 0)
            {
                _health = 0;
                UnlockMelee();
                SafeStopAgent();
                SelectionController.Instance?.Deselect(this);
                var drv = GetComponent<UnitAnimationDriver>();
                if (drv != null)
                    drv.NotifyDeath(0.48f);
                else
                    Destroy(gameObject, 0.15f);
                return;
            }

            // Auto-retaliate: non-worker units fight back when hit while idle or patrolling
            if (attacker != null && attacker.IsAlive
                && Archetype != UnitArchetype.Worker
                && (_order == UnitOrder.Idle || _order == UnitOrder.Patrol)
                && !_wantsAttackMove)
            {
                OrderAttack(attacker);
            }
        }
    }

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
                    if (m.HasProperty("_BaseColor"))
                        m.SetColor("_BaseColor", col);
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
                    {
                        _cargoVisual.transform.position = head.position
                            + transform.forward * 0.08f
                            + Vector3.down * 0.02f;
                    }
                    else
                    {
                        _cargoVisual.transform.localPosition = new Vector3(0f, 0.35f, 0.55f);
                    }
                }
            }
        }
    }
    }
