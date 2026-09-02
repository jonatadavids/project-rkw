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
    /// regression guard. After three rounds of tuning adjustments that each
    /// looked correct on paper (mathematical validation, automated tests)
    /// but did not match the founder's actual hands-on feel ("18 HP ainda
    /// pesada, ainda mais lenta que a 13 HP" even after equalizing the
    /// front-axle steering curve and reducing engine braking / tightening
    /// the friction ellipse), this stops guessing at individual tuning
    /// values from reading the .asset files and instead runs BOTH
    /// categories through the exact same scripted input, side by side, in
    /// the real production KartDynamics component -- then logs every
    /// relevant number so the actual difference can be read directly from
    /// this test's own Unity log output instead of inferred from static
    /// configuration.
    ///
    /// Round 4 (raising RentalSport's LateralGripG 1.7 -> 2.0) targeted the
    /// specific mechanism found in the first run of this test: yaw rate is
    /// grip-limited to (LateralGripG * g) / speed once past a low threshold,
    /// so a faster category needs proportionally more grip just to match a
    /// slower one's rotation. But the founder's follow-up feedback after
    /// that change was that the 18 HP STILL feels heavy, and specifically
    /// "mesmo em curvas lentas, ja reduzido" -- i.e. even at LOW speed,
    /// already slowed down, not just when carrying too much speed into the
    /// corner. That rules out the round-4 mechanism as the (sole) cause,
    /// since yaw-rate-over-speed is at its LEAST restrictive at low speed
    /// (plenty of grip budget relative to the small speed in the
    /// denominator). Phase 4 below adds a controlled, fixed-low-speed
    /// comparison (bypassing acceleration entirely by setting the
    /// Rigidbody's velocity directly) to see whether a real difference
    /// shows up there that the earlier phases (which only sampled
    /// deliberately mismatched, higher, and decaying speeds) couldn't
    /// reveal.
    ///
    /// This test intentionally has almost no assertions (just basic
    /// finite/NaN sanity checks) -- its purpose is the Debug.Log lines,
    /// which land in rkw_playmode_tests.log from a normal
    /// build_deploy_verify.sh run. Search that log for "[DIAG]" to find
    /// this test's output.
    /// </summary>
    public sealed class KartCategoryComparisonDiagnosticTest
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
        public IEnumerator CompareThirteenHpAndEighteenHp_IdenticalInputScript_LogsSideBySideTelemetry()
        {
            var scene = SceneManager.CreateScene("CategoryComparisonDiagnostic");
            SceneManager.SetActiveScene(scene);

            var school = SpawnKart("KartPhysics/PrototypeSchoolTuning", new Vector3(0f, 0f, 0f));
            var rental = SpawnKart("KartPhysics/PrototypeRentalSportTuning", new Vector3(30f, 0f, 0f));
            var schoolBody = school.GetComponent<Rigidbody>();
            var rentalBody = rental.GetComponent<Rigidbody>();
            yield return null;

            // Dump the tuning values that most directly drive "feels
            // heavy" / "feels slow" complaints, up front, so the log is
            // self-contained -- no need to cross-reference the .asset
            // files separately while reading this.
            Debug.Log("[DIAG] === TUNING SNAPSHOT ===");
            Debug.Log($"[DIAG] School(13HP): maxSpeedKph={school.Tuning.MaxSpeedKph} zeroToMaxSeconds={school.Tuning.ZeroToMaxSeconds} " +
                      $"maxSteeringAngleDegrees={school.Tuning.MaxSteeringAngleDegrees} lateralGripG={school.Tuning.LateralGripG}");
            Debug.Log($"[DIAG] Rental(18HP): maxSpeedKph={rental.Tuning.MaxSpeedKph} zeroToMaxSeconds={rental.Tuning.ZeroToMaxSeconds} " +
                      $"maxSteeringAngleDegrees={rental.Tuning.MaxSteeringAngleDegrees} lateralGripG={rental.Tuning.LateralGripG}");

            // ---- Phase 1: straight-line acceleration, identical full-throttle input ----
            Debug.Log("[DIAG] === PHASE 1: ACCELERATION (steer=0, throttle=1, brake=0) ===");
            Debug.Log("[DIAG] ACC_HEADER tick,school_kph,school_throttleNorm,school_driveAccel,rental_kph,rental_throttleNorm,rental_driveAccel");
            school.SetInput(0f, 1f, 0f);
            rental.SetInput(0f, 1f, 0f);
            for (var tick = 0; tick < 300; tick++)
            {
                yield return new WaitForFixedUpdate();
                if (tick % 15 == 0)
                {
                    Debug.Log($"[DIAG] ACC {tick},{school.SpeedKph:F2},{school.NormalizedThrottle:F2},{school.RearLongitudinalDemand:F2}," +
                              $"{rental.SpeedKph:F2},{rental.NormalizedThrottle:F2},{rental.RearLongitudinalDemand:F2}");
                }
            }
            Debug.Log($"[DIAG] ACC_FINAL school_kph={school.SpeedKph:F2} rental_kph={rental.SpeedKph:F2} " +
                      $"(rental should be well ahead here -- higher top speed AND much quicker 0-to-max)");

            // ---- Phase 2: identical moderate cornering input, from whatever speed each kart reached ----
            Debug.Log("[DIAG] === PHASE 2: CORNERING (steer=0.6, throttle=0.6, brake=0) ===");
            Debug.Log("[DIAG] COR_HEADER tick,school_kph,school_yawDegS,school_frontGrip,school_rearGrip,school_frontSlip,school_rearSlip," +
                      "rental_kph,rental_yawDegS,rental_frontGrip,rental_rearGrip,rental_frontSlip,rental_rearSlip");
            school.SetInput(0.6f, 0.6f, 0f);
            rental.SetInput(0.6f, 0.6f, 0f);
            for (var tick = 0; tick < 150; tick++)
            {
                yield return new WaitForFixedUpdate();
                if (tick % 10 == 0)
                {
                    var schoolYaw = Mathf.Abs(schoolBody.angularVelocity.y) * Mathf.Rad2Deg;
                    var rentalYaw = Mathf.Abs(rentalBody.angularVelocity.y) * Mathf.Rad2Deg;
                    Debug.Log($"[DIAG] COR {tick},{school.SpeedKph:F2},{schoolYaw:F1},{school.FrontGripRatio:F2},{school.RearGripRatio:F2}," +
                              $"{school.FrontSlipAngleDegrees:F1},{school.RearSlipAngleDegrees:F1}," +
                              $"{rental.SpeedKph:F2},{rentalYaw:F1},{rental.FrontGripRatio:F2},{rental.RearGripRatio:F2}," +
                              $"{rental.FrontSlipAngleDegrees:F1},{rental.RearSlipAngleDegrees:F1}");
                }
            }

            // ---- Phase 3: identical corner-with-lift-and-reapply, matching real testing behavior ----
            Debug.Log("[DIAG] === PHASE 3: STEER HELD, THROTTLE LIFTED THEN REAPPLIED (mimics probing steering feel) ===");
            Debug.Log("[DIAG] LIFT_HEADER tick,school_kph,school_yawDegS,rental_kph,rental_yawDegS");
            for (var tick = 0; tick < 60; tick++)
            {
                school.SetInput(0.6f, 0f, 0f); // lift throttle, keep steering
                rental.SetInput(0.6f, 0f, 0f);
                yield return new WaitForFixedUpdate();
                if (tick % 10 == 0)
                {
                    var schoolYaw = Mathf.Abs(schoolBody.angularVelocity.y) * Mathf.Rad2Deg;
                    var rentalYaw = Mathf.Abs(rentalBody.angularVelocity.y) * Mathf.Rad2Deg;
                    Debug.Log($"[DIAG] LIFT {tick},{school.SpeedKph:F2},{schoolYaw:F1},{rental.SpeedKph:F2},{rentalYaw:F1}");
                }
            }
            for (var tick = 0; tick < 60; tick++)
            {
                school.SetInput(0.6f, 0.6f, 0f); // reapply throttle, keep steering
                rental.SetInput(0.6f, 0.6f, 0f);
                yield return new WaitForFixedUpdate();
                if (tick % 10 == 0)
                {
                    var schoolYaw = Mathf.Abs(schoolBody.angularVelocity.y) * Mathf.Rad2Deg;
                    var rentalYaw = Mathf.Abs(rentalBody.angularVelocity.y) * Mathf.Rad2Deg;
                    Debug.Log($"[DIAG] REAPPLY {tick},{school.SpeedKph:F2},{schoolYaw:F1},{rental.SpeedKph:F2},{rentalYaw:F1}");
                }
            }

            // ---- Phase 4: MATCHED LOW SPEED cornering, directly forced via Rigidbody velocity ----
            // This is the phase added for round 5, in direct response to the
            // founder's feedback that the heaviness happens "mesmo em
            // curvas lentas, ja reduzido" (even in slow corners, already
            // slowed down) -- NOT specifically when carrying too much speed
            // into the corner. Earlier phases only ever compared the two
            // karts at speeds that came out of acceleration/coasting, which
            // were never equal AND never deliberately low. This phase
            // side-steps that entirely: it snaps both karts to the SAME
            // fixed low speed (15 km/h, then again at 8 km/h) by writing
            // the Rigidbody velocity directly, zeroes any residual spin,
            // then applies identical, sustained, light-throttle steering
            // and holds it long enough (2.5s) for yaw rate to settle.
            foreach (var testSpeedKph in new[] { 70f, 60f, 50f, 40f, 30f, 25f, 15f, 8f })
            {
                var speedMps = testSpeedKph / 3.6f;
                schoolBody.linearVelocity = school.transform.TransformDirection(new Vector3(0f, 0f, speedMps));
                rentalBody.linearVelocity = rental.transform.TransformDirection(new Vector3(0f, 0f, speedMps));
                schoolBody.angularVelocity = Vector3.zero;
                rentalBody.angularVelocity = Vector3.zero;
                yield return null;

                Debug.Log($"[DIAG] === PHASE 4: MATCHED LOW SPEED CORNERING (fixed {testSpeedKph:F0} km/h, steer=0.6, throttle=0.2, brake=0) ===");
                Debug.Log("[DIAG] LOWSPEED_HEADER tick,school_kph,school_yawDegS,school_frontGrip,school_rearGrip,school_frontSlip,school_rearSlip," +
                          "rental_kph,rental_yawDegS,rental_frontGrip,rental_rearGrip,rental_frontSlip,rental_rearSlip");
                school.SetInput(0.6f, 0.2f, 0f);
                rental.SetInput(0.6f, 0.2f, 0f);
                for (var tick = 0; tick < 150; tick++)
                {
                    yield return new WaitForFixedUpdate();
                    if (tick % 10 == 0)
                    {
                        var schoolYaw = Mathf.Abs(schoolBody.angularVelocity.y) * Mathf.Rad2Deg;
                        var rentalYaw = Mathf.Abs(rentalBody.angularVelocity.y) * Mathf.Rad2Deg;
                        Debug.Log($"[DIAG] LOWSPEED{testSpeedKph:F0} {tick},{school.SpeedKph:F2},{schoolYaw:F1},{school.FrontGripRatio:F2},{school.RearGripRatio:F2}," +
                                  $"{school.FrontSlipAngleDegrees:F1},{school.RearSlipAngleDegrees:F1}," +
                                  $"{rental.SpeedKph:F2},{rentalYaw:F1},{rental.FrontGripRatio:F2},{rental.RearGripRatio:F2}," +
                                  $"{rental.FrontSlipAngleDegrees:F1},{rental.RearSlipAngleDegrees:F1}");
                    }
                }
            }

            Assert.That(float.IsFinite(school.transform.position.x), Is.True, "school position non-finite");
            Assert.That(float.IsFinite(rental.transform.position.x), Is.True, "rental position non-finite");

            yield return SceneManager.UnloadSceneAsync(scene);
        }
    }
}
