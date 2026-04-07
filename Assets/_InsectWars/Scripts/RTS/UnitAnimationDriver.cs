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
        [SerializeField] float idlePulseSpeed = 2f;
        [SerializeField] float idlePulseAmp = 0.02f;

        NavMeshAgent _agent;
        InsectUnit _unit;
        Vector3 _baseLocalPos;
        Vector3 _baseScale;
        float _attackAnimT;
        float _idleT;
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
            _idleT = Random.value * 10f;
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
                // Procedural Movement Bob
                var bob = moving ? Mathf.Sin(Time.time * proceduralBobSpeed) * proceduralBobAmp : 0f;
                
                // Procedural Idle Breathing
                float idleBob = 0f;
                if (!moving && _attackAnimT <= 0f)
                {
                    _idleT += Time.deltaTime;
                    idleBob = Mathf.Sin(_idleT * idlePulseSpeed) * idlePulseAmp;
                }
                
                modelRoot.localPosition = _baseLocalPos + new Vector3(0f, bob + idleBob, 0f);
            }

            // Procedural Attack Animation (Lunge/Squash)
            if (_attackAnimT > 0f)
            {
                _attackAnimT -= Time.deltaTime;
                float progress = 1f - (_attackAnimT / 0.35f); 
                
                if (modelRoot != null)
                {
                    float lunge = Mathf.Sin(progress * Mathf.PI) * 0.45f;
                    float squash = 1f + 0.18f * Mathf.Sin(progress * Mathf.PI * 2f);
                    
                    modelRoot.localPosition += modelRoot.forward * lunge;
                    modelRoot.localScale = Vector3.Scale(_baseScale, new Vector3(squash, 1f / squash, squash));
                }
            }
            else if (modelRoot != null)
            {
                modelRoot.localScale = _baseScale;
            }

            Vector3 face = Vector3.zero;
            if (moving)
                face = planar;
            else if (_unit.CurrentOrder == UnitOrder.Attack && _unit.AttackTarget != null)
            {
                var t = _unit.AttackTarget.position - transform.position;
                t.y = 0f;
                if (t.sqrMagnitude > 0.01f) face = t;
            }

            if (modelRoot != null)
            {
                // Base rotation from movement/facing
                if (face.sqrMagnitude > 0.01f)
                {
                    var q = Quaternion.LookRotation(face.normalized, Vector3.up);
                    modelRoot.rotation = Quaternion.RotateTowards(modelRoot.rotation, q, turnSpeed * Time.deltaTime);
                }

                // Add procedural Idle twitch (additive)
                if (!moving && _attackAnimT <= 0f)
                {
                    float twitch = (Mathf.PerlinNoise(_idleT * 3f, 0f) - 0.5f) * 12f;
                    modelRoot.rotation *= Quaternion.Euler(0f, twitch, 0f);
                }
            }
            }

        public void NotifyAttack()
        {
            if (animator != null && animator.runtimeAnimatorController != null)
                animator.SetTrigger(Attack);
            _attackAnimT = 0.35f;
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
