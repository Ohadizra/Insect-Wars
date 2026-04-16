using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using InsectWars.Data;

namespace InsectWars.RTS
{
    public enum UnitOrder
    {
        Idle,
        Move,
        Attack,
        AttackMove,
        AttackBuilding,
        AttackHive,
        Gather,
        ReturnDeposit,
        Patrol,
        Build
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    public class InsectUnit : MonoBehaviour
    {
        [Header("State")]
        [SerializeField] Team team = Team.Neutral;
        [SerializeField] UnitDefinition definition;
        [SerializeField] bool isSelected;
        [SerializeField] UnitOrder currentOrder = UnitOrder.Idle;

        [Header("Health")]
        [SerializeField] float currentHealth;
        [SerializeField] float maxHealth;

        NavMeshAgent _agent;
        bool _isAlive = true;

        // Command targets
        InsectUnit _attackTarget;
        ProductionBuilding _attackBuilding;
        HiveDeposit _attackHive;
        RottingFruitNode _gatherTarget;
        Vector3 _moveTarget;
        Vector3 _patrolA, _patrolB;
        bool _patrollingToB;

        public Team Team => team;
        public UnitDefinition Definition => definition;
        public UnitArchetype Archetype => definition != null ? definition.archetype : UnitArchetype.Worker;
        public bool IsAlive => _isAlive;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public UnitOrder CurrentOrder => currentOrder;
        public InsectUnit AttackTarget => _attackTarget;
        public NavMeshAgent Agent => _agent;

        public bool IsSelected
        {
            get => isSelected;
            set
            {
                isSelected = value;
                var circle = transform.Find("SelectionCircle");
                if (circle != null) circle.gameObject.SetActive(value);
            }
        }

        // New members based on compilation errors
        public float LastDamageTime { get; private set; }
        public bool IsCloaked { get; set; }
        public bool IsAirborne { get; set; }
        public int HealAuraFrame { get; set; }
        public Vector3 position => transform.position;

        void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            if (definition != null)
            {
                maxHealth = definition.maxHealth;
                currentHealth = maxHealth;
            }
            LastDamageTime = -999f;
        }

        void Start()
        {
            RtsSimRegistry.Register(this);
            IsSelected = isSelected;
        }

        void OnDestroy()
        {
            RtsSimRegistry.Unregister(this);
        }

        public void Configure(Team t, UnitDefinition def)
        {
            team = t;
            definition = def;
            maxHealth = def != null ? def.maxHealth : 40f;
            currentHealth = maxHealth;
            if (_agent != null)
            {
                _agent.speed = def != null ? def.moveSpeed : 4.5f;
                _agent.acceleration = (def != null ? def.moveSpeed : 4.5f) * 3f;
                _agent.angularSpeed = 600f;
                _agent.stoppingDistance = 0.5f;
            }
        }

        void Update()
        {
            if (!_isAlive || !Application.isPlaying) return;

            switch (currentOrder)
            {
                case UnitOrder.Move:
                    if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
                        currentOrder = UnitOrder.Idle;
                    break;
                case UnitOrder.Attack:
                    UpdateAttack();
                    break;
                case UnitOrder.AttackMove:
                    UpdateAttackMove();
                    break;
                case UnitOrder.AttackBuilding:
                    UpdateAttackBuilding();
                    break;
                case UnitOrder.AttackHive:
                    UpdateAttackHive();
                    break;
                case UnitOrder.Gather:
                    UpdateGather();
                    break;
                case UnitOrder.ReturnDeposit:
                    UpdateReturn();
                    break;
                case UnitOrder.Patrol:
                    UpdatePatrol();
                    break;
            }
        }

        // --- Public Commands ---

        public void OrderMove(Vector3 dest)
        {
            SetOrder(UnitOrder.Move);
            _moveTarget = dest;
            if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh)
            {
                _agent.isStopped = false;
                _agent.SetDestination(dest);
            }
        }

        public void OrderAttack(InsectUnit target)
        {
            SetOrder(UnitOrder.Attack);
            _attackTarget = target;
        }

