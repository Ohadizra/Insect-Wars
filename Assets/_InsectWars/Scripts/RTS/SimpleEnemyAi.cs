using UnityEngine;

namespace InsectWars.RTS
{
    /// <summary>
    /// Idle until a player unit enters vision, then attack-move toward nearest player.
    /// </summary>
    public class SimpleEnemyAi : MonoBehaviour, IAiController
    {
        [SerializeField] float thinkInterval = 1.2f;
        InsectUnit _self;
        float _timer;
        bool _aggro;

        void Awake()
        {
            _self = GetComponent<InsectUnit>();
        }

        void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Tick(float deltaTime)
        {
            if (_self == null || !_self.IsAlive || _self.Team != Team.Enemy) return;
            _timer -= deltaTime;
            if (_timer > 0) return;
            _timer = thinkInterval;
            var range = _self.Definition != null ? _self.Definition.visionRadius : 12f;
            if (!_aggro)
            {
                foreach (var u in FindObjectsByType<InsectUnit>(FindObjectsSortMode.None))
                {
                    if (u.Team != Team.Player || !u.IsAlive) continue;
                    if (Vector3.Distance(transform.position, u.transform.position) <= range)
                    {
                        _aggro = true;
                        break;
                    }
                }
                return;
            }
            InsectUnit best = null;
            var bestD = float.MaxValue;
            foreach (var u in FindObjectsByType<InsectUnit>(FindObjectsSortMode.None))
            {
                if (u.Team != Team.Player || !u.IsAlive) continue;
                var d = Vector3.Distance(transform.position, u.transform.position);
                if (d < bestD)
                {
                    bestD = d;
                    best = u;
                }
            }
            if (best != null &&
                !(_self.CurrentOrder == UnitOrder.Attack && _self.AttackTarget == best.transform))
                _self.OrderAttack(best);
        }
    }
}
