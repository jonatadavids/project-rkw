using UnityEngine;

namespace RKW.Physics
{
    /// <summary>
    /// Round 28 (2026-08-24) founder request: "estou colocando pedais
    /// animados de aceleração e freio" — mirrors
    /// <see cref="KartSteeringVisual"/>'s pattern for the front wheels and
    /// cockpit steering wheel, but for the two cockpit pedals. Each pedal
    /// is a real hinge in the founder's own modeled Pedals.obj (a
    /// "brake_hinge_pin"/"throttle_hinge_pin" object marks the pivot each
    /// pedal's parts rotate around — see
    /// KartPhysicsPrototypeBootstrap.CreateHingePivot for how that pivot
    /// GameObject gets built from the model's own geometry), so this only
    /// has to rotate two pivots around their local X axis (the hinge's own
    /// axis, confirmed from the model's raw vertex data — the pin is a
    /// small cylinder whose long axis runs along local X) by an angle
    /// proportional to how hard the pedal is pressed.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class KartPedalVisual : MonoBehaviour
    {
        // How far the pedal face tips forward/down when fully pressed.
        // Chosen to look like a real pedal stroke without the face
        // disappearing into the floor — not measured from a real kart,
        // flagged for the founder to confirm/adjust once seen on device.
        private const float MaxPedalPressDegrees = 22f;

        private Transform _brakePivot;
        private Transform _throttlePivot;
        private KartDynamics _dynamics;

        public void SetPivots(Transform brakePivot, Transform throttlePivot)
        {
            _brakePivot = brakePivot;
            _throttlePivot = throttlePivot;
        }

        public void Configure(KartDynamics dynamics)
        {
            _dynamics = dynamics;
        }

        private void LateUpdate()
        {
            if (_dynamics == null) return;

            if (_brakePivot != null)
            {
                _brakePivot.localRotation = Quaternion.Euler(_dynamics.BrakeInput * MaxPedalPressDegrees, 0f, 0f);
            }

            if (_throttlePivot != null)
            {
                _throttlePivot.localRotation = Quaternion.Euler(_dynamics.ThrottleInput * MaxPedalPressDegrees, 0f, 0f);
            }
        }
    }
}
