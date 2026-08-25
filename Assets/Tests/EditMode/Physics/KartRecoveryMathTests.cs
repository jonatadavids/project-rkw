using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace RKW.Physics.Tests.EditMode
{
    public sealed class KartRecoveryMathTests
    {
        private const int CollisionPropertySeed = 240824;
        private const int PropertyCaseCount = 100;

        [Test]
        public void EvaluateReason_CollisionAloneNeverTriggersRecoveryAcrossDeterministicCases()
        {
            var random = new System.Random(CollisionPropertySeed);
            for (var caseIndex = 0; caseIndex < PropertyCaseCount; caseIndex++)
            {
                var collisionSeverity = (float)random.NextDouble();
                var safeStuckDuration = (float)random.NextDouble() * 4f;
                var safeTilt = (float)random.NextDouble() * 85f;
                var reason = KartRecoveryMath.EvaluateReason(
                    safeStuckDuration, safeTilt, false, false);

                Assert.That(reason, Is.EqualTo(KartRecoveryReason.None),
                    $"seed={CollisionPropertySeed}, case={caseIndex}, collisionSeverity={collisionSeverity}");
            }
        }

        [Test]
        public void EvaluateReason_OnlyTriggersStuckAfterFourSeconds()
        {
            Assert.That(KartRecoveryMath.EvaluateReason(4f, 0f, false, false),
                Is.EqualTo(KartRecoveryReason.None));
            Assert.That(KartRecoveryMath.EvaluateReason(4.01f, 0f, false, false),
                Is.EqualTo(KartRecoveryReason.Stuck));
        }

        [Test]
        public void UpdateStuckDuration_ResetsWhileCountdownDisablesInputOrKartMoves()
        {
            Assert.That(KartRecoveryMath.UpdateStuckDuration(3.9f, false, 0f, 0.2f, 0.2f), Is.Zero);
            Assert.That(KartRecoveryMath.UpdateStuckDuration(3.9f, true, 0.21f, 0.2f, 0.2f), Is.Zero);
            Assert.That(KartRecoveryMath.UpdateStuckDuration(3.9f, true, 0.1f, 0.2f, 0.2f),
                Is.EqualTo(4.1f).Within(0.0001f));
        }

        [TestCase(85f, KartRecoveryReason.None)]
        [TestCase(85.1f, KartRecoveryReason.Inverted)]
        public void EvaluateReason_UsesStrictInversionThreshold(float tilt, KartRecoveryReason expected)
        {
            Assert.That(KartRecoveryMath.EvaluateReason(0f, tilt, false, false), Is.EqualTo(expected));
        }

        [Test]
        public void EvaluateReason_RecognizesOutsidePerimeterAndSafetyRisk()
        {
            Assert.That(KartRecoveryMath.EvaluateReason(0f, 0f, true, false),
                Is.EqualTo(KartRecoveryReason.OutsideRecoverablePerimeter));
            Assert.That(KartRecoveryMath.EvaluateReason(0f, 0f, false, true),
                Is.EqualTo(KartRecoveryReason.SafetyRisk));
        }

        [Test]
        public void TrackGeometry_SelectsNearestRecoveryAndDoesNotMisclassifyLongStraight()
        {
            var racingLine = new List<Vector3>
            {
                new Vector3(-60f, 0f, -14f),
                new Vector3(60f, 0f, -14f),
                new Vector3(60f, 0f, 14f),
                new Vector3(-60f, 0f, 14f),
            };
            var recoveryPoints = new List<Vector3>
            {
                new Vector3(0f, 0.5f, -14f),
                new Vector3(0f, 0.5f, 14f),
            };

            Assert.That(KartRecoveryMath.IsOutsideRecoverablePerimeter(
                new Vector3(0f, 0.5f, -14f), racingLine, 7f), Is.False);
            Assert.That(KartRecoveryMath.IsOutsideRecoverablePerimeter(
                new Vector3(0f, 0.5f, -50f), racingLine, 7f), Is.True);
            Assert.That(KartRecoveryMath.FindNearestPoint(recoveryPoints,
                new Vector3(5f, 0f, 10f)), Is.EqualTo(recoveryPoints[1]));
            Assert.That(KartRecoveryMath.FindTrackForward(racingLine, recoveryPoints[0]),
                Is.EqualTo(Vector3.right));
        }
    }
}
