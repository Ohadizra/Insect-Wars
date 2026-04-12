using UnityEngine;

namespace InsectWars.RTS
{
    public class ConstructionDust : MonoBehaviour
    {
        public float lifetime = 1.0f;
        public float maxScale = 2.0f;
        
        float _timer;
        Vector3 _startScale;
        Material _mat;
        Color _startColor;

        void Start()
        {
            _startScale = transform.localScale;
            var rend = GetComponent<Renderer>();
            if (rend != null)
            {
                _mat = rend.material;
                _startColor = _mat.color;
            }
        }

        void Update()
        {
            _timer += Time.deltaTime;
            float t = _timer / lifetime;
            
            if (t >= 1.0f)
            {
                Destroy(gameObject);
                return;
            }

            // Grow over time
            transform.localScale = Vector3.Lerp(_startScale, _startScale * maxScale, t);
            
            // Fade out
            if (_mat != null)
            {
                Color c = _startColor;
                c.a *= (1.0f - t);
                _mat.color = c;
            }
            
            // Drift upward
            transform.position += Vector3.up * Time.deltaTime * 0.5f;
        }
    }
}
