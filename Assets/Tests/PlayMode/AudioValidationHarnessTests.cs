using System.Collections;
using NUnit.Framework;
using RKW.Audio;
using UnityEngine;
using UnityEngine.TestTools;

namespace RKW.Tests.PlayMode
{
    public sealed class AudioValidationHarnessTests
    {
        [UnityTest]
        public IEnumerator Harness_ConfiguresMixerAndSupportsSourceLifecycle()
        {
            var root = new GameObject("Audio validation harness test");
            var harness = root.AddComponent<AudioValidationHarness>();
            yield return null;

            Assert.That(harness.IsInitialized, Is.True);
            Assert.That(harness.Sources, Has.Count.EqualTo(4));
            Assert.That(harness.Sources[AudioValidationLayer.Engine].loop, Is.True);
            Assert.That(harness.Sources[AudioValidationLayer.Road].loop, Is.True);
            Assert.That(harness.Sources[AudioValidationLayer.Ambience].loop, Is.True);
            Assert.That(harness.Sources[AudioValidationLayer.Impact].loop, Is.False);
            foreach (var source in harness.Sources.Values)
            {
                Assert.That(source.clip, Is.Not.Null);
                Assert.That(source.outputAudioMixerGroup, Is.Not.Null);
                Assert.That(source.playOnAwake, Is.False);
            }

            harness.StopAll();
            Assert.That(harness.LoopsRequested, Is.False);
            harness.StartLoops();
            Assert.That(harness.LoopsRequested, Is.True);
            harness.RestartLoops();
            Assert.That(harness.LoopsRequested, Is.True);

            harness.SetLayerEnabled(AudioValidationLayer.Road, false);
            Assert.That(harness.IsLayerEnabled(AudioValidationLayer.Road), Is.False);
            harness.SetLayerEnabled(AudioValidationLayer.Road, true);
            Assert.That(harness.IsLayerEnabled(AudioValidationLayer.Road), Is.True);

            harness.TriggerImpact();
            harness.TriggerImpact();
            Assert.That(harness.ImpactTriggerCount, Is.EqualTo(2));

            var engine = harness.Sources[AudioValidationLayer.Engine];
            harness.SetEngineTargets(0f, AudioValidationConfiguration.MaximumEnginePitch);
            harness.AdvanceEngineSmoothing(0.1f);
            Assert.That(engine.volume, Is.GreaterThan(0f));
            Assert.That(engine.volume, Is.LessThan(AudioValidationConfiguration.EngineVolume));
            Assert.That(engine.pitch, Is.GreaterThan(1f));
            Assert.That(engine.pitch, Is.LessThan(AudioValidationConfiguration.MaximumEnginePitch));

            Object.Destroy(root);
            yield return null;
        }
    }
}
