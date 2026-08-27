using UnityEngine;

namespace RKW.Physics
{
    /// <summary>
    /// Round 38 founder feedback: "a zebra poderia dar uma sensacao as
    /// vezes vibrar o celular como se tivesse subido mas nao é pra tirar
    /// velocidade nem travar" -- the curb ("zebra") has always been purely
    /// visual (a non-solid trigger, see every
    /// <see cref="KartPhysicsPrototypeBootstrap.CreateAlternatingCurbRibbon"/>
    /// call: solidCollider is always false, since the kart has no
    /// suspension/wheels to physically "climb" it -- see that method's own
    /// class doc). He is not asking for that to change (explicitly: no
    /// speed loss, no lock/blocking), just for the phone to buzz while
    /// riding over/through one, to fake the bump a real curb gives a real
    /// kart. Placed only on the PLAYER's kart (see
    /// <see cref="KartPhysicsPrototypeBootstrap.CreatePlayerKart"/>) --
    /// bots don't hold a phone.
    ///
    /// Unity's stock <see cref="Handheld.Vibrate"/> is a single fixed-length
    /// pulse with no "start/stop continuous rumble" API (that needs a
    /// native Android/iOS haptics plugin, out of scope here) -- this
    /// approximates a continuous buzz by calling it repeatedly at
    /// <see cref="RepeatIntervalSeconds"/> for as long as the kart's
    /// trigger volume overlaps ANY <see cref="CurbZoneMarker"/>-tagged
    /// collider, using an overlap COUNT (not a bool) so entering a second
    /// curb segment while still touching the first one (their pieces
    /// slightly overlap by design, see CreateAlternatingCurbRibbon) can
    /// never let one segment's OnTriggerExit stop the buzzing early while
    /// the kart is still physically on another one.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class KartCurbHapticsController : MonoBehaviour
    {
        private const float RepeatIntervalSeconds = 0.18f;

        private int _overlappingCurbCount;
        private float _nextVibrateAt;

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<CurbZoneMarker>() == null)
            {
                return;
            }

            _overlappingCurbCount++;
            if (_overlappingCurbCount == 1)
            {
                Vibrate();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponent<CurbZoneMarker>() == null)
            {
                return;
            }

            _overlappingCurbCount = Mathf.Max(0, _overlappingCurbCount - 1);
        }

        private void Update()
        {
            if (_overlappingCurbCount <= 0)
            {
                return;
            }

            if (Time.time >= _nextVibrateAt)
            {
                Vibrate();
            }
        }

        private void Vibrate()
        {
            Handheld.Vibrate();
            _nextVibrateAt = Time.time + RepeatIntervalSeconds;
        }
    }
}
