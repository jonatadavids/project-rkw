using UnityEngine;

namespace RKW.Physics
{
    /// <summary>
    /// Round 39 (continuation 4, 2026-08-25): founder request -- vibrate the
    /// device when the kart hits a wall or another kart, mirroring the
    /// existing zebra/curb vibration pattern (see
    /// <see cref="KartCurbHapticsController"/>), but for solid collisions
    /// instead of trigger zones.
    ///
    /// This reuses <see cref="CollisionHandler.CalculateSeverity"/> (an
    /// existing, already-tested helper that combines impact speed and
    /// impact angle into a single severity number) purely as a math utility.
    /// It deliberately does NOT attach/activate <see cref="CollisionHandler"/>
    /// itself, because that component also applies a speed-loss penalty on
    /// impact -- a separate, undocumented gameplay change the founder did
    /// not ask for. This controller only vibrates; it never touches the
    /// kart's velocity.
    ///
    /// Round 39 (continuation 5, 2026-08-25) FIX: founder reported the
    /// phone vibrating constantly through the whole race, "como se
    /// estivesse arrastando na parede" (as if dragging on the wall), even
    /// on the Oval which has no tight walls to scrape. Root cause: Unity's
    /// OnCollisionEnter/OnCollisionStay fire for ANY solid contact -- and
    /// the kart's own body collider is ALWAYS resting on the pavement
    /// mesh floor (added in continuation 3/4), which counts as a
    /// "collision" too, every physics step, for as long as the kart is
    /// moving faster than MinimumRelativeSpeedMetersPerSecond (i.e.
    /// basically the entire race). This class never distinguished "hit a
    /// wall" from "driving on the ground" -- both are solid contacts.
    /// Fixed by checking the contact's surface normal: the ground's
    /// normal points straight UP (perpendicular to the flat track), while
    /// a wall or another kart's side is close to horizontal. Only contacts
    /// past <see cref="MinimumWallAngleFromUpDegrees"/> away from "straight
    /// up" now count.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class KartImpactHapticsController : MonoBehaviour
    {
        // Ignore very light taps (e.g. barely grazing a tire wall) so the
        // phone doesn't buzz on every tiny contact.
        private const float MinimumRelativeSpeedMetersPerSecond = 0.5f;
        private const float MinimumSeverityToVibrate = 0.5f;

        // A contact normal within this many degrees of straight-up counts
        // as "driving on the ground", not a wall/kart hit -- see this
        // class's continuation-5 doc comment above.
        private const float MinimumWallAngleFromUpDegrees = 60f;

        // Same cadence idea as KartCurbHapticsController's repeat interval,
        // so a kart stuck grinding against a wall doesn't vibrate every
        // single physics step.
        private const float RepeatIntervalSeconds = 0.15f;

        private float _nextVibrateAt;

        private void OnCollisionEnter(Collision collision)
        {
            HandleCollision(collision);
        }

        private void OnCollisionStay(Collision collision)
        {
            HandleCollision(collision);
        }

        private void HandleCollision(Collision collision)
        {
            if (collision.relativeVelocity.magnitude < MinimumRelativeSpeedMetersPerSecond)
            {
                return;
            }

            if (collision.contactCount <= 0)
            {
                return;
            }

            var contact = collision.GetContact(0);
            var angleFromUp = Vector3.Angle(contact.normal, Vector3.up);
            if (angleFromUp < MinimumWallAngleFromUpDegrees)
            {
                // This is the ground/pavement, not a wall or another kart --
                // see the continuation-5 doc comment above.
                return;
            }

            var severity = CollisionHandler.CalculateSeverity(collision);
            if (severity < MinimumSeverityToVibrate)
            {
                return;
            }

            if (Time.time < _nextVibrateAt)
            {
                return;
            }

            Handheld.Vibrate();
            _nextVibrateAt = Time.time + RepeatIntervalSeconds;
        }
    }
}
