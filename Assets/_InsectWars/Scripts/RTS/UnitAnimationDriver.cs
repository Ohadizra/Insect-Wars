using InsectWars.Data;
using UnityEngine;
using UnityEngine.AI;

namespace InsectWars.RTS
{
    /// <summary>
    /// Handles procedural animations for units, with a specific focus on high-quality 
    /// long-form idle behaviors for the Mantis (Fighter) unit.
    /// </summary>
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
        float _instanceOffset;
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
            _instanceOffset = Random.value * 100f;
            _idleT = _instanceOffset;
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

            if (animator != null && animator.runtimeAnimatorController != null && Application.isPlaying)
            {
                animator.SetFloat(Speed, planar.magnitude);
                animator.SetBool(IsMoving, moving);
                animator.SetBool(Gathering, _unit.CurrentOrder == UnitOrder.Gather && _agent != null && _agent.isStopped);
            }

            if (modelRoot != null)
            {
                Vector3 face = Vector3.zero;
                if (moving) face = planar;
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

                // Smooth look-around twitch (de-synchronized)
                float twitch = 0f;
                if (!moving && _attackAnimT <= 0f && _unit.Archetype == UnitArchetype.BasicFighter)
                {
                    twitch = (Mathf.PerlinNoise(_idleT * 0.8f, _instanceOffset) - 0.5f) * 30f;
                }
                modelRoot.rotation = _lookRotation * Quaternion.Euler(0f, twitch, 0f);
            }
        }

        void LateUpdate()
        {
            if (_unit == null || !_unit.IsAlive || _dying || modelRoot == null) return;

            var vel = _agent != null ? _agent.velocity : Vector3.zero;
            var planar = new Vector3(vel.x, 0f, vel.z);
            var moving = planar.sqrMagnitude > 0.06f;

            float dt = Application.isPlaying ? Time.deltaTime : 0.016f;
            _idleT += dt;

            // Simple bob for all
            var bob = moving ? Mathf.Sin(Time.time * proceduralBobSpeed) * proceduralBobAmp : 0f;
            modelRoot.localPosition = _baseLocalPos + new Vector3(0f, bob, 0f);

            // 30-Second Complex Idle Loop (Mantis Only)
            if (!moving && _attackAnimT <= 0f && _unit.Archetype == UnitArchetype.BasicFighter)
            {
                ApplyMantisLoop(dt);
            }
            else
            {
                modelRoot.localScale = _baseScale;
                ResetBones(dt * 5f);
            }

            // Attack Overlay
            if (_attackAnimT > 0f)
            {
                _attackAnimT -= dt;
                float p = 1f - (_attackAnimT / 0.35f);
                float lunge = Mathf.Sin(p * Mathf.PI) * 0.45f;
                float squash = 1f + 0.18f * Mathf.Sin(p * Mathf.PI * 2f);
                modelRoot.localPosition += modelRoot.forward * lunge;
                modelRoot.localScale = Vector3.Scale(_baseScale, new Vector3(squash, 1f / squash, squash));
                if (_lArm != null) _lArm.localRotation *= Quaternion.Euler(Mathf.Sin(p * Mathf.PI) * -85f, 0f, 0f);
                if (_rArm != null) _rArm.localRotation *= Quaternion.Euler(Mathf.Sin(p * Mathf.PI) * -85f, 0f, 0f);
            }
        }

        void ApplyMantisLoop(float dt)
        {
            float loopTime = _idleT % 30f;
            float breath = 1f + Mathf.Sin(_idleT * 1.8f) * 0.015f;
            modelRoot.localScale = Vector3.Scale(_baseScale, new Vector3(breath, 1f, breath));

            // Random micro-jitters applied to everything for "life"
            float jitterHead = (Mathf.PerlinNoise(_idleT * 6f, _instanceOffset) - 0.5f) * 5f;
            float jitterTail = (Mathf.PerlinNoise(_idleT * 4f, _instanceOffset + 50f) - 0.5f) * 15f;

            // Always have a slight tail wag (user liked tail movement)
            if (_tail != null)
                _tail.localRotation = _tailBase * Quaternion.Euler(Mathf.Sin(_idleT * 2f) * 8f + jitterTail, 0f, 0f);

            // 30-Second Life Cycle
            // 0-10s: Casual Look Around
            // 10-18s: Clean/Sharpen Left Scythe
            // 18-24s: Curious Tilt
            // 24-30s: Clean/Sharpen Right Scythe

            if (loopTime < 10f) // Casual Look Around
            {
                float lookX = (Mathf.PerlinNoise(_idleT * 0.5f, _instanceOffset) - 0.5f) * 40f;
                float lookY = (Mathf.PerlinNoise(_idleT * 0.5f, _instanceOffset + 100f) - 0.5f) * 30f;
                
                if (_head != null)
                    _head.localRotation = Quaternion.Slerp(_head.localRotation, _headBase * Quaternion.Euler(lookY, lookX, jitterHead), dt * 2f);
                if (_chest != null)
                    _chest.localRotation = Quaternion.Slerp(_chest.localRotation, _chestBase * Quaternion.Euler(lookY * 0.2f, lookX * 0.3f, 0f), dt * 1.5f);
                
                ResetBonesOnly(dt * 3f, arms: true);
            }
            else if (loopTime >= 10f && loopTime < 18f) // Clean Left Scythe
            {
                float p = Mathf.InverseLerp(10f, 18f, loopTime);
                float lift = Mathf.Sin(p * Mathf.PI);
                float scrub = Mathf.Sin(_idleT * 15f) * 10f; // Rapid sharpening motion
                
                if (_lArm != null)
                    _lArm.localRotation = Quaternion.Slerp(_lArm.localRotation, _lArmBase * Quaternion.Euler(-45f * lift + scrub, -25f * lift, 15f * lift), dt * 6f);
                if (_head != null)
                    _head.localRotation = Quaternion.Slerp(_head.localRotation, _headBase * Quaternion.Euler(25f * lift, -35f * lift, scrub * 0.5f), dt * 6f);
                
                ResetBonesOnly(dt * 2f, rightArm: true, chest: true);
            }
            else if (loopTime >= 18f && loopTime < 24f) // Curious Tilt
            {
                float p = Mathf.InverseLerp(18f, 24f, loopTime);
                float tilt = Mathf.Sin(p * Mathf.PI);
                
                if (_head != null)
                    _head.localRotation = Quaternion.Slerp(_head.localRotation, _headBase * Quaternion.Euler(-10f * tilt, 15f * tilt, 35f * tilt), dt * 3f);
                if (_chest != null)
                    _chest.localRotation = Quaternion.Slerp(_chest.localRotation, _chestBase * Quaternion.Euler(0f, 10f * tilt, 5f * tilt), dt * 2f);
                
                ResetBonesOnly(dt * 3f, arms: true);
            }
            else // Clean Right Scythe (24-30s)
            {
                float p = Mathf.InverseLerp(24f, 30f, loopTime);
                float lift = Mathf.Sin(p * Mathf.PI);
                float scrub = Mathf.Sin(_idleT * 15f) * 10f;
                
                if (_rArm != null)
                    _rArm.localRotation = Quaternion.Slerp(_rArm.localRotation, _rArmBase * Quaternion.Euler(-45f * lift + scrub, 25f * lift, -15f * lift), dt * 6f);
                if (_head != null)
                    _head.localRotation = Quaternion.Slerp(_head.localRotation, _headBase * Quaternion.Euler(25f * lift, 35f * lift, scrub * -0.5f), dt * 6f);
                
                ResetBonesOnly(dt * 2f, leftArm: true, chest: true);
            }
        }

        void ResetBones(float speed) => ResetBonesOnly(speed, true, true, true, true, true);

        void ResetBonesOnly(float speed, bool arms = false, bool chest = false, bool head = false, bool tail = false, bool leftArm = false, bool rightArm = false)
        {
            if ((arms || leftArm) && _lArm != null) _lArm.localRotation = Quaternion.Lerp(_lArm.localRotation, _lArmBase, speed);
            if ((arms || rightArm) && _rArm != null) _rArm.localRotation = Quaternion.Lerp(_rArm.localRotation, _rArmBase, speed);
            if (chest && _chest != null) _chest.localRotation = Quaternion.Lerp(_chest.localRotation, _chestBase, speed);
            if (head && _head != null) _head.localRotation = Quaternion.Lerp(_head.localRotation, _headBase, speed);
            if (tail && _tail != null) _tail.localRotation = Quaternion.Lerp(_tail.localRotation, _tailBase, speed);
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
