using NUnit.Framework;
using UnityEngine;

namespace RKW.Track.Tests.EditMode
{
    /// <summary>
    /// M3-T02: Criar TrackConfigurationSO para a pista MVP.
    /// Validates: Requirements 16.2 (required fields), 16.7 (MVP: single
    /// clockwise configuration).
    /// </summary>
    public sealed class TrackConfigurationSOTests
    {
        private const string OvalMvpResourcePath = "Track/OvalMvpTrackConfiguration";

        [Test]
        public void OvalMvpConfiguration_LoadsFromResources()
        {
            var config = Resources.Load<TrackConfigurationSO>(OvalMvpResourcePath);

            Assert.That(config, Is.Not.Null,
                $"Expected a TrackConfigurationSO asset at Resources/{OvalMvpResourcePath}.asset");
        }

        [Test]
        public void OvalMvpConfiguration_IsValid()
        {
            var config = Resources.Load<TrackConfigurationSO>(OvalMvpResourcePath);
            Assert.That(config, Is.Not.Null);

            var isValid = config.IsValid(out var reason);

            Assert.That(isValid, Is.True, $"Oval MVP configuration should be valid, but: {reason}");
        }

        [Test]
        public void OvalMvpConfiguration_HasStableIds()
        {
            var config = Resources.Load<TrackConfigurationSO>(OvalMvpResourcePath);
            Assert.That(config, Is.Not.Null);

            Assert.That(config.TrackConfigurationId, Is.EqualTo("oval-mvp-cw"));
            Assert.That(config.TrackId, Is.EqualTo("oval-mvp"));
            Assert.That(config.Direction, Is.EqualTo(TrackDirection.Clockwise));
        }

        [Test]
        public void OvalMvpConfiguration_HasExactlyTenGridSlots()
        {
            var config = Resources.Load<TrackConfigurationSO>(OvalMvpResourcePath);
            Assert.That(config, Is.Not.Null);

            Assert.That(config.GridSlots.Count, Is.EqualTo(TrackConfigurationSO.RequiredGridSlotCount));
        }

        [Test]
        public void OvalMvpConfiguration_HasStartFinishCheckpointAndThreeSectors()
        {
            var config = Resources.Load<TrackConfigurationSO>(OvalMvpResourcePath);
            Assert.That(config, Is.Not.Null);

            Assert.That(config.TimingSectorCount, Is.EqualTo(3));

            var hasStartFinish = false;
            foreach (var checkpoint in config.Checkpoints)
            {
                if (checkpoint.IsStartFinish)
                {
                    hasStartFinish = true;
                }
            }

            Assert.That(hasStartFinish, Is.True, "Checkpoints must include a start/finish checkpoint.");
        }

        [Test]
        public void OvalMvpConfiguration_HasCalibratedPlayerRecoveryParameters()
        {
            var config = Resources.Load<TrackConfigurationSO>(OvalMvpResourcePath);
            Assert.That(config, Is.Not.Null);

            Assert.That(config.RecoveryPoints.Count, Is.GreaterThan(0));
            Assert.That(config.RecoveryStuckSeconds, Is.EqualTo(4f));
            Assert.That(config.RecoveryStoppedSpeedMetersPerSecond, Is.EqualTo(0.2f));
            Assert.That(config.RecoveryInvertedDegrees, Is.EqualTo(85f));
            Assert.That(config.RecoverySafetyHeightMeters, Is.EqualTo(2f));
            Assert.That(config.RecoveryCollisionGraceSeconds, Is.EqualTo(3f));
            Assert.That(config.RecoveryPerimeterMultiplier, Is.EqualTo(3f));
        }

        [Test]
        public void EmptyConfiguration_IsInvalid()
        {
            var config = ScriptableObject.CreateInstance<TrackConfigurationSO>();

            var isValid = config.IsValid(out var reason);

            Assert.That(isValid, Is.False);
            Assert.That(reason, Is.Not.Empty);

            Object.DestroyImmediate(config);
        }
    }
}
