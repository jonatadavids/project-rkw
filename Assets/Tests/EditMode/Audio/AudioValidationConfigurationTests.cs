using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.Audio;

namespace RKW.Audio.Tests.EditMode
{
    public sealed class AudioValidationConfigurationTests
    {
        private const string MixerPath = "Assets/RKW/Audio/Resources/RKWAudioValidation.mixer";

        [Test]
        public void Mixer_ContainsEveryRequiredValidationGroup()
        {
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);

            Assert.That(mixer, Is.Not.Null);
            foreach (var groupName in AudioValidationConfiguration.MixerGroupNames)
            {
                Assert.That(
                    AudioValidationConfiguration.FindRequiredGroup(mixer, groupName).name,
                    Is.EqualTo(groupName));
            }
        }

        [Test]
        public void ProceduralLayers_AreSafeAndDistinct()
        {
            var signatures = new HashSet<long>();
            foreach (AudioValidationLayer layer in Enum.GetValues(typeof(AudioValidationLayer)))
            {
                var samples = ProceduralAudioFactory.GenerateSamples(layer, AudioValidationConfiguration.SampleRate);
                Assert.That(samples, Is.Not.Empty, layer.ToString());
                Assert.That(samples.Max(sample => Math.Abs(sample)), Is.LessThanOrEqualTo(0.6001f), layer.ToString());

                long signature = 0;
                for (var index = 0; index < Math.Min(samples.Length, 4096); index += 17)
                {
                    signature = unchecked((signature * 397) ^ (long)(samples[index] * 100000f));
                }

                Assert.That(signatures.Add(signature), Is.True, $"Synthetic layer is not distinct: {layer}.");
            }
        }

        [Test]
        public void SafeVolumes_StayBelowUnityFullScale()
        {
            foreach (AudioValidationLayer layer in Enum.GetValues(typeof(AudioValidationLayer)))
            {
                Assert.That(AudioValidationConfiguration.SafeVolume(layer), Is.InRange(0f, 0.25f));
            }
        }
    }
}
