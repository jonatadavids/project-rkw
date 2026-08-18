using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace RKW.Physics.Tests.EditMode
{
    public sealed class KartDynamicsMathTests
    {
        [Test]
        public void GripCurve_ReachesPeakThenFallsProgressively()
        {
            var atPeak = KartDynamicsMath.EvaluateGripCurve(8f, 8f, 28f, 0.32f);
            var afterPeak = KartDynamicsMath.EvaluateGripCurve(16f, 8f, 28f, 0.32f);
            var atLoss = KartDynamicsMath.EvaluateGripCurve(28f, 8f, 28f, 0.32f);

            Assert.That(atPeak, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(afterPeak, Is.LessThan(atPeak).And.GreaterThan(atLoss));
            Assert.That(atLoss, Is.EqualTo(0.32f).Within(0.0001f));
        }

        [Test]
        public void GripCurve_IsSymmetricAcrossSlipDirection()
        {
            var left = KartDynamicsMath.EvaluateGripCurve(-13f, 8f, 28f, 0.32f);
            var right = KartDynamicsMath.EvaluateGripCurve(13f, 8f, 28f, 0.32f);
            Assert.That(left, Is.EqualTo(right).Within(0.0001f));
        }

        [Test]
        public void WeightTransfer_IncreasesWithSteeringAtFixedSpeed()
        {
            var previous = 0f;
            for (var index = 0; index <= 20; index++)
            {
                var steering = index / 20f;
                var transfer = KartDynamicsMath.CalculateLateralWeightTransferRatio(
                    12f, steering, 15.28f, 0.22f, 1.05f, 3.4f);
                Assert.That(transfer, Is.GreaterThanOrEqualTo(previous));
                previous = transfer;
            }
        }

        [Test]
        public void InnerRearLift_OnlyBeginsAtConfiguredThreshold()
        {
            Assert.That(KartDynamicsMath.CalculateInnerRearLift(0.4f, 0.62f), Is.Zero);
            Assert.That(KartDynamicsMath.CalculateInnerRearLift(0.8f, 0.62f), Is.InRange(0f, 1f));
            Assert.That(KartDynamicsMath.CalculateInnerRearLift(1f, 0.62f), Is.EqualTo(1f));
        }

        [Test]
        public void Acceleration_DecreasesSmoothlyTowardMaximumSpeed()
        {
            var low = KartDynamicsMath.CalculateAccelerationMetersPerSecondSquared(1f, 15f, 8f);
            var medium = KartDynamicsMath.CalculateAccelerationMetersPerSecondSquared(8f, 15f, 8f);
            var maximum = KartDynamicsMath.CalculateAccelerationMetersPerSecondSquared(15f, 15f, 8f);
            Assert.That(low, Is.GreaterThan(medium));
            Assert.That(medium, Is.GreaterThan(maximum));
            Assert.That(maximum, Is.Zero.Within(0.0001f));
        }

        [Test]
        public void SteeringSpeedLoss_IncreasesWithSteeringMagnitude()
        {
            var light = KartDynamicsMath.CalculateSteeringSpeedLoss(0.2f, 12f, 15f, 2.5f);
            var heavy = KartDynamicsMath.CalculateSteeringSpeedLoss(0.8f, 12f, 15f, 2.5f);
            Assert.That(heavy, Is.GreaterThan(light));
        }

        [Test]
        public void PrototypeTuning_IsValidAndRemainsAnExplicitHypothesis()
        {
            var tuning = AssetDatabase.LoadAssetAtPath<KartCategorySO>(
                "Assets/RKW/Physics/Resources/KartPhysics/PrototypeSchoolTuning.asset");
            Assert.That(tuning, Is.Not.Null);
            Assert.That(tuning.IsValid(out var reason), Is.True, reason);
            Assert.That(tuning.MaxSpeedKph, Is.EqualTo(55f));
            Assert.That(tuning.ZeroToMaxSeconds, Is.EqualTo(8f));
        }

        [Test]
        public void TechnicalScene_IsNotPartOfNormalBuildSettings()
        {
            Assert.That(EditorBuildSettings.scenes.Select(scene => scene.path),
                Does.Not.Contain("Assets/Scenes/KartPhysicsPrototype.unity"));
        }
    }
}
