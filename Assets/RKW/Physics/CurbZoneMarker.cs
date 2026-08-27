using UnityEngine;

namespace RKW.Physics
{
    /// <summary>
    /// Round 38 founder feedback: "a zebra poderia dar uma sensacao as
    /// vezes vibrar o celular como se tivesse subido mas nao é pra tirar
    /// velocidade nem travar" -- a plain, empty marker component attached
    /// to every curb ("zebra") segment when it is created (see both
    /// <see cref="KartPhysicsPrototypeBootstrap.CreateAlternatingCurbRibbon"/>
    /// overloads), so <see cref="KartCurbHapticsController"/> can tell a
    /// curb trigger apart from every OTHER non-solid trigger collider in
    /// the scene (the start/finish line, the grid-slot markers, checkpoint
    /// gates) using the exact same <c>GetComponent&lt;T&gt;() != null</c>
    /// pattern <see cref="KartCheckpointDetector"/> already uses for
    /// checkpoints -- no new detection mechanism invented, just this
    /// marker's presence/absence.
    ///
    /// Deliberately carries no data and no behavior: the curb's actual
    /// visual/collision setup is untouched, this is purely a tag.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CurbZoneMarker : MonoBehaviour
    {
    }
}
