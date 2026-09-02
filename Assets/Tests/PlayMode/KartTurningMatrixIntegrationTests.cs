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
    /// RECOVERY tuning round (2026-08-31) -- "matriz de curva" (turning
    /// matrix) validation requested explicitly by the founder after real
    /// play-testing showed steering had become heavy/unresponsive and
    /// corners very hard to complete (see
    /// claude/etapa-2-12-fisica-relatorio-final.md's RECOVERY section for
    /// the full writeup). A Python port of the same formulas
    /// (kartgrid_turning_matrix.py) was used during tuning to sweep a wide
    /// speed/steering grid quickly; THIS test confirms the same property
    /// against the REAL production KartDynamics component end to end
    /// (Rigidbody + PhysX), which is what the founder's directive
    /// explicitly asked for ("teste isso em codigo de producao, nao so em
    /// Python").
    ///
    /// The property under test: at a fixed, realistic speed, pushing MORE
    /// steering must never make the kart's turn radius WORSE (bigger).
    /// Once the front axle is fully grip-limited it is fine (expected,
    /// realistic understeer) for the radius to plateau rather than keep
    /// shrinking -- what must never happen again is the radius GROWING as
    /// more wheel is added, which was the exact "mais volante, menos
    /// curva" self-reinforcing loop the founder reported (root cause:
    /// CalculateFrontAxleSlipAngleDegrees reads the commanded wheel angle
    /// itself as near-full slip the instant a player steers, before yaw
    /// has caught up -- see KartCategorySO.cs's frontPeakSlipAngleDegrees
    /// comment for the full root-cause writeup).
    /// </summary>
    public sealed class KartTurningMatrixIntegrationTests
    {
        private static KartDynamics SpawnKart(string resourceName)
        {
            var root = new GameObject("TurningMatrixTestKart");
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

        private static float EffectiveRadiusMeters(KartDynamics kart)
        {
            var speedMps = kart.SpeedKph / 3.6f;
            var yawRateRad = Mathf.Abs(kart.FinalYawRateDegPerSec) * Mathf.Deg2Rad;
            return yawRateRad > 0.001f ? speedMps / yawRateRad : float.PositiveInfinity;
        }

        private static IEnumerator RunTurningMatrixCheck(string resourceName)
        {
            var scene = SceneManager.CreateScene("TurningMatrixTest");
            SceneManager.SetActiveScene(scene);
            var kart = SpawnKart(resourceName);
            yield return null;

            // Get up to a representative mid-range speed in a straight line first.
            kart.SetInput(0f, 1f, 0f);
            for (var tick = 0; tick < 200; tick++) yield return new WaitForFixedUpdate();
            Assert.That(kart.SpeedKph, Is.GreaterThan(15f),
                "Test setup: kart should be at real speed before turning.");

            // Assists off/ignored per the RECOVERY directive -- SetInput is
            // the raw, un-assisted steering path (KartAssistController is a
            // separate opt-in layer, not exercised here).
            var steeringLevels = new[] { 0.25f, 0.5f, 0.75f, 1.0f };
            var radii = new List<float>();
            foreach (var steer in steeringLevels)
            {
                // Light, steady throttle (not full) so the settle window
                // measures the corner's own steady-state radius rather than
                // a kart that is also still decelerating from the
                // steering-loss term -- matches how a real driver holds
                // partial throttle through a corner instead of coasting.
                kart.SetInput(steer, 0.5f, 0f);
                for (var tick = 0; tick < 90; tick++) yield return new WaitForFixedUpdate();

                Assert.That(float.IsFinite(kart.transform.position.x), Is.True,
                    $"[{resourceName}] steer={steer}: position.x non-finite");
                Assert.That(float.IsFinite(kart.transform.position.z), Is.True,
                    $"[{resourceName}] steer={steer}: position.z non-finite");

                var radius = EffectiveRadiusMeters(kart);
                Assert.That(float.IsNaN(radius), Is.False, $"[{resourceName}] steer={steer}: radius is NaN");
                radii.Add(radius);
            }

            for (var i = 0; i < radii.Count - 1; i++)
            {
                if (float.IsPositiveInfinity(radii[i]))
                {
                    continue; // essentially not turning yet at this level; nothing to compare
                }

                // 10% + 1m tolerance for tick-level noise around a real plateau
                // (grip-limited understeer legitimately holds an almost-flat
                // radius rather than a mathematically exact one).
                var allowedRadius = radii[i] * 1.10f + 1.0f;
                Assert.That(radii[i + 1], Is.LessThanOrEqualTo(allowedRadius),
                    $"[{resourceName}] steering {steeringLevels[i + 1]:F2} produced a WORSE (bigger) turn radius " +
                    $"({radii[i + 1]:F1}m) than steering {steeringLevels[i]:F2} ({radii[i]:F1}m) -- this is exactly " +
                    "the 'mais volante, menos curva' regression the RECOVERY tuning round exists to fix.");
            }

            yield return SceneManager.UnloadSceneAsync(scene);
        }

        [UnityTest]
        public IEnumerator MoreSteering_AtFixedSpeed_NeverWorsensTurnRadius_ThirteenHp()
        {
            yield return RunTurningMatrixCheck("KartPhysics/PrototypeSchoolTuning");
        }

        [UnityTest]
        public IEnumerator MoreSteering_AtFixedSpeed_NeverWorsensTurnRadius_EighteenHp()
        {
            yield return RunTurningMatrixCheck("KartPhysics/PrototypeRentalSportTuning");
        }
    }
}
