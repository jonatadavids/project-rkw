using NUnit.Framework;

namespace RKW.Audio.Tests.EditMode
{
    /// <summary>
    /// Kart engine/skid audio feel pass (founder playtest feedback,
    /// 2026-08-19: "sem som não dá aquela sensação legal").
    /// Pure logic tests — no AudioSource/AudioClip involved.
    /// </summary>
    public sealed class KartAudioMathTests
    {
        [Test]
        public void EnginePitch_AtStandstillNoThrottle_IsMinimum()
        {
            var pitch = KartAudioMath.CalculateEnginePitch(0f, 0f, 0.75f, 1.5f);

            Assert.That(pitch, Is.EqualTo(0.75f).Within(0.001f));
        }

        [Test]
        public void EnginePitch_AtTopSpeed_IsMaximum()
        {
            var pitch = KartAudioMath.CalculateEnginePitch(1f, 1f, 0.75f, 1.5f);

            Assert.That(pitch, Is.EqualTo(1.5f).Within(0.001f));
        }

        [Test]
        public void EnginePitch_FullThrottleFromStandstill_RevsAboveIdle()
        {
            // High throttle, zero speed (launching from a stop) should still
            // raise pitch — an engine revs before the kart is moving.
            var pitch = KartAudioMath.CalculateEnginePitch(0f, 1f, 0.75f, 1.5f);

            Assert.That(pitch, Is.GreaterThan(0.75f));
        }

        [Test]
        public void EngineVolume_NoThrottle_IsIdleVolume()
        {
            var volume = KartAudioMath.CalculateEngineVolume(0f, 0.05f, 0.18f);

            Assert.That(volume, Is.EqualTo(0.05f).Within(0.001f));
        }

        [Test]
        public void EngineVolume_FullThrottle_IsMaxVolume()
        {
            var volume = KartAudioMath.CalculateEngineVolume(1f, 0.05f, 0.18f);

            Assert.That(volume, Is.EqualTo(0.18f).Within(0.001f));
        }

        [Test]
        public void SkidIntensity_BelowSpeedThreshold_IsSilent()
        {
            var intensity = KartAudioMath.CalculateSkidIntensity(
                gripRatio: 0.3f, minimumGripRatio: 0.28f, speedKph: 2f, speedThresholdKph: 8f);

            Assert.That(intensity, Is.EqualTo(0f));
        }

        [Test]
        public void SkidIntensity_FullGrip_IsZero()
        {
            var intensity = KartAudioMath.CalculateSkidIntensity(
                gripRatio: 1f, minimumGripRatio: 0.28f, speedKph: 40f, speedThresholdKph: 8f);

            Assert.That(intensity, Is.EqualTo(0f));
        }

        [Test]
        public void SkidIntensity_AtMinimumGrip_IsMaximum()
        {
            var intensity = KartAudioMath.CalculateSkidIntensity(
                gripRatio: 0.28f, minimumGripRatio: 0.28f, speedKph: 40f, speedThresholdKph: 8f);

            Assert.That(intensity, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void SkidIntensity_PartialGripLoss_IsBetweenZeroAndOne()
        {
            var intensity = KartAudioMath.CalculateSkidIntensity(
                gripRatio: 0.64f, minimumGripRatio: 0.28f, speedKph: 40f, speedThresholdKph: 8f);

            Assert.That(intensity, Is.GreaterThan(0f));
            Assert.That(intensity, Is.LessThan(1f));
        }

        [Test]
        public void SkidIntensity_SlightGripLossFromOrdinaryCornering_IsSilent()
        {
            // Founder playtest feedback, 2026-08-20 (round 8): ordinary
            // cornering (a small dip in grip) should not sound like a skid.
            var intensity = KartAudioMath.CalculateSkidIntensity(
                gripRatio: 0.9f, minimumGripRatio: 0.28f, speedKph: 40f, speedThresholdKph: 8f);

            Assert.That(intensity, Is.EqualTo(0f));
        }

        [Test]
        public void SkidIntensity_SignificantGripLoss_IsAudible()
        {
            var intensity = KartAudioMath.CalculateSkidIntensity(
                gripRatio: 0.4f, minimumGripRatio: 0.28f, speedKph: 40f, speedThresholdKph: 8f);

            Assert.That(intensity, Is.GreaterThan(0f));
        }
    }
}
