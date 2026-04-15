using System.Collections.Generic;
using InsectWars.Data;
using UnityEngine;
using UnityEngine.AI;

namespace InsectWars.RTS
{
    /// <summary>
    /// Procedural and Animator-based driver for unit animations.
    /// Handles the Black Widow's complex multi-phase motions through code
    /// to compensate for the lack of skeletal rigging in the current model.
    /// </summary>
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
        Quaternion _baseLocalRot;
        Quaternion _lookRotation;
        
        // Universal bone references for procedural animation (if present)
        Transform _lArm, _rArm, _chest, _head, _tail;
        Quaternion _lArmBase, _rArmBase, _chestBase, _headBase, _tailBase;
        
        Transform _lWing, _rWing;
        Quaternion _lWingBase, _rWingBase;
        
        float _attackAnimT;
        float _attackAnimDuration;
        float _buildAnimTimer; 
        float _idleT;
        float _instanceOffset;
        bool _dying;

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
                _baseLocalRot = modelRoot.localRotation;
                if (float.IsNaN(_baseLocalPos.x) || float.IsNaN(_baseLocalPos.y) || float.IsNaN(_baseLocalPos.z))
                    _baseLocalPos = Vector3.zero;
                if (float.IsNaN(_baseScale.x) || float.IsNaN(_baseScale.y) || float.IsNaN(_baseScale.z)
                    || _baseScale == Vector3.zero)
                    _baseScale = Vector3.one;
                if (float.IsNaN(_baseLocalRot.x) || float.IsNaN(_baseLocalRot.y) || float.IsNaN(_baseLocalRot.z)
                    || _baseLocalRot == new Quaternion(0, 0, 0, 0))
                    _baseLocalRot = Quaternion.identity;
                
                // Initialize _lookRotation to the transform's heading
                _lookRotation = transform.rotation;

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

                _lWing = FindRecursive(modelRoot, "L_wing") ?? FindRecursive(modelRoot, "wing_L")
                    ?? FindRecursive(modelRoot, "LeftWing") ?? FindRecursive(modelRoot, "Wing_L");
                _rWing = FindRecursive(modelRoot, "R_wing") ?? FindRecursive(modelRoot, "wing_R")
                    ?? FindRecursive(modelRoot, "RightWing") ?? FindRecursive(modelRoot, "Wing_R");
                if (_lWing != null) _lWingBase = _lWing.localRotation;
                if (_rWing != null) _rWingBase = _rWing.localRotation;
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

        bool IsBlackWidow()
        {
            if (_unit != null && _unit.Archetype == UnitArchetype.BlackWidow) return true;
            return name.Contains("BlackWidow");
        }

        bool IsStickSpy()
        {
            if (_unit != null && _unit.Archetype == UnitArchetype.StickSpy) return true;
            return name.Contains("Stick") || name.Contains("Sentinel");
        }

        bool NeedsPitchCorrection() => IsBlackWidow() || IsStickSpy();

