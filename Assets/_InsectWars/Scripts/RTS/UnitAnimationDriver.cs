using InsectWars.Data;
using UnityEngine;
using UnityEngine.AI;

namespace InsectWars.RTS
{
    [DisallowMultipleComponent]
    public class UnitAnimationDriver : MonoBehaviour
    {
        static readonly int Speed = Animator.StringToHash("Speed");
        static readonly int IsMoving = Animator.StringToHash("IsMoving");
        static readonly int Attack = Animator.StringToHash("Attack");
        static readonly int Gathering = Animator.StringToHash("Gathering");
        static readonly int Death = Animator.StringToHash("Death");

        [SerializeField] Transform modelRoot;
        [SerializeField] Animator animator;
        [SerializeField] float turnSpeed = 540f;
        [SerializeField] float proceduralBobSpeed = 10f;
        [SerializeField] float proceduralBobAmp = 0.035f;

        NavMeshAgent _agent;
        InsectUnit _unit;
        Vector3 _baseLocalPos;
        Vector3 _baseScale;
        float _attackSquashT;
        bool _dying;

        void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _unit = GetComponent<InsectUnit>();
            if (modelRoot == null)
            {
                var v = transform.Find("Visual");
                modelRoot = v != null ? v : transform;
            }
            if (animator == null && modelRoot != null)
                animator = modelRoot.GetComponentInChildren<Animator>(true);
            if (modelRoot != null)
            {
                _baseLocalPos = modelRoot.localPosition;
                _baseScale = modelRoot.localScale;
            }
        }

        void Update()
        {
            if (_unit == null || !_unit.IsAlive || _dying)
            {
                if (modelRoot != null && _dying)
                    modelRoot.localScale = Vector3.Lerp(modelRoot.localScale, Vector3.zero, Time.deltaTime * 5f);
                return;
            }

            var vel = _agent != null ? _agent.velocity : Vector3.zero;
            var planar = new Vector3(vel.x, 0f, vel.z);
            var moving = planar.sqrMagnitude > 0.06f;

            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetFloat(Speed, planar.magnitude);
                animator.SetBool(IsMoving, moving);
                animator.SetBool(Gathering, _unit.CurrentOrder == UnitOrder.Gather && _agent != null && _agent.isStopped);
            }
            else if (modelRoot != null)
            {
                var bob = moving ? Mathf.Sin(Time.time * proceduralBobSpeed) * proceduralBobAmp : 0f;
                modelRoot.localPosition = _baseLocalPos + new Vector3(0f, bob, 0f);
            }

            if (_attackSquashT > 0f)
            {
                _attackSquashT -= Time.deltaTime;
                var s = 1f + 0.12f * Mathf.Sin(_attackSquashT * 25f);
                if (modelRoot != null)
                    modelRoot.localScale = Vector3.Scale(_baseScale, new Vector3(s, 1f / s, s));
            }
            else if (modelRoot != null)
                modelRoot.localScale = _baseScale;

            Vector3 face = Vector3.zero;
            if (moving)
                face = planar;
            else if (_unit.CurrentOrder == UnitOrder.Attack && _unit.AttackTarget != null)
            {
                var t = _unit.AttackTarget.position - transform.position;
                t.y = 0f;
                if (t.sqrMagnitude > 0.01f) face = t;
            }

            if (face.sqrMagnitude > 0.01f && modelRoot != null)
            {
                var q = Quaternion.LookRotation(face.normalized, Vector3.up);
                modelRoot.rotation = Quaternion.RotateTowards(modelRoot.rotation, q, turnSpeed * Time.deltaTime);
            }
        }

        public void NotifyAttack()
        {
            if (animator != null && animator.runtimeAnimatorController != null)
                animator.SetTrigger(Attack);
            _attackSquashT = 0.22f;
        }

        public void NotifyDeath(float destroyDelay = 0.45f)
        {
            if (_dying) return;
            _dying = true;
            if (animator != null && animator.runtimeAnimatorController != null)
                animator.SetTrigger(Death);
            Destroy(gameObject, destroyDelay);
        }

        public Vector3 GetProjectileSpawnPoint()
        {
            var fp = transform.Find("Visual/FirePoint");
            if (fp != null) return fp.position;
            if (modelRoot != null)
                return modelRoot.position + modelRoot.forward * 0.35f + Vector3.up * 0.25f;
            return transform.position + Vector3.up * 0.4f;
        }
    }
}
