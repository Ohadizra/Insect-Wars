using UnityEngine;

namespace InsectWars.RTS
{
    /// <summary>
    /// Auto-cast web net ability for the Black Widow.
    /// Every <see cref="Cooldown"/> seconds, casts a web in a forward cone
    /// that slows enemies by 50% for <see cref="SlowDuration"/> seconds.
    /// Only fires while the unit has an attack target.
    /// </summary>
    public class WebNetAbility : MonoBehaviour
    {
        const float Cooldown = 10f;
        const float Range = 5f;
        const float ConeHalfAngle = 45f;
        const float SlowDuration = 8f;
        const float SlowFactor = 0.5f;
        const float VfxAutoDestroy = 1.5f;

        static readonly Collider[] s_hits = new Collider[64];
        static Material s_webMat;

        float _timer;
        InsectUnit _unit;

        void Awake()
        {
            _unit = GetComponent<InsectUnit>();
        }

        void Update()
        {
            if (_unit == null || !_unit.IsAlive) return;
            if (_unit.AttackTarget == null) return;

            _timer += Time.deltaTime;
            if (_timer < Cooldown) return;

            var dir = _unit.AttackTarget.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) return;

            _timer = 0f;
            CastWeb(dir.normalized);
        }

        void CastWeb(Vector3 direction)
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, Range, s_hits);
            for (int i = 0; i < count; i++)
            {
                var target = s_hits[i].GetComponentInParent<InsectUnit>();
                if (target == null || !target.IsAlive || target.Team == _unit.Team) continue;

                var toTarget = target.transform.position - transform.position;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude < 0.01f) continue;

                float angle = Vector3.Angle(direction, toTarget);
                if (angle > ConeHalfAngle) continue;

                ApplySlow(target);
            }

            var animDriver = GetComponent<UnitAnimationDriver>();
            animDriver?.NotifyWebCast();

            SpawnWebVFX(transform.position + Vector3.up * 0.3f, direction);
        }

        static void ApplySlow(InsectUnit target)
        {
            var existing = target.GetComponent<WebSlowDebuff>();
            if (existing != null)
            {
                existing.Refresh(SlowDuration);
                return;
            }
            var debuff = target.gameObject.AddComponent<WebSlowDebuff>();
            debuff.Apply(SlowDuration, SlowFactor);
        }

        static void SpawnWebVFX(Vector3 origin, Vector3 direction)
        {
            var go = new GameObject("WebNetVFX");
            go.transform.position = origin;
            go.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 0.3f;
            main.playOnAwake = false;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.0f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 7f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.9f, 0.9f, 0.95f, 0.8f),
                new Color(0.8f, 0.8f, 0.85f, 0.6f));
            main.gravityModifier = 0.3f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 40;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, 20),
                new ParticleSystem.Burst(0.1f, 15)
            });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = ConeHalfAngle * 0.6f;
            shape.radius = 0.1f;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f,
                AnimationCurve.Linear(0f, 0.5f, 1f, 2f));

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.95f, 0.95f, 1f), 0f),
                    new GradientColorKey(new Color(0.7f, 0.7f, 0.75f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.7f, 0.15f),
                    new GradientAlphaKey(0.4f, 0.6f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = grad;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = GetWebMaterial();

            ps.Play();
            Object.Destroy(go, VfxAutoDestroy);
        }

        static Material GetWebMaterial()
        {
            if (s_webMat != null) return s_webMat;

            var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (sh == null) sh = Shader.Find("Particles/Standard Unlit");
            if (sh == null) sh = Shader.Find("Sprites/Default");

            s_webMat = new Material(sh) { name = "IW_WebNetMat" };

            if (s_webMat.HasProperty("_Surface"))
                s_webMat.SetFloat("_Surface", 1f);
            s_webMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            s_webMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            s_webMat.SetInt("_ZWrite", 0);
            s_webMat.color = new Color(0.9f, 0.9f, 0.95f, 0.7f);
            s_webMat.renderQueue = 3100;
            return s_webMat;
        }
    }

    /// <summary>
    /// Movement speed debuff applied by the Black Widow's web net.
    /// Reduces NavMeshAgent speed by a percentage for a duration.
    /// </summary>
    public class WebSlowDebuff : MonoBehaviour
    {
        float _remaining;
        float _slowFactor;
        InsectUnit _target;
        float _originalSpeed;
        bool _applied;

        public void Apply(float duration, float slowFactor)
        {
            _remaining = duration;
            _slowFactor = slowFactor;
            _target = GetComponent<InsectUnit>();
            if (_target != null && _target.Agent != null && !_applied)
            {
                _originalSpeed = _target.Agent.speed;
                _target.Agent.speed = _originalSpeed * _slowFactor;
                _applied = true;
            }
        }

        public void Refresh(float duration)
        {
            _remaining = duration;
        }

        void Update()
        {
            if (_target == null || !_target.IsAlive)
            {
                Destroy(this);
                return;
            }

            _remaining -= Time.deltaTime;
            if (_remaining <= 0f)
            {
                RestoreSpeed();
                Destroy(this);
            }
        }

        void OnDestroy()
        {
            RestoreSpeed();
        }

        void RestoreSpeed()
        {
            if (!_applied || _target == null || _target.Agent == null) return;
            _target.Agent.speed = _originalSpeed;
            _applied = false;
        }
    }
}
