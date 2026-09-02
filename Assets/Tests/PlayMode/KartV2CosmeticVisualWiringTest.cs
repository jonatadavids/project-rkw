using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RKW.Physics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RKW.Tests.PlayMode
{
    /// <summary>
    /// RECOVERY etapa, oitava rodada (2026-09-01) -- direct answer to the
    /// founder's question "will you check whether the steering-wheel/
    /// front-wheel-turn animation and the exhaust smoke actually work on
    /// the RIGHT kart" (his exact words: "verifica pra mim se realmente vc
    /// estava no kart certo... esse de 80 hp que vc acabou de arrumar a
    /// direcao").
    ///
    /// The founder's report of the 3D model he sent ("fazia inclusive a
    /// fumaca" -- it even had smoke) can ONLY be KartV2 (resource path
    /// KartVisualV2ResourcePath, the model behind the in-game "18 HP"
    /// button): confirmed by reading the raw .obj files directly --
    /// RacingKart.obj (the "13 HP" button's model) has zero "smoke_puff*"
    /// named parts, while KartV2.obj has smoke_puff_0 through
    /// smoke_puff_4. So this test targets KartV2 + PrototypeSportPlusTuning
    /// specifically (the exact pair the "18 HP" button loads via
    /// RebuildKartVisual), not the "13 HP" pair.
    ///
    /// Unlike the physics-tuning mixup found earlier this round, the
    /// KartPhysicsPrototypeBootstrap.cs comments show the steering-wheel/
    /// wheel-turn/smoke code (rounds 27, 32, 33, 40, 42) WAS written with
    /// KartV2's actual geometry in mind -- e.g. "Round 32: kartv2 has
    /// extra baked-in steering-wheel parts this project's old external
    /// SteeringWheel.obj prop replaces". But round 42's own comment
    /// already admits "there is no Unity Editor available in this
    /// environment, so this still cannot be visually confirmed here
    /// before shipping" -- meaning it was reasoned through, never actually
    /// seen rendered. No automated test existed for any of it until now.
    ///
    /// This test cannot see pixels either (still no Editor here), but it
    /// exercises the EXACT same code path the "18 HP" button calls
    /// (KartPhysicsPrototypeBootstrap.RebuildKartVisual with KartV2's real
    /// resource paths) and checks, structurally and functionally:
    /// 1. The wiring succeeded at all (KartSteeringVisual/
    ///    KartWheelSpinVisual/KartExhaustSmokeController got attached --
    ///    they only do if CreateWheelSteeringPivot/FindPartsByPrefixes
    ///    actually found matching named parts on the real KartV2 model).
    /// 2. The front-wheel pivots and the cockpit steering-wheel pivot
    ///    ACTUALLY rotate in response to real steering input, by reading
    ///    their world Transform before/after -- not just "component
    ///    exists", which could still hide a pivot silently pointing at
    ///    the wrong/empty geometry.
    /// 3. The smoke pool actually grows a puff to non-zero size after a
    ///    couple of real Update() ticks with throttle applied.
    /// </summary>
    public sealed class KartV2CosmeticVisualWiringTest
    {
        [UnityTest]
        public IEnumerator SportPlusKartV2_RebuiltViaToggleButtonCodePath_HasWorkingSteeringWheelSmokeWiring()
        {
            var scene = SceneManager.CreateScene("KartV2CosmeticWiringTest");
            SceneManager.SetActiveScene(scene);

            var root = new GameObject("KartV2WiringTestKart");
            var body = root.AddComponent<Rigidbody>();
            body.linearDamping = 0.02f;
            body.angularDamping = 0.6f;
            var dynamics = root.AddComponent<KartDynamics>();
            var tuning = Resources.Load<KartCategorySO>("KartPhysics/PrototypeSportPlusTuning");
            Assert.That(tuning, Is.Not.Null, "Could not load PrototypeSportPlusTuning from Resources.");
            dynamics.Configure(tuning);
            yield return null;

            // Exactly the call KartCategoryToggleButton makes when the
            // player taps "KART: 18 HP · 80 km/h" -- same resource paths,
            // same method, no shortcuts.
            KartPhysicsPrototypeBootstrap.RebuildKartVisual(
                dynamics,
                KartPhysicsPrototypeBootstrap.KartVisualV2ResourcePath,
                KartPhysicsPrototypeBootstrap.TuningV2ResourcePath,
                Color.red,
                7);
            yield return null;

            Assert.That(dynamics.VisualRoot, Is.Not.Null, "RebuildKartVisual produced no visual at all for KartV2.");

            var allChildren = dynamics.VisualRoot.GetComponentsInChildren<Transform>(true);
            Debug.Log($"[DIAG3] KartV2 visual instantiated with {allChildren.Length} transforms total.");
            foreach (var t in allChildren)
            {
                var comps = t.GetComponents<Component>().Select(c => c.GetType().Name);
                Debug.Log($"[DIAG5]   - name='{t.name}', components=[{string.Join(",", comps)}]");
            }

            var steeringVisual = dynamics.VisualRoot.GetComponentInChildren<KartSteeringVisual>();
            Assert.That(steeringVisual, Is.Not.Null,
                "KartSteeringVisual was not added -- CreateWheelSteeringPivot found NO matching " +
                "steering_wheel/wheel_front_left_/wheel_front_right_ parts on the real KartV2 model. " +
                "The founder's 'wheels/steering wheel never turn' complaint would be fully explained by this.");

            var wheelSpinVisual = dynamics.VisualRoot.GetComponentInChildren<KartWheelSpinVisual>();
            Assert.That(wheelSpinVisual, Is.Not.Null,
                "KartWheelSpinVisual was not added -- no rolling-wheel pivot was found on KartV2.");

            var smokeController = dynamics.VisualRoot.GetComponentInChildren<KartExhaustSmokeController>();
            Assert.That(smokeController, Is.Not.Null,
                "KartExhaustSmokeController was not added -- no smoke_puff_* parts were found on KartV2 " +
                "(this is the exact feature the founder said his 3D model 'fazia inclusive a fumaca').");

            var frontLeftPivot = allChildren.FirstOrDefault(t => t.name == "FrontLeftSteeringPivot");
            var frontRightPivot = allChildren.FirstOrDefault(t => t.name == "FrontRightSteeringPivot");
            var cockpitWheelPivot = allChildren.FirstOrDefault(t => t.name == "SteeringWheelPivot (round 42)");

            Assert.That(frontLeftPivot, Is.Not.Null, "FrontLeftSteeringPivot was not created on KartV2.");
            Assert.That(frontRightPivot, Is.Not.Null, "FrontRightSteeringPivot was not created on KartV2.");
            Assert.That(cockpitWheelPivot, Is.Not.Null,
                "SteeringWheelPivot (round 42) -- the cockpit steering-wheel prop -- was not created on KartV2.");

            var frontLeftBefore = frontLeftPivot.localRotation;
            var frontRightBefore = frontRightPivot.localRotation;
            var cockpitWheelBefore = cockpitWheelPivot.localRotation;

            // Full steering to the right, no throttle/brake -- and give
            // KartSteeringVisual's LateUpdate at least one real frame to
            // react to the new SteeringInput (SetInput applies it
            // synchronously, no ramp -- see KartDynamics.SetInput).
            dynamics.SetInput(1f, 0f, 0f);
            yield return null;

            var frontLeftAfter = frontLeftPivot.localRotation;
            var frontRightAfter = frontRightPivot.localRotation;
            var cockpitWheelAfter = cockpitWheelPivot.localRotation;

            var frontLeftDeltaDegrees = Quaternion.Angle(frontLeftBefore, frontLeftAfter);
            var frontRightDeltaDegrees = Quaternion.Angle(frontRightBefore, frontRightAfter);
            var cockpitWheelDeltaDegrees = Quaternion.Angle(cockpitWheelBefore, cockpitWheelAfter);

            Debug.Log($"[DIAG3] After full-right steering input: frontLeftDelta={frontLeftDeltaDegrees:F2} deg, " +
                      $"frontRightDelta={frontRightDeltaDegrees:F2} deg, cockpitWheelDelta={cockpitWheelDeltaDegrees:F2} deg");

            Assert.That(frontLeftDeltaDegrees, Is.GreaterThan(1f),
                "KartV2's front-left wheel pivot did not visibly rotate in response to full steering input.");
            Assert.That(frontRightDeltaDegrees, Is.GreaterThan(1f),
                "KartV2's front-right wheel pivot did not visibly rotate in response to full steering input.");
            Assert.That(cockpitWheelDeltaDegrees, Is.GreaterThan(1f),
                "KartV2's cockpit steering-wheel prop did not visibly rotate in response to full steering input -- " +
                "this is exactly the founder's original round-42 request ('o da esquerda gira, e o 3d que mandei " +
                "pra voce girava tambem').");

            // ---- Smoke: apply throttle and let the pool run a couple of real Update() ticks ----
            dynamics.SetInput(0f, 1f, 0f);
            var puffs = allChildren.Where(t => t.name == "ExhaustPuff").ToList();
            Assert.That(puffs.Count, Is.GreaterThan(0), "KartV2's exhaust smoke pool has zero puff objects.");

            yield return null; // tick 1: KartExhaustSmokeController.Update() spawns a puff (still scale zero)
            yield return null; // tick 2: UpdateActivePuffs grows the now-active puff's scale above zero

            var anyPuffVisible = puffs.Any(p => p.localScale.magnitude > 0.001f);
            Debug.Log($"[DIAG3] Exhaust smoke pool size={puffs.Count}, any puff visibly scaled up: {anyPuffVisible}");
            Assert.That(anyPuffVisible, Is.True,
                "None of KartV2's exhaust puff objects grew to a visible size after two Update() ticks with " +
                "throttle applied -- the smoke effect is wired up but not actually emitting.");

            yield return SceneManager.UnloadSceneAsync(scene);
        }
    }
}
