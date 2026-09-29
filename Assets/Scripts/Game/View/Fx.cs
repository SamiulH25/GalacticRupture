using UnityEngine;

namespace GalacticRupture.Game
{
    // Pooled world-space particle systems driven through Emit(): one system per look,
    // any number of effects in flight.
    public sealed class Fx : MonoBehaviour
    {
        GameArt _art;
        ParticleSystem _flash, _sparks, _fireball, _embers, _shield, _smoke;

        public void Init(GameArt art)
        {
            _art = art;
            _flash = Make("Flash", art.Flash, ParticleSystemRenderMode.Billboard, fadeFast: true, grow: 1.6f);
            _sparks = Make("Sparks", art.Spark, ParticleSystemRenderMode.Stretch, fadeFast: false, grow: 0.6f);
            _fireball = Make("Fireball", art.Fire, ParticleSystemRenderMode.Billboard, fadeFast: false, grow: 2.2f, fireGradient: true);
            _embers = Make("Embers", art.Spark, ParticleSystemRenderMode.Stretch, fadeFast: false, grow: 0.8f, drag: 0.4f);
            _shield = Make("Shield", art.Shield, ParticleSystemRenderMode.Billboard, fadeFast: true, grow: 1.4f);
            _smoke = Make("Glow", art.Fire, ParticleSystemRenderMode.Billboard, fadeFast: false, grow: 1.8f, fireGradient: true);
        }

