using UnityEngine;

namespace InsectWars.RTS
{
    /// <summary>
    /// Giant Stag Beetle's ground stomp — AoE slow that triggers only while
    /// the beetle is in combat (attacking a unit, attacking a building, or
    /// attack-moving). Pulses every <see cref="StompInterval"/> seconds.
    /// </summary>
    public class GroundStomp : MonoBehaviour
    {
        const float StompRadius = 6f;
        const float StompInterval = 8f;
        const float SlowDuration = 3f;
        const float MoveSpeedMultiplier = 0.5f;
        const float AttackSpeedMultiplier = 0.6f;

        InsectUnit _unit;
        float _stompTimer;

        void Awake() => _unit = GetComponent<InsectUnit>();

        static bool IsInCombat(InsectUnit u)
        {
            var order = u.CurrentOrder;
            return order == UnitOrder.Attack
                || order == UnitOrder.AttackBuilding
                || order == UnitOrder.AttackMove;
        }

        void Update()
        {
            if (_unit == null || !_unit.IsAlive) return;
            if (!IsInCombat(_unit)) return;

            _stompTimer += Time.deltaTime;
            if (_stompTimer < StompInterval) return;
            _stompTimer = 0f;

            if (GetComponentInChildren<UnitAnimationDriver>() is { } driver)
                driver.NotifyStomp();

            foreach (var u in RtsSimRegistry.Units)
            {
                if (u == null || !u.IsAlive || u.Team == _unit.Team) continue;
                if (Vector3.Distance(transform.position, u.transform.position) > StompRadius) continue;

                var debuff = u.GetComponent<StompSlow>();
if (debuff == null) debuff = u.gameObject.AddComponent<StompSlow>();
                debuff.Apply(SlowDuration, MoveSpeedMultiplier, AttackSpeedMultiplier);
            }
        }
    }

    /// <summary>
    /// Debuff applied to enemies by <see cref="GroundStomp"/>.
    /// Reduces NavMeshAgent speed and stretches attack cooldown.
    /// Stacks refresh duration but do not multiply.
    /// </summary>
    public class StompSlow : MonoBehaviour
    {
        float _remaining;
        float _moveMultiplier = 1f;
        float _atkMultiplier = 1f;

        float _originalSpeed = -1f;
        UnityEngine.AI.NavMeshAgent _agent;
        InsectUnit _unit;
        GameObject _debuffContainer;

        public float AttackSpeedMultiplier => _remaining > 0f ? _atkMultiplier : 1f;

        public void Apply(float duration, float moveMult, float atkMult)
        {
            if (_agent == null) _agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (_unit == null) _unit = GetComponent<InsectUnit>();

            if (_originalSpeed < 0f && _agent != null)
                _originalSpeed = _agent.speed;

            _remaining = duration;
            _moveMultiplier = moveMult;
            _atkMultiplier = atkMult;

            if (_agent != null && _originalSpeed >= 0f)
                _agent.speed = _originalSpeed * _moveMultiplier;

            EnsureStars();
        }

        void EnsureStars()
        {
            if (_debuffContainer != null)
            {
                _debuffContainer.SetActive(true);
                return;
            }

            _debuffContainer = new GameObject("StompStunStars");
            _debuffContainer.transform.SetParent(transform, false);
            _debuffContainer.transform.localPosition = new Vector3(0f, 1.8f, 0f);
            
            var starSprite = Resources.Load<Sprite>("VFX/StunStar") ?? Resources.Load<Sprite>("UI/Debuff_Slow");
            
            for (int i = 0; i < 3; i++)
            {
                var star = new GameObject("Star_" + i);
                star.transform.SetParent(_debuffContainer.transform, false);
                var sr = star.AddComponent<SpriteRenderer>();
                sr.sprite = starSprite;
                sr.color = new Color(1f, 0.9f, 0.2f, 0.8f);
                star.transform.localScale = Vector3.one * 0.15f;
                var orbiter = star.AddComponent<StunStarOrbiter>();
                orbiter.index = i;
            }
        }

        void Update()
        {
            if (_remaining <= 0f) return;

            _remaining -= Time.deltaTime;
            if (_remaining <= 0f)
            {
                if (_agent != null && _originalSpeed >= 0f)
                    _agent.speed = _originalSpeed;
                _originalSpeed = -1f;
                if (_debuffContainer != null) _debuffContainer.SetActive(false);
            }
        }

        void OnDestroy()
        {
            if (_agent != null && _originalSpeed >= 0f)
                _agent.speed = _originalSpeed;
        }
    }

    public class StunStarOrbiter : MonoBehaviour
    {
        public int index;
        float _timer;

        void Update()
        {
            _timer += Time.deltaTime * 4f;
            float angle = _timer + (index * (Mathf.PI * 2f / 3f));
            float radius = 0.4f + Mathf.Sin(_timer * 0.5f) * 0.05f;
            transform.localPosition = new Vector3(Mathf.Cos(angle) * radius, 0.2f * Mathf.Sin(angle * 0.5f), Mathf.Sin(angle) * radius);
            
            if (Camera.main != null)
                transform.rotation = Camera.main.transform.rotation;
        }
    }

    public class StompVfxFade : MonoBehaviour
    {
        public float duration = 1.0f;
        public float maxScale = 6.0f;
        float _timer;
        MeshRenderer _renderer;
        Material _mat;
        Vector3 _baseScale;

        void Start()
        {
            _renderer = GetComponent<MeshRenderer>();
            if (_renderer != null) _mat = _renderer.material;
            _baseScale = transform.localScale;
            Destroy(gameObject, duration + 0.1f);
        }

        void Update()
        {
            _timer += Time.deltaTime;
            float p = Mathf.Clamp01(_timer / duration);
            
            // Expand like a wave
            float scaleMult = Mathf.Pow(p, 0.4f); 
            transform.localScale = _baseScale * scaleMult * maxScale / 5f;

            // Fade out
            float alpha = 1f - Mathf.Pow(p, 2f);
            if (_mat != null)
            {
                if (_mat.HasProperty("_BaseColor"))
                {
                    Color col = _mat.GetColor("_BaseColor");
                    col.a = alpha;
                    _mat.SetColor("_BaseColor", col);
                }
                else if (_mat.HasProperty("_Color"))
                {
                    Color col = _mat.color;
                    col.a = alpha;
                    _mat.color = col;
                }
            }
        }
    }
    }
