using InsectWars.Data;
using UnityEngine;
using UnityEngine.AI;

namespace InsectWars.RTS
{
    [DisallowMultipleComponent]
    [ExecuteAlways]
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
        Quaternion _lookRotation;
        
        // Universal bone references for procedural animation
        Transform _lArm, _rArm, _chest, _head, _tail;
        Quaternion _lArmBase, _rArmBase, _chestBase, _headBase, _tailBase;
        
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
                _lookRotation = modelRoot.rotation;

                // Find Bones (Flexible for both Prefab and Primitives)
                _lArm = FindRecursive(modelRoot, "frontleg") ?? FindRecursive(modelRoot, "LeftArm");
                _rArm = FindRecursive(modelRoot, "R_frontleg") ?? FindRecursive(modelRoot, "RightArm");
                _chest = FindRecursive(modelRoot, "chest");
                _head = FindRecursive(modelRoot, "head");
                _tail = FindRecursive(modelRoot, "tail");

                if (_lArm != null) _lArmBase = _lArm.localRotation;
                if (_rArm != null) _rArmBase = _rArm.localRotation;
                if (_chest != null) _chestBase = _chest.localRotation;
                if (_head != null) _headBase = _head.localRotation;
                if (_tail != null) _tailBase = _tail.localRotation;
            }
            _idleT = Random.value * 10f;
        }

        Transform FindRecursive(Transform parent, string name)
        {
            if (parent.name.Contains(name)) return parent;
            foreach (Transform child in parent)
            {
                var found = FindRecursive(child, name);
                if (found != null) return found;
            }
            return null;
        }

        void Update()
        {
            if (_unit == null || !_unit.IsAlive || _dying)
            {
                if (Application.isPlaying && modelRoot != null && _dying)
                    modelRoot.localScale = Vector3.Lerp(modelRoot.localScale, Vector3.zero, Time.deltaTime * 5f);
                return;
            }

            var vel = _agent != null ? _agent.velocity : Vector3.zero;
            var planar = new Vector3(vel.x, 0f, vel.z);
            var moving = planar.sqrMagnitude > 0.06f;

            // Handle Animator parameters
            if (animator != null && animator.runtimeAnimatorController != null && Application.isPlaying)
            {
                animator.SetFloat(Speed, planar.magnitude);
                animator.SetBool(IsMoving, moving);
                animator.SetBool(Gathering, _unit.CurrentOrder == UnitOrder.Gather && _agent != null && _agent.isStopped);
            }

            // Handle rotation logic
            if (modelRoot != null)
            {
                Vector3 face = Vector3.zero;
                if (moving)
                    face = planar;
                else if (_unit.CurrentOrder == UnitOrder.Attack && _unit.AttackTarget != null)
                {
                    var t = _unit.AttackTarget.position - transform.position;
                    t.y = 0f;
                    if (t.sqrMagnitude > 0.01f) face = t;
                }

                if (face.sqrMagnitude > 0.01f)
                {
                    var q = Quaternion.LookRotation(face.normalized, Vector3.up);
                    _lookRotation = Quaternion.RotateTowards(_lookRotation, q, turnSpeed * Time.deltaTime);
                }

                // Smooth look-around Twitch
                float twitch = (!moving && _attackAnimT <= 0f) ? (Mathf.PerlinNoise(_idleT * 1.2f, 0f) - 0.5f) * 25f : 0f;
                modelRoot.rotation = _lookRotation * Quaternion.Euler(0f, twitch, 0f);
            }
        }

        void LateUpdate()
        {
            if (_unit == null || !_unit.IsAlive || _dying || modelRoot == null) return;

            var vel = _agent != null ? _agent.velocity : Vector3.zero;
            var planar = new Vector3(vel.x, 0f, vel.z);
            var moving = planar.sqrMagnitude > 0.06f;

            // Update timers
            float dt = Application.isPlaying ? Time.deltaTime : 0.016f; // Fallback for editor
            _idleT += dt;

            // 1. Procedural Movement Bob
            var bob = moving ? Mathf.Sin(Time.time * proceduralBobSpeed) * proceduralBobAmp : 0f;
            
            // 2. Procedural Idle Logic
            float idleBob = 0f;
            float breathScale = 1f;
            
            if (!moving && _attackAnimT <= 0f)
            {
                idleBob = Mathf.Sin(_idleT * idlePulseSpeed) * idlePulseAmp;
                breathScale = 1f + Mathf.Sin(_idleT * idlePulseSpeed * 0.5f) * 0.03f;

                float sway = Mathf.Sin(_idleT * 1.2f);
                float microTwitch = (Mathf.PerlinNoise(_idleT * 8f, 0f) - 0.5f) * 4f;
                
                // Overlay bone rotations
                if (_chest != null)
                    _chest.localRotation *= Quaternion.Euler(sway * 5f, sway * 8f, 0f);
                
                if (_head != null)
                    _head.localRotation *= Quaternion.Euler(sway * -3f + microTwitch, sway * 12f, microTwitch);

                if (_lArm != null)
                    _lArm.localRotation *= Quaternion.Euler(Mathf.Sin(_idleT * 1.8f) * 10f + microTwitch, 0f, sway * 5f);
                
                if (_rArm != null)
                    _rArm.localRotation *= Quaternion.Euler(Mathf.Sin(_idleT * 1.8f) * -10f + microTwitch, 0f, sway * -5f);

                if (_tail != null)
                    _tail.localRotation *= Quaternion.Euler(Mathf.Sin(_idleT * 2.5f) * 6f, 0f, 0f);
            }
            
            modelRoot.localPosition = _baseLocalPos + new Vector3(0f, bob + idleBob, 0f);

            // 3. Attack Animation (Lunge/Squash)
            if (_attackAnimT > 0f)
            {
                _attackAnimT -= dt;
                float progress = 1f - (_attackAnimT / 0.35f); 
                float lunge = Mathf.Sin(progress * Mathf.PI) * 0.45f;
                float squash = 1f + 0.18f * Mathf.Sin(progress * Mathf.PI * 2f);
                
                modelRoot.localPosition += modelRoot.forward * lunge;
                modelRoot.localScale = Vector3.Scale(_baseScale, new Vector3(squash, 1f / squash, squash));

                if (_lArm != null)
                    _lArm.localRotation *= Quaternion.Euler(Mathf.Sin(progress * Mathf.PI) * -85f, 0f, 0f);
                if (_rArm != null)
                    _rArm.localRotation *= Quaternion.Euler(Mathf.Sin(progress * Mathf.PI) * -85f, 0f, 0f);
            }
            else
            {
                modelRoot.localScale = Vector3.Scale(_baseScale, new Vector3(breathScale, 1f, breathScale));
            }
        }

        void ResetBones(float speed)
        {
            if (_lArm != null) _lArm.localRotation = Quaternion.Lerp(_lArm.localRotation, _lArmBase, speed);
            if (_rArm != null) _rArm.localRotation = Quaternion.Lerp(_rArm.localRotation, _rArmBase, speed);
            if (_chest != null) _chest.localRotation = Quaternion.Lerp(_chest.localRotation, _chestBase, speed);
            if (_head != null) _head.localRotation = Quaternion.Lerp(_head.localRotation, _headBase, speed);
            if (_tail != null) _tail.localRotation = Quaternion.Lerp(_tail.localRotation, _tailBase, speed);
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
            if (Application.isPlaying) Destroy(gameObject, destroyDelay);
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
