using UnityEngine;

namespace FruitNinjaVR
{
    public static class ProceduralAudio
    {
        private static AudioClip s_SliceClip;
        private static AudioClip s_LaunchClip;
        private static AudioClip s_GrabClip;
        private static AudioClip s_FizzleClip;

        public static AudioClip SliceClip
        {
            get
            {
                if (s_SliceClip == null) s_SliceClip = GenerateSliceSound();
                return s_SliceClip;
            }
        }

        public static AudioClip LaunchClip
        {
            get
            {
                if (s_LaunchClip == null) s_LaunchClip = GenerateLaunchSound();
                return s_LaunchClip;
            }
        }

        public static AudioClip GrabClip
        {
            get
            {
                if (s_GrabClip == null) s_GrabClip = GenerateGrabSound();
                return s_GrabClip;
            }
        }

        public static AudioClip FizzleClip
        {
            get
            {
                if (s_FizzleClip == null) s_FizzleClip = GenerateFizzleSound();
                return s_FizzleClip;
            }
        }

        private static AudioClip GenerateSliceSound()
        {
            const int sampleRate = 44100;
            const float duration = 0.22f;
            int numSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;

                // High-to-low swoosh freq
                float freq = Mathf.Lerp(1200f, 250f, Mathf.Pow(progress, 0.7f));
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t);

                // Noise squish burst
                float noise = (Random.value * 2f - 1f) * (1f - progress);

                // Envelope: instant attack, exponential decay
                float envelope = Mathf.Exp(-progress * 9f);

                samples[i] = Mathf.Clamp((wave * 0.55f + noise * 0.45f) * envelope * 0.9f, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("ProceduralSlice", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip GenerateLaunchSound()
        {
            const int sampleRate = 44100;
            const float duration = 0.28f;
            int numSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;

                // Pitch sweep upward then settle
                float freq = Mathf.Lerp(90f, 220f, Mathf.Sin(progress * Mathf.PI * 0.5f));
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t);
                float noise = (Random.value * 2f - 1f) * 0.25f;

                float envelope = Mathf.Sin(progress * Mathf.PI) * (1f - progress * 0.5f);
                samples[i] = Mathf.Clamp((wave * 0.8f + noise * 0.2f) * envelope * 0.75f, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("ProceduralLaunch", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip GenerateGrabSound()
        {
            const int sampleRate = 44100;
            const float duration = 0.15f;
            int numSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;

                // Metallic ping
                float freq1 = 880f;
                float freq2 = 1760f;
                float wave = Mathf.Sin(2f * Mathf.PI * freq1 * t) * 0.6f + Mathf.Sin(2f * Mathf.PI * freq2 * t) * 0.4f;
                float envelope = Mathf.Exp(-progress * 15f);

                samples[i] = Mathf.Clamp(wave * envelope * 0.6f, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("ProceduralGrab", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip GenerateFizzleSound()
        {
            const int sampleRate = 44100;
            const float duration = 0.45f;
            int numSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;

                // High frequency crackle/sizzle with rising airy resonance
                float noise = (Random.value * 2f - 1f);
                float hissFreq = Mathf.Lerp(2200f, 3800f, progress);
                float hissWave = Mathf.Sin(2f * Mathf.PI * hissFreq * t);

                // Bubbling pop mod
                float bubbleMod = Mathf.Sin(2f * Mathf.PI * 45f * t) * 0.3f + 0.7f;

                // Envelope: quick attack, bubbling fizzle decay
                float envelope = Mathf.Pow(1f - progress, 1.8f);
                samples[i] = Mathf.Clamp((noise * 0.75f + hissWave * 0.25f) * bubbleMod * envelope * 0.65f, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("ProceduralFizzle", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
