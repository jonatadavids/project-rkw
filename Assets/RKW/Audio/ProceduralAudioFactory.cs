using System;
using UnityEngine;

namespace RKW.Audio
{
    internal static class ProceduralAudioFactory
    {
        internal static AudioClip Create(AudioValidationLayer layer)
        {
            var samples = GenerateSamples(layer, AudioValidationConfiguration.SampleRate);
            var clip = AudioClip.Create(
                $"RKW Synthetic {layer}",
                samples.Length,
                1,
                AudioValidationConfiguration.SampleRate,
                false);
            clip.SetData(samples, 0);
            return clip;
        }

        internal static float[] GenerateSamples(AudioValidationLayer layer, int sampleRate)
        {
            if (sampleRate <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sampleRate));
            }

            var durationSeconds = layer == AudioValidationLayer.Impact ? 0.35f : 1f;
            var samples = new float[Mathf.CeilToInt(sampleRate * durationSeconds)];
            var noiseState = 0x13579BDFu;

            for (var index = 0; index < samples.Length; index++)
            {
                var time = index / (float)sampleRate;
                float value;
                switch (layer)
                {
                    case AudioValidationLayer.Engine:
                        value = 0.32f * Mathf.Sin(2f * Mathf.PI * 110f * time)
                            + 0.12f * Mathf.Sin(2f * Mathf.PI * 220f * time);
                        break;
                    case AudioValidationLayer.Road:
                        noiseState = NextNoise(noiseState);
                        var pulse = Mathf.Sin(2f * Mathf.PI * 18f * time) >= 0f ? 1f : -1f;
                        value = 0.20f * pulse + 0.08f * ToBipolar(noiseState);
                        break;
                    case AudioValidationLayer.Impact:
                        noiseState = NextNoise(noiseState);
                        var envelope = Mathf.Exp(-18f * time);
                        value = envelope * (0.45f * ToBipolar(noiseState)
                            + 0.15f * Mathf.Sin(2f * Mathf.PI * 75f * time));
                        break;
                    case AudioValidationLayer.Ambience:
                        value = 0.12f * Mathf.Sin(2f * Mathf.PI * 220f * time)
                            + 0.08f * Mathf.Sin(2f * Mathf.PI * 330f * time);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(layer), layer, null);
                }

                samples[index] = Mathf.Clamp(value, -0.6f, 0.6f);
            }

            return samples;
        }

        private static uint NextNoise(uint state)
        {
            return (1664525u * state) + 1013904223u;
        }

        private static float ToBipolar(uint value)
        {
            return ((value >> 8) / 8388607.5f) - 1f;
        }
    }
}
