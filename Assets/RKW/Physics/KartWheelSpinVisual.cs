using System.Collections.Generic;
using UnityEngine;

namespace RKW.Physics
{
    /// <summary>
    /// Round 32 (2026-08-24) founder feedback on the new kart model: "as
    /// rodas giram e viram, igual ao volantes porem percebi que pela roda
    /// ser preta parece que nao esta girando". Checking the existing code
    /// (KartSteeringVisual) found the real cause: only the STEERING turn
    /// (wheels pointing left/right in a corner, rotating around the kart's
    /// own vertical axis) was ever implemented — there was no forward
    /// ROLLING rotation at all, on either kart model, so the wheels really
    /// were not spinning, not just hard to see. This component adds that:
    /// every frame, spins each wheel's own pivot around its axle axis
    /// (local X once parented under a pivot whose rotation matches the
    /// kart body — see KartPhysicsPrototypeBootstrap.CreateWheelSpinPivot)
    /// at a rate derived from the kart's own forward speed and that
    /// wheel's own radius (measured from its model bounds, so front and
    /// rear wheels of different sizes each spin at their own correct
    /// rate), the same way a real wheel's rotation is speed divided by
    /// circumference.
    ///
    /// Not wired up for the ghost kart, for the same reason
    /// KartSteeringVisual isn't (see that class's doc): GhostController has
    /// no KartDynamics to read a speed from. Configure() simply never gets
    /// called for it, so this component quietly does nothing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class KartWheelSpinVisual : MonoBehaviour
    {
        private readonly List<Transform> _pivots = new();
        private readonly List<float> _radiiMeters = new();
        private readonly List<float> _accumulatedDegrees = new();

        private KartDynamics _dynamics;

        /// <summary>
        /// Registers one wheel's spin pivot and its measured radius. Safe
        /// to call with a null pivot or a near-zero radius (e.g. a future
        /// model swap whose parts don't match the expected name prefixes)
        /// — that wheel is simply skipped rather than throwing or
        /// division-by-zero.
        /// </summary>
        public void AddWheel(Transform spinPivot, float radiusMeters)
        {
            if (spinPivot == null || radiusMeters <= 0.001f)
            {
                return;
            }

            _pivots.Add(spinPivot);
            _radiiMeters.Add(radiusMeters);
            _accumulatedDegrees.Add(0f);
        }

        public void Configure(KartDynamics dynamics)
        {
            _dynamics = dynamics;
        }

        private void LateUpdate()
        {
            if (_dynamics == null)
            {
                return;
            }

            var speedMetersPerSecond = _dynamics.SignedForwardSpeedKph / 3.6f;

            for (var i = 0; i < _pivots.Count; i++)
            {
                var circumferenceMeters = 2f * Mathf.PI * _radiiMeters[i];
                if (circumferenceMeters < 0.001f)
                {
                    continue;
                }

                var degreesPerSecond = (speedMetersPerSecond / circumferenceMeters) * 360f;
                var degrees = Mathf.Repeat(_accumulatedDegrees[i] + degreesPerSecond * Time.deltaTime, 360f);
                _accumulatedDegrees[i] = degrees;
                _pivots[i].localRotation = Quaternion.Euler(degrees, 0f, 0f);
            }
        }
    }
}
