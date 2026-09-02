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
    /// Etapa 1 (2026-08-31) integration tests for the per-axle tire model,
    /// exercising the real KartDynamics component end to end (Rigidbody +
    /// PhysX) rather than just the pure math in KartDynamicsMath -- see
    /// KartAxleTireModelTests (EditMode) for the unit-level coverage.
    /// </summary>
    public sealed class KartAxleTireModelIntegrationTests
    {
        private static KartDynamics SpawnKart()
        {
            var root = new GameObject("AxleTireModelTestKart");
            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(1.0f, 0.5f, 1.8f);
            collider.center = new Vector3(0f, 0.25f, 0f);
            var body = root.AddComponent<Rigidbody>();
            body.linearDamping = 0.02f;
            body.angularDamping = 0.6f;
            var dynamics = root.AddComponent<KartDynamics>();
            var tuning = Resources.Load<KartCategorySO>("KartPhysics/PrototypeRentalSportTuning");
            Assert.That(tuning, Is.Not.Null, "Could not load PrototypeRentalSportTuning from Resources.");
            dynamics.Configure(tuning);
            return dynamics;
        }

        // ---- Teste de integracao: subesterco ----
        [UnityTest]
        public IEnumerator Understeer_HighSpeedIncreasingSteering_FrontSlipGrowsWithSteering()
        {
            var scene = SceneManager.CreateScene("KartAxleUndersteerTest");
            SceneManager.SetActiveScene(scene);
            var kart = SpawnKart();
            var body = kart.GetComponent<Rigidbody>();
            yield return null;

            // Get the kart up to real speed in a straight line first.
            kart.SetInput(0f, 1f, 0f);
            for (var tick = 0; tick < 200; tick++)
            {
                yield return new WaitForFixedUpdate();
            }
            Assert.That(kart.SpeedKph, Is.GreaterThan(20f), "Test setup: kart should be at real speed before turning.");

            // Moderate steering, then a much sharper steering, both held at
            // the same (still-high) speed -- compare how much the front
            // axle actually slides in each case.
            kart.SetInput(0.5f, 0.3f, 0f);
            var moderateFrontSlips = new List<float>();
            for (var tick = 0; tick < 60; tick++)
            {
                yield return new WaitForFixedUpdate();
                moderateFrontSlips.Add(Mathf.Abs(kart.FrontSlipAngleDegrees));
            }

            kart.SetInput(1f, 0.3f, 0f);
            var sharpFrontSlips = new List<float>();
            var yawRates = new List<float>();
            for (var tick = 0; tick < 60; tick++)
            {
                yield return new WaitForFixedUpdate();
                sharpFrontSlips.Add(Mathf.Abs(kart.FrontSlipAngleDegrees));
                yawRates.Add(Mathf.Abs(body.angularVelocity.y) * Mathf.Rad2Deg);
            }

            var averageModerateSlip = Average(moderateFrontSlips);
            var averageSharpSlip = Average(sharpFrontSlips);
            Assert.That(averageSharpSlip, Is.GreaterThan(averageModerateSlip),
                "Pushing more steering at high speed should increase the front slip angle.");

            // Deliberately NOT asserting a specific final yaw-rate number --
            // the founder explicitly asked not to require an arbitrary
            // number without first observing a real baseline. This only
            // checks that yaw rate stays within the category's own
            // configured ceiling rather than climbing unbounded with
            // steering input, and that the kart never goes non-finite
            // (NaN/Infinity position) while doing it.
            foreach (var yawRate in yawRates)
            {
                Assert.That(yawRate, Is.LessThanOrEqualTo(kart.Tuning.MaximumYawRateDegrees + 1f));
            }

            Assert.That(float.IsFinite(kart.transform.position.x), Is.True);
            Assert.That(float.IsFinite(kart.transform.position.z), Is.True);

            yield return SceneManager.UnloadSceneAsync(scene);
        }

        // ---- Teste de integracao: estabilidade ----
        [UnityTest]
        public IEnumerator Stability_StraightLineSmallPerturbation_DoesNotSelfAmplifyIntoSpin()
        {
            var scene = SceneManager.CreateScene("KartAxleStabilityTest");
            SceneManager.SetActiveScene(scene);
            var kart = SpawnKart();
            var body = kart.GetComponent<Rigidbody>();
            yield return null;

            kart.SetInput(0f, 1f, 0f);
            for (var tick = 0; tick < 150; tick++)
            {
                yield return new WaitForFixedUpdate();
            }

            // A small lateral nudge, then release steering to zero and just
            // observe what the rear axle (stability) does on its own.
            body.AddForce(kart.transform.right * 2f, ForceMode.VelocityChange);
            kart.SetInput(0f, 0.5f, 0f);

            var yawRates = new List<float>();
            for (var tick = 0; tick < 120; tick++)
            {
                yield return new WaitForFixedUpdate();
                yawRates.Add(Mathf.Abs(body.angularVelocity.y) * Mathf.Rad2Deg);
            }

            // Stable = the perturbation settles down over time rather than
            // growing (comparing the back half of the sample window to the
            // front half smooths over any single noisy tick).
            var firstHalf = yawRates.GetRange(0, yawRates.Count / 2);
            var secondHalf = yawRates.GetRange(yawRates.Count / 2, yawRates.Count - yawRates.Count / 2);
            Assert.That(Average(secondHalf), Is.LessThanOrEqualTo(Average(firstHalf) + 2f),
                "A small perturbation with no steering input must not self-amplify into a growing spin.");
            Assert.That(float.IsFinite(kart.transform.position.x), Is.True);
            Assert.That(float.IsFinite(kart.transform.position.z), Is.True);

            yield return SceneManager.UnloadSceneAsync(scene);
        }

        private static float Average(List<float> values)
        {
            if (values.Count == 0)
            {
                return 0f;
            }

            var sum = 0f;
            foreach (var value in values)
            {
                sum += value;
            }

            return sum / values.Count;
        }
    }
}
