using System.Collections.Generic;
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
        static readonly int Build = Animator.StringToHash("Build");
        static readonly int Death = Animator.StringToHash("Death");
        static readonly int WebCast = Animator.StringToHash("WebCast");

        [SerializeField] Transform modelRoot;
        [SerializeField] Animator animator;
        [SerializeField] float turnSpeed = 540f;
        [SerializeField] float proceduralBobSpeed = 10f;
        [SerializeField] float proceduralBobAmp = 0.035f;
        [SerializeField] float idlePulseSpeed = 2f;
        [SerializeField] float idlePulseAmp = 0.02f;

        public float previewSpeed;

        NavMeshAgent _agent;
        InsectUnit _unit;
        Vector3 _baseLocalPos;
        Vector3 _baseScale;
        Quaternion _lookRotation;
        
        // Universal bone references for procedural animation
        Transform _lArm, _rArm, _chest, _head, _tail;
        Quaternion _lArmBase, _rArmBase, _chestBase, _headBase, _tailBase;
        
        float _attackAnimT;
        float _attackAnimDuration;
        float _buildAnimTimer; // Active while NotifyBuild() is called
        float _idleT;
        float _instanceOffset;
        bool _dying;

        // Parameter existence cache
        bool _hasSpeed, _hasIsMoving, _hasGathering, _hasBuild, _hasAttack, _hasDeath, _hasWebCast;

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
            
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                foreach (var p in animator.parameters)
                {
                    if (p.nameHash == Speed) _hasSpeed = true;
                    if (p.nameHash == IsMoving) _hasIsMoving = true;
                    if (p.nameHash == Gathering) _hasGathering = true;
                    if (p.nameHash == Build) _hasBuild = true;
                    if (p.nameHash == Attack) _hasAttack = true;
                    if (p.nameHash == Death) _hasDeath = true;
                    if (p.nameHash == WebCast) _hasWebCast = true;
                }
            }

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
                    modelRoot.localScale = Vector3.Lerp(modelRoot.localScale, Vector3.zero, Time.unscaledDeltaTime * 5f);
                return;
            }

            var vel = (_agent != null && _agent.enabled) ? _agent.velocity : transform.forward * previewSpeed;
            var planar = new Vector3(vel.x, 0f, vel.z);
            var moving = planar.sqrMagnitude > 0.001f;

            if (animator != null && animator.runtimeAnimatorController != null && Application.isPlaying)
            {
                if (_hasSpeed) animator.SetFloat(Speed, planar.magnitude);
                if (_hasIsMoving) animator.SetBool(IsMoving, moving);
                if (_hasGathering) animator.SetBool(Gathering, _unit.CurrentOrder == UnitOrder.Gather && _agent != null && _agent.isStopped);
                if (_hasBuild) animator.SetBool(Build, _buildAnimTimer > 0f);
            }

            if (modelRoot != null)
            {
                Vector3 face = Vector3.zero;
                if (moving) face = planar;
                else if (_unit.CurrentOrder == UnitOrder.Attack && _unit.AttackTarget != null)
                {
                    var t = _unit.AttackTarget.position - transform.position;
                    t.y = 0f;
                    if (t.sqrMagnitude > 0.01f)
                        face = _unit.Archetype == UnitArchetype.BasicRanged ? -t : t;
                }

                if (face.sqrMagnitude > 0.01f)
                {
                    var q = Quaternion.LookRotation(face.normalized, Vector3.up);
                    _lookRotation = Quaternion.RotateTowards(_lookRotation, q, turnSpeed * Time.unscaledDeltaTime);
                }

                // Strictly no procedural side-to-side twitching
                modelRoot.rotation = _lookRotation;
            }
        }

        void LateUpdate()
        {
            if (_unit == null || !_unit.IsAlive || _dying || modelRoot == null) return;

            var vel = (_agent != null && _agent.enabled) ? _agent.velocity : transform.forward * previewSpeed;
            var planar = new Vector3(vel.x, 0f, vel.z);
            var moving = planar.sqrMagnitude > 0.001f;

            float dt = Application.isPlaying ? Time.unscaledDeltaTime : 0.016f;
            _idleT += dt;

            var bob = moving ? Mathf.Sin(_idleT * proceduralBobSpeed) * proceduralBobAmp : 0f;
            
            // Build animation - focused mandible and leg labor
            if (_buildAnimTimer > 0f)
            {
                _buildAnimTimer -= dt;
                
                // Work cycles
                float workSpeed = 14f;
                float cycle = _idleT * workSpeed;
                
                // Front legs: Picking/Placing motion (tamping)
                // Offset the phases for a more natural "busy" look
                float leftLegMove = Mathf.Sin(cycle) * 25f;
                float rightLegMove = Mathf.Sin(cycle + 2.5f) * 25f;
                
                // Head nodding: Synchronized with the picking motion
                float headNod = Mathf.Sin(cycle * 2f) * 10f; 
                // Subtle searching: Slow side-to-side head tilt
                float headSearch = Mathf.Sin(_idleT * 3f) * 8f;

                if (_head != null) 
                    _head.localRotation = _headBase * Quaternion.Euler(40f + headNod, headSearch, 0f);
                
                if (_chest != null)
                {
                    // Subtle torso rocking to show weight transfer
                    float chestRock = Mathf.Sin(cycle * 0.5f) * 3f;
                    _chest.localRotation = _chestBase * Quaternion.Euler(20f, 0f, chestRock);
                }
                
                // Manual labor with front legs
                if (_lArm != null) _lArm.localRotation = _lArmBase * Quaternion.Euler(-35f + leftLegMove, 12f, 0f);
                if (_rArm != null) _rArm.localRotation = _rArmBase * Quaternion.Euler(-35f + rightLegMove, -12f, 0f);
                
                // Abdomen pulsing (effort/breathing)
                if (_tail != null) 
                    _tail.localRotation = _tailBase * Quaternion.Euler(Mathf.Sin(_idleT * 6f) * 12f, 0f, 0f);

                // Absolutely grounded
                bob = 0f; 

                ResetBonesOnly(dt * 3f, arms: false, chest: false, head: false, tail: false);
            }
            else if (!moving && _attackAnimT <= 0f && _unit.Archetype == UnitArchetype.BasicFighter)
                {
                ApplyMantisLoop(dt);
                }
                else
                {
                modelRoot.localScale = _baseScale;
                // Only reset bones procedurally if we don't have an animator taking over.
                // If we do have an animator, it will handle the bones itself.
                if (!(animator != null && animator.runtimeAnimatorController != null))
                {
                    ResetBones(dt * 5f);
                }
                }

                modelRoot.localPosition = _baseLocalPos + new Vector3(0f, bob, 0f);

                if (_attackAnimT > 0f)
            {
                _attackAnimT -= dt;
                float p = 1f - (Mathf.Max(0f, _attackAnimT) / _attackAnimDuration);

                if (_unit.Archetype == UnitArchetype.BasicRanged)
                {
                    // Real bombardier beetles pulse their spray rapidly. 
                    // This "jitter" and "staccato" movement reflects the explosive biological mechanism.
                    float sprayActive = (p > 0.05f && p < 0.65f) ? 1f : 0f;
                    float jitter = sprayActive * Mathf.Sin(p * 180f) * 6f;
                    
                    float tailLift = Mathf.Sin(p * Mathf.PI) * 60f;
                    if (_tail != null)
                        _tail.localRotation = _tailBase * Quaternion.Euler(-tailLift + jitter, jitter * 0.4f, 0f);

                    float brace = Mathf.Sin(p * Mathf.PI) * 0.08f;
                    modelRoot.localScale = Vector3.Scale(_baseScale,
                        new Vector3(1f + brace, 1f - brace * 0.5f, 1f + brace));

                    // Pulsing staccato recoil
                    float recoilBase = Mathf.Sin(p * Mathf.PI) * 0.16f;
                    float recoilPulse = sprayActive * Mathf.Sin(p * 90f) * 0.035f;
                    float recoil = recoilBase + recoilPulse;
                    
                    // Rear-facing: forward is the direction the beetle is technically "facing" (the front)
                    // Recoil from the rear should push it forward.
                    modelRoot.localPosition += modelRoot.forward * recoil;

                    if (_chest != null)
                        _chest.localRotation = _chestBase * Quaternion.Euler(Mathf.Sin(p * Mathf.PI) * -18f, 0f, 0f);
                }
                else
                {
                    float lunge = Mathf.Sin(p * Mathf.PI) * 0.45f;
                    float squash = 1f + 0.18f * Mathf.Sin(p * Mathf.PI * 2f);
                    modelRoot.localPosition += modelRoot.forward * lunge;
                    modelRoot.localScale = Vector3.Scale(_baseScale, new Vector3(squash, 1f / squash, squash));

                    if (_lArm != null) _lArm.localRotation *= Quaternion.Euler(Mathf.Sin(p * Mathf.PI) * -85f, 0f, 0f);
                    if (_rArm != null) _rArm.localRotation *= Quaternion.Euler(Mathf.Sin(p * Mathf.PI) * -85f, 0f, 0f);
                }
            }
            else if (_unit.Archetype != UnitArchetype.BasicFighter)
            {
                float breath = 1f + Mathf.Sin(_idleT * idlePulseSpeed * 0.5f) * idlePulseAmp;
                modelRoot.localScale = Vector3.Scale(_baseScale, new Vector3(breath, 1f, breath));
            }
        }

        void ApplyMantisLoop(float dt)
        {
            float loopTime = _idleT % 30f;
            float breath = 1f + Mathf.Sin(_idleT * 1.8f) * 0.015f;
            modelRoot.localScale = Vector3.Scale(_baseScale, new Vector3(breath, 1f, breath));

            if (_tail != null)
                _tail.localRotation = _tailBase * Quaternion.Euler(Mathf.Sin(_idleT * 2f) * 8f, 0f, 0f);

            if (loopTime < 10f) // Vertical Look
            {
                float lookY = (Mathf.PerlinNoise(_idleT * 0.5f, _instanceOffset + 100f) - 0.5f) * 35f;
                if (_head != null) _head.localRotation = Quaternion.Slerp(_head.localRotation, _headBase * Quaternion.Euler(lookY, 0f, 0f), dt * 2f);
                if (_chest != null) _chest.localRotation = Quaternion.Slerp(_chest.localRotation, _chestBase * Quaternion.Euler(lookY * 0.2f, 0f, 0f), dt * 1.5f);
                ResetBonesOnly(dt * 3f, arms: true);
            }
            else if (loopTime >= 10f && loopTime < 18f) // Vertical Scythe Maintenance Left
            {
                float p = Mathf.InverseLerp(10f, 18f, loopTime);
                float lift = Mathf.Sin(p * Mathf.PI);
                float scrub = Mathf.Sin(_idleT * 15f) * 8f;
                if (_lArm != null) _lArm.localRotation = Quaternion.Slerp(_lArm.localRotation, _lArmBase * Quaternion.Euler(-45f * lift + scrub, 0f, 0f), dt * 6f);
                if (_head != null) _head.localRotation = Quaternion.Slerp(_head.localRotation, _headBase * Quaternion.Euler(30f * lift, 0f, 0f), dt * 6f);
                ResetBonesOnly(dt * 2f, rightArm: true, chest: true);
            }
            else if (loopTime >= 18f && loopTime < 24f) // Head Dip
            {
                float p = Mathf.InverseLerp(18f, 24f, loopTime);
                float dip = Mathf.Sin(p * Mathf.PI);
                if (_head != null) _head.localRotation = Quaternion.Slerp(_head.localRotation, _headBase * Quaternion.Euler(40f * dip, 0f, 0f), dt * 3f);
                ResetBonesOnly(dt * 3f, arms: true, chest: true);
            }
            else // Vertical Scythe Maintenance Right
            {
                float p = Mathf.InverseLerp(24f, 30f, loopTime);
                float lift = Mathf.Sin(p * Mathf.PI);
                float scrub = Mathf.Sin(_idleT * 15f) * 8f;
                if (_rArm != null) _rArm.localRotation = Quaternion.Slerp(_rArm.localRotation, _rArmBase * Quaternion.Euler(-45f * lift + scrub, 0f, 0f), dt * 6f);
                if (_head != null) _head.localRotation = Quaternion.Slerp(_head.localRotation, _headBase * Quaternion.Euler(30f * lift, 0f, 0f), dt * 6f);
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

        public bool HasAnimatorAttack =>
            animator != null && animator.runtimeAnimatorController != null;

        public void NotifyAttack()
        {
            if (_hasAttack)
                animator.SetTrigger(Attack);
            _attackAnimDuration = _unit != null ? _unit.Archetype switch
            {
                UnitArchetype.BasicRanged => 0.5f,
                UnitArchetype.BlackWidow => 0.4f,
                _ => 0.35f
            } : 0.35f;
            _attackAnimT = _attackAnimDuration;
        }

        public void NotifyWebCast()
        {
            if (_hasWebCast)
                animator.SetTrigger(WebCast);
            _attackAnimDuration = 0.5f;
            _attackAnimT = _attackAnimDuration;
        }

        public void NotifyBuild()
        {
            // Reset the timer while building to maintain the pose
            _buildAnimTimer = 0.2f; 
        }

        public void NotifyDeath(float destroyDelay = 0.45f)
        {
            if (_dying) return;
            _dying = true;
            if (_hasDeath)
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

        public Vector3 GetSprayOrigin()
        {
            if (_tail != null)
                return _tail.position;
            if (modelRoot != null)
                return modelRoot.position - modelRoot.forward * 0.4f + Vector3.up * 0.35f;
            return transform.position + Vector3.up * 0.4f;
        }

        /// <summary>Human-readable lines for the unit spotlight / codex UI.</summary>
        public IReadOnlyList<string> GetSpotlightLines()
        {
            var lines = new List<string>(12);
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                lines.Add($"Animator: {animator.runtimeAnimatorController.name}");
                lines.Add("Parameters (set by code):");
                lines.Add("· Speed (float) — planar speed");
                lines.Add("· IsMoving (bool)");
                lines.Add("· Gathering (bool) — while gather + stopped");
                lines.Add("· Attack (trigger) — NotifyAttack()");
                lines.Add("· Death (trigger) — NotifyDeath()");
                lines.Add("Plus: root faces move/attack direction (turnSpeed).");
            }
            else
            {
                lines.Add("Procedural (no Animator controller)");
                lines.Add("· Move bob: sinusoidal Y, proceduralBobSpeed / proceduralBobAmp");
                lines.Add("· Attack: 0.35s lunge + arm pitch + squash (NotifyAttack)");
                if (_unit != null && _unit.Archetype == UnitArchetype.BasicFighter)
                {
                    lines.Add("· Mantis idle loop (~30s): head look, scythe maintenance, tail sway");
                    lines.Add("· Bones: frontleg/R_frontleg, chest, head, tail (if named in mesh)");
                }
                else if (_unit != null)
                {
                    lines.Add("· Idle: subtle chest breath scale (idlePulseSpeed / idlePulseAmp)");
                }
                lines.Add("· Death: scale-down shrink unless Animator handles it");
            }

            return lines;
        }
    }
}
