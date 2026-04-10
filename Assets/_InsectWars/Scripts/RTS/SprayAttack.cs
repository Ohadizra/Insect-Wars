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
                u.ApplyDamage(damage * falloff, owner);
            }
        }

        static void SpawnSprayVFX(Vector3 origin, Vector3 direction, float range)
        {
            var go = new GameObject("BombardierSpray");
            go.transform.position = origin;
            go.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.3f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(ParticleLifetime * 0.7f, ParticleLifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(ParticleSpeed * 0.7f, ParticleSpeed);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.25f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 1f, 1f, 0.85f),
                new Color(0.95f, 0.92f, 0.82f, 0.7f));
            main.gravityModifier = -0.15f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = ParticleBurst + 10;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, ParticleBurst) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = ConeHalfAngle;
            shape.radius = 0.08f;
            shape.radiusThickness = 1f;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f,
                AnimationCurve.Linear(0f, 0.4f, 1f, 1f));

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.9f, 0.88f, 0.78f), 1f) },
                new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.5f, 0.5f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = grad;

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.6f;
            noise.frequency = 3f;
            noise.scrollSpeed = 1.5f;
            noise.quality = ParticleSystemNoiseQuality.Medium;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = GetSprayMaterial();

            Object.Destroy(go, VfxAutoDestroy);
        }

        static Material GetSprayMaterial()
        {
            if (s_sprayMat != null) return s_sprayMat;
            var sh = Shader.Find("Particles/Standard Unlit");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            s_sprayMat = new Material(sh) { name = "IW_SprayMat" };
            s_sprayMat.SetFloat("_Mode", 1f); // additive-ish
            if (s_sprayMat.HasProperty("_ColorMode"))
                s_sprayMat.SetFloat("_ColorMode", 1f);
            s_sprayMat.color = new Color(1f, 1f, 1f, 0.7f);
            s_sprayMat.renderQueue = 3100;
            return s_sprayMat;
        }
    }
}
