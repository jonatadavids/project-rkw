using NUnit.Framework;
using RKW.Physics;
using UnityEditor;
using UnityEngine;

namespace RKW.Physics.Tests.EditMode
{
    /// <summary>
    /// Etapa 3 (2026-08-31) pure-math and ScriptableObject-property coverage
    /// for the rigid rear axle refinement: per-corner rear load estimates,
    /// the resulting axle bind factor, the yaw-rate scrub-resistance term,
    /// and chassisFlexFactor's effect on the effective lift threshold. See
    /// KartRigidAxleIntegrationTests (PlayMode) for the end-to-end
    /// Rigidbody+PhysX coverage of the load estimates.
    /// </summary>
    public sealed class KartRigidAxleRefinementTests
    {
        [Test]
        public void RearCornerLoad_Outside_IncreasesWithTransferRatio()
        {
            const float staticLoad = 200f;
            var atZero = KartDynamicsMath.CalculateRearCornerLoadNewtons(staticLoad, 0f, isOutsideCorner: true);
            var atHalf = KartDynamicsMath.CalculateRearCornerLoadNewtons(staticLoad, 0.5f, isOutsideCorner: true);
            var atFull = KartDynamicsMath.CalculateRearCornerLoadNewtons(staticLoad, 1f, isOutsideCorner: true);

            Assert.That(atZero, Is.EqualTo(staticLoad).Within(0.001f));
            Assert.That(atHalf, Is.GreaterThan(atZero));
            Assert.That(atFull, Is.GreaterThan(atHalf));
            Assert.That(atFull, Is.EqualTo(staticLoad * 2f).Within(0.001f));
        }

        [Test]
        public void RearCornerLoad_Inside_DecreasesWithTransferRatio_ClampedAtZero()
        {
            const float staticLoad = 200f;
            var atZero = KartDynamicsMath.CalculateRearCornerLoadNewtons(staticLoad, 0f, isOutsideCorner: false);
            var atHalf = KartDynamicsMath.CalculateRearCornerLoadNewtons(staticLoad, 0.5f, isOutsideCorner: false);
            var atFull = KartDynamicsMath.CalculateRearCornerLoadNewtons(staticLoad, 1f, isOutsideCorner: false);

            Assert.That(atZero, Is.EqualTo(staticLoad).Within(0.001f));
            Assert.That(atHalf, Is.LessThan(atZero));
            Assert.That(atFull, Is.EqualTo(0f).Within(0.001f));

            // Overdriven ratio (should never happen in practice, but the
            // clamp inside the ratio calculation must still hold) never
            // goes negative -- a wheel cannot have negative load.
            var overdriven = KartDynamicsMath.CalculateRearCornerLoadNewtons(staticLoad, 5f, isOutsideCorner: false);
            Assert.That(overdriven, Is.EqualTo(0f));
        }

        [Test]
        public void RearCornerLoad_ConservesTotalAxleLoad_BeforeInsideHitsZero()
        {
            const float staticLoad = 150f;
            foreach (var ratio in new[] { 0f, 0.2f, 0.5f, 0.8f, 1f })
            {
                var inside = KartDynamicsMath.CalculateRearCornerLoadNewtons(staticLoad, ratio, isOutsideCorner: false);
                var outside = KartDynamicsMath.CalculateRearCornerLoadNewtons(staticLoad, ratio, isOutsideCorner: true);
                Assert.That(inside + outside, Is.EqualTo(staticLoad * 2f).Within(0.01f),
                    $"ratio={ratio}: total rear axle load should be conserved while inside has not hit the zero clamp.");
            }
        }

        [Test]
        public void RearAxleBindingFactor_IsOneMinusInnerRearLift()
        {
            Assert.That(KartDynamicsMath.CalculateRearAxleBindingFactor(0f), Is.EqualTo(1f));
            Assert.That(KartDynamicsMath.CalculateRearAxleBindingFactor(1f), Is.EqualTo(0f));
            Assert.That(KartDynamicsMath.CalculateRearAxleBindingFactor(0.3f), Is.EqualTo(0.7f).Within(0.0001f));
        }