        ParticleSystem Make(string name, Material mat, ParticleSystemRenderMode mode, bool fadeFast, float grow,
            bool fireGradient = false, float drag = 0f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 20000;
            main.startSpeed = 0f;

            var emission = ps.emission;
            emission.enabled = false;
            var shape = ps.shape;
            shape.enabled = false;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            if (fireGradient)
            {
                g.SetKeys(
                    new[] { new GradientColorKey(new Color(1f, 0.95f, 0.8f), 0f), new GradientColorKey(new Color(1f, 0.55f, 0.2f), 0.25f), new GradientColorKey(new Color(0.6f, 0.12f, 0.05f), 0.7f), new GradientColorKey(new Color(0.2f, 0.05f, 0.03f), 1f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.3f), new GradientAlphaKey(0f, 1f) });
            }
            else
            {
                g.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(fadeFast ? 0.3f : 0.8f, fadeFast ? 0.3f : 0.5f), new GradientAlphaKey(0f, 1f) });
            }
            col.color = g;

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f / grow, 1f, 1f));

            if (drag > 0f)
            {
                var limit = ps.limitVelocityOverLifetime;
                limit.enabled = true;
                limit.drag = drag;
                limit.multiplyDragByParticleSize = false;
                limit.multiplyDragByParticleVelocity = false;
            }

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.renderMode = mode;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.maxParticleSize = 4f;
            if (mode == ParticleSystemRenderMode.Stretch)
            {
                r.velocityScale = 0.04f;
                r.lengthScale = 1.5f;
            }
            ps.Play();
            return ps;
        }

        static void Emit(ParticleSystem ps, Vector3 pos, Vector3 vel, float size, float life, Color color)
        {
            var e = new ParticleSystem.EmitParams
            {
                position = pos,
                velocity = vel,
                startSize = size,
                startLifetime = life,
                startColor = color,
                rotation = Random.Range(0f, 360f),
            };
            ps.Emit(e, 1);
        }

        public void MuzzleFlash(Vector3 pos, Vector3 dir, Vector3 shipVel, float scale, Color tint)
        {
            Emit(_flash, pos + dir * scale * 0.6f, shipVel, scale * 3.2f, 0.09f, tint);
            Emit(_flash, pos, shipVel, scale * 1.6f, 0.14f, Color.white);
            int n = Mathf.CeilToInt(scale * 2f);
            for (int i = 0; i < n; i++)
            {
                Vector3 v = (dir + Random.insideUnitSphere * 0.25f).normalized * Random.Range(40f, 160f) * scale * 0.6f;
                Emit(_sparks, pos, shipVel + v, scale * 0.25f, Random.Range(0.1f, 0.3f), tint);
            }
            Emit(_smoke, pos + dir * scale, shipVel + dir * 8f * scale, scale * 2.2f, 0.5f, new Color(0.5f, 0.35f, 0.25f, 0.35f));
        }

        public void HullImpact(Vector3 pos, Vector3 normal, float scale)
        {
            Emit(_flash, pos, Vector3.zero, scale * 6f, 0.12f, new Color(1f, 0.75f, 0.45f));
            Emit(_fireball, pos, normal * 5f, scale * 4f, 0.6f, Color.white);
            int n = Mathf.CeilToInt(4 + scale * 5f);
            for (int i = 0; i < n; i++)
            {
                Vector3 v = (normal + Random.insideUnitSphere * 0.9f).normalized * Random.Range(30f, 140f);
                Emit(_embers, pos, v, Random.Range(0.3f, 0.8f), Random.Range(0.4f, 1.4f), new Color(1f, 0.7f, 0.35f));
            }
        }

        public void ShieldImpact(Vector3 pos, Vector3 normal, float scale)
        {
            Emit(_shield, pos, Vector3.zero, scale * 14f, 0.35f, new Color(0.45f, 0.8f, 1f));
            Emit(_shield, pos, Vector3.zero, scale * 5f, 0.15f, Color.white);
            for (int i = 0; i < 4; i++)
            {
                Vector3 v = (normal + Random.insideUnitSphere).normalized * Random.Range(20f, 60f);
                Emit(_sparks, pos, v, 0.4f, 0.25f, new Color(0.5f, 0.85f, 1f));
            }
        }

        public void Explosion(Vector3 pos, Vector3 vel, float radius)
        {
            float s = radius;
            Emit(_flash, pos, vel, s * 9f, 0.25f, new Color(1f, 0.9f, 0.75f));
            Emit(_flash, pos, vel, s * 22f, 0.5f, new Color(1f, 0.55f, 0.25f, 0.5f));
            int balls = Mathf.Clamp((int)(radius * 1.5f), 8, 60);
            for (int i = 0; i < balls; i++)
            {
                Vector3 offset = Random.insideUnitSphere * s * 1.5f;
                Emit(_fireball, pos + offset, vel + offset * Random.Range(0.5f, 2f), s * Random.Range(1.2f, 3f), Random.Range(0.8f, 2.4f), Color.white);
            }
            int embers = Mathf.Clamp((int)(radius * 8f), 30, 400);
            for (int i = 0; i < embers; i++)
            {
                Vector3 v = Random.onUnitSphere * Random.Range(40f, 260f) * Mathf.Sqrt(radius / 6f);
                Emit(_embers, pos, vel + v, Random.Range(0.4f, 1.4f) * Mathf.Sqrt(radius / 6f), Random.Range(1f, 4f), new Color(1f, 0.65f, 0.3f));
            }
        }

        // A burning spot that stays with the ship: flame streams left behind as it moves.
        public static ParticleSystem MakeHullFire(GameObject go, GameArt art, float size)
        {
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size * 1.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(size * 0.5f, size * 2f);
            main.startColor = Color.white;
            main.maxParticles = 200;
            var emission = ps.emission;
            emission.rateOverTime = 26f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = size * 0.3f;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 0.85f, 0.55f), 0f), new GradientColorKey(new Color(1f, 0.4f, 0.12f), 0.3f), new GradientColorKey(new Color(0.3f, 0.07f, 0.03f), 1f) },
                new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.6f, 0.4f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var sz = ps.sizeOverLifetime;
            sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, 1.4f));
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = art.Fire;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.maxParticleSize = 4f;
            ps.Play();
            return ps;
        }
    }
}
