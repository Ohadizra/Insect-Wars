using UnityEngine;

namespace InsectWars.RTS
{
    /// <summary>
    /// WC3-style finite gather node. Workers stay for <see cref="GatherTickSeconds"/>
    /// then carry <see cref="gatherPerTick"/> calories back to the hive.
    /// Calorie count is shown in the bottom-bar UI when the node is selected.
    /// </summary>
    public class RottingFruitNode : MonoBehaviour
    {
        [SerializeField] int chargesRemaining = 100000;
        [SerializeField] int gatherPerTick = 10;
        [SerializeField] float gatherTickSeconds = 5f;

        int _initialCalories;

        public int ChargesRemaining => chargesRemaining;
        public int InitialCalories => _initialCalories;
        public bool Depleted => chargesRemaining <= 0;
        public float GatherTickSeconds => gatherTickSeconds;

        /// <summary>How close a worker must be (XZ) to start gathering.</summary>
        public float GatherRange => Mathf.Max(transform.localScale.x, transform.localScale.z) * 0.5f + 1.5f;

        /// <summary>Visual radius plus buffer — ants should navigate to this distance, not the center.</summary>
        public float StopRadius => Mathf.Max(transform.localScale.x, transform.localScale.z) * 0.5f + 0.5f;

        /// <summary>Returns a world position on the fruit surface facing the requester.</summary>
        public Vector3 GetGatherPoint(Vector3 fromPosition)
        {
            var dir = fromPosition - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) dir = Vector3.forward;
            return transform.position + dir.normalized * StopRadius;
        }

        void Awake()
        {
            _initialCalories = chargesRemaining;
        }

        void OnEnable()
        {
            RtsSimRegistry.Register(this);
        }

        void OnDisable()
        {
            RtsSimRegistry.Unregister(this);
        }

        public void Configure(int calories, int perPickup, float gatherSeconds)
        {
            chargesRemaining = calories;
            _initialCalories = calories;
            gatherPerTick = perPickup;
            gatherTickSeconds = gatherSeconds;
        }

        public bool TryHarvest(out int amount)
        {
            amount = 0;
            if (Depleted) return false;
            amount = Mathf.Min(gatherPerTick, chargesRemaining);
            chargesRemaining -= amount;
            if (chargesRemaining <= 0)
            {
                chargesRemaining = 0;
                var r = GetComponent<Renderer>();
                if (r != null)
                {
                    var b = new MaterialPropertyBlock();
                    r.GetPropertyBlock(b);
                    b.SetColor("_BaseColor", new Color(0.25f, 0.18f, 0.08f));
                    r.SetPropertyBlock(b);
                }
            }
            return true;
        }
    }
}