        [Test]
        public void RearAxleBindingFactor_ClampsOverdrivenInputs()
        {
            Assert.That(KartDynamicsMath.CalculateRearAxleBindingFactor(-1f), Is.EqualTo(1f));
            Assert.That(KartDynamicsMath.CalculateRearAxleBindingFactor(2f), Is.EqualTo(0f));
        }

        [Test]
        public void ScrubYawRateLoss_ZeroWhenMaxScrubIsZero()
        {
            // The Etapa-3 default for every asset predating this etapa --
            // must be an exact no-op regardless of how bound the axle is.
            foreach (var binding in new[] { 0f, 0.25f, 0.5f, 1f })
            {
                var loss = KartDynamicsMath.CalculateRearAxleScrubYawRateLossDegPerSec(binding, 0f);
                Assert.That(loss, Is.EqualTo(0f),
                    $"binding={binding}: max scrub 0 must mean zero yaw-rate loss (safe default, no regression).");
            }
        }

        [Test]
        public void ScrubYawRateLoss_ScalesWithBindingFactorAndMax()
        {
            var atNoBind = KartDynamicsMath.CalculateRearAxleScrubYawRateLossDegPerSec(0f, 20f);
            var atHalfBind = KartDynamicsMath.CalculateRearAxleScrubYawRateLossDegPerSec(0.5f, 20f);
            var atFullBind = KartDynamicsMath.CalculateRearAxleScrubYawRateLossDegPerSec(1f, 20f);

            Assert.That(atNoBind, Is.EqualTo(0f));
            Assert.That(atHalfBind, Is.EqualTo(10f).Within(0.001f));
            Assert.That(atFullBind, Is.EqualTo(20f).Within(0.001f));
        }

        [Test]
        public void ScrubYawRateLoss_NeverNegative_EvenWithOverdrivenInputs()
        {
            var loss = KartDynamicsMath.CalculateRearAxleScrubYawRateLossDegPerSec(-5f, -10f);
            Assert.That(loss, Is.GreaterThanOrEqualTo(0f));
            Assert.That(float.IsFinite(loss), Is.True);
        }

        private static KartCategorySO CreateTuning(float innerRearLiftThreshold, float chassisFlexFactor)
        {
            var so = ScriptableObject.CreateInstance<KartCategorySO>();
            var serialized = new SerializedObject(so);
            serialized.FindProperty("innerRearLiftThreshold").floatValue = innerRearLiftThreshold;
            serialized.FindProperty("chassisFlexFactor").floatValue = chassisFlexFactor;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return so;
        }

        [Test]
        public void EffectiveInnerRearLiftThreshold_DefaultFlexFactor_EqualsRawThreshold()
        {
            var tuning = CreateTuning(0.6f, 1f);
            Assert.That(tuning.EffectiveInnerRearLiftThreshold, Is.EqualTo(0.6f).Within(0.0001f));
            Object.DestroyImmediate(tuning);
        }

        [Test]
        public void EffectiveInnerRearLiftThreshold_HigherFlex_LowersThreshold()
        {
            var tuning = CreateTuning(0.6f, 2f);
            Assert.That(tuning.EffectiveInnerRearLiftThreshold, Is.EqualTo(0.3f).Within(0.0001f));
            Object.DestroyImmediate(tuning);
        }

        [Test]
        public void EffectiveInnerRearLiftThreshold_LowerFlex_RaisesThreshold_ClampedToOne()
        {
            // 0.6 / 0.2 = 3.0, but the effective threshold is a 0..1 ratio
            // (it feeds InverseLerp as the lower bound), so it must clamp.
            var tuning = CreateTuning(0.6f, 0.2f);
            Assert.That(tuning.EffectiveInnerRearLiftThreshold, Is.EqualTo(1f).Within(0.0001f));
            Object.DestroyImmediate(tuning);
        }
    }
}
