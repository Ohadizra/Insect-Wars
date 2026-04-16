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
        static readonly int Stomp = Animator.StringToHash("Stomp");

        [SerializeField] Transform modelRoot;
        [SerializeField] Animator animator;
        [SerializeField] float turnSpeed = 540f;
        [SerializeField] float proceduralBobSpeed = 10f;
        [SerializeField] float proceduralBobAmp = 0.035f;
        [SerializeField] float idlePulseSpeed = 2f;
        [SerializeField] float idlePulseAmp = 0.02f;
        [SerializeField] GameObject stompVfxPrefab;

        public float previewSpeed;

        NavMeshAgent _agent;
        InsectUnit _unit;
        Vector3 _baseLocalPos;
        Vector3 _baseScale;
        Quaternion _baseLocalRot;
        Quaternion _lookRotation;
        
        Transform _lArm, _rArm, _chest, _head, _tail;
        Quaternion _lArmBase, _rArmBase, _chestBase, _headBase, _tailBase;
        
        Transform _lWing, _rWing;
        Quaternion _lWingBase, _rWingBase;

        Transform _lMandible, _rMandible;
        Quaternion _lMandibleBase, _rMandibleBase;
        
        float _attackAnimT;
        float _attackAnimDuration;
        float _stompAnimT;
        float _stompAnimDuration;
        float _buildAnimTimer; 
        float _idleT;
        float _instanceOffset;
        float _impactStaggerT;
        Vector3 _impactStaggerDir;
        float _confusionT;
        bool _dying;
        bool _stompImpactTriggered;

        bool _hasSpeed, _hasIsMoving, _hasGathering, _hasBuild, _hasAttack, _hasDeath, _hasWebCast, _hasStomp;

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
                    if (p.nameHash == Stomp) _hasStomp = true;
                }
            }

            if (modelRoot != null)
            {
                _baseLocalPos = modelRoot.localPosition;
                _baseScale = modelRoot.localScale;
                _baseLocalRot = modelRoot.localRotation;
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

                _lMandible = FindRecursive(modelRoot, "earend");
                _rMandible = FindRecursive(modelRoot, "R_earend");
                if (_lMandible != null) _lMandibleBase = _lMandible.localRotation;
                if (_rMandible != null) _rMandibleBase = _rMandible.localRotation;

                if (stompVfxPrefab == null && IsStagBeetle())
                    stompVfxPrefab = Resources.Load<GameObject>("VFX/StompGroundEffect");

                _lWing = FindRecursive(modelRoot, "L_wing") ?? FindRecursive(modelRoot, "wing_L");
                _rWing = FindRecursive(modelRoot, "R_wing") ?? FindRecursive(modelRoot, "wing_R");
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

        bool IsBlackWidow() => (_unit != null && _unit.Archetype == UnitArchetype.BlackWidow) || name.Contains("BlackWidow");
        bool IsStickSpy() => (_unit != null && _unit.Archetype == UnitArchetype.StickSpy) || name.Contains("Stick") || name.Contains("Sentinel");
        bool IsStagBeetle() => (_unit != null && _unit.Archetype == UnitArchetype.GiantStagBeetle) || name.Contains("StagBeetle") || name.Contains("Stag");

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
                Vector3 face = moving ? planar : Vector3.zero;
                if (!moving && _unit.CurrentOrder == UnitOrder.Attack && _unit.AttackTarget != null)
                {
                    var t = _unit.AttackTarget.position - transform.position; t.y = 0f;
                    if (t.sqrMagnitude > 0.01f) face = _unit.Archetype == UnitArchetype.BasicRanged ? -t : t;
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
            if (IsStickSpy() || IsBlackWidow() || IsStagBeetle()) modelRoot.rotation = _lookRotation * _baseLocalRot;

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
                if (_head != null) _head.localRotation = _headBase * Quaternion.Euler(40f + Mathf.Sin(cycle * 2f) * 10f, Mathf.Sin(_idleT * 3f) * 8f, 0f);
                if (_chest != null) _chest.localRotation = _chestBase * Quaternion.Euler(20f, 0f, Mathf.Sin(cycle * 0.5f) * 3f);
                if (_lArm != null) _lArm.localRotation = _lArmBase * Quaternion.Euler(-35f + Mathf.Sin(cycle) * 25f, 12f, 0f);
                if (_rArm != null) _rArm.localRotation = _rArmBase * Quaternion.Euler(-35f + Mathf.Sin(cycle + 2.5f) * 25f, -12f, 0f);
                if (_tail != null) _tail.localRotation = _tailBase * Quaternion.Euler(Mathf.Sin(_idleT * 6f) * 12f, 0f, 0f);
                bob = 0f; 
                ResetBonesOnly(dt * 3f, arms: false, chest: false, head: false, tail: false);
            }
            else if (IsStagBeetle()) ApplyStagBeetleLoop(dt, moving, planar.magnitude);
            else if (!moving && _attackAnimT <= 0f && _unit.Archetype == UnitArchetype.BasicFighter) ApplyMantisLoop(dt);
            else
            {
                modelRoot.localScale = _baseScale;
                if (!(animator != null && animator.runtimeAnimatorController != null)) ResetBones(dt * 5f);
            }

            {
                float heightOffset = IsBlackWidow() ? 0.35f : (IsStickSpy() ? 0.66f : (IsStagBeetle() ? 0.53f : 0f));
                modelRoot.localPosition = _baseLocalPos + new Vector3(0f, bob + heightOffset, 0f);
            }

            if (_impactStaggerT > 0f)
            {
                _impactStaggerT -= dt;
                float p = Mathf.Clamp01(_impactStaggerT / 0.6f);
                float jitter = Mathf.Sin(p * 50f) * 0.08f * p;
                float squash = 1f - 0.25f * Mathf.Sin(p * Mathf.PI) * p;
                modelRoot.localPosition += _impactStaggerDir * jitter;
                modelRoot.localScale = Vector3.Scale(modelRoot.localScale, new Vector3(1.2f - squash, squash, 1.2f - squash));
            }

            if (_confusionT > 0f)
            {
                _confusionT -= dt;
                float p = Mathf.Clamp01(_confusionT);
                float swayAmount = 15f * p;
                float freq = 8f;
                modelRoot.localRotation *= Quaternion.Euler(Mathf.Sin(_idleT * freq) * swayAmount, Mathf.Cos(_idleT * freq * 0.7f) * swayAmount, 0f);
                if (_head != null) _head.localRotation *= Quaternion.Euler(Mathf.Sin(_idleT * freq * 1.5f) * 20f * p, 0f, 0f);
            }

            if (_stompAnimT > 0f)
            {
                _stompAnimT -= dt;
                float p = 1f - (Mathf.Max(0f, _stompAnimT) / _stompAnimDuration);
                ApplyStagBeetleStomp(p);
            }
            else if (_attackAnimT > 0f)
            {
                _attackAnimT -= dt;
                float p = 1f - (Mathf.Max(0f, _attackAnimT) / _attackAnimDuration);
                if (IsBlackWidow()) ApplyBlackWidowSpecial(p, _attackAnimDuration);
                else if (IsStagBeetle()) ApplyStagBeetleAttack(p);
                else if (_unit.Archetype == UnitArchetype.BasicRanged)
                {
                    float sprayActive = (p > 0.05f && p < 0.65f) ? 1f : 0f;
                    float jtr = sprayActive * Mathf.Sin(p * 180f) * 6f;
                    float tailLift = Mathf.Sin(p * Mathf.PI) * 60f;
                    if (_tail != null) _tail.localRotation = _tailBase * Quaternion.Euler(-tailLift + jtr, jtr * 0.4f, 0f);
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
            else if (_unit.Archetype != UnitArchetype.BasicFighter && !IsBlackWidow() && !IsStagBeetle())
            {
                float breath = 1f + Mathf.Sin(_idleT * idlePulseSpeed * 0.5f) * idlePulseAmp;
                modelRoot.localScale = Vector3.Scale(_baseScale, new Vector3(breath, 1f, breath));
            }
        }

        void ApplyStagBeetleLoop(float dt, bool moving, float speed)
        {
            if (moving)
            {
                float cycle = _idleT * 6.0f;
                float groupA = Mathf.Sin(cycle);
                modelRoot.localRotation *= Quaternion.Euler(Mathf.Abs(groupA) * 2.5f, Mathf.Sin(cycle * 0.5f) * 3.5f, groupA * 1.5f);
                modelRoot.localPosition += new Vector3(0f, Mathf.Pow(Mathf.Abs(Mathf.Sin(cycle)), 2f) * 0.06f, 0f);
            }
            else
            {
                float breath = 1f + Mathf.Sin(_idleT * 1.2f) * 0.015f;
                if (_head != null) _head.localRotation = _headBase * Quaternion.Euler(0f, Mathf.Sin(_idleT * 0.3f) * 5f, 0f);
                if (_lMandible != null) _lMandible.localRotation = _lMandibleBase * Quaternion.Euler(0f, -Mathf.Sin(_idleT * 0.5f) * 10f, 0f);
                if (_rMandible != null) _rMandible.localRotation = _rMandibleBase * Quaternion.Euler(0f, Mathf.Sin(_idleT * 0.5f) * 10f, 0f);
                modelRoot.localScale = Vector3.Scale(_baseScale, new Vector3(breath, 1f, breath));
            }
        }

        void ApplyStagBeetleAttack(float p)
        {
            float lunge = Mathf.Sin(p * Mathf.PI) * 0.45f;
            float squash = 1f + 0.18f * Mathf.Sin(p * Mathf.PI * 2f);
            modelRoot.localPosition += modelRoot.forward * lunge;
            modelRoot.localScale = Vector3.Scale(_baseScale, new Vector3(squash, 1f / squash, squash));
            if (_lArm != null) _lArm.localRotation *= Quaternion.Euler(Mathf.Sin(p * Mathf.PI) * -85f, 0f, 0f);
            if (_rArm != null) _rArm.localRotation *= Quaternion.Euler(Mathf.Sin(p * Mathf.PI) * -85f, 0f, 0f);
            float mandibleOpen = Mathf.Sin(p * Mathf.PI) * 40f;
            if (_lMandible != null) _lMandible.localRotation = _lMandibleBase * Quaternion.Euler(0f, -mandibleOpen, 0f);
            if (_rMandible != null) _rMandible.localRotation = _rMandibleBase * Quaternion.Euler(0f, mandibleOpen, 0f);
        }

        void ApplyStagBeetleStomp(float p)
        {
            float rise = 0f, mandibleOpen = 0f, compression = 0f;
            if (p < 0.3f) { float lp = p / 0.3f; rise = -25f * lp; mandibleOpen = 50f * lp; }
            else if (p < 0.5f) { float lp = (p - 0.3f) / 0.2f; rise = -25f + 35f * lp; mandibleOpen = 50f * (1f - lp); }
            else if (p < 0.6f) { float lp = (p - 0.5f) / 0.1f; compression = -0.15f * lp; }
            else { float lp = (p - 0.6f) / 0.4f; compression = -0.15f * (1f - lp); }
            modelRoot.localRotation *= Quaternion.Euler(rise, 0f, 0f);
            modelRoot.localPosition += new Vector3(0f, compression, 0f);
            if (_lMandible != null) _lMandible.localRotation = _lMandibleBase * Quaternion.Euler(0f, -mandibleOpen, 0f);
            if (_rMandible != null) _rMandible.localRotation = _rMandibleBase * Quaternion.Euler(0f, mandibleOpen, 0f);
            if (p >= 0.5f && !_stompImpactTriggered) { _stompImpactTriggered = true; if (stompVfxPrefab != null) Instantiate(stompVfxPrefab, transform.position, Quaternion.identity); }
        }

        public void NotifyStomp() { _stompAnimDuration = 1.7f; _stompAnimT = _stompAnimDuration; _stompImpactTriggered = false; if (_hasStomp) animator.SetTrigger(Stomp); }
        public void NotifyConfusion(float duration) { _confusionT = Mathf.Max(_confusionT, duration); }

        void ApplyBlackWidowLoop(float dt, bool moving, float speed)
        {
            if (moving)
            {
                float cycle = _idleT * 9.0f;
                float groupA = Mathf.Sin(cycle);
                float plantImpact = Mathf.Pow(Mathf.Abs(Mathf.Sin(cycle * 2f)), 2.5f);
                modelRoot.localRotation *= Quaternion.Euler(0f, Mathf.Cos(cycle) * 4.5f, Mathf.Sign(groupA) * Mathf.Pow(Mathf.Abs(groupA), 0.6f) * 6.5f);
                modelRoot.localPosition += new Vector3(0f, plantImpact * 0.045f, 0f);
                float str = 1f + (1f - plantImpact) * 0.03f;
                modelRoot.localScale = Vector3.Scale(_baseScale, new Vector3(1f / str, str, 1f / str));
            }
            else
            {
                float breath = 1f + Mathf.Sin(_idleT * 2.2f) * 0.025f;
                modelRoot.localRotation *= Quaternion.Euler(Mathf.Sin(_idleT * 1.4f) * 3.5f, 0f, Mathf.Sin(_idleT * 0.8f) * 2.5f);
                modelRoot.localScale = Vector3.Scale(_baseScale, new Vector3(breath, 1f, breath));
            }
        }

        void ApplyStickLoop(float dt, bool moving, float speed)
        {
            if (moving)
            {
                float cycle = _idleT * 12.0f;
                float groupA = Mathf.Sin(cycle);
                modelRoot.localRotation *= Quaternion.Euler(Mathf.Abs(groupA) * 4.5f, Mathf.Sin(cycle * 0.5f) * 6.5f, groupA * 3f);
                modelRoot.localPosition += new Vector3(0f, Mathf.Pow(Mathf.Abs(Mathf.Sin(cycle)), 2f) * 0.12f, 0f);
            }
            else
            {
                modelRoot.localRotation *= Quaternion.Euler(Mathf.Sin(_idleT * 0.8f) * 2.5f, 0f, 0f);
                float b = 1f + Mathf.Sin(_idleT * 1.8f) * 0.02f; modelRoot.localScale = Vector3.Scale(_baseScale, new Vector3(b, 1f, b));
            }
        }

        void ApplyBlackWidowSpecial(float p, float duration)
        {
            if (duration > 0.45f)
            {
                float production = Mathf.Clamp01((p - 0.24f) / 0.36f);
                float pump = (production > 0 && production < 1f) ? Mathf.Sin(production * Mathf.PI * 2f) * 0.15f : 0f;
                modelRoot.localRotation *= Quaternion.Euler(Mathf.Sin(Mathf.Clamp01(p / 0.24f) * Mathf.PI * 0.5f) * -25f, 0f, 0f);
                modelRoot.localScale = Vector3.Scale(_baseScale, new Vector3(1f + pump, 1f - pump, 1f + pump));
            }
            else
            {
                float angle = 0f, lunge = 0f, squash = 1f;
                if (p < 0.2f) angle = (p / 0.2f) * -25f;
                else if (p < 0.5f) { float sp = (p - 0.2f) / 0.3f; angle = -25f + sp * 45f; lunge = Mathf.Sin(sp * Mathf.PI) * 0.6f; squash = 1.15f; }
                else angle = 20f * (1f - (p - 0.5f) / 0.5f);
                modelRoot.localRotation *= Quaternion.Euler(angle, 0f, 0f);
                modelRoot.localPosition += modelRoot.forward * lunge;
                modelRoot.localScale = Vector3.Scale(_baseScale, new Vector3(squash, 1f / squash, squash));
            }
        }

        void ApplyMantisLoop(float dt)
        {
            float breath = 1f + Mathf.Sin(_idleT * 1.8f) * 0.015f;
            modelRoot.localScale = Vector3.Scale(_baseScale, new Vector3(breath, 1f, breath));
            if (_tail != null) _tail.localRotation = _tailBase * Quaternion.Euler(Mathf.Sin(_idleT * 2f) * 8f, 0f, 0f);
            if (_idleT % 30f < 10f) { if (_head != null) _head.localRotation = Quaternion.Slerp(_head.localRotation, _headBase * Quaternion.Euler((Mathf.PerlinNoise(_idleT * 0.5f, _instanceOffset + 100f) - 0.5f) * 35f, 0f, 0f), dt * 2f); ResetBonesOnly(dt * 3f, arms: true); }
            else ResetBonesOnly(dt * 2f);
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
        public void NotifyDeath(float delay = 0.45f) { if (_dying) return; _dying = true; if (_hasDeath) animator.SetTrigger(Death); if (Application.isPlaying) Destroy(gameObject, delay); }
        public void NotifyStompImpact(Vector3 source) { _impactStaggerT = 0.6f; _impactStaggerDir = (transform.position - source).normalized; if (_impactStaggerDir.sqrMagnitude < 0.01f) _impactStaggerDir = Vector3.up; }

        public Vector3 GetProjectileSpawnPoint() { return modelRoot.position + modelRoot.forward * 0.35f + Vector3.up * 0.25f; }
        public Vector3 GetSprayOrigin() { if (_tail != null) return _tail.position; return modelRoot != null ? modelRoot.position - modelRoot.forward * 0.4f + Vector3.up * 0.35f : transform.position + Vector3.up * 0.4f; }

        public IReadOnlyList<string> GetSpotlightLines()
        {
            var lines = new List<string>(8);
            if (_unit != null && _unit.Archetype == UnitArchetype.BlackWidow)
            {
                lines.Add("Black Widow Procedural Driver:");
                lines.Add("· Orientation: Flat to Ground");
                lines.Add("· Walk: Realistic body sway + bob");
                lines.Add("· Bite: 3-phase strike");
            }
            else if (_unit != null && _unit.Archetype == UnitArchetype.GiantStagBeetle)
            {
                lines.Add("Giant Stag Beetle Driver:");
                lines.Add("· Walk: Heavy quadruped gait");
                lines.Add("· Attack: Mandible snap lunge");
                lines.Add("· Ability: Ground Stomp AoE");
            }
            else lines.Add("Generic Procedural Driver");
            return lines;
        }
    }
}
