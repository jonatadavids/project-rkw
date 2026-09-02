using System.Collections;
using NUnit.Framework;
using RKW.Physics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RKW.Tests.PlayMode
{
    /// <summary>
    /// Etapa 3 (2026-08-31) integration tests for the rigid rear axle
    /// refinement (per-corner rear load estimates, axle bind factor),
    /// exercising the real KartDynamics component end to end (Rigidbody +
    /// PhysX). Uses the default PrototypeRentalSportTuning asset, whose
    /// rearAxleMaxScrubYawRateLossDegPerSec defaults to 0 (feature off) --
    /// see KartRigidAxleRefinementTests (EditMode) for the pure-math
    /// coverage of the scrub-resistance term itself, including with a
    /// nonzero tuning value (that test uses SerializedObject to configure
    /// a ScriptableObject in-editor, which is not available to this
    /// PlayMode assembly on device builds).
    /// </summary>
    public sealed class KartRigidAxleIntegrationTests
    {
        private static KartDynamics SpawnKart()
        {
            var root = new GameObject("RigidAxleTestKart");
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

        [UnityTest]
        public IEnumerator CorneringHard_RearOutsideLoadRisesAboveInside_BothFiniteAndNonNegative()
        {
            var scene = SceneManager.CreateScene("RigidAxleLoadTest");
            SceneManager.SetActiveScene(scene);
            var kart = SpawnKart();
            yield return null;

            kart.SetInput(0f, 1f, 0f);
            for (var i = 0; i < 150; i++) yield return new WaitForFixedUpdate();
            Assert.That(kart.SpeedKph, Is.GreaterThan(15f), "Test setup: kart should be at real speed before cornering.");

            kart.SetInput(0.9f, 0.5f, 0f);
            var sawOutsideExceedInside = false;
            for (var i = 0; i < 90; i++)
            {
                yield return new WaitForFixedUpdate();

                Assert.That(float.IsFinite(kart.RearInsideLoadNewtons), Is.True, $"tick {i}: inside load non-finite");
                Assert.That(float.IsFinite(kart.RearOutsideLoadNewtons), Is.True, $"tick {i}: outside load non-finite");
                Assert.That(kart.RearInsideLoadNewtons, Is.GreaterThanOrEqualTo(0f), $"tick {i}: inside load negative");
                Assert.That(kart.RearOutsideLoadNewtons, Is.GreaterThanOrEqualTo(0f), $"tick {i}: outside load negative");
                Assert.That(float.IsFinite(kart.RearAxleBindingFactor), Is.True, $"tick {i}: binding factor non-finite");
                Assert.That(kart.RearAxleBindingFactor, Is.InRange(0f, 1f), $"tick {i}: binding factor out of 0..1");

                if (kart.RearOutsideLoadNewtons > kart.RearInsideLoadNewtons)
                {
                    sawOutsideExceedInside = true;
                }
            }

            Assert.That(sawOutsideExceedInside, Is.True,
                "Sustained hard cornering should eventually load the outside rear corner more than the inside one.");

            yield return SceneManager.UnloadSceneAsync(scene);
        }

        [UnityTest]
        public IEnumerator DefaultTuning_ZeroMaxScrub_NeverReducesRequestedYawRate()
        {
            // Regression guard: PrototypeRentalSportTuning (and every asset
            // predating Etapa 3) has rearAxleMaxScrubYawRateLossDegPerSec
            // == 0. The pure-math test already proves the formula returns
            // exactly 0 whenever max scrub is 0 -- this just confirms the
            // real per-tick KartDynamics wiring agrees: the kart must still
            // be able to actually turn (measurable yaw rate) rather than
            // being frozen by some accidental non-zero scrub.
            var scene = SceneManager.CreateScene("RigidAxleNoRegressionTest");
            SceneManager.SetActiveScene(scene);
            var kart = SpawnKart();
            var body = kart.GetComponent<Rigidbody>();
            yield return null;

            kart.SetInput(0f, 1f, 0f);
            for (var i = 0; i < 100; i++) yield return new WaitForFixedUpdate();

            kart.SetInput(0.6f, 0.6f, 0f);
            for (var i = 0; i < 60; i++) yield return new WaitForFixedUpdate();

            Assert.That(float.IsFinite(body.angularVelocity.y), Is.True);
            var yawRateDegPerSec = Mathf.Abs(
                kart.transform.InverseTransformDirection(body.angularVelocity).y * Mathf.Rad2Deg);
            Assert.That(yawRateDegPerSec, Is.GreaterThan(1f),
                "Kart should be visibly yawing/turning with default (scrub-off) tuning and sustained steering input.");

            yield return SceneManager.UnloadSceneAsync(scene);
        }
    }
}
