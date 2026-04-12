using UnityEngine;

namespace InsectWars.RTS
{
    public class Projectile : MonoBehaviour
    {
        Team _ownerTeam;
        float _damage;
        float _speed;
        float _life;
        InsectUnit _homingTarget;
        Vector3 _lastTargetPos;
        static int s_unitsLayer = -1;
        static Material s_fallbackProjectileMat;
        static readonly MaterialPropertyBlock s_pb = new();

        public static Projectile SpawnHoming(Vector3 position, InsectUnit target, Team ownerTeam, float damage,
            float speed, float maxLifetime, GameObject prefab = null)
        {
            var dir = target != null ? (target.transform.position + Vector3.up * 0.35f - position).normalized : Vector3.forward;
            GameObject go;
            if (prefab != null)
                go = Instantiate(prefab, position, Quaternion.LookRotation(dir, Vector3.up));
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = "Projectile";
                go.transform.position = position;
                go.transform.localScale = Vector3.one * 0.22f;
                Object.Destroy(go.GetComponent<Collider>());
                var col = go.AddComponent<SphereCollider>();
                col.isTrigger = true;
                col.radius = 1f;
                var rb = go.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
                ApplyGlowMat(go, new Color(1f, 0.5f, 0.1f));
            }

            var p = go.GetComponent<Projectile>();
            if (p == null) p = go.AddComponent<Projectile>();
            p._ownerTeam = ownerTeam;
            p._damage = damage;
            p._speed = speed;
            p._life = maxLifetime;
            p._homingTarget = target;
            if (target != null) p._lastTargetPos = target.transform.position + Vector3.up * 0.35f;
            if (s_unitsLayer < 0) s_unitsLayer = LayerMask.NameToLayer("Units");
            if (s_unitsLayer >= 0) go.layer = s_unitsLayer;
            return p;
        }

        static void ApplyGlowMat(GameObject go, Color c)
        {
            var r = go.GetComponent<Renderer>();
            if (r == null) return;
            if (s_fallbackProjectileMat == null)
            {
                var sh = Shader.Find("Universal Render Pipeline/Lit");
                if (sh == null) sh = Shader.Find("Sprites/Default");
                s_fallbackProjectileMat = new Material(sh) { name = "IW_ProjectileFallback" };
                if (s_fallbackProjectileMat.HasProperty("_EmissionColor"))
                    s_fallbackProjectileMat.EnableKeyword("_EMISSION");
            }
            r.sharedMaterial = s_fallbackProjectileMat;
            s_pb.Clear();
            if (s_fallbackProjectileMat.HasProperty("_BaseColor")) s_pb.SetColor("_BaseColor", c);
            if (s_fallbackProjectileMat.HasProperty("_Color")) s_pb.SetColor("_Color", c);
            if (s_fallbackProjectileMat.HasProperty("_EmissionColor"))
                s_pb.SetColor("_EmissionColor", c * 2.2f);
            r.SetPropertyBlock(s_pb);
        }

        void Update()
        {
            _life -= Time.deltaTime;
            if (_life <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 dir;
            if (_homingTarget != null && _homingTarget.IsAlive)
            {
                _lastTargetPos = _homingTarget.transform.position + Vector3.up * 0.35f;
                dir = (_lastTargetPos - transform.position).normalized;
            }
            else
                dir = (_lastTargetPos - transform.position).normalized;

            if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;
            transform.position += dir * (_speed * Time.deltaTime);
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }

        void OnTriggerEnter(Collider other)
        {
            var u = other.GetComponentInParent<InsectUnit>();
            if (u != null && u.IsAlive && u.Team != _ownerTeam)
            {
                u.ApplyDamage(_damage);
                Destroy(gameObject);
            }
        }
    }
}
