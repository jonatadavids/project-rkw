using UnityEngine;

namespace RKW.Physics
{
    /// <summary>
    /// Founder request, 2026-08-24 (round 27): "Esterçamento Ativo:
    /// Sincronizar a rotação do volante com a animação de rotação das
    /// rodas dianteiras" — every frame, turns the kart's front-left/front-
    /// right wheel pivots (see KartPhysicsPrototypeBootstrap's
    /// CreateWheelSteeringPivot) by the SAME angle the physics model is
    /// actually using (tuning.MaxSteeringAngleDegrees — the exact value
    /// KartDynamics.ApplySteering already feeds into its Ackermann yaw-
    /// rate math), so the wheels always visually show what the physics is
    /// really doing rather than an unrelated cosmetic number. Also spins
    /// the cockpit steering-wheel prop (round 27's new SteeringWheel.obj,
    /// see KartPhysicsPrototypeBootstrap.ReplaceCockpitProp) around its
    /// own local Z axis — that axis was picked earlier this round from the
    /// model's own bounding box (0.43m x 0.33m x 0.086m — Z is by far the
    /// thinnest, i.e. the direction the wheel "faces"), then aligned to
    /// the kart's own forward axis when the prop was placed.
    ///
    /// Not wired up for the ghost kart: GhostController only records
    /// position/yaw (see GhostSample), never steering input, and a ghost
    /// has no KartDynamics component at all to read from. Configure() is
    /// simply never called for it, so this component quietly does nothing
    /// — the ghost's cockpit still gets the new prop visually (placed by
    /// the same CreateKartVisual path every kart uses), it just doesn't
    /// animate.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class KartSteeringVisual : MonoBehaviour
    {
        // Founder feedback precedent, 2026-08-19 (KartPrototypeInput's own
        // UI wheel icon): the on-screen steering wheel visually rotates up
        // to 90° at full steering input (MaxWheelVisualRotationDegrees).
        // The cockpit's 3D steering wheel reuses that same number so the
        // 2D icon and the 3D prop agree, rather than inventing a second,
        // different visual rotation range.
        private const float SteeringWheelVisualMaxDegrees = 90f;

        private Transform _frontLeftPivot;
        private Transform _frontRightPivot;
        private Transform _steeringWheelProp;
        private KartDynamics _dynamics;
        private float _maxSteeringAngleDegrees = 30f;
        // Etapa 4 (2026-08-31): wheelbase/track needed for the per-wheel
        // Ackermann angle split below -- defaults are the same fallback
        // values KartCategorySO itself uses, only relevant if Configure()
        // is somehow never called (see Configure's own doc comment).
        private float _wheelbaseMeters = 1.05f;
        private float _frontTrackWidthMeters = 1.05f;

        // Round 40 (2026-08-26): see this class's doc comment above --
        // the real physics steering angle (24-28 degrees depending on
        // category) reads as too subtle on screen, so the WHEEL PIVOTS
        // (not the physics, not the cockpit wheel prop) turn further than
        // the real angle for a clearer visual cue, capped so it never
        // looks physically absurd even at a category's largest real angle.
        private const float VisualFrontWheelExaggerationMultiplier = 1.6f;
        private const float MaxVisualFrontWheelAngleDegrees = 38f;

        /// <summary>
        /// Set once, right after this component is created inside
        /// CreateKartVisual — any of the three may be null (e.g. a future
        /// model swap that doesn't have matching part names), in which
        /// case that specific piece simply never rotates instead of
        /// throwing.
        /// </summary>
        public void SetPivots(Transform frontLeftPivot, Transform frontRightPivot, Transform steeringWheelProp)
        {
            _frontLeftPivot = frontLeftPivot;
            _frontRightPivot = frontRightPivot;
            _steeringWheelProp = steeringWheelProp;
        }

        /// <summary>
        /// Wires this visual to the kart's own physics (called from
        /// CreateKartInstance, after KartDynamics exists — it doesn't yet
        /// at the point CreateKartVisual runs). Never called for the ghost
        /// kart (see class doc) or for the primitive fallback visual (no
        /// pivots/prop to wire in that case, and this component is never
        /// added to it — see CreateKartVisual).
        /// </summary>
        public void Configure(KartDynamics dynamics, KartCategorySO tuning)
        {
            _dynamics = dynamics;
            if (tuning != null)
            {
                _maxSteeringAngleDegrees = tuning.MaxSteeringAngleDegrees;
                _wheelbaseMeters = tuning.WheelbaseMeters;
                _frontTrackWidthMeters = tuning.FrontTrackWidthMeters;
            }
        }

        private void LateUpdate()
        {
            if (_dynamics == null)
            {
                return;
            }

            var steeringInput = _dynamics.SteeringInput;

            // Round 40: visual-only exaggeration, see the fields' comment
            // above -- the cockpit wheel prop below still uses the raw
            // steeringInput directly (its own 90-degree range already
            // reads clearly, no exaggeration needed there).
            var visualCentralWheelDegrees = Mathf.Clamp(
                steeringInput * _maxSteeringAngleDegrees * VisualFrontWheelExaggerationMultiplier,
                -MaxVisualFrontWheelAngleDegrees, MaxVisualFrontWheelAngleDegrees);

            // Etapa 4 (2026-08-31): real Ackermann geometry -- the wheel on
            // the inside of the turn points sharper than the outside one,
            // instead of both pivots turning by the identical angle (see
            // KartDynamicsMath.CalculateAckermannWheelAnglesDegrees). This
            // is purely cosmetic: the physics (ApplySteering) already uses
            // a single correct "bicycle model" angle for the whole axle,
            // untouched by this change. Positive steeringInput == steering
            // right (see KartBotMath), so the FRONT-RIGHT wheel is inner
            // when turning right, front-left is outer, and vice versa.
            KartDynamicsMath.CalculateAckermannWheelAnglesDegrees(
                visualCentralWheelDegrees, _wheelbaseMeters, _frontTrackWidthMeters,
                out var innerWheelDegrees, out var outerWheelDegrees);
            var turningRight = steeringInput > 0f;
            var frontLeftDegrees = turningRight ? outerWheelDegrees : innerWheelDegrees;
            var frontRightDegrees = turningRight ? innerWheelDegrees : outerWheelDegrees;

            if (_frontLeftPivot != null)
            {
                _frontLeftPivot.localRotation = Quaternion.Euler(0f, frontLeftDegrees, 0f);
            }

            if (_frontRightPivot != null)
            {
                _frontRightPivot.localRotation = Quaternion.Euler(0f, frontRightDegrees, 0f);
            }

            if (_steeringWheelProp != null)
            {
                _steeringWheelProp.localRotation =
                    Quaternion.Euler(0f, 0f, -steeringInput * SteeringWheelVisualMaxDegrees);
            }
        }
    }
}
