using System;
using NUnit.Framework;
using UnityEngine;

namespace RKW.Physics.Tests.EditMode
{
    /// <summary>
    /// Round 43 (2026-09-01): validates the plane-fitting math that decides
    /// how KartV2's cockpit steering wheel pivot is oriented (see
    /// KartPhysicsPrototypeBootstrap.CreateWheelSteeringPivot). This math
    /// has no Unity Editor rendering involved -- it is pure vector math --
    /// so it can and should be checked against known, hand-built geometry
    /// before ever trusting it on the real 168-part kart model. Each test
    /// builds points on a plane with a KNOWN normal direction and checks
    /// that ComputeBestFitPlaneNormal recovers it (an eigenvector's sign is
    /// ambiguous, so tests compare the absolute value of the dot product,
    /// not the vector directly).
    /// </summary>
    public sealed class KartVisualGeometryMathTests
    {
        private const float AngleToleranceDegrees = 0.5f;

        [Test]
        public void FlatPlaneInXY_RecoversNormalPointingAlongZ()
        {
            var random = new System.Random(1);
            var points = new Vector3[60];
            for (var i = 0; i < points.Length; i++)
            {
                points[i] = new Vector3(RandomRange(random, -5f, 5f), RandomRange(random, -5f, 5f), 0f);
            }

            var normal = KartVisualGeometryMath.ComputeBestFitPlaneNormal(points);
            AssertNormalsParallel(Vector3.forward, normal);
        }

        [Test]
        public void PlaneTiltedFortyFiveDegrees_RecoversExpectedNormal()
        {
            var expected = new Vector3(0f, 1f, 1f).normalized;
            var points = BuildPlanePoints(expected, 250, seed: 2);

            var normal = KartVisualGeometryMath.ComputeBestFitPlaneNormal(points);
            AssertNormalsParallel(expected, normal);
        }

        [Test]
        public void PlaneMatchingKartV2SteeringWheelTilt_RecoversMeasuredNormal()
        {
            // This is the ACTUAL face normal measured directly from
            // KartV2.obj's own steering-wheel-rim vertex data (see the
            // RECOVERY etapa report, section on the steering wheel
            // centering fix) -- not a made-up number. If this test ever
            // starts failing, the math itself regressed, independent of
            // whatever the current KartV2 model looks like.
            var expected = new Vector3(0f, 0.607450107f, -0.794357833f).normalized;
            var points = BuildPlanePoints(expected, 300, seed: 3, centroidOffset: new Vector3(0f, 58f, 42f));

            var normal = KartVisualGeometryMath.ComputeBestFitPlaneNormal(points);
            AssertNormalsParallel(expected, normal);
        }

        [Test]
        public void ComputeBestFitPlaneNormal_IsIndependentOfInputPointOrder()
        {
            // Round 45 (2026-09-01) founder feedback: the steering wheel
            // came up tilted a different way ("direita" vs "esquerda")
            // between two separate app launches, even though the source
            // 3D geometry never changes -- the prime suspect is
            // floating-point summation order (see the fix in
            // ComputeBestFitPlaneNormal's own comment). This test builds
            // the SAME point set, shuffled several different ways, and
            // requires the result to be EXACTLY identical every time (not
            // just "close" or "parallel" -- bit-for-bit the same Vector3),
            // which is only possible once the order the points are summed
            // in no longer depends on the order they were passed in.
            var basePoints = BuildPlanePoints(
                new Vector3(0f, 0.607450107f, -0.794357833f).normalized, 300, seed: 7,
                centroidOffset: new Vector3(0f, 58f, 42f));

            var reference = KartVisualGeometryMath.ComputeBestFitPlaneNormal(basePoints);
            Assert.That(reference, Is.Not.EqualTo(Vector3.zero));

            for (var shuffleSeed = 0; shuffleSeed < 5; shuffleSeed++)
            {
                var shuffled = Shuffle(basePoints, shuffleSeed);
                var result = KartVisualGeometryMath.ComputeBestFitPlaneNormal(shuffled);
                Assert.That(result, Is.EqualTo(reference),
                    $"Shuffle seed {shuffleSeed} produced a different normal ({result}) than the unshuffled input ({reference}) -- the result still depends on input order.");
            }
        }

        [Test]
        public void FewerThanThreePoints_ReturnsZero()
        {
            var normal = KartVisualGeometryMath.ComputeBestFitPlaneNormal(new[] { Vector3.zero, Vector3.one });
            Assert.That(normal, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void AllPointsCoincident_ReturnsZeroInsteadOfGarbage()
        {
            var points = new[] { new Vector3(3f, 3f, 3f), new Vector3(3f, 3f, 3f), new Vector3(3f, 3f, 3f) };
            var normal = KartVisualGeometryMath.ComputeBestFitPlaneNormal(points);
            Assert.That(normal, Is.EqualTo(Vector3.zero));
        }

        private static Vector3[] BuildPlanePoints(Vector3 normal, int count, int seed, Vector3? centroidOffset = null)
        {
            var offset = centroidOffset ?? Vector3.zero;
            var reference = Mathf.Abs(Vector3.Dot(normal, Vector3.right)) < 0.9f ? Vector3.right : Vector3.up;
            var tangentA = Vector3.Cross(normal, reference).normalized;
            var tangentB = Vector3.Cross(normal, tangentA).normalized;

            var random = new System.Random(seed);
            var points = new Vector3[count];
            for (var i = 0; i < count; i++)
            {
                var s = RandomRange(random, -20f, 20f);
                var t = RandomRange(random, -20f, 20f);
                points[i] = offset + s * tangentA + t * tangentB;
            }

            return points;
        }

        private static float RandomRange(System.Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }

        private static Vector3[] Shuffle(Vector3[] source, int seed)
        {
            var copy = (Vector3[])source.Clone();
            var random = new System.Random(seed);
            for (var i = copy.Length - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (copy[i], copy[j]) = (copy[j], copy[i]);
            }
            return copy;
        }

        private static void AssertNormalsParallel(Vector3 expected, Vector3 actual)
        {
            Assert.That(actual, Is.Not.EqualTo(Vector3.zero), "Solver returned a zero vector -- could not fit a plane.");
            var angleDegrees = Vector3.Angle(expected.normalized, actual.normalized);
            // A plane normal's sign is ambiguous (the same plane has two
            // opposite valid normals) -- accept either direction.
            var angleFromOpposite = 180f - angleDegrees;
            var bestAngle = Mathf.Min(angleDegrees, angleFromOpposite);
            Assert.That(bestAngle, Is.LessThan(AngleToleranceDegrees),
                $"Expected normal parallel to {expected}, got {actual} ({bestAngle:F2} deg off).");
        }
    }
}
