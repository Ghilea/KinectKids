using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KinectKids3D
{
    public sealed partial class SpokjaktenGame
    {
        private static AudioClip[] LoadClipSet(string resourceFolder, string namePrefix, AudioClip fallback)
        {
            AudioClip[] loaded = Resources.LoadAll<AudioClip>(resourceFolder)
                .Where(clip => clip != null && clip.name.StartsWith(namePrefix, StringComparison.OrdinalIgnoreCase))
                .OrderBy(clip => clip.name)
                .ToArray();
            return loaded.Length > 0 ? loaded : new[] { fallback };
        }

        private static AudioClip[] LoadNamedClips(AudioClip fallback, params string[] resourcePaths)
        {
            AudioClip[] loaded = resourcePaths
                .Select(path => Resources.Load<AudioClip>(path))
                .Where(clip => clip != null)
                .ToArray();
            return loaded.Length > 0 ? loaded : new[] { fallback };
        }

        private static AudioClip RandomClip(AudioClip[] clips)
        {
            return clips == null || clips.Length == 0 ? null : clips[UnityEngine.Random.Range(0, clips.Length)];
        }

        private static void PlayRandom(AudioSource source, AudioClip[] clips, float volume = 1f)
        {
            AudioClip clip = RandomClip(clips);
            if (source != null && clip != null) source.PlayOneShot(clip, volume);
        }

        private static AudioClip CreateTone(string clipName, float frequency, float duration, float volume)
        {
            const int sampleRate = 44100;
            int length = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)sampleRate;
                float envelope = 1f - i / (float)length;
                samples[i] = Mathf.Sin(t * frequency * Mathf.PI * 2f) * envelope * volume;
            }
            AudioClip clip = AudioClip.Create(clipName, length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateAmbience()
        {
            const int sampleRate = 22050;
            const int seconds = 4;
            float[] samples = new float[sampleRate * seconds];
            var random = new System.Random(731);
            float filteredNoise = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)sampleRate;
                float noise = (float)(random.NextDouble() * 2 - 1);
                filteredNoise = Mathf.Lerp(filteredNoise, noise, 0.015f);
                samples[i] = (Mathf.Sin(t * 43f * Mathf.PI * 2f) * 0.035f + filteredNoise * 0.06f);
            }
            AudioClip clip = AudioClip.Create("Spöktunnelns atmosfär", samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateRideMusic()
        {
            const int sampleRate = 22050;
            const int seconds = 16;
            float[] samples = new float[sampleRate * seconds];
            float[] melody = { 220f, 261.63f, 293.66f, 329.63f, 293.66f, 261.63f, 246.94f, 196f };
            float[] bass = { 55f, 65.41f, 49f, 55f };
            var random = new System.Random(1313);
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)sampleRate;
                int melodyStep = Mathf.FloorToInt(t * 2f) % melody.Length;
                int bassStep = Mathf.FloorToInt(t / 4f) % bass.Length;
                float notePhase = (t * 2f) % 1f;
                float noteEnvelope = Mathf.Clamp01(1f - notePhase * 1.15f);
                float arpeggio = Mathf.Sin(t * melody[melodyStep] * Mathf.PI * 2f) * noteEnvelope * 0.055f;
                float bell = Mathf.Sin(t * melody[melodyStep] * 2f * Mathf.PI * 2f) * noteEnvelope * 0.014f;
                float lowDrone = Mathf.Sin(t * bass[bassStep] * Mathf.PI * 2f) * 0.052f;
                float beatPhase = (t * 2f) % 1f;
                float beat = beatPhase < 0.055f
                    ? ((float)random.NextDouble() * 2f - 1f) * (1f - beatPhase / 0.055f) * 0.055f
                    : 0f;
                float swell = Mathf.Sin(t * Mathf.PI / 4f) * 0.018f;
                samples[i] = Mathf.Clamp(arpeggio + bell + lowDrone + beat + swell, -0.35f, 0.35f);
            }
            AudioClip clip = AudioClip.Create("Spökjaktens musik", samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateGhostVoice(string clipName, float duration, float baseFrequency, int seed)
        {
            const int sampleRate = 22050;
            int length = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[length];
            var random = new System.Random(seed);
            float breath = 0f;
            float phase = 0f;
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)sampleRate;
                float normalized = i / (float)length;
                float envelope = Mathf.Sin(normalized * Mathf.PI);
                float wobble = Mathf.Sin(t * 3.1f + seed) * 18f + Mathf.Sin(t * 7.7f) * 6f;
                phase += (baseFrequency + wobble) / sampleRate * Mathf.PI * 2f;
                float noise = (float)random.NextDouble() * 2f - 1f;
                breath = Mathf.Lerp(breath, noise, 0.035f);
                float voice = Mathf.Sin(phase) * 0.55f
                    + Mathf.Sin(phase * 0.503f) * 0.24f
                    + breath * 0.34f;
                samples[i] = voice * envelope * (0.11f + Mathf.Sin(t * 5.2f) * 0.025f);
            }
            AudioClip clip = AudioClip.Create(clipName, length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateEvilLaugh(string clipName, float duration, int seed)
        {
            const int sampleRate = 22050;
            int length = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[length];
            var random = new System.Random(seed);
            float phase = 0f;
            float rasp = 0f;
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)sampleRate;
                float normalized = i / (float)length;
                float syllable = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * 5.4f * Mathf.PI)), 1.7f);
                float envelope = Mathf.Sin(normalized * Mathf.PI) * (0.22f + syllable * 0.78f);
                float pitch = 132f + Mathf.Sin(t * 4.6f) * 31f + syllable * 42f;
                phase += pitch / sampleRate * Mathf.PI * 2f;
                float noise = (float)random.NextDouble() * 2f - 1f;
                rasp = Mathf.Lerp(rasp, noise, 0.08f);
                samples[i] = (Mathf.Sin(phase) * 0.12f + Mathf.Sin(phase * 0.51f) * 0.06f
                    + rasp * 0.025f) * envelope;
            }
            AudioClip clip = AudioClip.Create(clipName, length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateWarningSound()
        {
            const int sampleRate = 22050;
            const float duration = 0.58f;
            int length = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)sampleRate;
                float envelope = Mathf.Sin(i / (float)length * Mathf.PI);
                float pulse = 0.45f + Mathf.Max(0f, Mathf.Sin(t * 13f * Mathf.PI)) * 0.55f;
                samples[i] = (Mathf.Sin(t * 520f * Mathf.PI * 2f) * 0.08f
                    + Mathf.Sin(t * 780f * Mathf.PI * 2f) * 0.035f) * envelope * pulse;
            }
            AudioClip clip = AudioClip.Create("Varning inför rörelsehinder", length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateCreakSound()
        {
            const int sampleRate = 22050;
            const float duration = 1.85f;
            int length = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[length];
            var random = new System.Random(5531);
            float rough = 0f;
            float phase = 0f;
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)sampleRate;
                float normalized = i / (float)length;
                float envelope = Mathf.Sin(normalized * Mathf.PI);
                rough = Mathf.Lerp(rough, (float)random.NextDouble() * 2f - 1f, 0.055f);
                phase += (72f + Mathf.Sin(t * 5.2f) * 24f) / sampleRate * Mathf.PI * 2f;
                samples[i] = (Mathf.Sin(phase) * 0.07f + rough * 0.075f) * envelope;
            }
            AudioClip clip = AudioClip.Create("Tung port öppnas", length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateMetalRattle()
        {
            const int sampleRate = 22050;
            const float duration = 1.18f;
            int length = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[length];
            var random = new System.Random(8802);
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)sampleRate;
                float hit = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * 19f * Mathf.PI)), 7f);
                float metal = Mathf.Sin(t * 1360f * Mathf.PI * 2f) + Mathf.Sin(t * 1870f * Mathf.PI * 2f) * 0.55f;
                float noise = (float)random.NextDouble() * 2f - 1f;
                samples[i] = (metal * 0.045f + noise * 0.022f) * hit * (1f - i / (float)length);
            }
            AudioClip clip = AudioClip.Create("Kedjor skramlar", length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateNoiseBurst(string clipName, float duration, float volume, int seed)
        {
            const int sampleRate = 22050;
            int length = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[length];
            var random = new System.Random(seed);
            float filtered = 0f;
            for (int i = 0; i < length; i++)
            {
                float envelope = Mathf.Pow(1f - i / (float)length, 2f);
                float noise = (float)random.NextDouble() * 2f - 1f;
                filtered = Mathf.Lerp(filtered, noise, 0.16f);
                samples[i] = filtered * envelope * volume;
            }
            AudioClip clip = AudioClip.Create(clipName, length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void OnDestroy()
        {
            if (aimProvider != null) aimProvider.Dispose();
            foreach (Light reticleLight in reticleLights.Values)
                if (reticleLight != null) Destroy(reticleLight.gameObject);
        }
    }
}