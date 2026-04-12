using UnityEngine;

namespace InsectWars.RTS
{
    public static class SprayAttack
    {
        const float ConeHalfAngle = 35f;
        const int ParticleBurst = 50;
        const float ParticleLifetime = 0.45f;
        const float ParticleSpeed = 9f;
        const float VfxAutoDestroy = 1.2f;

        static readonly Collider[] s_hits = new Collider[64];
        static Material s_sprayMat;
        static Texture2D s_softCircle;

        public static void Fire(Vector3 origin, Vector3 direction, float range,
            Team ownerTeam, float damage, InsectUnit owner)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f) direction = Vector3.forward;
            direction.Normalize();

            DealConeDamage(origin, direction, range, ownerTeam, damage, owner);
            SpawnSprayVFX(origin, direction, range);
        }

        static void DealConeDamage(Vector3 origin, Vector3 dir, float range,
            Team ownerTeam, float damage, InsectUnit owner)
        {
            int count = Physics.OverlapSphereNonAlloc(origin, range, s_hits);
            for (int i = 0; i < count; i++)
            {
                var u = s_hits[i].GetComponentInParent<InsectUnit>();
                if (u == null || !u.IsAlive || u.Team == ownerTeam) continue;

                var toTarget = u.transform.position - origin;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude < 0.01f) continue;

                float angle = Vector3.Angle(dir, toTarget);
                if (angle > ConeHalfAngle) continue;

                float distFraction = toTarget.magnitude / range;
                float falloff = Mathf.Lerp(1f, 0.6f, distFraction);
                u.ApplyDamage(damage * falloff);
            }
        }

        static void SpawnSprayVFX(Vector3 origin, Vector3 direction, float range)
        {
            var go = new GameObject("BombardierSpray");
            go.transform.position = origin;
            go.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

            var ps = go.AddComponent<ParticleSystem>();
            // Ensure the system is stopped before we configure duration and other locked properties
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 0.5f;
            main.playOnAwake = false;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(ParticleLifetime * 0.9f, ParticleLifetime * 1.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(ParticleSpeed * 0.9f, ParticleSpeed * 1.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.55f); // Increased size
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 1f, 1f, 1.0f),
                new Color(0.98f, 0.95f, 0.8f, 1.0f)); // Higher initial alpha
            main.gravityModifier = -0.05f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = ParticleBurst + 60;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            // High-frequency pulsed spray (more staccato)
            emission.SetBursts(new[] { 
                new ParticleSystem.Burst(0.00f, ParticleBurst / 5),
                new ParticleSystem.Burst(0.05f, ParticleBurst / 5),
                new ParticleSystem.Burst(0.10f, ParticleBurst / 5),
                new ParticleSystem.Burst(0.15f, ParticleBurst / 5),
                new ParticleSystem.Burst(0.20f, ParticleBurst / 5)
            });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = ConeHalfAngle * 0.75f; 
            shape.radius = 0.05f;
            shape.radiusThickness = 1f;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f,
                AnimationCurve.Linear(0f, 0.4f, 1f, 1.6f)); // Larger dissipation

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var grad = new Gradient();
            // Higher opacity and more toxic yellowish-brown tones
            grad.SetKeys(
                new[] { 
                    new GradientColorKey(Color.white, 0f), 
                    new GradientColorKey(new Color(1f, 0.95f, 0.6f), 0.3f), // Caustic yellow
                    new GradientColorKey(new Color(0.8f, 0.75f, 0.5f), 1f)  // Dissipating brown
                },
                new[] { 
                    new GradientAlphaKey(0.0f, 0f), 
                    new GradientAlphaKey(1.0f, 0.1f), // Fast build up
                    new GradientAlphaKey(0.8f, 0.5f), // High mid-life opacity
                    new GradientAlphaKey(0.0f, 1f) 
                });
            colorOverLifetime.color = grad;

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 1.8f; // More violent jitter
            noise.frequency = 4f;
            noise.scrollSpeed = 2.5f;
            noise.quality = ParticleSystemNoiseQuality.Medium;

            var trail = ps.trails;
            trail.enabled = true;
            trail.ratio = 0.45f;
            trail.lifetime = 0.15f;
            trail.widthOverTrail = new ParticleSystem.MinMaxCurve(0.5f, 0.1f);
            trail.colorOverLifetime = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.95f, 0.7f, 0.6f), 
                new Color(1f, 1f, 1f, 0f));

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = GetSprayMaterial();
            renderer.trailMaterial = GetSprayMaterial();

            ps.Play();
            Object.Destroy(go, VfxAutoDestroy);
        }

        static Material GetSprayMaterial()
        {
            if (s_sprayMat != null) return s_sprayMat;

            var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (sh == null) sh = Shader.Find("Particles/Standard Unlit");
            if (sh == null) sh = Shader.Find("Sprites/Default");

            s_sprayMat = new Material(sh) { name = "IW_SprayMat" };

            // Load the generated high-quality spray puff
        #if UNITY_EDITOR
            var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_InsectWars/Sprites/VFX/BombardierSprayPuff.png");
            if (tex != null) s_sprayMat.mainTexture = tex;
            else s_sprayMat.mainTexture = GetSoftCircleTexture();
        #else
            s_sprayMat.mainTexture = GetSoftCircleTexture();
        #endif

            if (s_sprayMat.HasProperty("_Surface"))
                s_sprayMat.SetFloat("_Surface", 1f);  // Transparent
            
            // Standard Alpha Blending for better visibility on all backgrounds
            if (s_sprayMat.HasProperty("_Blend"))
                s_sprayMat.SetFloat("_Blend", 0f);    // 0 = Alpha, 1 = Additive
            
            s_sprayMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            s_sprayMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            s_sprayMat.SetInt("_ZWrite", 0);
            
            s_sprayMat.color = new Color(1f, 1f, 1f, 1f);
            s_sprayMat.renderQueue = 3100;
            return s_sprayMat;
            }

            static Texture2D GetSoftCircleTexture()
            {
            if (s_softCircle != null) return s_softCircle;
            const int res = 64;
            s_softCircle = new Texture2D(res, res, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            float center = (res - 1) * 0.5f;
            var pixels = new Color[res * res];
            for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                float dx = (x - center) / center;
                float dy = (y - center) / center;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - d);
                a *= a;
                pixels[y * res + x] = new Color(1f, 1f, 1f, a);
            }
            s_softCircle.SetPixels(pixels);
            s_softCircle.Apply(false, true);
            return s_softCircle;
            }
            }
            }
