using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace RKW.Physics.Tests.EditMode
{
    /// <summary>
    /// Ghost kart playback interpolation. Founder request, 2026-08-24:
    /// "vamos tentar seguir talvez o fantasma fique mais legal" — quick,
    /// fun-first ghost, see GhostMath's XML doc for how this relates to the
    /// formal M4-T06/T13 spec in tasks.md.
    /// </summary>
    public sealed class GhostMathTests
    {
        [Test]
        public void TrySamplePose_NullSamples_ReturnsFalse()
        {
            var result = GhostMath.TrySamplePose(null, 0.1f, 1f, out _, out _);

            Assert.That(result, Is.False);
        }

        [Test]
        public void TrySamplePose_EmptySamples_ReturnsFalse()
        {
            var result = GhostMath.TrySamplePose(new List<GhostSample>(), 0.1f, 1f, out _, out _);

            Assert.That(result, Is.False);
        }

        [Test]
        public void TrySamplePose_ZeroInterval_ReturnsFalse()
        {
            var samples = new List<GhostSample> { new GhostSample(Vector3.zero, 0f) };

            var result = GhostMath.TrySamplePose(samples, 0f, 1f, out _, out _);

            Assert.That(result, Is.False);
        }

        [Test]
        public void TrySamplePose_SingleSample_AlwaysReturnsThatSample()
        {
            var samples = new List<GhostSample> { new GhostSample(new Vector3(1f, 0f, 2f), 45f) };

            var result = GhostMath.TrySamplePose(samples, 0.1f, 5f, out var position, out var yaw);

            Assert.That(result, Is.True);
            Assert.That(position, Is.EqualTo(new Vector3(1f, 0f, 2f)));
            Assert.That(yaw, Is.EqualTo(45f));
        }

        [Test]
        public void TrySamplePose_NegativeOrZeroElapsed_ReturnsFirstSample()
        {
            var samples = new List<GhostSample>
            {
                new GhostSample(new Vector3(0f, 0f, 0f), 0f),
                new GhostSample(new Vector3(10f, 0f, 0f), 90f),
            };

            var result = GhostMath.TrySamplePose(samples, 0.1f, -1f, out var position, out var yaw);

            Assert.That(result, Is.True);
            Assert.That(position, Is.EqualTo(Vector3.zero));
            Assert.That(yaw, Is.EqualTo(0f));
        }

        [Test]
        public void TrySamplePose_ExactlyOnASample_ReturnsThatSampleUninterpolated()
        {
            var samples = new List<GhostSample>
            {
                new GhostSample(new Vector3(0f, 0f, 0f), 0f),
                new GhostSample(new Vector3(10f, 0f, 0f), 0f),
                new GhostSample(new Vector3(20f, 0f, 0f), 0f),
            };

            // Sample interval 0.1s, so elapsed 0.2s lands exactly on index 2.
            var result = GhostMath.TrySamplePose(samples, 0.1f, 0.2f, out var position, out _);

            Assert.That(result, Is.True);
            Assert.That(position, Is.EqualTo(new Vector3(20f, 0f, 0f)));
        }

        [Test]
        public void TrySamplePose_BetweenTwoSamples_LerpsPosition()
        {
            var samples = new List<GhostSample>
            {
                new GhostSample(new Vector3(0f, 0f, 0f), 0f),
                new GhostSample(new Vector3(10f, 0f, 0f), 0f),
            };

            // Halfway between index 0 (t=0) and index 1 (t=0.1) is t=0.05.
            var result = GhostMath.TrySamplePose(samples, 0.1f, 0.05f, out var position, out _);

            Assert.That(result, Is.True);
            Assert.That(position.x, Is.EqualTo(5f).Within(0.001f));
        }

        [Test]
        public void TrySamplePose_BetweenTwoSamples_LerpsYawTheShortWayAround()
        {
            var samples = new List<GhostSample>
            {
                new GhostSample(Vector3.zero, 350f),
                new GhostSample(Vector3.zero, 10f), // 20° the short way, not 340° the long way
            };

            var result = GhostMath.TrySamplePose(samples, 0.1f, 0.05f, out _, out var yaw);

            Assert.That(result, Is.True);
            // LerpAngle wraps, so halfway should land at 0° (350 + 10, mod 360), not 180°.
            Assert.That(Mathf.DeltaAngle(yaw, 0f), Is.EqualTo(0f).Within(0.01f));
        }

        [Test]
        public void TrySamplePose_ElapsedPastLastSample_ClampsToLastSample()
        {
            var samples = new List<GhostSample>
            {
                new GhostSample(new Vector3(0f, 0f, 0f), 0f),
                new GhostSample(new Vector3(10f, 0f, 0f), 90f),
            };

            var result = GhostMath.TrySamplePose(samples, 0.1f, 999f, out var position, out var yaw);

            Assert.That(result, Is.True);
            Assert.That(position, Is.EqualTo(new Vector3(10f, 0f, 0f)));
            Assert.That(yaw, Is.EqualTo(90f));
        }

        [Test]
        public void TrySamplePose_ElapsedExactlyAtLastSampleIndex_ClampsToLastSample()
        {
            var samples = new List<GhostSample>
            {
                new GhostSample(new Vector3(0f, 0f, 0f), 0f),
                new GhostSample(new Vector3(10f, 0f, 0f), 0f),
            };

            // lastIndex = 1, so elapsed = 1 * interval lands exactly on the
            // ">=" boundary — must not try to read a nonexistent sample 2.
            var result = GhostMath.TrySamplePose(samples, 0.1f, 0.1f, out var position, out _);

            Assert.That(result, Is.True);
            Assert.That(position, Is.EqualTo(new Vector3(10f, 0f, 0f)));
        }
    }
}
