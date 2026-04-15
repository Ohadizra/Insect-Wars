using UnityEngine;

namespace InsectWars.RTS
{
    /// <summary>
    /// Passive healing aura — heals nearby friendly units over time.
    /// Multiple auras do NOT stack: each unit can only be healed by
    /// one aura per frame (tracked via <see cref="InsectUnit.HealAuraFrame"/>).
    /// The moth does not heal itself.
    /// </summary>
    public class HealingAura : MonoBehaviour
    {
        const float Radius = 8f;
        const float HealPerSecond = 3f;

        static readonly Collider[] s_hits = new Collider[64];

        InsectUnit _unit;

        void Awake()
        {
            _unit = GetComponent<InsectUnit>();
        }

        void Update()
        {
            if (_unit == null || !_unit.IsAlive) return;

            int frame = Time.frameCount;
            float heal = HealPerSecond * Time.deltaTime;

            int count = Physics.OverlapSphereNonAlloc(transform.position, Radius, s_hits);
            for (int i = 0; i < count; i++)
            {
                var target = s_hits[i].GetComponentInParent<InsectUnit>();
                if (target == null || !target.IsAlive) continue;
                if (target == _unit) continue;
                if (target.Team != _unit.Team) continue;
                if (target.HealAuraFrame == frame) continue;
                if (target.CurrentHealth >= target.MaxHealth) continue;

                target.Heal(heal);
                target.HealAuraFrame = frame;
            }
        }
    }
}
