using UnityEngine;

namespace InsectWars.RTS
{
    /// <summary>
    /// Single-use cacti seed pickup scattered around the map.
    /// Workers walk to it, pick it up instantly, then carry it
    /// to the nearest team nest for deposit.
    /// </summary>
    public class CactiSeedNode : MonoBehaviour
    {
        bool _pickedUp;

        public bool PickedUp => _pickedUp;

        public float PickupRange => Mathf.Max(transform.localScale.x, transform.localScale.z) * 0.5f + 1.5f;

        void OnEnable()
        {
            RtsSimRegistry.Register(this);
        }

        void OnDisable()
        {
            RtsSimRegistry.Unregister(this);
        }

        /// <summary>
        /// Try to pick up this seed. Returns true exactly once; subsequent calls return false.
        /// </summary>
        public bool TryPickup()
        {
            if (_pickedUp) return false;
            _pickedUp = true;
            gameObject.SetActive(false);
            return true;
        }
    }
}
