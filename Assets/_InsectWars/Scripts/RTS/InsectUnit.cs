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

        void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            if (s_unitsLayer < 0)
                s_unitsLayer = LayerMask.NameToLayer("Units");
        }

        void LateUpdate()
        {
            SyncSelectionRing();
        }

        void SyncSelectionRing()
        {
            if (team != Team.Player) return;
            if (_selectionRing == null)
            {
                _selectionRing = new GameObject("SelectionRing");
                _selectionRing.transform.SetParent(transform, false);
                _selectionRing.transform.localPosition = new Vector3(0f, 0.07f, 0f);
                _selectionRing.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

                var mf = _selectionRing.AddComponent<MeshFilter>();
                mf.sharedMesh = BuildRingMesh(0.55f, 0.65f, 48);
                var mr = _selectionRing.AddComponent<MeshRenderer>();

                var sh = Shader.Find("Sprites/Default");
                if (sh == null) sh = Shader.Find("Universal Render Pipeline/Unlit");
                var m = new Material(sh);
                var col = new Color(0.3f, 1f, 0.5f, 0.7f);
                if (m.HasProperty("_Color")) m.color = col;
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
                mr.sharedMaterial = m;
                _selectionRing.SetActive(false);
            }
            var show = IsSelected && IsAlive;
            if (_selectionRing.activeSelf != show)
                _selectionRing.SetActive(show);
        }

        static Mesh BuildRingMesh(float inner, float outer, int segments)
        {
            var verts = new Vector3[segments * 2];
            var tris = new int[segments * 6];
            for (int i = 0; i < segments; i++)
            {
                float a = Mathf.PI * 2f * i / segments;
                float c = Mathf.Cos(a), s = Mathf.Sin(a);
                verts[i * 2] = new Vector3(c * inner, s * inner, 0f);
                verts[i * 2 + 1] = new Vector3(c * outer, s * outer, 0f);
                int next = (i + 1) % segments;
                int ti = i * 6;
                tris[ti] = i * 2;
                tris[ti + 1] = next * 2;
                tris[ti + 2] = i * 2 + 1;
                tris[ti + 3] = next * 2;
                tris[ti + 4] = next * 2 + 1;
                tris[ti + 5] = i * 2 + 1;
            }
            var mesh = new Mesh { vertices = verts, triangles = tris };
            mesh.RecalculateNormals();
            return mesh;
        }

        void Start()
        {
            if (definition == null)
                definition = UnitDefinition.CreateRuntimeDefault(UnitArchetype.Worker,
                    TeamPalette.UnitBody(team, UnitArchetype.Worker));
            ApplyDefinition();
            _health = definition.maxHealth;
            if (team == Team.Enemy)
                _health *= GameSession.DifficultyEnemyHpMultiplier;
            if (GetComponent<UnitHealthBar>() == null)
                gameObject.AddComponent<UnitHealthBar>();
        }

        public void Configure(Team t, UnitDefinition def)
        {
            team = t;
            definition = def;
            ApplyDefinition();
            _health = definition.maxHealth;
            if (team == Team.Enemy)
                _health *= GameSession.DifficultyEnemyHpMultiplier;
            if (GetComponent<UnitHealthBar>() == null)
                gameObject.AddComponent<UnitHealthBar>();
        }

        void ApplyDefinition()
        {
            if (definition == null) return;
            _agent.speed = definition.moveSpeed;
            _agent.stoppingDistance = definition.archetype == UnitArchetype.BasicRanged ? definition.attackRange * 0.85f : 1.2f;
        }

        void Update()
        {
            if (!IsAlive) return;
            _attackCooldown -= Time.deltaTime;
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
            _agent.isStopped = false;
            _agent.SetDestination(world);
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
            _agent.isStopped = false;
            _agent.SetDestination(world);
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
            _agent.isStopped = false;
        }

        public void OrderStop()
        {
            if (!IsAlive) return;
            ClearTargets();
            _wantsAttackMove = false;
            _holdPosition = false;
            _patrolActive = false;
            _order = UnitOrder.Idle;
            _agent.isStopped = true;
            _agent.ResetPath();
        }

        public void OrderHoldPosition()
        {
            if (!IsAlive) return;
            ClearTargets();
            _wantsAttackMove = false;
            _patrolActive = false;
            _holdPosition = true;
            _order = UnitOrder.Idle;
            _agent.isStopped = true;
            _agent.ResetPath();
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
            _agent.isStopped = false;
            _agent.SetDestination(_patrolB);
        }

        public void OrderGather(RottingFruitNode node)
        {
            if (!IsAlive || node == null || node.Depleted || definition == null || !definition.canGather) return;

            var inv = GetComponent<WorkerInventory>();
            if (inv != null && inv.Carrying > 0 && HiveDeposit.PlayerHive != null)
            {
                _lastGatherTarget = node;
                _gatherTarget = null;
                _wantsAttackMove = false;
                _holdPosition = false;
                _patrolActive = false;
                _order = UnitOrder.ReturnDeposit;
                _agent.ResetPath();
                _agent.isStopped = false;
                _agent.SetDestination(HiveDeposit.PlayerHive.DepositPoint);
                return;
            }

            ClearTargets();
            _wantsAttackMove = false;
            _holdPosition = false;
            _patrolActive = false;
            _gatherTarget = node;
            _lastGatherTarget = node;
            _order = UnitOrder.Gather;
            _agent.ResetPath();
            _agent.isStopped = false;
            _agent.SetDestination(node.transform.position);
        }

        public void OrderReturnToHive()
        {
            if (!IsAlive || HiveDeposit.PlayerHive == null) return;
            ClearTargets();
            _wantsAttackMove = false;
            _holdPosition = false;
            _patrolActive = false;
            _order = UnitOrder.ReturnDeposit;
            _agent.isStopped = false;
            _agent.SetDestination(HiveDeposit.PlayerHive.DepositPoint);
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
            _agent.updatePosition = true;
            _agent.Warp(transform.position);
        }

        void TickPatrol()
        {
            if (!_patrolActive) return;
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

            if (_agent.pathPending) return;
            if (!_wantsAttackMove && _agent.hasPath && _agent.remainingDistance <= _agent.stoppingDistance + 0.2f)
                _order = UnitOrder.Idle;
        }

        void TickAttackMoveScan()
        {
            if (!_wantsAttackMove || _order != UnitOrder.Move) return;
            if (s_unitsLayer < 0) return;
            var mask = 1 << s_unitsLayer;
            var cols = Physics.OverlapSphere(transform.position, 8f, mask, QueryTriggerInteraction.Ignore);
            InsectUnit best = null;
            var bestD = 8f;
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
            var diff = transform.position - _gatherTarget.transform.position;
            diff.y = 0f;
            if (diff.magnitude > _gatherTarget.GatherRange)
            {
                _agent.isStopped = false;
                _agent.SetDestination(_gatherTarget.transform.position);
                return;
            }
            _agent.isStopped = true;
            _gatherTimer += Time.deltaTime;
            if (_gatherTimer < _gatherTarget.GatherTickSeconds) return;
            _gatherTimer = 0f;
            if (!_gatherTarget.TryHarvest(out var amt)) return;
            var inv = GetComponent<WorkerInventory>();
            if (inv == null) inv = gameObject.AddComponent<WorkerInventory>();
            inv.Carrying += amt;
            _gatherTarget = null;
            _order = UnitOrder.ReturnDeposit;
            _agent.ResetPath();
            _agent.isStopped = false;
            if (HiveDeposit.PlayerHive != null)
                _agent.SetDestination(HiveDeposit.PlayerHive.DepositPoint);
        }

        void TickReturn()
        {
            if (HiveDeposit.PlayerHive == null)
            {
                _order = UnitOrder.Idle;
                return;
            }
            var dest = HiveDeposit.PlayerHive.DepositPoint;
            var diff = transform.position - dest;
            diff.y = 0f;
            if (diff.magnitude > 2f)
            {
                _agent.isStopped = false;
                _agent.SetDestination(dest);
                return;
            }
            _agent.isStopped = true;
            var inv = GetComponent<WorkerInventory>();
            if (inv != null && inv.Carrying > 0 && PlayerResources.Instance != null)
            {
                PlayerResources.Instance.AddCalories(inv.Carrying);
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

        void TickIdleAutoGather()
        {
            if (definition == null || !definition.canGather) return;
            _idleScanTimer -= Time.deltaTime;
            if (_idleScanTimer > 0f) return;
            _idleScanTimer = 0.5f;
            var cols = Physics.OverlapSphere(transform.position, 6f);
            RottingFruitNode best = null;
            float bestDist = float.MaxValue;
            foreach (var c in cols)
            {
                var node = c.GetComponent<RottingFruitNode>();
                if (node == null || node.Depleted) continue;
                var d = Vector3.Distance(transform.position, node.transform.position);
                if (d < bestDist) { bestDist = d; best = node; }
            }
            if (best != null) OrderGather(best);
        }

        void TickAttack()
        {
            if (_attackTarget == null)
            {
                ResumeAfterAttack();
                return;
            }
            var targetUnit = _attackTarget.GetComponent<InsectUnit>();
            if (targetUnit == null || !targetUnit.IsAlive)
            {
                ResumeAfterAttack();
                return;
            }
            var dist = Vector3.Distance(transform.position, _attackTarget.position);
            var range = definition.attackRange;
            bool isMelee = definition.archetype != UnitArchetype.BasicRanged;

            if (isMelee)
            {
                float leashRange = range * 1.5f;
                bool locked = _meleeLockedPos.HasValue;

                if (locked && dist <= leashRange)
                {
                    transform.position = _meleeLockedPos.Value;

                    var dir = _attackTarget.position - transform.position;
                    dir.y = 0f;
                    if (dir.sqrMagnitude > 0.001f)
                        transform.rotation = Quaternion.LookRotation(dir);

                    if (_attackCooldown <= 0f)
                    {
                        targetUnit.ApplyDamage(definition.attackDamage);
                        GetComponent<UnitAnimationDriver>()?.NotifyAttack();
                        _attackCooldown = definition.attackCooldown;
                    }
                    return;
                }

                if (locked)
                    UnlockMelee();

                if (dist <= range)
                {
                    _meleeLockedPos = transform.position;
                    _agent.isStopped = true;
                    _agent.ResetPath();
                    _agent.velocity = Vector3.zero;
                    _agent.updatePosition = false;
                    return;
                }

                _agent.isStopped = false;
                _agent.SetDestination(_attackTarget.position);
                return;
            }

            if (dist > range)
            {
                _agent.isStopped = false;
                _agent.SetDestination(_attackTarget.position);
                return;
            }

            _agent.isStopped = true;
            _agent.ResetPath();
            _agent.velocity = Vector3.zero;

            var rangedDir = _attackTarget.position - transform.position;
            rangedDir.y = 0f;
            if (rangedDir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(rangedDir), Time.deltaTime * 12f);

            if (_attackCooldown > 0) return;

            var lib = SkirmishDirector.ActiveVisualLibrary;
            var spd = lib != null ? lib.projectileSpeed : 38f;
            var life = lib != null ? lib.projectileMaxLifetime : 4f;
            var prefab = lib != null ? lib.projectilePrefab : null;
            var origin = GetComponent<UnitAnimationDriver>() != null
                ? GetComponent<UnitAnimationDriver>().GetProjectileSpawnPoint()
                : transform.position + Vector3.up * 0.45f;
            Projectile.SpawnHoming(origin, targetUnit, team, definition.attackDamage, spd, life, prefab);
            GetComponent<UnitAnimationDriver>()?.NotifyAttack();
            _attackCooldown = definition.attackCooldown;
        }

        void ResumeAfterAttack()
        {
            _attackTarget = null;
            UnlockMelee();
            if (_wantsAttackMove)
            {
                _order = UnitOrder.Move;
                _agent.isStopped = false;
                _agent.SetDestination(_attackMoveDest);
                return;
            }
            _order = UnitOrder.Idle;
        }

        public void ApplyDamage(float dmg)
        {
            if (_health <= 0f) return;
            _health -= dmg;
            LastDamageTime = Time.time;
            if (_health <= 0)
            {
                _health = 0;
                UnlockMelee();
                _agent.isStopped = true;
                SelectionController.Instance?.Deselect(this);
                var drv = GetComponent<UnitAnimationDriver>();
                if (drv != null)
                    drv.NotifyDeath(0.48f);
                else
                    Destroy(gameObject, 0.15f);
            }
        }
    }

    public class WorkerInventory : MonoBehaviour
    {
        public int Carrying;
        GameObject _cargoVisual;

        void LateUpdate()
        {
            bool show = Carrying > 0;
            if (show && _cargoVisual == null)
            {
                _cargoVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                _cargoVisual.name = "CargoVisual";
                _cargoVisual.transform.SetParent(transform, false);
                _cargoVisual.transform.localPosition = new Vector3(0f, 0.75f, -0.2f);
                _cargoVisual.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
                Destroy(_cargoVisual.GetComponent<Collider>());
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
            if (_cargoVisual != null && _cargoVisual.activeSelf != show)
                _cargoVisual.SetActive(show);
        }
    }
}