        public void OrderAttackMove(Vector3 dest)
        {
            SetOrder(UnitOrder.AttackMove);
            _moveTarget = dest;
            if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh)
            {
                _agent.isStopped = false;
                _agent.SetDestination(dest);
            }
        }

        public void OrderAttackBuilding(ProductionBuilding target)
        {
            SetOrder(UnitOrder.AttackBuilding);
            _attackBuilding = target;
        }

        public void OrderAttackHive(HiveDeposit target)
        {
            SetOrder(UnitOrder.AttackHive);
            _attackHive = target;
        }

        public void OrderGather(RottingFruitNode target)
        {
            SetOrder(UnitOrder.Gather);
            _gatherTarget = target;
        }

        public void OrderPatrol(Vector3 a, Vector3 b)
        {
            SetOrder(UnitOrder.Patrol);
            _patrolA = a;
            _patrolB = b;
            _patrollingToB = true;
            if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh)
            {
                _agent.isStopped = false;
                _agent.SetDestination(b);
            }
        }

        public void OrderBuild(ProductionBuilding target)
        {
            SetOrder(UnitOrder.Build);
            if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh)
            {
                _agent.isStopped = false;
                _agent.SetDestination(target.transform.position);
            }
        }

        public void OrderHoldPosition()
        {
            SetOrder(UnitOrder.Idle);
            if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh) _agent.isStopped = true;
        }

        public void OrderStop()
        {
            SetOrder(UnitOrder.Idle);
            if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh) _agent.isStopped = true;
        }

        public void ApplyDamage(float amount, InsectUnit attacker = null)
{
            if (!_isAlive) return;
            currentHealth -= amount;
            LastDamageTime = Time.time;
            if (currentHealth <= 0) Die();
        }

        public void Heal(float amount)
        {
            if (!_isAlive) return;
            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        }

        public void TakeDamage(float amount, InsectUnit attacker)
        {
            ApplyDamage(amount, attacker);
        }

        void Die()
        {
            _isAlive = false;
            currentOrder = UnitOrder.Idle;
            if (_agent != null) _agent.enabled = false;
        }

        void SetOrder(UnitOrder order)
        {
            currentOrder = order;
            _attackTarget = null;
            _attackBuilding = null;
            _attackHive = null;
            _gatherTarget = null;
            if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh) _agent.isStopped = false;
        }

        // --- Logic Loops ---

        void UpdateAttack()
        {
            if (_attackTarget == null || !_attackTarget.IsAlive)
            {
                currentOrder = UnitOrder.Idle;
                if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh) _agent.isStopped = true;
                return;
            }

            float dist = Vector3.Distance(transform.position, _attackTarget.transform.position);
            float range = definition != null ? definition.attackRange : 2f;
            
            if (dist > range * 0.9f)
            {
                if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh)
                {
                    _agent.isStopped = false;
                    _agent.SetDestination(_attackTarget.transform.position);
                }
            }
            else
            {
                if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh) _agent.isStopped = true;
            }
        }

        void UpdateAttackMove()
        {
            InsectUnit nearest = FindNearestEnemy(12f);
            if (nearest != null)
            {
                OrderAttack(nearest);
                return;
            }

            if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh)
            {
                if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
                    currentOrder = UnitOrder.Idle;
            }
        }

        void UpdateAttackBuilding()
        {
            if (_attackBuilding == null || !_attackBuilding.IsAlive)
            {
                currentOrder = UnitOrder.Idle;
                return;
            }
            float dist = Vector3.Distance(transform.position, _attackBuilding.transform.position);
            if (dist > 3f)
            {
                if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh) _agent.SetDestination(_attackBuilding.transform.position);
            }
            else
            {
                if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh) _agent.isStopped = true;
            }
        }

        void UpdateAttackHive()
        {
            if (_attackHive == null || !_attackHive.IsAlive)
            {
                currentOrder = UnitOrder.Idle;
                return;
            }
            float dist = Vector3.Distance(transform.position, _attackHive.transform.position);
            if (dist > 6f)
            {
                if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh) _agent.SetDestination(_attackHive.transform.position);
            }
            else
            {
                if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh) _agent.isStopped = true;
            }
        }

        void UpdateGather()
        {
            if (_gatherTarget == null || _gatherTarget.Depleted)
            {
                currentOrder = UnitOrder.Idle;
                return;
            }
            float dist = Vector3.Distance(transform.position, _gatherTarget.transform.position);
            if (dist > 2f)
            {
                if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh) _agent.SetDestination(_gatherTarget.transform.position);
            }
            else
            {
                if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh) _agent.isStopped = true;
            }
        }

        void UpdateReturn()
        {
            var hive = team == Team.Player ? HiveDeposit.PlayerHive : HiveDeposit.EnemyHive;
            if (hive == null) { currentOrder = UnitOrder.Idle; return; }
            float dist = Vector3.Distance(transform.position, hive.transform.position);
            if (dist > 4f)
            {
                if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh) _agent.SetDestination(hive.transform.position);
            }
            else
            {
                if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh) _agent.isStopped = true;
            }
        }

        void UpdatePatrol()
        {
            if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh)
            {
                if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
                {
                    _patrollingToB = !_patrollingToB;
                    _agent.SetDestination(_patrollingToB ? _patrolB : _patrolA);
                }
            }
        }

        InsectUnit FindNearestEnemy(float radius)
        {
            InsectUnit best = null;
            float bestDist = radius;
            foreach (var u in RtsSimRegistry.Units)
            {
                if (u == null || !u.IsAlive || u.Team == team || u.Team == Team.Neutral) continue;
                float d = Vector3.Distance(transform.position, u.transform.position);
                if (d < bestDist) { bestDist = d; best = u; }
            }
            return best;
        }
    }
}
