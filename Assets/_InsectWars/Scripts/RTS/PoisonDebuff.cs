using UnityEngine;

namespace InsectWars.RTS
{
    /// <summary>
    /// Damage-over-time debuff applied by Black Widow melee attacks.
    /// Re-application refreshes the duration without stacking damage.
    /// </summary>
    public class PoisonDebuff : MonoBehaviour
    {
        public const float DefaultDuration = 15f;
        public const float DefaultDps = 2f;

        float _remaining;
        float _tickTimer;
        float _damagePerSecond;
        InsectUnit _target;

        public void Apply(float duration, float dps)
        {
            _remaining = duration;
            _damagePerSecond = dps;
        }

        void Awake()
        {
            _target = GetComponent<InsectUnit>();
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
                Destroy(this);
                return;
            }

            _tickTimer += Time.deltaTime;
            if (_tickTimer >= 1f)
            {
                _tickTimer -= 1f;
                _target.ApplyDamage(_damagePerSecond);
            }
        }
    }
}
