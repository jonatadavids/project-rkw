using UnityEngine;

namespace RKW.Physics
{
    /// <summary>Which view <see cref="KartPrototypeCamera"/> is currently rendering.</summary>
    public enum CameraViewMode
    {
        Chase,
        Cockpit
    }

    /// <summary>
    /// Founder request, 2026-08-23: "não sei se temos as 2 opções de
    /// câmera previstas queria aquela visão do piloto era seria perfeita
    /// para o nosso jogo" — this project only ever had a single third-person
    /// chase camera; there was no cockpit/first-person view and no plan for
    /// one. Adds a second view mode, toggled at runtime via
    /// <see cref="CameraViewToggleButton"/>.
    ///
    /// Cockpit mode deliberately does NOT reuse the chase-cam's
    /// spring-damper smoothing (<see cref="positionSharpness"/>/
    /// <see cref="rotationSharpness"/>): a first-person view that lags
    /// behind the kart's own rotation reads as disorienting/nauseating
    /// rather than "sitting in the kart", so it rigidly matches the kart's
    /// transform every frame instead — like the camera is welded to the
    /// driver's head.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class KartPrototypeCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        // Round 27 (2026-08-24) founder feedback: "aproximar a câmera de 3ª
        // pessoa... para que o kart ocupe um espaço maior na tela,
        // destacando os detalhes do modelo 3D" — makes sense now that the
        // kart is the detailed modeled RacingKart (round 26) instead of a
        // generic placeholder; the old offset was tuned back when there was
        // nothing worth seeing up close. ~28% closer on both axes (was
        // (0, 4.6, -7.2)) keeps the same viewing angle, just nearer.
        // Round 28 (2026-08-24): "aquela outra [câmera de 3ª pessoa]
        // melhorou mas achei um pouco distante ainda" — another ~20%
        // closer on top of round 27's pass (was (0, 3.3, -5.2)).
        // Round 32 (2026-08-24): "vi em outros karts como por exemplo o
        // mario kart que a camera 3 pessoa é bem mais próxima da pra ver
        // bem o carrinho se quiser colocar mais proximo ainda vai ser bem
        // legal" — another ~28% closer on top of round 28's pass (was
        // (0, 2.64, -4.16)). Cockpit (1st person) deliberately left
        // untouched this round — explicitly asked to keep it as-is
        // ("o 1 pessoa acho que se manter como esta").
        [SerializeField] private Vector3 chaseLocalOffset = new(0f, 1.90f, -3.00f);
        [SerializeField] private float positionSharpness = 7f;
        [SerializeField] private float rotationSharpness = 9f;

        // Rough seated-driver eye position for a real rental kart's low,
        // reclined seat: close to the kart's own pivot (not far forward or
        // back), low height (a kart seat puts the driver's hips only
        // ~0.15-0.2m off the ground, reclined, eyes roughly 0.6-0.8m up),
        // slightly toward the nose. Kept as its own serialized field (not
        // hardcoded into the math below) so it can be tuned in the
        // Inspector once a real kart model's actual seat position is known
        // — see docs/30-founder-playtest-log.md round 23 — without
        // touching code.
        // Round 28 (2026-08-24) founder feedback after seeing round 27's
        // first pitch-down attempt: "a câmera não mostra o volante...
        // talvez seria um pouco antes essa câmera" — the wheel prop sits
        // only ~0.15m ahead of this eye position in Z (see the
        // cockpitPitchDegrees comment below for the exact numbers), which
        // is too tight to fit both the wheel and any useful track view in
        // frame even after tilting down. Pulling the eye back (0.25 ->
        // -0.05, i.e. slightly BEHIND the kart's own pivot) opens that gap
        // up to ~0.45m, which the math below shows brings the wheel
        // comfortably inside frame instead of at the very edge.
        [SerializeField] private Vector3 cockpitLocalOffset = new(0f, 0.75f, -0.05f);

        // Round 27 founder feedback: "na camera de dentro do kart ficou só
        // a pista". Round 28 follow-up after testing the first fix: shows
        // hood now, but still no wheel. Redone with the actual numbers:
        // wheel prop center is at roughly (0, 0.56, 0.4) in the kart's
        // local space. With the OLD eye position (0, 0.75, 0.25) that's
        // ~52° below level — miles outside frame even tilted. With the
        // NEW eye position above (0, 0.75, -0.05), the wheel is only
        // ~23° below level, so a smaller tilt now centers it with room to
        // spare for the track above. Paired with a wider cockpit-only
        // field of view (close interior geometry needs more FOV than a
        // distant chase view to avoid feeling cropped).
        // Still a best-evidence fix, not something I can see rendered —
        // needs your eyes on the phone again to confirm.
        [SerializeField] private float cockpitPitchDegrees = 14f;
        [SerializeField] private float chaseFieldOfView = 62f;
        [SerializeField] private float cockpitFieldOfView = 78f;

        private Camera _camera;

        public CameraViewMode ViewMode { get; private set; } = CameraViewMode.Chase;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        public void Configure(Transform followTarget)
        {
            target = followTarget;
            Snap();
        }

        /// <summary>
        /// Switches view mode and snaps immediately — deliberately does NOT
        /// let the chase-cam's smoothing lerp in from wherever the OTHER
        /// mode's camera happened to be; switching views should read as an
        /// instant cut, not a camera that visibly flies across the scene.
        /// </summary>
        public void SetViewMode(CameraViewMode mode)
        {
            if (ViewMode == mode)
            {
                return;
            }

            ViewMode = mode;
            Snap();
        }

        public void ToggleViewMode()
        {
            SetViewMode(ViewMode == CameraViewMode.Chase ? CameraViewMode.Cockpit : CameraViewMode.Chase);
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            if (ViewMode == CameraViewMode.Cockpit)
            {
                ApplyCockpitTransform();
                return;
            }

            if (_camera != null)
            {
                _camera.fieldOfView = chaseFieldOfView;
            }

            var desiredPosition = target.TransformPoint(chaseLocalOffset);
            transform.position = Vector3.Lerp(transform.position, desiredPosition,
                1f - Mathf.Exp(-positionSharpness * Time.deltaTime));
            var lookTarget = target.position + target.forward * 3f + Vector3.up * 0.8f;
            var desiredRotation = Quaternion.LookRotation(lookTarget - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation,
                1f - Mathf.Exp(-rotationSharpness * Time.deltaTime));
        }

        private void Snap()
        {
            if (target == null)
            {
                return;
            }

            if (ViewMode == CameraViewMode.Cockpit)
            {
                ApplyCockpitTransform();
                return;
            }

            if (_camera != null)
            {
                _camera.fieldOfView = chaseFieldOfView;
            }

            transform.position = target.TransformPoint(chaseLocalOffset);
            transform.LookAt(target.position + target.forward * 3f + Vector3.up * 0.8f);
        }

        private void ApplyCockpitTransform()
        {
            if (_camera != null)
            {
                _camera.fieldOfView = cockpitFieldOfView;
            }

            transform.position = target.TransformPoint(cockpitLocalOffset);
            // Pitch down from the kart's own forward direction (see the
            // cockpitPitchDegrees field doc above for why this is needed).
            transform.rotation = target.rotation * Quaternion.Euler(cockpitPitchDegrees, 0f, 0f);
        }
    }
}
