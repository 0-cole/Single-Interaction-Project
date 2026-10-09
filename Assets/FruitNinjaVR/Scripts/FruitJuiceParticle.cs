using UnityEngine;

namespace FruitNinjaVR
{
    public static class FruitJuiceParticle
    {
        private static Material s_ParticleMaterial;

        public static void SpawnSplash(Vector3 position, Color juiceColor)
        {
            GameObject splashGo = new GameObject("JuiceSplash");
            splashGo.transform.position = position;

            ParticleSystem ps = splashGo.AddComponent<ParticleSystem>();
            ParticleSystemRenderer psRenderer = splashGo.GetComponent<ParticleSystemRenderer>();

            // Stop default emission
            var main = ps.main;
            main.loop = false;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 5.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.09f);
            main.startColor = juiceColor;
            main.gravityModifier = 1.2f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0f, 30, 45)
            });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 1f);
            curve.AddKey(1f, 0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

            if (s_ParticleMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
                if (shader == null) shader = Shader.Find("Standard");

                s_ParticleMaterial = new Material(shader);
            }

            psRenderer.material = s_ParticleMaterial;
            ps.Play();
        }

        public static void SpawnFizzlePuff(Vector3 position, Color color)
        {
            GameObject fizzleGo = new GameObject("FruitFizzlePuff");
            fizzleGo.transform.position = position;

            ParticleSystem ps = fizzleGo.AddComponent<ParticleSystem>();
            ParticleSystemRenderer psRenderer = fizzleGo.GetComponent<ParticleSystemRenderer>();

            var main = ps.main;
            main.loop = false;
            main.duration = 0.8f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
            Color puffColor = Color.Lerp(color, Color.white, 0.4f);
            puffColor.a = 0.8f;
            main.startColor = puffColor;
            main.gravityModifier = -0.3f; // Float upward gently like smoke/steam
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0f, 20, 30)
            });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 0.4f);
            curve.AddKey(0.4f, 1f);
            curve.AddKey(1f, 0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

            if (s_ParticleMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
                if (shader == null) shader = Shader.Find("Standard");

                s_ParticleMaterial = new Material(shader);
            }

            psRenderer.material = s_ParticleMaterial;
            ps.Play();
        }
    }
}
