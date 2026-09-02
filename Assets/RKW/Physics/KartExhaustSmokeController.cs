using System.Collections.Generic;
using UnityEngine;

namespace RKW.Physics
{
    /// <summary>
    /// Round 40 (2026-08-26) founder request: "não da pra ver a fumacinha
    /// saindo do kart novo" (kartv2). The model already ships static
    /// "smoke_puff_0..4" mesh pieces near the exhaust (see
    /// docs/30-founder-playtest-log.md rodada 33), but nothing in the game
    /// ever moved or emitted from them -- they just sat there as unmoving
    /// geometry, easy to miss and not convincing as smoke. This hides that
    /// static geometry (see CreateKartVisual, where the original
    /// "smoke_puff*" parts get SetActive(false), the exact same technique
    /// ReplaceCockpitProp already uses for swapped-out parts) and replaces
    /// it with a small hand-rolled puff effect from this component.
    ///
    /// Deliberately NOT Unity's ParticleSystem with a transparent/soft
    /// shader: KartPhysicsPrototypeBootstrap.CreateMaterial's own comment
    /// explains why -- a runtime Shader.Find for anything other than this
    /// project's one vetted base material reliably renders as the Android
    /// IL2CPP missing-shader magenta fallback in this project's build
    /// pipeline, and there is no live Unity Editor in this environment to
    /// verify a new particle shader/material actually renders correctly
    /// on-device before it ships. A pool of plain opaque spheres that grow
    /// then shrink back to nothing needs no transparency at all, so it is
    /// guaranteed to render with the exact same material technique already
    /// proven to work everywhere else in this game (see AddWheelSpinMark
    /// for the same reasoning applied to the wheel-spin rim mark).
    ///
    /// Honest trade-off: this reads as small solid grey balls popping,
    /// growing and shrinking near the exhaust, not a soft wispy cloud --
    /// less pretty than a real particle system, but the safe choice when
    /// the result can't be seen rendered before the founder's own build.
    /// Emission rate scales with throttle input (more puffs the harder
    /// the engine is working), with a slow idle rate even at zero
    /// throttle so the exhaust never looks completely dead.
    ///
    /// Not wired up for the ghost kart, for the same reason
    /// KartSteeringVisual/KartWheelSpinVisual aren't (see those classes'
    /// docs): GhostController has no KartDynamics to read a throttle
    /// input from. Configure() simply never gets called for it, so this
    /// component quietly does nothing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class KartExhaustSmokeController : MonoBehaviour
    {
        private const int PoolSize = 6;
        private const float PuffLifetimeSeconds = 1.1f;
        // Round 43 (2026-09-01): founder playtest, "eu nao vi a fumacinha
        // saindo" -- at 0.09m max radius (18cm across) these opaque grey
        // puffs were apparently too small/subtle to notice during real
        // driving, even though the automated wiring test confirms they
        // ARE spawning and growing. Bumped up (0.09 -> 0.16) to make them
        // easier to spot without turning them into something silly-huge.
        private const float PuffMaxRadiusMeters = 0.16f;
        private const float PuffRiseSpeedMetersPerSecond = 0.5f;
        private const float PuffBackDriftSpeedMetersPerSecond = 0.3f;
        private const float PuffSidewaysSpreadMeters = 0.05f;

        // Idle (throttle = 0) still puffs slowly -- a real go-kart's
        // 2-stroke engine never looks completely dead at idle -- ramping
        // to a much faster rate at full throttle.
        private const float IdleEmissionIntervalSeconds = 0.9f;
        private const float FullThrottleEmissionIntervalSeconds = 0.22f;

        private static readonly Color PuffColor = new Color(0.55f, 0.55f, 0.55f);

        private Transform _emissionPoint;
        private Material _puffMaterial;
        private KartDynamics _dynamics;

        private readonly List<Transform> _pool = new List<Transform>();
        private readonly List<float> _ageSeconds = new List<float>();
        private readonly List<bool> _active = new List<bool>();
        private float _timeUntilNextPuff;

        /// <summary>
        /// Set once, right after this component is created inside
        /// CreateKartVisual -- <paramref name="emissionPoint"/> is the
        /// anchor placed at the hidden smoke_puff parts' own bounds center
        /// (see CreateKartVisual), so puffs spawn exactly where the
        /// founder's own model already marks the exhaust. Builds the pool
        /// immediately so Update never allocates.
        /// </summary>
        public void SetEmissionPoint(Transform emissionPoint, Material puffMaterial)
        {
            _emissionPoint = emissionPoint;
            _puffMaterial = puffMaterial;

            for (var i = 0; i < PoolSize; i++)
            {
                var puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                var puffCollider = puff.GetComponent<Collider>();
                if (puffCollider != null)
                {
                    DestroyImmediate(puffCollider);
                }

                puff.name = "ExhaustPuff";
                puff.transform.SetParent(emissionPoint, false);
                puff.transform.localScale = Vector3.zero;

                var renderer = puff.GetComponent<Renderer>();
                if (renderer != null && _puffMaterial != null)
                {
                    renderer.sharedMaterial = _puffMaterial;
                }

                _pool.Add(puff.transform);
                _ageSeconds.Add(0f);
                _active.Add(false);
            }
        }

        /// <summary>
        /// Wires this effect to the kart's own throttle input (called from
        /// CreateKartInstance/RebuildKartVisual, after KartDynamics exists
        /// -- it doesn't yet at the point CreateKartVisual runs).
        /// </summary>
        public void Configure(KartDynamics dynamics)
        {
            _dynamics = dynamics;
        }

        private void Update()
        {
            if (_emissionPoint == null || _pool.Count == 0)
            {
                return;
            }

            UpdateActivePuffs();

            if (_dynamics == null)
            {
                return;
            }

            _timeUntilNextPuff -= Time.deltaTime;
            if (_timeUntilNextPuff > 0f)
            {
                return;
            }

            var throttle01 = Mathf.Clamp01(_dynamics.ThrottleInput);
            SpawnPuff();
            _timeUntilNextPuff = Mathf.Lerp(
                IdleEmissionIntervalSeconds, FullThrottleEmissionIntervalSeconds, throttle01);
        }

        private void SpawnPuff()
        {
            for (var i = 0; i < _pool.Count; i++)
            {
                if (_active[i])
                {
                    continue;
                }

                _active[i] = true;
                _ageSeconds[i] = 0f;
                _pool[i].localPosition = new Vector3(
                    Random.Range(-PuffSidewaysSpreadMeters, PuffSidewaysSpreadMeters),
                    0f,
                    0f);
                _pool[i].localScale = Vector3.zero;
                return;
            }
            // Pool exhausted (every puff still animating) -- simply skip
            // this emission tick rather than growing the pool at runtime;
            // the next Update tries again once one frees up.
        }

        private void UpdateActivePuffs()
        {
            for (var i = 0; i < _pool.Count; i++)
            {
                if (!_active[i])
                {
                    continue;
                }

                _ageSeconds[i] += Time.deltaTime;
                var t = Mathf.Clamp01(_ageSeconds[i] / PuffLifetimeSeconds);
                if (t >= 1f)
                {
                    _active[i] = false;
                    _pool[i].localScale = Vector3.zero;
                    continue;
                }

                // Grows for the first third of its life, shrinks back to
                // nothing over the remaining two thirds -- reads as a puff
                // billowing out then dissipating, no transparency needed.
                var sizeT = t < 0.33f ? Mathf.InverseLerp(0f, 0.33f, t) : 1f - Mathf.InverseLerp(0.33f, 1f, t);
                var radius = PuffMaxRadiusMeters * sizeT;
                _pool[i].localScale = Vector3.one * (radius * 2f);

                // Local-space drift: since this pivot is itself a child of
                // the moving/rotating kart body, incrementing local
                // position "up and back" each frame makes the puff lag
                // further behind the kart in world space over time -- the
                // same effect a real trail of smoke has -- without needing
                // to reparent to world space.
                var local = _pool[i].localPosition;
                local.y += PuffRiseSpeedMetersPerSecond * Time.deltaTime;
                local.z -= PuffBackDriftSpeedMetersPerSecond * Time.deltaTime;
                _pool[i].localPosition = local;
            }
        }
    }
}
