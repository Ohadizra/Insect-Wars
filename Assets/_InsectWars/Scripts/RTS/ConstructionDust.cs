using UnityEngine;

namespace InsectWars.RTS
{
    public class ConstructionDust : MonoBehaviour
    {
        public float lifetime = 1.0f;
        public float maxScale = 2.0f;
        
        float _timer;
        Vector3 _startScale;
        Vector3 _driftDir;
        Material _mat;
        Color _startColor;

        void Start()
        {
            _startScale = transform.localScale;
            // Random drift direction for natural dispersion
            _driftDir = new Vector3(Random.Range(-0.3f, 0.3f), 1.0f, Random.Range(-0.3f, 0.3f)).normalized;
            
            var rend = GetComponent<Renderer>();
            if (rend != null)
            {
                _mat = rend.material;
                if (_mat.HasProperty("_BaseColor"))
                    _startColor = _mat.GetColor("_BaseColor");
                else if (_mat.HasProperty("_Color"))
                    _startColor = _mat.color;
                else
                    _startColor = new Color(0.85f, 0.85f, 0.85f, 0.5f); // Realistic grey fallback
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

            // Grow over time (dissipation)
            transform.localScale = Vector3.Lerp(_startScale, _startScale * maxScale, t);
            
            // Fade out
            if (_mat != null)
            {
                Color c = _startColor;
                // Cubic fade out for softer end
                c.a *= (1.0f - (t * t * t));
                
                if (_mat.HasProperty("_BaseColor"))
                    _mat.SetColor("_BaseColor", c);
                else
                    _mat.color = c;
            }
            
            // Drift and disperse
            float speed = Mathf.Lerp(0.6f, 0.2f, t); // Slow down as it expands
            transform.position += _driftDir * Time.deltaTime * speed;
        }
    }
}
