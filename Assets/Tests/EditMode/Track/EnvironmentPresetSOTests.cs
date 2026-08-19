using NUnit.Framework;
using UnityEngine;

namespace RKW.Track.Tests.EditMode
{
    /// <summary>
    /// M3-T03: Criar EnvironmentPresetSO "Day".
    /// Validates: Requirement 17.2 (required fields), 17.6 (MVP: only "Day" exists).
    /// </summary>
    public sealed class EnvironmentPresetSOTests
    {
        private const string DayResourcePath = "Track/DayEnvironmentPreset";

        [Test]
        public void DayPreset_LoadsFromResources()
        {
            var preset = Resources.Load<EnvironmentPresetSO>(DayResourcePath);

            Assert.That(preset, Is.Not.Null,
                $"Expected an EnvironmentPresetSO asset at Resources/{DayResourcePath}.asset");
        }

        [Test]
        public void DayPreset_IsValid()
        {
            var preset = Resources.Load<EnvironmentPresetSO>(DayResourcePath);
            Assert.That(preset, Is.Not.Null);

            var isValid = preset.IsValid(out var reason);

            Assert.That(isValid, Is.True, $"Day preset should be valid, but: {reason}");
        }

        [Test]
        public void DayPreset_UsesBakedLightingAndHasNoSpotlights()
        {
            var preset = Resources.Load<EnvironmentPresetSO>(DayResourcePath);
            Assert.That(preset, Is.Not.Null);

            // Requirement 17.3: prefer baked/mixed lighting for mobile performance.
            Assert.That(preset.UsesBakedLighting, Is.True);
            // Daytime racing has no need for venue spotlights (those are for Requirement 17.4's night preset).
            Assert.That(preset.HasSpotlights, Is.False);
        }

        [Test]
        public void DayPreset_HasPerformanceBudgetForEveryQualityTier()
        {
            var preset = Resources.Load<EnvironmentPresetSO>(DayResourcePath);
            Assert.That(preset, Is.Not.Null);

            Assert.That(preset.PerformanceBudgets.Count, Is.EqualTo(3));
        }

        [Test]
        public void EmptyPreset_IsInvalid()
        {
            var preset = ScriptableObject.CreateInstance<EnvironmentPresetSO>();

            var isValid = preset.IsValid(out var reason);

            Assert.That(isValid, Is.False);
            Assert.That(reason, Is.Not.Empty);

            Object.DestroyImmediate(preset);
        }
    }
}
