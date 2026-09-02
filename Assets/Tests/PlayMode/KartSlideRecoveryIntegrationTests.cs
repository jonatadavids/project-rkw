using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using RKW.Physics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RKW.Tests.PlayMode
{
    /// <summary>
    /// RECOVERY tuning round (2026-08-31) -- reproduces, in real production
    /// code (not just the Python validation script), the exact bug the
    /// founder reported after play-testing: braking while steering at real
    /// speed could send the rear into a slide that felt "practically
    /// infinite" -- it did not recover even after lifting off the brake
    /// and getting back on the throttle. Uses PrototypeRentalSportTuning
    /// (the 18 HP / ~85 km/h target) specifically, since that is the
    /// category the founder named in the original report (at the time
    /// still called "13 HP" under the old category naming -- see
    /// claude/etapa-2-12-fisica-relatorio-final.md's RECOVERY section for
    /// the categoryId/asset mapping).
    ///
    /// This does NOT assert a specific recovery time or exact numeric
    /// trajectory -- the founder's own directive explicitly warned against
    /// requiring an arbitrary number without first observing a real
    /// baseline (see KartAxleTireModelIntegrationTests for the same
    /// principle already established this session). It asserts the
    /// qualitative property the bug report was about: a real slide gets
    /// induced, and then -- once the driver lifts the brake, backs off the
    /// steering, and gets back on the throttle -- the rear slip angle and
    /// yaw rate trend back down instead of staying pinned near their
    /// slide-time peak or growing further.
    /// </summary>
    public sealed class KartSlideRecoveryIntegrationTests
    {
        private static KartDynamics SpawnKart(string resourceName)
        {
            var root = new GameObject("SlideRecoveryTestKart");
            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(1.0f, 0.5f, 1.8f);
            collider.center = new Vector3(0f, 0.25f, 0f);
            var body = root.AddComponent<Rigidbody>();
            body.linearDamping = 0.02f;
            body.angularDamping = 0.6f;
            var dynamics = root.AddComponent<KartDynamics>();
            var tuning = Resources.Load<KartCategorySO>(resourceName);
            Assert.That(tuning, Is.Not.Null, $"Could not load {resourceName} from Resources.");
            dynamics.Configure(tuning);
            return dynamics;
        }

        private static float Average(List<float> values, int startIndex, int count)
        {
            var total = 0f;
            var actualCount = 0;
            for (var i = startIndex; i < startIndex + count && i < values.Count; i++)
            {
                total += values[i];
                actualCount++;
            }
            return actualCount > 0 ? total / actualCount : 0f;
        }

        [UnityTest]
        public IEnumerator BrakeAndSteerInducedSlide_RecoversInsteadOfStayingPinnedSideways()
        {
            var scene = SceneManager.CreateScene("SlideRecoveryTest");
            SceneManager.SetActiveScene(scene);
            var kart = SpawnKart("KartPhysics/PrototypeRentalSportTuning");
            var body = kart.GetComponent<Rigidbody>();
            yield return null;

            // Phase 1: get up to real speed in a straight line.
            kart.SetInput(0f, 1f, 0f);
            for (var tick = 0; tick < 200; tick++) yield return new WaitForFixedUpdate();
            Assert.That(kart.SpeedKph, Is.GreaterThan(30f),
                "Test setup: kart should be at real speed before the slide is induced.");

            // Phase 2: enter a real corner first (steering + light throttle,
            // building genuine cornering slip), THEN trail-brake hard on top
            // of it -- this is the classic "freio + giro = piao" scenario
            // the founder described, not just braking in a straight line.
            kart.SetInput(0.8f, 0.3f, 0f);
            for (var tick = 0; tick < 40; tick++) yield return new WaitForFixedUpdate();

            kart.SetInput(0.8f, 0f, 1f); // hold the same steering, full brake, no throttle
            var slideRearSlips = new List<float>();
            var slideYawRates = new List<float>();
            for (var tick = 0; tick < 40; tick++)
            {
                yield return new WaitForFixedUpdate();
                slideRearSlips.Add(Mathf.Abs(kart.RearSlipAngleDegrees));
                slideYawRates.Add(Mathf.Abs(body.angularVelocity.y) * Mathf.Rad2Deg);
                Assert.That(float.IsFinite(body.angularVelocity.y), Is.True, $"slide tick {tick}: angularVelocity non-finite");
                Assert.That(float.IsFinite(kart.transform.position.x), Is.True, $"slide tick {tick}: position.x non-finite");
            }

            var peakRearSlipDuringSlide = 0f;
            var peakYawRateDuringSlide = 0f;
            foreach (var v in slideRearSlips) peakRearSlipDuringSlide = Mathf.Max(peakRearSlipDuringSlide, v);
            foreach (var v in slideYawRates) peakYawRateDuringSlide = Mathf.Max(peakYawRateDuringSlide, v);

            Assert.That(peakRearSlipDuringSlide, Is.GreaterThan(3f),
                "Test setup: trail-braking hard mid-corner should induce a real, measurable rear slide " +
                $"(got only {peakRearSlipDuringSlide:F1} degrees of rear slip -- the test scenario itself " +
                "may need revisiting if this ever fails, since without a real slide there is nothing to recover FROM).");

            // Phase 3: recovery -- release the brake, CENTER the steering
            // (not just ease it), and get back on the throttle
            // progressively. This is exactly "solta o freio, endireita o
            // volante, volta o acelerador aos poucos" from the bug report.
            //
            // Centering fully (0f), not partially, is deliberate and was
            // confirmed necessary by a real failed run of this test
            // (2026-08-31): holding even a SMALL same-direction steering
            // input (0.15) while throttle and speed rebuild during recovery
            // made the yaw rate climb again, because a nonzero steering
            // input at a higher speed legitimately asks for more yaw
            // (CalculateAckermannYawRateDegreesPerSecond scales with
            // speed) -- once grip has recovered enough to deliver it, the
            // kart correctly follows that request. That is honest,
            // responsive steering working as intended, not a bug -- but it
            // does mean the test (and a real driver) must actually
            // straighten the wheel to ask for a straight line, not just
            // ease off it, matching real kart-driving technique.
            var recoveryRearSlips = new List<float>();
            var recoveryYawRates = new List<float>();
            for (var tick = 0; tick < 180; tick++)
            {
                var recoveryProgress = Mathf.Clamp01(tick / 60f); // throttle ramps in over the first ~1.2s of recovery
                kart.SetInput(0f, Mathf.Lerp(0.1f, 0.6f, recoveryProgress), 0f);
                yield return new WaitForFixedUpdate();
                recoveryRearSlips.Add(Mathf.Abs(kart.RearSlipAngleDegrees));
                recoveryYawRates.Add(Mathf.Abs(body.angularVelocity.y) * Mathf.Rad2Deg);
                Assert.That(float.IsFinite(body.angularVelocity.y), Is.True, $"recovery tick {tick}: angularVelocity non-finite");
                Assert.That(float.IsFinite(kart.transform.position.x), Is.True, $"recovery tick {tick}: position.x non-finite");
                Assert.That(float.IsFinite(kart.transform.position.z), Is.True, $"recovery tick {tick}: position.z non-finite");
            }

            // The core "does it recover" assertion: comparing the FIRST
            // second of recovery against the LAST second smooths over any
            // single noisy tick while still requiring real, sustained
            // improvement -- not staying pinned near the slide-time peak,
            // and not still growing.
            var earlyRecoveryRearSlip = Average(recoveryRearSlips, 0, 50);
            var lateRecoveryRearSlip = Average(recoveryRearSlips, recoveryRearSlips.Count - 50, 50);
            Assert.That(lateRecoveryRearSlip, Is.LessThan(earlyRecoveryRearSlip),
                $"Rear slip should trend DOWN during recovery (early={earlyRecoveryRearSlip:F1} deg, " +
                $"late={lateRecoveryRearSlip:F1} deg) -- a flat or growing trend here is exactly the " +
                "'practically infinite slide' the founder reported.");

            var earlyRecoveryYawRate = Average(recoveryYawRates, 0, 50);
            var lateRecoveryYawRate = Average(recoveryYawRates, recoveryYawRates.Count - 50, 50);
            Assert.That(lateRecoveryYawRate, Is.LessThan(earlyRecoveryYawRate),
                $"Yaw rate should trend DOWN (converge) during recovery (early={earlyRecoveryYawRate:F1} deg/s, " +
                $"late={lateRecoveryYawRate:F1} deg/s) -- a kart that keeps spinning at a steady or growing " +
                "rate never actually realigns, matching the founder's 'nao recupera mesmo soltando o freio' report.");

            // And the kart must have genuinely calmed down in absolute
            // terms too, not just "less than before" while still spinning
            // hard -- realigned means back to something a driver would call
            // "under control", not merely "improving".
            Assert.That(lateRecoveryYawRate, Is.LessThan(peakYawRateDuringSlide * 0.5f),
                $"By the end of the recovery window the yaw rate ({lateRecoveryYawRate:F1} deg/s) should have " +
                $"dropped well below the slide's own peak ({peakYawRateDuringSlide:F1} deg/s), not just off its peak.");

            yield return SceneManager.UnloadSceneAsync(scene);
        }
    }
}