        void Update()
        {
            if (_unit == null || !_unit.IsAlive || _dying)
            {
                if (Application.isPlaying && modelRoot != null && _dying)
                {
                    modelRoot.localScale = Vector3.Lerp(modelRoot.localScale, Vector3.zero, Time.unscaledDeltaTime * 5f);
                }
                return;
            }

            var vel = (_agent != null && _agent.enabled) ? _agent.velocity : transform.forward * previewSpeed;
            var planar = new Vector3(vel.x, 0f, vel.z);
            var moving = planar.sqrMagnitude > 0.01f;

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

                modelRoot.rotation = _lookRotation * _baseLocalRot;
                }
                }

                void LateUpdate()
                {
                if (_unit == null || !_unit.IsAlive || _dying || modelRoot == null) return;

                if (IsStickSpy() || IsBlackWidow())
                {
                // Force orientation to win against Animator root rotation/FBX bad orientation
                modelRoot.rotation = _lookRotation * _baseLocalRot;
                }

                var vel = (_agent != null && _agent.enabled) ? _agent.velocity : transform.forward * previewSpeed;
                var planar = new Vector3(vel.x, 0f, vel.z);
                var moving = planar.sqrMagnitude > 0.01f;

                float dt = Application.isPlaying ? Time.unscaledDeltaTime : 0.016f;
                _idleT += dt;

                var bob = moving ? Mathf.Sin(_idleT * proceduralBobSpeed) * proceduralBobAmp : 0f;
            
                if (_buildAnimTimer > 0f)
                {
                _buildAnimTimer -= dt;
                float workSpeed = 14f;
                float cycle = _idleT * workSpeed;
                float leftLegMove = Mathf.Sin(cycle) * 25f;
                float rightLegMove = Mathf.Sin(cycle + 2.5f) * 25f;
                float headNod = Mathf.Sin(cycle * 2f) * 10f; 
                float headSearch = Mathf.Sin(_idleT * 3f) * 8f;

                if (_head != null) _head.localRotation = _headBase * Quaternion.Euler(40f + headNod, headSearch, 0f);
                if (_chest != null) _chest.localRotation = _chestBase * Quaternion.Euler(20f, 0f, Mathf.Sin(cycle * 0.5f) * 3f);
                if (_lArm != null) _lArm.localRotation = _lArmBase * Quaternion.Euler(-35f + leftLegMove, 12f, 0f);
                if (_rArm != null) _rArm.localRotation = _rArmBase * Quaternion.Euler(-35f + rightLegMove, -12f, 0f);
                if (_tail != null) _tail.localRotation = _tailBase * Quaternion.Euler(Mathf.Sin(_idleT * 6f) * 12f, 0f, 0f);
                bob = 0f; 
                ResetBonesOnly(dt * 3f, arms: false, chest: false, head: false, tail: false);
                }
                else if (IsBlackWidow())
                {
                ApplyBlackWidowLoop(dt, moving, planar.magnitude);
                }
                else if (IsStickSpy())
                {
                    bool hasWalkClip = animator != null && animator.runtimeAnimatorController != null && _hasIsMoving;
                    if (!hasWalkClip)
                        ApplyStickLoop(dt, moving, planar.magnitude);
                    else
                        modelRoot.localScale = _baseScale;
                }
                else if (!moving && _attackAnimT <= 0f && _unit.Archetype == UnitArchetype.BasicFighter)
                {
                ApplyMantisLoop(dt);
                }
                else
                {
                modelRoot.localScale = _baseScale;
                if (!(animator != null && animator.runtimeAnimatorController != null))
                    ResetBones(dt * 5f);
                }

                {
                float heightOffset = 0f;
                if (IsBlackWidow()) heightOffset = 0.35f;
                else if (IsStickSpy()) heightOffset = 0.66f;

                var newPos = _baseLocalPos + new Vector3(0f, bob + heightOffset, 0f);
                if (!float.IsNaN(newPos.y))
                    modelRoot.localPosition = newPos;
                }

            if (_attackAnimT > 0f)
            {
                _attackAnimT -= dt;
                float p = 1f - (Mathf.Max(0f, _attackAnimT) / _attackAnimDuration);

                if (IsBlackWidow())
                {
                    ApplyBlackWidowSpecial(p, _attackAnimDuration);
                }
                else if (_unit.Archetype == UnitArchetype.BasicRanged)
                {
                    float sprayActive = (p > 0.05f && p < 0.65f) ? 1f : 0f;
                    float jitter = sprayActive * Mathf.Sin(p * 180f) * 6f;
                    float tailLift = Mathf.Sin(p * Mathf.PI) * 60f;
                    if (_tail != null) _tail.localRotation = _tailBase * Quaternion.Euler(-tailLift + jitter, jitter * 0.4f, 0f);
                    float brace = Mathf.Sin(p * Mathf.PI) * 0.08f;
                    modelRoot.localScale = Vector3.Scale(_baseScale, new Vector3(1f + brace, 1f - brace * 0.5f, 1f + brace));
                    modelRoot.localPosition += modelRoot.forward * (Mathf.Sin(p * Mathf.PI) * 0.16f + sprayActive * Mathf.Sin(p * 90f) * 0.035f);
                    if (_chest != null) _chest.localRotation = _chestBase * Quaternion.Euler(Mathf.Sin(p * Mathf.PI) * -18f, 0f, 0f);
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
            else if (_unit.Archetype != UnitArchetype.BasicFighter && !IsBlackWidow())
            {
                float breath = 1f + Mathf.Sin(_idleT * idlePulseSpeed * 0.5f) * idlePulseAmp;
                modelRoot.localScale = Vector3.Scale(_baseScale, new Vector3(breath, 1f, breath));
            }
        }

        void ApplyBlackWidowLoop(float dt, bool moving, float speed)
        {
            if (moving)
            {
                // Walk: High-fidelity alternating tetrapod gait simulation.
                float cycleSpeed = 9.0f; 
                float cycle = _idleT * cycleSpeed;
                float groupA = Mathf.Sin(cycle);
                float roll = Mathf.Sign(groupA) * Mathf.Pow(Mathf.Abs(groupA), 0.6f) * 6.5f;
                float yaw = Mathf.Cos(cycle) * 4.5f;
                float pitch = 0f;
                float plantImpact = Mathf.Pow(Mathf.Abs(Mathf.Sin(cycle * 2f)), 2.5f);
                float walkBob = plantImpact * 0.045f;

                modelRoot.localRotation *= Quaternion.Euler(pitch, yaw, roll);
                modelRoot.localPosition += new Vector3(0f, walkBob, 0f);
                
                float stretch = 1f + (1f - plantImpact) * 0.03f;
                modelRoot.localScale = Vector3.Scale(_baseScale, new Vector3(1f / Mathf.Max(0.01f, stretch), stretch, 1f / Mathf.Max(0.01f, stretch)));
            }
            else
            {
                float breath = 1f + Mathf.Sin(_idleT * 2.2f) * 0.025f;
                float sway = Mathf.Sin(_idleT * 1.4f) * 3.5f;
                float rollSway = Mathf.Sin(_idleT * 0.8f) * 2.5f;
                
                modelRoot.localRotation *= Quaternion.Euler(sway, 0f, rollSway);
                modelRoot.localScale = Vector3.Scale(_baseScale, new Vector3(breath, 1f, breath));
            }
            }

            void ApplyStickLoop(float dt, bool moving, float speed)
            {
                if (moving)
                {
                    // Animated gait for the stick body (procedural because FBX has no clips)
                    float gaitSpeed = 12.0f;
                    float cycle = _idleT * gaitSpeed;
                    float groupA = Mathf.Sin(cycle);
                
                    float swayYaw = Mathf.Sin(cycle * 0.5f) * 6.5f;
                    float swayPitch = Mathf.Abs(groupA) * 4.5f;
                    float walkBob = Mathf.Pow(Mathf.Abs(Mathf.Sin(cycle)), 2f) * 0.12f;

                    modelRoot.localRotation *= Quaternion.Euler(swayPitch, swayYaw, groupA * 3f);
                    modelRoot.localPosition += new Vector3(0f, walkBob, 0f);
                }
                else
                {
                    float breath = 1f + Mathf.Sin(_idleT * 1.8f) * 0.02f;
                    float sway = Mathf.Sin(_idleT * 0.8f) * 2.5f;
                    modelRoot.localRotation *= Quaternion.Euler(sway, 0f, 0f);
                    modelRoot.localScale = Vector3.Scale(_baseScale, new Vector3(breath, 1f, breath));
                }
            }

            void ApplyBlackWidowSpecial(float p, float duration)
        {
            if (duration > 0.45f) // WebCast
            {
                float spinUp = Mathf.Clamp01(p / 0.24f);
                float production = Mathf.Clamp01((p - 0.24f) / 0.36f);
                float tilt = Mathf.Sin(spinUp * Mathf.PI * 0.5f) * -25f;
                float pump = 0f;
                if (production > 0 && production < 1f)
                    pump = Mathf.Sin(production * Mathf.PI * 2f) * 0.15f;

                modelRoot.localRotation *= Quaternion.Euler(tilt, 0f, 0f);
                modelRoot.localScale = Vector3.Scale(_baseScale, new Vector3(1f + pump, 1f - pump, 1f + pump));
            }
            else // Venomous Bite
            {
                float angle = 0f;
                float lunge = 0f;
                float squash = 1f;

                if (p < 0.2f) 
                {
                    angle = (p / 0.2f) * -25f; 
                }
                else if (p < 0.5f) 
                {
                    float strikeP = (p - 0.2f) / 0.3f;
                    angle = -25f + strikeP * 45f; 
                    lunge = Mathf.Sin(strikeP * Mathf.PI) * 0.6f;
                    squash = 1.15f;
                }
                else 
                {
                    float retractP = (p - 0.5f) / 0.5f;
                    angle = 20f * (1f - retractP);
                }

                modelRoot.localRotation *= Quaternion.Euler(angle, 0f, 0f);
                modelRoot.localPosition += modelRoot.forward * lunge;
                modelRoot.localScale = Vector3.Scale(_baseScale, new Vector3(squash, 1f / squash, squash));
            }
        }

        void ApplyMantisLoop(float dt)
        {
            float loopTime = _idleT % 30f;
            float breath = 1f + Mathf.Sin(_idleT * 1.8f) * 0.015f;
            modelRoot.localScale = Vector3.Scale(_baseScale, new Vector3(breath, 1f, breath));
            if (_tail != null) _tail.localRotation = _tailBase * Quaternion.Euler(Mathf.Sin(_idleT * 2f) * 8f, 0f, 0f);
            if (loopTime < 10f) {
                float lookY = (Mathf.PerlinNoise(_idleT * 0.5f, _instanceOffset + 100f) - 0.5f) * 35f;
                if (_head != null) _head.localRotation = Quaternion.Slerp(_head.localRotation, _headBase * Quaternion.Euler(lookY, 0f, 0f), dt * 2f);
                ResetBonesOnly(dt * 3f, arms: true);
            } else { ResetBonesOnly(dt * 2f); }
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

        public void NotifyAttack() { _attackAnimDuration = 0.4f; _attackAnimT = _attackAnimDuration; if (_hasAttack) animator.SetTrigger(Attack); }
        public void NotifyWebCast() { _attackAnimDuration = 0.55f; _attackAnimT = _attackAnimDuration; if (_hasWebCast) animator.SetTrigger(WebCast); }
        public void NotifyBuild() { _buildAnimTimer = 0.2f; }
        public void NotifyTakeoff() { }
        public void NotifyDeath(float delay = 0.45f) 
        { 
            if (_dying) return; 
            _dying = true; 
            if (_hasDeath) animator.SetTrigger(Death); 
            if (Application.isPlaying) Destroy(gameObject, delay); 
        }
        
        public Vector3 GetProjectileSpawnPoint() { return modelRoot.position + modelRoot.forward * 0.35f + Vector3.up * 0.25f; }

        public Vector3 GetSprayOrigin()
        {
            if (_tail != null) return _tail.position;
            if (modelRoot != null) return modelRoot.position - modelRoot.forward * 0.4f + Vector3.up * 0.35f;
            return transform.position + Vector3.up * 0.4f;
        }

        public IReadOnlyList<string> GetSpotlightLines()
        {
            var lines = new List<string>(8);
            if (_unit != null && _unit.Archetype == UnitArchetype.BlackWidow)
            {
                lines.Add("Black Widow Procedural Driver:");
                lines.Add("· Orientation: Flat to Ground");
                lines.Add("· Height: +0.35 Ground Clear");
                lines.Add("· Walk: Realistic body sway + bob");
                lines.Add("· Bite: 3-phase rear/strike/retract");
                lines.Add("· Web: Abdomen forward + pumping");
            }
            else
            {
                lines.Add("Generic Procedural Driver");
            }
            return lines;
        }
    }
}
