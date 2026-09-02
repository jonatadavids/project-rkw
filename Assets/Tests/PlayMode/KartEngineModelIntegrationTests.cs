using System.Collections;
using NUnit.Framework;
using RKW.Physics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RKW.Tests.PlayMode
{
    /// <summary>
    /// Etapa 10 (2026-08-31) integration tests: EngineRPM is always
    /// computed from real wheel speed on a live KartDynamics (default
    /// tuning still uses the legacy acceleration formula --
    /// UseTorqueCurveEngineModel defaults false -- but RPM itself is
    /// independent of that flag). See KartEngineModelTests (EditMode) for
    /// the pure-math coverage of both the RPM formula and the opt-in
    /// torque-curve acceleration formula.
    /// </summary>
    public sealed class KartEngineModelIntegrationTests
    {
        private static KartDynamics SpawnKart()
        {
            var root = new GameObject("EngineModelTestKart");
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
        public IEnumerator EngineRPM_StartsAtIdle_AndRisesAsKartAccelerates()
        {
            var scene = SceneManager.CreateScene("EngineRpmTest");
            SceneManager.SetActiveScene(scene);
            var kart = SpawnKart();
            yield return null;

            Assert.That(kart.EngineRPM, Is.EqualTo(kart.Tuning.EngineIdleRPM).Within(1f),
                "A stationary kart's engine should read at idle RPM.");

            kart.SetInput(0f, 1f, 0f);
            var rpmSamples = new System.Collections.Generic.List<float>();
            for (var i = 0; i < 150; i++)
            {
                yield return new WaitForFixedUpdate();
                Assert.That(float.IsFinite(kart.EngineRPM), Is.True, $"tick {i}: EngineRPM non-finite");
                Assert.That(kart.EngineRPM, Is.InRange(kart.Tuning.EngineIdleRPM - 1f, kart.Tuning.EngineRedlineRPM + 1f),
                    $"tick {i}: EngineRPM out of the tuned idle..redline range");
                rpmSamples.Add(kart.EngineRPM);
            }

            Assert.That(rpmSamples[rpmSamples.Count - 1], Is.GreaterThan(rpmSamples[0]),
                "EngineRPM should have risen well above idle after sustained full-throttle acceleration.");

            yield return SceneManager.UnloadSceneAsync(scene);
        }

        [UnityTest]
        public IEnumerator DefaultTuning_StillUsesLegacyAccelerationFormula_TopSpeedUnaffected()
        {
            // Regression guard: UseTorqueCurveEngineModel defaults false,
            // so top speed should still be governed by MaxSpeedKph exactly
            // as before Etapa 10, regardless of EngineRPM now existing
            // alongside it.
            var scene = SceneManager.CreateScene("EngineModelRegressionTest");
            SceneManager.SetActiveScene(scene);
            var kart = SpawnKart();
            Assert.That(kart.Tuning.UseTorqueCurveEngineModel, Is.False,
                "Test setup: PrototypeRentalSportTuning must default to the legacy acceleration model.");
            yield return null;

            kart.SetInput(0f, 1f, 0f);
            for (var i = 0; i < 400; i++) yield return new WaitForFixedUpdate();

            Assert.That(kart.SpeedKph, Is.LessThanOrEqualTo(kart.Tuning.MaxSpeedKph + 1f),
                $"Kart exceeded its tuned MaxSpeedKph ({kart.Tuning.MaxSpeedKph}) -- speed cap regression.");

            yield return SceneManager.UnloadSceneAsync(scene);
        }
    }
}
