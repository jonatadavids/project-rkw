using System.Collections;
using NUnit.Framework;
using RKW.Physics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RKW.Tests.PlayMode
{
    /// <summary>
    /// RECOVERY tuning round (2026-08-31) -- diagnostic-only test, not a
    /// regression guard. Rounds 1-8 of this RECOVERY session compared
    /// PrototypeSchoolTuning against PrototypeRentalSportTuning, on the
    /// assumption that RentalSport was the founder's "18 HP" complaint
    /// kart. Every one of those diagnostics showed RentalSport performing
    /// equal-or-better than School at every matched speed, which never
    /// matched the founder's lived experience -- because it turns out that
    /// comparison was between the wrong pair of assets.
    ///
    /// KartPhysicsPrototypeBootstrap's in-game kart-swap button
    /// (KartCategoryToggleButton) does NOT toggle between School and
    /// RentalSport. It toggles between:
    ///   - TuningResourcePath   = PrototypeRentalSportTuning (default kart,
    ///     button shows "KART: 13 HP - 60 km/h" -- note this label text is
    ///     stale, the asset's actual MaxSpeedKph is 85, not 60)
    ///   - TuningV2ResourcePath = PrototypeSportPlusTuning (swapped-in
    ///     kart, button shows "KART: 18 HP - 80 km/h", paired with the
    ///     KartV2 visual model -- the one with the "carenagem"/fairing)
    ///
    /// So the founder's "13 HP, ótimo, quase 80 km/h" kart is actually
    /// RentalSport (matches: same asset already tuned across rounds 1-8,
    /// real top speed 85 kph), and the founder's "18 HP / 80HP, ruim, nao
    /// faz a curva" kart is actually SportPlus -- an asset that was never
    /// examined, tested, or changed even once before this round.
    ///
    /// This test runs the REAL problem pair (RentalSport vs SportPlus)
    /// through the identical scripted-input methodology used for the
    /// School/RentalSport comparison, so any real difference shows up
    /// directly in this test's own Unity log output ([DIAG] lines in
    /// rkw_playmode_tests.log) instead of being inferred from the static
    /// .asset values alone. Static comparison already suggests real
    /// causes -- SportPlus has lower LateralGripG (1.5 vs RentalSport's
    /// 2.0) at the SAME 85 kph top speed, a smaller MaxSteeringAngleDegrees
    /// (24 vs 30), a lower MaximumYawRateDegrees cap (120 vs 150), and
    /// front/rear slip angles that are completely undifferentiated (7/7
    /// peak, 24/24 full-loss) compared to RentalSport's carefully
    /// asymmetric 36/14 peak, 75/50 full-loss -- but per this session's own
    /// track record (rounds 4-8), static analysis alone has repeatedly
    /// been wrong, so this confirms it against the real KartDynamics
    /// component before any tuning value is changed.
    /// </summary>
    public sealed class KartRentalSportVsSportPlusDiagnosticTest
    {
        private static KartDynamics SpawnKart(string resourceName, Vector3 position)
        {
            var root = new GameObject("DiagnosticTestKart_" + resourceName.Replace("/", "_"));
            root.transform.position = position;
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

        [UnityTest]
        public IEnumerator CompareRentalSportAndSportPlus_IdenticalInputScript_LogsSideBySideTelemetry()
        {
            var scene = SceneManager.CreateScene("RentalSportVsSportPlusDiagnostic");
            SceneManager.SetActiveScene(scene);

            var rental = SpawnKart("KartPhysics/PrototypeRentalSportTuning", new Vector3(0f, 0f, 0f));
            var sportPlus = SpawnKart("KartPhysics/PrototypeSportPlusTuning", new Vector3(30f, 0f, 0f));
            var rentalBody = rental.GetComponent<Rigidbody>();
            var sportPlusBody = sportPlus.GetComponent<Rigidbody>();
            yield return null;

            Debug.Log("[DIAG2] === TUNING SNAPSHOT ===");
            Debug.Log($"[DIAG2] Rental(button:13HP): maxSpeedKph={rental.Tuning.MaxSpeedKph} zeroToMaxSeconds={rental.Tuning.ZeroToMaxSeconds} " +
                      $"maxSteeringAngleDegrees={rental.Tuning.MaxSteeringAngleDegrees} lateralGripG={rental.Tuning.LateralGripG}");
            Debug.Log($"[DIAG2] SportPlus(button:18HP): maxSpeedKph={sportPlus.Tuning.MaxSpeedKph} zeroToMaxSeconds={sportPlus.Tuning.ZeroToMaxSeconds} " +
                      $"maxSteeringAngleDegrees={sportPlus.Tuning.MaxSteeringAngleDegrees} lateralGripG={sportPlus.Tuning.LateralGripG}");

            // ---- Phase 1: straight-line acceleration, identical full-throttle input ----
            Debug.Log("[DIAG2] === PHASE 1: ACCELERATION (steer=0, throttle=1, brake=0) ===");
            Debug.Log("[DIAG2] ACC_HEADER tick,rental_kph,rental_throttleNorm,rental_driveAccel,sportPlus_kph,sportPlus_throttleNorm,sportPlus_driveAccel");
            rental.SetInput(0f, 1f, 0f);
            sportPlus.SetInput(0f, 1f, 0f);
            for (var tick = 0; tick < 300; tick++)
            {
                yield return new WaitForFixedUpdate();
                if (tick % 15 == 0)
                {
                    Debug.Log($"[DIAG2] ACC {tick},{rental.SpeedKph:F2},{rental.NormalizedThrottle:F2},{rental.RearLongitudinalDemand:F2}," +
                              $"{sportPlus.SpeedKph:F2},{sportPlus.NormalizedThrottle:F2},{sportPlus.RearLongitudinalDemand:F2}");
                }
            }
            Debug.Log($"[DIAG2] ACC_FINAL rental_kph={rental.SpeedKph:F2} sportPlus_kph={sportPlus.SpeedKph:F2}");

            // ---- Phase 2: identical moderate cornering input, from whatever speed each kart reached ----
            Debug.Log("[DIAG2] === PHASE 2: CORNERING (steer=0.6, throttle=0.6, brake=0) ===");
            Debug.Log("[DIAG2] COR_HEADER tick,rental_kph,rental_yawDegS,rental_frontGrip,rental_rearGrip,rental_frontSlip,rental_rearSlip," +
                      "sportPlus_kph,sportPlus_yawDegS,sportPlus_frontGrip,sportPlus_rearGrip,sportPlus_frontSlip,sportPlus_rearSlip");
            rental.SetInput(0.6f, 0.6f, 0f);
            sportPlus.SetInput(0.6f, 0.6f, 0f);
            for (var tick = 0; tick < 150; tick++)
            {
                yield return new WaitForFixedUpdate();
                if (tick % 10 == 0)
                {
                    var rentalYaw = Mathf.Abs(rentalBody.angularVelocity.y) * Mathf.Rad2Deg;
                    var sportPlusYaw = Mathf.Abs(sportPlusBody.angularVelocity.y) * Mathf.Rad2Deg;
                    Debug.Log($"[DIAG2] COR {tick},{rental.SpeedKph:F2},{rentalYaw:F1},{rental.FrontGripRatio:F2},{rental.RearGripRatio:F2}," +
                              $"{rental.FrontSlipAngleDegrees:F1},{rental.RearSlipAngleDegrees:F1}," +
                              $"{sportPlus.SpeedKph:F2},{sportPlusYaw:F1},{sportPlus.FrontGripRatio:F2},{sportPlus.RearGripRatio:F2}," +
                              $"{sportPlus.FrontSlipAngleDegrees:F1},{sportPlus.RearSlipAngleDegrees:F1}");
                }
            }

            // ---- Phase 3: MATCHED speed cornering across a range, directly forced via Rigidbody velocity ----
            foreach (var testSpeedKph in new[] { 70f, 60f, 50f, 40f, 30f, 25f, 15f, 8f })
            {
                var speedMps = testSpeedKph / 3.6f;
                rentalBody.linearVelocity = rental.transform.TransformDirection(new Vector3(0f, 0f, speedMps));
                sportPlusBody.linearVelocity = sportPlus.transform.TransformDirection(new Vector3(0f, 0f, speedMps));
                rentalBody.angularVelocity = Vector3.zero;
                sportPlusBody.angularVelocity = Vector3.zero;
                yield return null;

                Debug.Log($"[DIAG2] === PHASE 3: MATCHED SPEED CORNERING (fixed {testSpeedKph:F0} km/h, steer=0.6, throttle=0.2, brake=0) ===");
                Debug.Log("[DIAG2] MATCHED_HEADER tick,rental_kph,rental_yawDegS,rental_frontGrip,rental_rearGrip,rental_frontSlip,rental_rearSlip," +
                          "sportPlus_kph,sportPlus_yawDegS,sportPlus_frontGrip,sportPlus_rearGrip,sportPlus_frontSlip,sportPlus_rearSlip");
                rental.SetInput(0.6f, 0.2f, 0f);
                sportPlus.SetInput(0.6f, 0.2f, 0f);
                for (var tick = 0; tick < 150; tick++)
                {
                    yield return new WaitForFixedUpdate();
                    if (tick % 10 == 0)
                    {
                        var rentalYaw = Mathf.Abs(rentalBody.angularVelocity.y) * Mathf.Rad2Deg;
                        var sportPlusYaw = Mathf.Abs(sportPlusBody.angularVelocity.y) * Mathf.Rad2Deg;
                        Debug.Log($"[DIAG2] MATCHED{testSpeedKph:F0} {tick},{rental.SpeedKph:F2},{rentalYaw:F1},{rental.FrontGripRatio:F2},{rental.RearGripRatio:F2}," +
                                  $"{rental.FrontSlipAngleDegrees:F1},{rental.RearSlipAngleDegrees:F1}," +
                                  $"{sportPlus.SpeedKph:F2},{sportPlusYaw:F1},{sportPlus.FrontGripRatio:F2},{sportPlus.RearGripRatio:F2}," +
                                  $"{sportPlus.FrontSlipAngleDegrees:F1},{sportPlus.RearSlipAngleDegrees:F1}");
                    }
                }
            }

            Assert.That(float.IsFinite(rental.transform.position.x), Is.True, "rental position non-finite");
            Assert.That(float.IsFinite(sportPlus.transform.position.x), Is.True, "sportPlus position non-finite");

            yield return SceneManager.UnloadSceneAsync(scene);
        }
    }
}
