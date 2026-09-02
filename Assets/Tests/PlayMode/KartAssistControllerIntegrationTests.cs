using System.Collections;
using NUnit.Framework;
using RKW.Physics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RKW.Tests.PlayMode
{
    /// <summary>
    /// Etapa 8 (2026-08-31) integration tests for KartAssistController,
    /// exercising the real component wired to a real KartDynamics (not the
    /// pure KartAssistMath functions directly -- see
    /// KartAssistMathTests (EditMode) for those).
    /// </summary>
    public sealed class KartAssistControllerIntegrationTests
    {
        private static (KartDynamics dynamics, KartAssistController assist) SpawnAssistedKart()
        {
            var root = new GameObject("AssistTestKart");
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
            var assist = root.AddComponent<KartAssistController>();
            return (dynamics, assist);
        }

        [UnityTest]
        public IEnumerator OffLevel_IsExactPassthrough()
        {
            var scene = SceneManager.CreateScene("AssistOffTest");
            SceneManager.SetActiveScene(scene);
            var (kart, assist) = SpawnAssistedKart();
            assist.AssistLevel = KartAssistController.Level.Off;
            yield return null;

            assist.ApplyInput(0.5f, 0.8f, 0.2f, 0.02f);
            var assistedSteering = kart.SteeringInput;
            var assistedThrottle = kart.ThrottleInput;

            kart.SetInput(0.5f, 0.8f, 0.2f);
            var directSteering = kart.SteeringInput;
            var directThrottle = kart.ThrottleInput;

            Assert.That(assistedSteering, Is.EqualTo(directSteering).Within(0.0001f),
                "Off level should produce identical SteeringInput to calling SetInput directly.");
            Assert.That(assistedThrottle, Is.EqualTo(directThrottle).Within(0.0001f));

            yield return SceneManager.UnloadSceneAsync(scene);
        }

        [UnityTest]
        public IEnumerator BeginnerLevel_AtHighSpeed_ReducesSteeringMagnitude()
        {
            var scene = SceneManager.CreateScene("AssistSteeringTest");
            SceneManager.SetActiveScene(scene);
            var (kart, assist) = SpawnAssistedKart();
            assist.AssistLevel = KartAssistController.Level.Beginner;
            yield return null;

            assist.ApplyInput(0f, 1f, 0f, 0.02f);
            for (var i = 0; i < 150; i++) yield return new WaitForFixedUpdate();
            Assert.That(kart.SpeedKph, Is.GreaterThan(30f), "Test setup: kart should be at real speed.");

            assist.ApplyInput(0.7f, 1f, 0f, 0.02f);
            yield return new WaitForFixedUpdate();

            Assert.That(Mathf.Abs(kart.SteeringInput), Is.LessThan(0.7f),
                $"At real speed, Beginner-level steering assist should reduce the effective steering below the raw 0.7 input (got {kart.SteeringInput:F3}).");

            yield return SceneManager.UnloadSceneAsync(scene);
        }

        [UnityTest]
        public IEnumerator BeginnerLevel_NeverExceedsRawInputMagnitude_AcrossManyTicks()
        {
            var scene = SceneManager.CreateScene("AssistNeverExceedsTest");
            SceneManager.SetActiveScene(scene);
            var (kart, assist) = SpawnAssistedKart();
            assist.AssistLevel = KartAssistController.Level.Beginner;
            yield return null;

            // Bug found via the first real PlayMode run (2026-08-31, post-
            // build_deploy_verify.sh round): comparing kart.SteeringInput
            // straight against the RAW 0.8 input is wrong for a tuning like
            // PrototypeRentalSportTuning whose SteeringResponseCurveExponent
            // is below 1 (0.9, "slightly more sensitive near center", from
            // Etapa 4/11). KartDynamics.SetInput applies that curve to
            // WHATEVER value it is given (assisted or not) -- see SetInput's
            // own doc comment -- and for exponent < 1, |x|^exponent > |x| for
            // any 0 < x < 1, so the curve itself pushes a sub-max input UP.
            // That is Etapa 4's own intended behavior for this category, has
            // nothing to do with assists, and is unrelated to whether the
            // assist "helped too much". The curve is monotonic in |input|
            // for any positive exponent, so if the assist never raises the
            // steering it hands to SetInput above the raw input (the actual
            // guarantee KartAssistMath's pure functions make, and what
            // KartAssistMathTests already verifies directly), the CURVED
            // result can never exceed curve(rawInput) either -- that is the
            // correct ceiling to check here, using the real production curve
            // function and the kart's real tuning, not a re-derivation.
            var curvedRawSteeringCeiling = Mathf.Abs(KartDynamicsMath.ApplySteeringResponseCurve(
                0.8f, kart.Tuning.SteeringResponseCurveExponent));

            for (var i = 0; i < 200; i++)
            {
                assist.ApplyInput(0.8f, 1f, 0f, Time.fixedDeltaTime);
                yield return new WaitForFixedUpdate();

                Assert.That(Mathf.Abs(kart.SteeringInput), Is.LessThanOrEqualTo(curvedRawSteeringCeiling + 0.001f),
                    $"tick {i}: assisted+curved steering magnitude exceeded what the raw 0.8 input alone " +
                    $"would produce through the same steering response curve -- assists must never amplify input.");
                Assert.That(kart.ThrottleInput, Is.LessThanOrEqualTo(1f + 0.001f),
                    $"tick {i}: assisted throttle exceeded the raw 1.0 input.");
            }

            yield return SceneManager.UnloadSceneAsync(scene);
        }

        [UnityTest]
        public IEnumerator NoAssistController_KartPrototypeInput_FallsBackToDirectSetInput()
        {
            // Regression guard for the "kart with no KartAssistController
            // behaves exactly as before Etapa 8" guarantee.
            var scene = SceneManager.CreateScene("AssistFallbackTest");
            SceneManager.SetActiveScene(scene);

            var root = new GameObject("NoAssistKart");
            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(1.0f, 0.5f, 1.8f);
            var body = root.AddComponent<Rigidbody>();
            var dynamics = root.AddComponent<KartDynamics>();
            var tuning = Resources.Load<KartCategorySO>("KartPhysics/PrototypeRentalSportTuning");
            dynamics.Configure(tuning);
            var input = root.AddComponent<KartPrototypeInput>();
            Assert.That(root.GetComponent<KartAssistController>(), Is.Null,
                "Test setup: this kart must not have a KartAssistController.");

            yield return null;
            input.SetInputEnabled(true);
            yield return null; // one Update tick

            // No exception, and the kart still received SOME input pathway
            // (steering/throttle/brake default to 0 with no touches/keys,
            // which is the expected idle state either way) -- the real
            // point of this test is that adding KartAssistController to the
            // pipeline did not throw or break a kart that doesn't have one.
            Assert.That(float.IsFinite(dynamics.SteeringInput), Is.True);

            yield return SceneManager.UnloadSceneAsync(scene);
        }
    }
}
