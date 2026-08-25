using System;
using System.Collections.Generic;
using RKW.Telemetry;
using RKW.Track;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace RKW.Physics
{
    [DisallowMultipleComponent]
    public sealed class KartPhysicsPrototypeBootstrap : MonoBehaviour
    {
        // Founder playtest feedback, 2026-08-19: "no máximo 30 km" felt too
        // slow for "emoção". School (55 kph target, 8s 0-max) is the
        // intentionally underpowered training category by design (M2-T12);
        // Rental Sport (85 kph target, 5s 0-max, tighter/faster-decaying
        // grip curve) is the category meant to feel exciting, so the
        // playtest/demo build now drives that one instead.
        // Round 32 (2026-08-24): retuned 85 -> 60 km/h per founder
        // request ("pode ser 13 HP velocidade max 60") — see
        // docs/30-founder-playtest-log.md rodada 32 for the exact
        // numbers and the category-gap risk flagged there (School sits
        // at 55 km/h, so the gap to this category shrank a lot).
        // internal (was private) so KartCategoryToggleButton can
        // reference it directly instead of duplicating the string.
        internal const string TuningResourcePath = "KartPhysics/PrototypeRentalSportTuning";
        // Round 32: the founder's second, faster kart category — "18 HP,
        // ~80 km/h" — see PrototypeSportPlusTuning.asset.
        internal const string TuningV2ResourcePath = "KartPhysics/PrototypeSportPlusTuning";
        // Round 26 (2026-08-24) founder request: he modeled a real racing
        // kart (tubular chassis, carbon floor/nose/pods, bucket seat,
        // finned engine, 149 named parts across 11 materials — rubber,
        // magnesium, chromed_steel, carbon, seat_shell, black_plastic,
        // brass, red_anodized, cast_alloy, translucent_tank, number_plate)
        // via Claude in Cowork's separate 3D tool and asked to have it
        // imported into this project. Placed at
        // Assets/RKW/Physics/Resources/KartPhysics/Models/RacingKart.obj
        // (+ .mtl alongside, filenames matched so Unity's built-in OBJ
        // importer — no extra package needed, unlike FBX this ships
        // natively — resolves the mtllib reference and Kd colors into
        // named Material assets automatically). Founder chose "everyone,
        // accept the cost" when asked player-only vs. every kart vs. park
        // it for later — see CreateKartVisual's doc below for what "the
        // cost" is and docs/30-founder-playtest-log.md rodada 26 for the
        // full tradeoff writeup. KartVisual.fbx (Kenney Car Kit) kept on
        // disk, unreferenced, in case this ever needs reverting.
        internal const string KartVisualResourcePath = "KartPhysics/Models/RacingKart";
        // Round 32 (2026-08-24) founder request: a second, higher-
        // performance kart model ("kartv2"). This one arrived as only
        // .glb + .mtl (no companion .obj like every previous model), so
        // it was converted to .obj offline (this project's whole art
        // pipeline assumes Unity's native OBJ importer, same reasoning
        // as RacingKart.obj above) — geometry, materials and part names
        // (wheel_front_left_*, steering_wheel_*, pedal_brake_*, etc.)
        // preserved from the source file. See
        // docs/30-founder-playtest-log.md rodada 32.
        internal const string KartVisualV2ResourcePath = "KartPhysics/Models/KartV2";
        private const float KartVisualTargetLengthMeters = 1.8f;

        // Founder playtest feedback, 2026-08-20 (round 12): "o ideal seria
        // pista de kart mesmo com pneus em volta zebras, carrinho parecido
        // com kart real comecar a sair da animacao e deixa o jogo mais
        // real". Kenney's Racing Kit and Car Kit (both CC0, already vetted
        // and sitting in Assets/Art/Kenney/ — see kenney.nl/assets/racing-kit
        // and kenney.nl/assets/car-kit) were sourced for exactly this. The
        // outer barrier walls now show a tiled real fence model instead of
        // a flat colored cube; see CreateFenceVisual. KartVisual.fbx itself
        // (loaded via KartVisualResourcePath above) was swapped from an
        // unlabeled model to Car Kit's actual "kart-oodi" go-kart racer —
        // a file copy, no code change needed for that part.
        private const string FenceModelResourcePath = "KartPhysics/Models/TrackFence";
        // Real geometry measured from the source .dae (parsed its
        // <float_array> vertex positions directly — no live Unity Editor to
        // eyeball it against): 1m long along local X, 0.5m tall, pivot at
        // the piece's own corner rather than centered.
        private const float FenceTileLengthMeters = 1f;
        private const float FenceNativeHeightMeters = 0.5f;
        // Caps instance count on the two 84m-long outer walls (would
        // otherwise be 84 individual tiles each) — stretches tiles slightly
        // instead, trading a little visual repetition for a bounded worst
        // case of 4 * 48 = 192 procedurally-instantiated objects at scene
        // start. All tiles are marked isStatic for batching; still worth
        // checking actual draw calls in the Editor's Stats window.
        private const int MaxFenceTilesPerWall = 48;

        // Founder request, 2026-08-20 (round 13): looked ahead at the
        // project's own plan (docs/12-art-audio-performance.md + tasks.md
        // M3-T01) instead of only reacting to playtest reports — M3-T01
        // explicitly lists zebras, postos de fiscal (placeholder) and pit
        // entry/exit (placeholder) as required, still unchecked. This round
        // closes those three specific gaps using the same Kenney Racing Kit
        // pack already vetted (CC0) and integrated for the fence/kart.
        private const string MarshalFlagModelResourcePath = "KartPhysics/Models/MarshalFlag";
        private const string CheckeredFlagModelResourcePath = "KartPhysics/Models/CheckeredFlag";
        private const string CheckeredFlagTextureResourcePath = "KartPhysics/Models/Textures/checkers";
        private const string PitBuildingModelResourcePath = "KartPhysics/Models/PitBuilding";
        // Measured from the source .dae's <float_array> vertex positions
        // (same approach as the fence tile in round 12): pole+flag mesh
        // spans Y 0..1.25m with the pivot already at ground level, so no
        // vertical offset is needed when placing it.
        private const float MarshalFlagNativeHeightMeters = 1.25f;
        private static PhysicsMaterial _lowFrictionMaterial;
        private static PhysicsMaterial _kartCollisionMaterial;

        public KartDynamics SpawnedKart { get; private set; }

        private TrackConfigurationSO _trackConfiguration;
        private TimingManagerLite _timing;

        // Founder playtest feedback, 2026-08-20 (round 16): "o meu carrinho
        // sempre larga em primeiro talvez seja legal colocar... posicao no
        // grid de largada de forma aleatória". The player kart used to
        // always spawn at grid slot 0 (hardcoded), and bots always filled
        // slots 1..N in palette order — so the player was always P1 before
        // the race even started. Computed once in Awake (grid slot count is
        // fixed regardless of how many bots get picked later in the setup
        // menu) and shared with both CreatePlayerKart and SpawnBots so
        // every kart's starting slot — player included — is a genuine
        // random draw from the full grid, not just "player skips slot 0".
        private List<int> _shuffledGridSlotIndices;

        // Founder playtest feedback, 2026-08-20 (round 16): "talvez seja
        // legal colocar numeros nos carros de forma aleatório". A shuffled
        // pool of 1..MaxRaceNumber gives every kart in the race (player
        // included) a unique-looking race number, same shuffle-once-per-
        // race pattern as the grid slots and bot names above.
        private const int MaxRaceNumber = 20;
        private List<int> _shuffledRaceNumbers;
        private int _playerRaceNumber;

        private void Awake()
        {
            if (FindFirstObjectByType<KartDynamics>() != null)
            {
                return;
            }

            UnityEngine.Physics.gravity = new Vector3(0f, -9.81f, 0f);
            _trackConfiguration = LoadTrackConfiguration();
            CreateLighting();
            CreateCourse();
            _shuffledGridSlotIndices = BuildShuffledGridSlotIndices(_trackConfiguration);
            _shuffledRaceNumbers = BuildShuffledRaceNumbers();
            _playerRaceNumber = _shuffledRaceNumbers[0];
            SpawnedKart = CreatePlayerKart(_trackConfiguration, _shuffledGridSlotIndices, _playerRaceNumber);
            SetupPlayerRecovery(SpawnedKart, _trackConfiguration);
            // Kept locked until RaceStartController releases it after the
            // countdown — otherwise the player could drive around freely
            // while the setup menu below is still on screen.
            SpawnedKart.GetComponent<KartPrototypeInput>()?.SetInputEnabled(false);
            var prototypeCamera = CreateCamera(SpawnedKart.transform);
            _timing = SetupTiming(SpawnedKart);
            SetupTelemetry();

            // Founder playtest feedback, 2026-08-20: "poderia ter um botão
            // para fazer o start novamente da sessão... a qualquer tempo".
            var restartObject = new GameObject("RaceRestartButton");
            restartObject.AddComponent<RaceRestartButton>();

            // Founder request, 2026-08-23: "queria aquela visão do piloto
            // era seria perfeita para o nosso jogo" — this project only
            // ever had a single third-person chase camera; see
            // KartPrototypeCamera's XML doc for the new cockpit view and
            // CameraViewToggleButton for the on-screen toggle this wires up.
            var cameraToggleObject = new GameObject("CameraViewToggleButton");
            cameraToggleObject.AddComponent<CameraViewToggleButton>().Configure(prototypeCamera);

            // Round 32 (2026-08-24) founder request: a second, faster kart
            // to compare against the existing one — see
            // KartCategoryToggleButton's own class doc for why this is a
            // simple pre-race toggle button rather than a proper
            // category-selection screen (not attempted this round).
            var kartToggleObject = new GameObject("KartCategoryToggleButton");
            kartToggleObject.AddComponent<KartCategoryToggleButton>().Configure(
                SpawnedKart, SpawnedKart.GetComponent<KartPrototypeInput>(),
                new Color(0.15f, 0.35f, 0.85f), _playerRaceNumber);

            // Founder playtest feedback, 2026-08-19: "se for tranquilo
            // escolher entre 1, 3 e 5 voltas e até 10 bots e o nível deles".
            // Bots and the race itself are only spawned/started once the
            // player picks lap count / bot count / difficulty here, so the
            // grid (and the countdown) reflect what was actually chosen.
            var menuObject = new GameObject("RaceSetupMenu");
            var menu = menuObject.AddComponent<RaceSetupMenu>();
            menu.Configure(OnRaceSetupConfirmed);
        }

        private void OnRaceSetupConfirmed(int laps, int botCount, BotDifficulty difficulty)
        {
            var path = _trackConfiguration != null ? _trackConfiguration.BotPathPoints : Array.Empty<Vector3>();
            // Same track-signature scoping as LapRecordStore/GhostController
            // (round 23/24, same day) so leaderboard/ghost data never mixes
            // across different track layouts.
            var trackSignature = LapRecordMath.FormatTrackSignature(LapRecordMath.CalculateClosedPathLengthMeters(path));
            var kartCategoryId = SpawnedKart.Tuning.CategoryId;
            var comparisonScope = new PrototypeCompetitiveScope(trackSignature, kartCategoryId);

            var bots = SpawnBots(_trackConfiguration, botCount, difficulty, _shuffledGridSlotIndices, _shuffledRaceNumbers);

            var raceManagerObject = new GameObject("RaceManager");
            var raceManager = raceManagerObject.AddComponent<RaceManager>();
            var botControllers = new KartBotController[bots.Count];
            for (var i = 0; i < bots.Count; i++)
            {
                botControllers[i] = bots[i].GetComponent<KartBotController>();
            }
            // Founder playtest feedback, 2026-08-20 (round 9): "no final vc
            // mostra o top melhores voltas mas nao mostra a classificacao
            // da corrida, poderia mostrar" — RaceManager now also gets the
            // player transform/name/path so it can compute this race's
            // actual finishing order for the finish screen, the same way
            // RaceStandingsHud ranks the live panel below. Round-22 founder
            // feedback: "a tela final de classificação não mostra os
            // números de corrida" — also pass _playerRaceNumber (bots
            // already carry their own via KartBotController.RaceNumber),
            // same pool RaceStandingsHud's live panel already draws from.
            raceManager.Configure(_timing, laps, difficulty, SpawnedKart.GetComponent<KartPrototypeInput>(),
                botControllers, comparisonScope, SpawnedKart.transform, PlayerNameStore.GetName(), path, _playerRaceNumber);

            // Founder playtest feedback, 2026-08-20 (round 8): "não mostrou
            // a classificação... nem durante a corrida nem o nome dos
            // bots" — live standings, separate from RaceManager's
            // finish-only leaderboard.
            var standingsObject = new GameObject("RaceStandingsHud");
            var standings = standingsObject.AddComponent<RaceStandingsHud>();
            standings.Configure(SpawnedKart.transform, _timing, PlayerNameStore.GetName(), _playerRaceNumber, path, botControllers);

            // Founder request, 2026-08-24: "vamos tentar seguir talvez o
            // fantasma fique mais legal" — records/replays the player's own
            // best FULL RACE; see GhostController's XML doc for why this is
            // the quick, fun-first version rather than the formal M4-T06/T13
            // system, and for why it's keyed by lap count (1/3/5) instead
            // of a single looping best lap. No physics: CreateKartVisual
            // already strips every collider off the model, and this root
            // never gets a Rigidbody/KartDynamics, so GhostController just
            // repositions this transform directly instead of driving real
            // physics. Same track-signature scoping as LapRecordStore
            // (round 23, same day) so a ghost from an old track layout
            // never replays in the wrong place after the track changes.
            var ghostRoot = new GameObject("Ghost Kart");
            // Round 27: the ghost is the player's own recorded best run,
            // so it shows the player's own race number too — GhostSample
            // never recorded a race number of its own (see
            // GhostController's XML doc), and reusing _playerRaceNumber
            // reads correctly ("this is still you, from your best race")
            // instead of inventing an unrelated number.
            var ghostVisual = CreateKartVisual(ghostRoot.transform, GhostTintColor, _playerRaceNumber);
            var ghostControllerObject = new GameObject("GhostController");
            ghostControllerObject.AddComponent<GhostController>()
                .Configure(_timing, SpawnedKart.transform, ghostVisual, comparisonScope, laps);

            // M3-T01 "Evidência: Screenshot + profiler stats" — see
            // ScenePerformanceLogger for why this makes that evidence show
            // up automatically in the next build_deploy_verify.sh run
            // instead of needing a separate manual step.
            new GameObject("ScenePerformanceLogger").AddComponent<ScenePerformanceLogger>();

            SetupRaceStart(SpawnedKart, bots);
        }

        /// <summary>
        /// Founder playtest feedback, 2026-08-19: "largada 3 2 1 já com as
        /// bandeiras". Holds player and every bot's input until the
        /// countdown ends.
        /// </summary>
        private static void SetupRaceStart(KartDynamics player, System.Collections.Generic.List<KartDynamics> bots)
        {
            var starterObject = new GameObject("RaceStartSequence");
            var starter = starterObject.AddComponent<RaceStartController>();
            var playerInput = player != null ? player.GetComponent<KartPrototypeInput>() : null;
            var botControllers = new System.Collections.Generic.List<KartBotController>();
            foreach (var bot in bots)
            {
                var controller = bot != null ? bot.GetComponent<KartBotController>() : null;
                if (controller != null)
                {
                    botControllers.Add(controller);
                }
            }

            starter.Configure(playerInput, botControllers);
        }

        /// <summary>
        /// M3-T07: attaches the performance telemetry runner so FPS/memory/
        /// thermal samples are logged during on-device testing (including
        /// the upcoming M3-T08 30-minute real-device run). No analytics
        /// backend is wired in yet — see the comment on
        /// <see cref="RKW.Telemetry.ITelemetrySink"/> for why.
        /// </summary>
        private static void SetupTelemetry()
        {
            var telemetryObject = new GameObject("PerformanceTelemetry");
            telemetryObject.AddComponent<PerformanceTelemetryRunner>();
        }

        /// <summary>
        /// M3-T02: loads the TrackConfigurationSO at runtime so it is exercised
        /// outside of EditMode tests too. The greybox oval geometry below is
        /// still generated procedurally, not yet driven by this
        /// configuration's grid/checkpoint/spline data — except
        /// <see cref="TrackConfigurationSO.BotPathPoints"/>, which now
        /// drives the demo bots' waypoint following (see
        /// <see cref="SpawnBots"/>). Fully wiring the rest up is a
        /// separate, deliberate follow-up so it does not risk the already
        /// verified-on-device track generation in CreateCourse().
        /// </summary>
        private static TrackConfigurationSO LoadTrackConfiguration()
        {
            var trackConfiguration = Resources.Load<TrackConfigurationSO>("Track/OvalMvpTrackConfiguration");
            if (trackConfiguration == null)
            {
                Debug.LogWarning("KartPhysicsPrototypeBootstrap: no TrackConfigurationSO found at " +
                    "Resources/Track/OvalMvpTrackConfiguration.");
                return null;
            }

            if (!trackConfiguration.IsValid(out var reason))
            {
                Debug.LogWarning("KartPhysicsPrototypeBootstrap: TrackConfigurationSO " +
                    $"'{trackConfiguration.TrackConfigurationId}' failed validation: {reason}");
                return null;
            }

            Debug.Log("KartPhysicsPrototypeBootstrap: loaded track configuration " +
                $"'{trackConfiguration.TrackConfigurationId}' ({trackConfiguration.DisplayName}), " +
                $"direction={trackConfiguration.Direction}, grid slots={trackConfiguration.GridSlots.Count}.");
            return trackConfiguration;
        }

        private static TimingManagerLite SetupTiming(KartDynamics kart)
        {
            var timingObject = new GameObject("TimingManager");
            var timing = timingObject.AddComponent<TimingManagerLite>();
            timing.Configure(3); // 3 checkpoints (not counting start/finish)
            timingObject.AddComponent<TimingHUD>();

            var detector = kart.gameObject.AddComponent<KartCheckpointDetector>();
            detector.Configure(timing);
            return timing;
        }

        private static void SetupPlayerRecovery(KartDynamics kart, TrackConfigurationSO trackConfiguration)
        {
            if (kart == null || trackConfiguration == null)
            {
                return;
            }

            var recoveryPoints = new List<Vector3>();
            for (var i = 0; i < trackConfiguration.RecoveryPoints.Count; i++)
            {
                recoveryPoints.Add(trackConfiguration.RecoveryPoints[i].WorldPosition);
            }

            kart.gameObject.AddComponent<KartRecoveryController>().Configure(
                kart.GetComponent<KartPrototypeInput>(), recoveryPoints,
                trackConfiguration.RacingSplinePoints, trackConfiguration.TrackWidthMeters,
                trackConfiguration.RecoveryStuckSeconds,
                trackConfiguration.RecoveryStoppedSpeedMetersPerSecond,
                trackConfiguration.RecoveryInvertedDegrees,
                trackConfiguration.RecoverySafetyHeightMeters,
                trackConfiguration.RecoveryCollisionGraceSeconds,
                trackConfiguration.RecoveryPerimeterMultiplier);
        }

        private static void CreateLighting()
        {
            if (FindFirstObjectByType<Light>() != null)
            {
                return;
            }

            var lightObject = new GameObject("Technical Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightObject.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
        }

        private static void CreateCourse()
        {
            // --- GROUND (grass) ---
            // Founder playtest feedback history: y=-0.05 (11cm step) then
            // y=0.04 (2cm step) both trapped the kart at the grass/track
            // seam (PhysX "internal edge" catching between two separate,
            // non-coplanar colliders). Round 8 made the collision flush
            // with the track's top (0.06) to kill the step entirely — which
            // round 9 feedback confirmed fixed the trapping, but introduced
            // a new problem: "o frame ficou piscando... a pista" — with the
            // SAME object's mesh rendered exactly coplanar with the track's
            // rendered top face, the two overlapping surfaces z-fight
            // (flicker) wherever they touch. Splitting collision from
            // rendering fixes both at once: the (invisible) collision slab
            // stays perfectly flush at 0.06 so there's still no seam, while
            // the visible grass mesh sits a hair below (0.045) so it never
            // occupies the exact same depth as the track's rendered top —
            // that 1.5cm gap is imperceptible at kart-cam angles but enough
            // to stop the z-fight.
            // Round 25: widened from 150 to 200 (x) so it still comfortably
            // covers the track after the straight got longer — the
            // farthest-out props are now the E/W marshal posts and escape
            // areas at |x| ~78-82.5 (see StadiumHalfStraightMeters's round-25
            // comment below), same margin philosophy as before (old 150
            // comfortably covered the old ~60.5m farthest prop). z (120)
            // is untouched — nothing in z scales with straight length.
            var groundCollision = new GameObject("Grass Ground Collision");
            groundCollision.transform.position = new Vector3(0f, 0.06f, 0f);
            var groundCollider = groundCollision.AddComponent<BoxCollider>();
            groundCollider.size = new Vector3(200f, 0.02f, 120f);
            groundCollider.sharedMaterial = GetLowFrictionMaterial();

            var groundVisual = GameObject.CreatePrimitive(PrimitiveType.Plane);
            groundVisual.name = "Grass Ground";
            groundVisual.transform.position = new Vector3(0f, 0.045f, 0f);
            groundVisual.transform.localScale = new Vector3(20f, 1f, 12f);
            groundVisual.GetComponent<Renderer>().sharedMaterial = CreateMaterial("Grass", new Color(0.25f, 0.55f, 0.18f));
            Destroy(groundVisual.GetComponent<Collider>());

            // --- TRACK SURFACE ---
            // Founder playtest feedback, 2026-08-20 (round 19): reference
            // photos + hand-drawn track sketches sent in, with the direct
            // complaint that the round-18 track "mudou, [mas] ficou um
            // quadrado" (real curves wanted, not axis-aligned boxes) and
            // "poucos pneus". Asked explicitly whether to invest in real
            // rotated geometry vs. more corners built from the same
            // axis-aligned boxes; founder chose the bigger investment
            // ("curvas de verdade com peças rotacionadas").
            //
            // This is a genuine architecture change: every piece-creation
            // helper up to round 18 only ever built axis-aligned boxes
            // (position + scale, identity rotation) — there was no way to
            // place a piece at an angle. Round 20 adds that capability
            // (CreateTrackPieceOriented/CreateWallOriented, further down in
            // this file) and uses it to replace the old "4 corner fill
            // boxes" topology with a proper closed "stadium" (discorectangle)
            // shape: 2 straights + 2 true semicircular ends, each end built
            // from 8 short rotated pavement slabs (22.5° each) that hug a
            // real circular arc instead of one big flat box guessing at the
            // curve.
            //
            // The stadium's radius is NOT a free choice: two straights
            // joined by two semicircle end-caps only close into a loop if
            // both radii equal exactly half the straights' separation (see
            // GenerateStadiumCenterline's own comment for the derivation) —
            // asymmetric-radius ends (a "fast" end and a "technical" end,
            // like round 18 had) would need a fundamentally different,
            // non-stadium topology. Deliberately NOT attempted this round:
            // one real, closed, gap-validated curved loop first, asymmetric
            // ends/chicanes/esses as a follow-up once this is confirmed
            // stable on device (see docs/30-founder-playtest-log.md).
            // Radius (14m) was chosen deliberately tight — a lazy 24m+
            // sweeper would fix "square" but not "curva sem dificuldade"
            // (round 17 feedback); at 14m radius with this tuning's
            // 1.0-1.2g lateral grip, physical max cornering speed works out
            // to ~42-46 km/h (sqrt(radius * grip)), a real reduction from
            // straight-line speed that needs braking, not a formality.
            //
            // Every segment below (pavement, both barrier rings) was
            // generated from the exact same closed-form circle/straight
            // formulas and validated in a standalone Python model before
            // any of this was written: dense sampling of the TRUE circular
            // arc (not just the polygon approximation) at multiple lateral
            // offsets found zero pavement gaps anywhere a kart's wheels
            // could be — the exact bug class (a clipped/gapped corner) that
            // has cost a dedicated round to fix every previous time this
            // project touched track geometry.
            var asphaltMat = CreateMaterial("Asphalt", new Color(0.22f, 0.22f, 0.25f));
            var whiteMat = CreateMaterial("White", new Color(0.95f, 0.95f, 0.95f));

            // Stadium parameters (meters). See GenerateStadiumCenterline's
            // comment for why the two end radii can't be chosen freely once
            // the straight half-length (xc) is picked.
            //
            // Round 25 (2026-08-24) founder request: "ja pode aumentar o
            // circuito". Only the straight half-length changed (38 -> 60,
            // +58%, total lap length ~240m -> ~328m); the radius is
            // untouched on purpose — the curves are the one part of this
            // geometry that was hard-won gap-free (round 20/21, validated
            // against a standalone Python model), and GenerateStadiumCenterline's
            // arc math is a pure function of the radius, never the
            // half-straight, so leaving the radius alone means the curve
            // shape is BYTE-IDENTICAL to before, just translated further
            // out along x — zero new gap risk. Every other position below
            // that used to be a hand-picked literal (grid, checkpoints,
            // pit lines, marshal posts, escape areas, the ground plane...)
            // is now computed from these two constants instead, so the
            // next track-size change is a constant edit here, not another
            // find-every-hardcoded-number hunt. See
            // docs/30-founder-playtest-log.md round 25 for the full
            // before/after derivation and
            // Assets/RKW/Track/Resources/Track/OvalMvpTrackConfiguration.asset
            // (gridSlots/botPathPoints, the two fields actually read at
            // runtime — see that file's own round-25 note) which was
            // regenerated in lockstep with these same formulas.
            const float StadiumHalfStraightMeters = 60f;
            const float StadiumRadiusMeters = 14f;
            // Round 21 founder feedback: "ficou um pouco desformatado com
            // pontos de grama" — the round-20 curve used 8 arc segments per
            // end plus an unrotated joint square at every vertex to hide the
            // gaps. That joint-square approach produced a visible jagged
            // "cog wheel" silhouette on the curves (each joint square was
            // LARGER than the rotated chord segments it sat between).
            // Bumped to 12 segments per end (smaller, smoother chords) and
            // switched CreateRibbon/CreateWallRibbon to an overlap-extension
            // technique instead of joints — see those methods' comments.
            const int StadiumArcSegmentsPerEnd = 12;
            const float TrackWidthMeters = 7f;
            const float OuterBarrierRunoffMeters = 3f;
            const float InnerBarrierRunoffMeters = 1.5f;

            // Round 25: named anchor points so every fixed marker below
            // reads as "23m before the east end" etc. instead of a bare
            // literal that silently goes stale the next time
            // StadiumHalfStraightMeters changes. WestEndX/EastEndX are the
            // x where each straight meets its arc; WestArcTipX/EastArcTipX
            // are the true tip of each semicircle (end +/- radius) — e.g.
            // CP3 sits exactly on the west arc tip, not just "near" the
            // west end.
            const float WestEndX = -StadiumHalfStraightMeters;
            const float EastEndX = StadiumHalfStraightMeters;
            const float WestArcTipX = -(StadiumHalfStraightMeters + StadiumRadiusMeters);
            const float EastArcTipX = StadiumHalfStraightMeters + StadiumRadiusMeters;

            var pavementCenterline = GenerateStadiumCenterline(
                StadiumHalfStraightMeters, StadiumRadiusMeters, StadiumArcSegmentsPerEnd);
            var outerBarrierCenterline = GenerateStadiumCenterline(
                StadiumHalfStraightMeters, StadiumRadiusMeters + TrackWidthMeters * 0.5f + OuterBarrierRunoffMeters,
                StadiumArcSegmentsPerEnd);
            var innerBarrierCenterline = GenerateStadiumCenterline(
                StadiumHalfStraightMeters, StadiumRadiusMeters - TrackWidthMeters * 0.5f - InnerBarrierRunoffMeters,
                StadiumArcSegmentsPerEnd);

            // Pavement ribbon: 26 rotated slabs (2 straights + 24 arc
            // chords), each arc chord stretched 1.35x its exact length so
            // neighbors overlap and cover the small wedge a straight-chord
            // approximation of a circle always leaves at a kink — no joint
            // squares, so no blocky silhouette. Validated gap-free in a
            // standalone Python model (dense sampling of the TRUE circular
            // arc, multiple lateral offsets) before this was written; see
            // docs/30-founder-playtest-log.md round 21.
            CreateRibbon("Pavement", pavementCenterline, TrackWidthMeters, 0.12f, asphaltMat, StadiumArcSegmentsPerEnd);

            // --- CURBS (zebras) ---
            // Non-solid for the same reason as round 18 (raised geometry a
            // wheel-less rigid-BoxCollider kart can't climb). The old track
            // had 4 discrete corners to mark; this one is a continuous
            // curve, so 2 apex markers (widest point of each end) replace
            // the old 4-corner set — an honest simplification for a shape
            // that no longer has discrete corners to mark.
            CreateZebraCurb("Curb_East_Apex", new Vector3(StadiumHalfStraightMeters + StadiumRadiusMeters - TrackWidthMeters * 0.5f + 1f, 0.13f, 0f), new Vector3(4f, 0.08f, 4f));
            CreateZebraCurb("Curb_West_Apex", new Vector3(-(StadiumHalfStraightMeters + StadiumRadiusMeters - TrackWidthMeters * 0.5f + 1f), 0.13f, 0f), new Vector3(4f, 0.08f, 4f));

            // --- BARRIERS (outer wall, full curved ribbon) ---
            // Same ~3m runoff margin established in round 8/18, now applied
            // uniformly around the whole curve (not just the straights) —
            // OuterBarrierRunoffMeters above sets outerBarrierCenterline's
            // radius to StadiumRadiusMeters + half-width + 3m. Fence panels
            // and tire-stack decoration (both round 12/19 founder requests)
            // now tile along every segment of the ring, curved sections
            // included — previously only the 2 straight walls got them.
            CreateWallRibbon("Barrier_Outer", outerBarrierCenterline, thicknessMeters: 0.5f, heightMeters: 1f,
                arcSegmentsPerEnd: StadiumArcSegmentsPerEnd, solidCollider: true, useFenceVisual: true, useTireVisual: true);

            // Inner barriers (infield hole). Round 8: an early, bigger ring
            // clipped the pavement so it was made non-solid. Round 17:
            // re-solidified, shrunk to sit safely inside the true infield
            // hole. Round 20: the infield hole is now a true oval (offset
            // inward by half-width + 1.5m runoff from the pavement
            // centerline), so the ring is just the same
            // GenerateStadiumCenterline formula at a smaller radius —
            // InnerBarrierRunoffMeters above keeps it a validated 9m from
            // the stadium center, comfortably clear of the 10.5m pavement
            // inner edge. None of the bot waypoints sit anywhere near it.
            CreateWallRibbon("Barrier_Inner", innerBarrierCenterline, thicknessMeters: 0.4f, heightMeters: 0.8f,
                arcSegmentsPerEnd: StadiumArcSegmentsPerEnd, solidCollider: true);

            // --- START/FINISH LINE ---
            // Non-solid for the same reason as the curbs above: it sits proud of
            // the asphalt surface and was blocking the kart right after spawn.
            // Round 25: 23m from the west end (WestEndX), same runway
            // distance from the grid as before the track got longer — see
            // WestEndX's comment above.
            CreateTrackPiece("StartFinish_Line", new Vector3(WestEndX + 23f, 0.13f, -14f), new Vector3(0.3f, 0.02f, 7f), whiteMat, solidCollider: false);

            // --- GRASS SURFACE TRIGGER (infield) ---
            // A single axis-aligned rectangle safely inscribed inside the
            // oval infield hole (inner barrier centerline radius 9m at
            // x=±StadiumHalfStraightMeters, wider between) rather than
            // trying to exactly hug the oval shape — same "surfaces as
            // grass, not a fall-through void" reasoning as the pavement's
            // own tolerated edge gaps (see the round-18 log entry); the
            // giant grass ground collision plane created above already
            // covers every inch of the play area regardless. Round 25:
            // half-extent keeps the same 8m margin from the straight end
            // as before (was 30 at StadiumHalfStraightMeters=38).
            CreateSurface("Grass_Inner", new Vector3(0f, 0.5f, 0f), new Vector3((StadiumHalfStraightMeters - 8f) * 2f, 2f, 14f), 0.5f, 0f, true);

            // --- CHECKPOINTS (triggers spanning track width) ---
            // Checkpoints stay axis-aligned boxes (TrackConfigurationSO's
            // checkpoint data has no rotation field, and CheckpointTrigger/
            // KartCheckpointDetector don't need one either — see
            // docs/30-founder-playtest-log.md round 20). That's not a
            // limitation here: on a stadium shape, travel direction is
            // exactly world +X on both straights and exactly world ±Z at
            // both arc tips (EastArcTipX,0)/(WestArcTipX,0) — every
            // checkpoint below sits at one of those 4 naturally
            // axis-aligned points, so no rotated checkpoint support was
            // needed.
            CreateCheckpoint("StartFinish", new Vector3(WestEndX + 23f, 1f, -14f), new Vector3(0.5f, 3f, 8f), 0, true);
            // Checkpoint 1: end of main straight, before the east curve (23m before the east end, mirrors StartFinish's offset)
            CreateCheckpoint("CP1", new Vector3(EastEndX - 23f, 1f, -14f), new Vector3(0.5f, 3f, 8f), 0, false);
            // Checkpoint 2: back straight (dead center — unaffected by straight length)
            CreateCheckpoint("CP2", new Vector3(0f, 1f, 14f), new Vector3(0.5f, 3f, 8f), 1, false);
            // Checkpoint 3: apex of the west curve (exact arc tip, travel is pure -Z there)
            CreateCheckpoint("CP3", new Vector3(WestArcTipX, 1f, 0f), new Vector3(8f, 3f, 0.5f), 2, false);

            // --- MARSHAL POSTS (placeholder) ---
            // Placed just outside the outer barrier ring (radius
            // StadiumRadiusMeters + half-width + 3m on the curves, same
            // z=±20.5 on the straights regardless of straight length).
            // Round 25: S/N posts are spaced along the straight at the
            // same fraction of its length as before (20/38 ≈ 53%), not a
            // fixed distance from either end — they're spectator viewing
            // points, not track-geometry markers, so scaling with the
            // straight reads better than staying pinned near the old
            // (now much shorter, relatively) straight's midpoint. E/W
            // posts stay tip-anchored (same fixed 8.5m outside the arc
            // tip as before).
            var marshalFlagMaterial = CreateMaterial("MarshalFlag", new Color(0.15f, 0.55f, 0.2f));
            const float MarshalStraightOffsetX = StadiumHalfStraightMeters * 20f / 38f;
            CreateMarshalFlag("MarshalPost_S1", new Vector3(-MarshalStraightOffsetX, 0f, -22.5f), marshalFlagMaterial);
            CreateMarshalFlag("MarshalPost_S2", new Vector3(MarshalStraightOffsetX, 0f, -22.5f), marshalFlagMaterial);
            CreateMarshalFlag("MarshalPost_N1", new Vector3(-MarshalStraightOffsetX, 0f, 22.5f), marshalFlagMaterial);
            CreateMarshalFlag("MarshalPost_N2", new Vector3(MarshalStraightOffsetX, 0f, 22.5f), marshalFlagMaterial);
            CreateMarshalFlag("MarshalPost_E1", new Vector3(EastArcTipX + 8.5f, 0f, -8f), marshalFlagMaterial);
            CreateMarshalFlag("MarshalPost_E2", new Vector3(EastArcTipX + 8.5f, 0f, 8f), marshalFlagMaterial);
            CreateMarshalFlag("MarshalPost_W1", new Vector3(WestArcTipX - 8.5f, 0f, -8f), marshalFlagMaterial);
            CreateMarshalFlag("MarshalPost_W2", new Vector3(WestArcTipX - 8.5f, 0f, 8f), marshalFlagMaterial);

            // --- CHECKERED FLAG (decoration, start/finish) ---
            CreateCheckeredFlag(new Vector3(WestEndX + 23f, 0f, -18f));

            // --- PIT BUILDING (placeholder for "pit entry/exit") ---
            // Behind the outer barrier (outer wall at z=-20.5 on the south
            // straight), same non-colliding placeholder as round 13/18 —
            // no pit-lane gameplay yet, matching the task's own
            // "(placeholder)" wording. Position is x=0 (dead center),
            // unaffected by straight length.
            CreatePitBuilding(new Vector3(0f, 0f, -26f));
            var pitLineMat = CreateMaterial("PitLine", new Color(0.95f, 0.55f, 0.05f));
            // Round 25: pit entry/exit lines keep the same offsets from
            // the StartFinish line as before (+21m / -10m) rather than
            // staying at their old raw coordinates, so they still visually
            // bracket the start/finish/pit cluster instead of drifting
            // away from it as the straight gets longer.
            CreateTrackPiece("PitEntry_Line", new Vector3(WestEndX + 23f + 21f, 0.13f, -14f), new Vector3(0.3f, 0.02f, 7f), pitLineMat, solidCollider: false);
            CreateTrackPiece("PitExit_Line", new Vector3(WestEndX + 23f - 10f, 0.13f, -14f), new Vector3(0.3f, 0.02f, 7f), pitLineMat, solidCollider: false);
        }

        /// <summary>
        /// Subdivides a corner curb's footprint into an NxN checkerboard of
        /// red/white segments — an actual zebra pattern instead of a flat
        /// color block. Same total footprint and non-solid trigger collider
        /// as before, so this is visual-only and doesn't change driving.
        /// </summary>
        private static void CreateZebraCurb(string cornerName, Vector3 center, Vector3 footprint, int segmentsPerSide = 3)
        {
            var segX = footprint.x / segmentsPerSide;
            var segZ = footprint.z / segmentsPerSide;
            var matRed = CreateMaterial($"{cornerName}_Red", new Color(0.85f, 0.12f, 0.12f));
            var matWhite = CreateMaterial($"{cornerName}_White", new Color(0.92f, 0.92f, 0.92f));
            var x0 = center.x - footprint.x * 0.5f + segX * 0.5f;
            var z0 = center.z - footprint.z * 0.5f + segZ * 0.5f;

            for (var ix = 0; ix < segmentsPerSide; ix++)
            {
                for (var iz = 0; iz < segmentsPerSide; iz++)
                {
                    var isRed = (ix + iz) % 2 == 0;
                    var pos = new Vector3(x0 + ix * segX, center.y, z0 + iz * segZ);
                    CreateTrackPiece($"{cornerName}_{ix}_{iz}", pos, new Vector3(segX, footprint.y, segZ),
                        isRed ? matRed : matWhite, solidCollider: false);
                }
            }
        }

        /// <summary>
        /// Places one Kenney marshal-flag prop at ground level. Decoration
        /// only: collider stripped entirely (not even a trigger) since it
        /// sits outside the barrier ring and can never be reached.
        /// </summary>
        private static void CreateMarshalFlag(string name, Vector3 groundPosition, Material material)
        {
            var flagModel = Resources.Load<GameObject>(MarshalFlagModelResourcePath);
            if (flagModel == null)
            {
                return;
            }

            var flag = Instantiate(flagModel);
            flag.name = name;
            // Native pivot is already at ground level (measured Y range
            // 0..1.25m from the source .dae), so no vertical offset needed.
            flag.transform.position = groundPosition;
            flag.transform.localScale = Vector3.one * 1.2f;
            ApplyDecorationMaterial(flag, material);
        }

        /// <summary>
        /// Places the checkered flag prop near start/finish, textured
        /// (not flat-colored) so the checker pattern actually reads —
        /// see the texture overload on CreateMaterial.
        /// </summary>
        private static void CreateCheckeredFlag(Vector3 groundPosition)
        {
            var flagModel = Resources.Load<GameObject>(CheckeredFlagModelResourcePath);
            if (flagModel == null)
            {
                return;
            }

            var flag = Instantiate(flagModel);
            flag.name = "CheckeredFlag_StartFinish";
            flag.transform.position = groundPosition;
            flag.transform.localScale = Vector3.one * 1.2f;
            var texture = Resources.Load<Texture2D>(CheckeredFlagTextureResourcePath);
            var material = CreateMaterial("CheckeredFlag", Color.white, texture);
            ApplyDecorationMaterial(flag, material);
        }

        /// <summary>
        /// Places the pit-building placeholder. Purely visual — see the
        /// M3-T01 "pit entry/exit (placeholder)" comment above CreatePitBuilding's call site.
        /// </summary>
        private static void CreatePitBuilding(Vector3 groundPosition)
        {
            var buildingModel = Resources.Load<GameObject>(PitBuildingModelResourcePath);
            if (buildingModel == null)
            {
                return;
            }

            var building = Instantiate(buildingModel);
            building.name = "PitBuilding_Placeholder";
            building.transform.position = groundPosition;
            // Native footprint measured from the .dae is ~1m x 1m x 0.7m
            // tall (a modular tile meant to be combined) — scaled up to
            // read as a small standalone building.
            building.transform.localScale = new Vector3(4f, 4f, 4f);
            var material = CreateMaterial("PitBuilding", new Color(0.55f, 0.58f, 0.62f));
            ApplyDecorationMaterial(building, material);
        }

        /// <summary>
        /// Shared cleanup for imported Kenney decoration props: strip every
        /// collider (these are placed where they can never be reached, so
        /// even a trigger is unnecessary) and force every renderer onto a
        /// known-good URP material instead of the .dae's own COLLADA
        /// material (the missing-shader/magenta failure documented on
        /// CreateFenceVisual above).
        /// </summary>
        private static void ApplyDecorationMaterial(GameObject instance, Material material)
        {
            foreach (var col in instance.GetComponentsInChildren<Collider>())
            {
                Destroy(col);
            }

            foreach (var rend in instance.GetComponentsInChildren<Renderer>())
            {
                var slots = new Material[rend.sharedMaterials.Length];
                for (var s = 0; s < slots.Length; s++)
                {
                    slots[s] = material;
                }
                rend.sharedMaterials = slots;
            }

            instance.isStatic = true;
        }

        private static void CreateTrackPiece(string name, Vector3 position, Vector3 scale, Material material,
            bool solidCollider = true)
        {
            var piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piece.name = name;
            piece.transform.position = position;
            piece.transform.localScale = scale;
            piece.GetComponent<Renderer>().sharedMaterial = material;
            var collider = piece.GetComponent<Collider>();
            if (solidCollider)
            {
                collider.sharedMaterial = GetLowFrictionMaterial();
            }
            else
            {
                // Visual-only decoration (curbs, line markings): keep the
                // collider as a trigger so it never physically blocks the
                // kart, which has no wheel colliders to climb small ledges.
                collider.isTrigger = true;
            }
        }

        /// <summary>
        /// Round-20 founder feedback: "mudou, [mas] ficou um quadrado" — every
        /// piece-creation helper up to round 18 only ever built axis-aligned
        /// boxes. This is the rotation-aware counterpart of
        /// <see cref="CreateTrackPiece"/>: same cube primitive, but oriented
        /// with an explicit yaw and described by length/width instead of a
        /// raw Vector3 scale, using the convention that a piece's local +X
        /// axis (before rotation) is its "length"/travel direction — see
        /// <see cref="YawDegreesForDirection"/> for how a world direction
        /// maps to the yaw that achieves that. Deliberately a separate
        /// method rather than adding a rotation parameter to
        /// <see cref="CreateTrackPiece"/> itself: every existing call site
        /// (round 1-18) stays byte-for-byte untouched, zero regression risk
        /// on track geometry already confirmed working.
        /// </summary>
        private static void CreateTrackPieceOriented(string name, Vector3 position, float yawDegrees,
            float lengthMeters, float widthMeters, float heightMeters, Material material,
            bool solidCollider = true)
        {
            var piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piece.name = name;
            piece.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yawDegrees, 0f));
            piece.transform.localScale = new Vector3(lengthMeters, heightMeters, widthMeters);
            piece.GetComponent<Renderer>().sharedMaterial = material;
            var collider = piece.GetComponent<Collider>();
            if (solidCollider)
            {
                collider.sharedMaterial = GetLowFrictionMaterial();
            }
            else
            {
                collider.isTrigger = true;
            }
        }

        /// <summary>
        /// Yaw (degrees, for <c>Quaternion.Euler(0, yaw, 0)</c>) that rotates
        /// a piece's local +X axis to point along the given unit world
        /// direction (dx, dz). Derived from Unity's own Y-axis Euler
        /// rotation (local (1,0,0) maps to world (cos th, -sin th) in
        /// (x,z)): solving cos th = dx, -sin th = dz gives
        /// th = atan2(-dz, dx). Verified against the existing convention
        /// elsewhere in this file — a world direction of (1,0) (facing
        /// +X, "east") already correctly resolves to th=0, matching every
        /// pre-round-20 axis-aligned piece (identity rotation, no yaw
        /// field at all), and (0,1) resolves to th=-90, NOT the "yaw=90
        /// faces +X" grid-slot convention used elsewhere for KARTS — karts
        /// and track pieces use their own independent yaw conventions, this
        /// helper is only for pieces built via
        /// <see cref="CreateTrackPieceOriented"/>/<see cref="CreateWallOriented"/>.
        /// </summary>
        private static float YawDegreesForDirection(float dx, float dz)
        {
            return Mathf.Atan2(-dz, dx) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// Generates a closed "stadium" (discorectangle) centerline: two
        /// straights at z=∓radiusMeters spanning x∈[-halfStraightMeters,
        /// halfStraightMeters], joined by two true semicircle end-caps of
        /// the given radius. IMPORTANT: the radius is NOT independent of
        /// the straight geometry — for the two straights (always parallel,
        /// both horizontal) to be tangent to both end-caps and the loop to
        /// close exactly, the semicircle radius must equal the straights'
        /// half-separation, i.e. every point here besides the straight
        /// half-length is generated purely from <paramref name="radiusMeters"/>.
        /// Round-20 track redesign (see docs/30-founder-playtest-log.md):
        /// this generator is reused at three different radii — the
        /// pavement centerline, and the outer/inner barrier rings offset
        /// outward/inward from it by half the track width plus a runoff
        /// margin — so all three rings are guaranteed to stay perfectly
        /// concentric and parallel with zero extra bookkeeping.
        /// Returns arcSegmentsPerEnd*2 + 2 points, NOT closed (no duplicate
        /// final point) — callers that need the closing edge index with
        /// modulo (see <see cref="CreateRibbon"/>/<see cref="CreateWallRibbon"/>).
        /// </summary>
        private static List<Vector3> GenerateStadiumCenterline(float halfStraightMeters, float radiusMeters,
            int arcSegmentsPerEnd)
        {
            var points = new List<Vector3>
            {
                new Vector3(-halfStraightMeters, 0f, -radiusMeters),
                new Vector3(halfStraightMeters, 0f, -radiusMeters)
            };
            AppendArcPoints(points, new Vector3(halfStraightMeters, 0f, 0f), radiusMeters, -90f, 90f, arcSegmentsPerEnd);
            points.Add(new Vector3(-halfStraightMeters, 0f, radiusMeters));
            AppendArcPoints(points, new Vector3(-halfStraightMeters, 0f, 0f), radiusMeters, 90f, 270f, arcSegmentsPerEnd);
            // The last AppendArcPoints call's final sample lands back on
            // points[0] by construction (270° wraps to the same point as
            // -90°) — drop that duplicate so the array can be walked with
            // modulo indexing instead of carrying a redundant closing point.
            points.RemoveAt(points.Count - 1);
            return points;
        }

        private static void AppendArcPoints(List<Vector3> points, Vector3 center, float radius,
            float startDegrees, float endDegrees, int segments)
        {
            for (var i = 1; i <= segments; i++)
            {
                var t = startDegrees + (endDegrees - startDegrees) * i / segments;
                var rad = t * Mathf.Deg2Rad;
                points.Add(new Vector3(center.x + radius * Mathf.Cos(rad), 0f, center.z + radius * Mathf.Sin(rad)));
            }
        }

        /// <summary>
        /// Default stretch applied to each ARC chord segment's length in
        /// <see cref="CreateRibbon"/>/<see cref="CreateWallRibbon"/> so
        /// neighboring rotated pieces overlap instead of leaving a wedge
        /// gap at the kink between them. Straight segments are left at
        /// their exact length (no adjacent-piece angle change, so no gap to
        /// cover). Validated gap-free by brute-force sampling of the TRUE
        /// circular arc (not just the polygon approximation) across
        /// overlap factors 1.30-1.50 and arc-segment counts 8/12/16 in a
        /// standalone Python model before this was written; 1.35 was
        /// picked as a comfortable middle of that validated range. See
        /// docs/30-founder-playtest-log.md round 21.
        /// </summary>
        private const float ArcSegmentOverlapFactor = 1.35f;

        /// <summary>
        /// Given an edge index into a centerline produced by
        /// <see cref="GenerateStadiumCenterline"/> (called with the same
        /// arcSegmentsPerEnd), returns true if that edge is one of the two
        /// straights rather than an arc chord. By construction (see
        /// GenerateStadiumCenterline) edge 0 is always the south straight
        /// and edge (arcSegmentsPerEnd + 1) is always the north straight —
        /// every other edge index is an arc chord on one of the two
        /// semicircle ends.
        /// </summary>
        private static bool IsStadiumStraightEdge(int edgeIndex, int arcSegmentsPerEnd)
        {
            return edgeIndex == 0 || edgeIndex == arcSegmentsPerEnd + 1;
        }

        /// <summary>
        /// Builds a closed loop of pavement from a centerline (see
        /// <see cref="GenerateStadiumCenterline"/>): one rotated slab per
        /// edge (<see cref="CreateTrackPieceOriented"/>). Round 20 covered
        /// the kink gap at every vertex with a separate unrotated joint
        /// square, which founder playtesting flagged as a visibly jagged
        /// "cog wheel" silhouette on the curves. Round 21 replaces that
        /// with an overlap-extension technique instead: every ARC chord
        /// (straights excluded — see <see cref="IsStadiumStraightEdge"/>)
        /// is stretched <see cref="ArcSegmentOverlapFactor"/>x its exact
        /// length around its own midpoint/yaw, so adjacent rotated strips
        /// naturally overlap and cover the kink with no separate joint
        /// piece at all — a continuous curved ribbon instead of chord +
        /// square + chord. Validated gap-free in a standalone Python model
        /// before this was written; see docs/30-founder-playtest-log.md
        /// round 21.
        /// </summary>
        private static void CreateRibbon(string prefix, List<Vector3> centerline, float widthMeters,
            float heightMeters, Material material, int arcSegmentsPerEnd)
        {
            var count = centerline.Count;
            for (var i = 0; i < count; i++)
            {
                var p0 = centerline[i];
                var p1 = centerline[(i + 1) % count];
                var delta = p1 - p0;
                var length = delta.magnitude;
                var mid = (p0 + p1) * 0.5f;
                var yaw = YawDegreesForDirection(delta.x / length, delta.z / length);
                var pieceLength = IsStadiumStraightEdge(i, arcSegmentsPerEnd)
                    ? length
                    : length * ArcSegmentOverlapFactor;
                CreateTrackPieceOriented($"{prefix}_Seg_{i:00}", mid, yaw, pieceLength, widthMeters, heightMeters, material);
            }
        }

        /// <summary>
        /// Wall-ring counterpart of <see cref="CreateRibbon"/>: same
        /// centerline-to-overlapping-segments approach (see that method's
        /// comment for the round-21 "no joint squares" rationale), but each
        /// segment is a <see cref="CreateWallOriented"/> (with the
        /// fence/tire visual options round 12/19 already established for
        /// straight walls, now available on curved ones too).
        /// </summary>
        private static void CreateWallRibbon(string prefix, List<Vector3> centerline, float thicknessMeters,
            float heightMeters, int arcSegmentsPerEnd, bool solidCollider = true,
            bool useFenceVisual = false, bool useTireVisual = false)
        {
            var count = centerline.Count;
            var verticalOffset = Vector3.up * (heightMeters * 0.5f);
            for (var i = 0; i < count; i++)
            {
                var p0 = centerline[i];
                var p1 = centerline[(i + 1) % count];
                var delta = p1 - p0;
                var length = delta.magnitude;
                var mid = (p0 + p1) * 0.5f + verticalOffset;
                var yaw = YawDegreesForDirection(delta.x / length, delta.z / length);
                var pieceLength = IsStadiumStraightEdge(i, arcSegmentsPerEnd)
                    ? length
                    : length * ArcSegmentOverlapFactor;
                CreateWallOriented($"{prefix}_Seg_{i:00}", mid, yaw, pieceLength, thicknessMeters, heightMeters,
                    solidCollider, useFenceVisual, useTireVisual);
            }
        }

        private const string BaseMaterialResourcePath = "KartPhysics/BaseURPLit";
        private static Material _baseMaterial;

        private static Material CreateMaterial(string name, Color color, Texture2D texture = null)
        {
            if (_baseMaterial == null)
            {
                // Load an explicit Material asset from Resources. This is the
                // reliable option for IL2CPP/Android: the shader it references
                // ships because the asset itself is a real project asset (not
                // just a runtime-only reference), so the build's shader
                // variant stripping keeps the variants it actually needs.
                // Grabbing sharedMaterial off a runtime-created primitive was
                // tried first but still produced the missing-shader magenta
                // fallback in device builds, so don't fall back to that path.
                _baseMaterial = Resources.Load<Material>(BaseMaterialResourcePath);

                if (_baseMaterial == null)
                {
                    Debug.LogError($"KartPhysicsPrototypeBootstrap: could not load '{BaseMaterialResourcePath}' " +
                        "from Resources. Materials will render with the engine's missing-shader fallback (pink).");
                    var temp = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    _baseMaterial = temp.GetComponent<Renderer>().sharedMaterial;
                    DestroyImmediate(temp);
                }
            }
            var mat = new Material(_baseMaterial);
            mat.name = name;

            // IL2CPP Android builds: Material.color relies on the shader's
            // "main color" metadata being resolved at runtime, which is
            // unreliable in stripped builds and results in materials
            // rendering with the shader's default (hot pink) color.
            // URP/Lit exposes the color via the _BaseColor property, so set
            // it explicitly (falling back to _Color for non-URP shaders).
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", color);
            }
            else if (mat.HasProperty("_Color"))
            {
                mat.SetColor("_Color", color);
            }
            else
            {
                mat.color = color;
            }

            // Checkered flag (round 13): the pattern itself has to come
            // from a texture, not a flat color — but still goes through
            // this same known-good URP material rather than the .dae's own
            // COLLADA material, for the same missing-shader/magenta reason
            // documented above on the fence.
            if (texture != null)
            {
                if (mat.HasProperty("_BaseMap"))
                {
                    mat.SetTexture("_BaseMap", texture);
                }
                else if (mat.HasProperty("_MainTex"))
                {
                    mat.SetTexture("_MainTex", texture);
                }
            }

            return mat;
        }

        private static void CreateCheckpoint(string name, Vector3 position, Vector3 size,
            int index, bool isStartFinish)
        {
            var obj = new GameObject(name);
            obj.transform.position = position;
            var col = obj.AddComponent<BoxCollider>();
            col.size = size;
            col.isTrigger = true;
            var cp = obj.AddComponent<CheckpointTrigger>();
            // The clockwise prototype crosses start/finish toward +X.
            // Other checkpoints use ordering only and have no direction gate.
            cp.Configure(index, isStartFinish, isStartFinish ? Vector3.right : Vector3.zero);
        }

        private static void CreateSurface(string name, Vector3 position, Vector3 size,
            float gripMultiplier, float instability, bool isOffTrack)
        {
            var obj = new GameObject(name);
            obj.transform.position = position;
            var col = obj.AddComponent<BoxCollider>();
            col.size = size;
            col.isTrigger = true;
            // Founder playtest feedback, 2026-08-20 (round 8): "o ideal é...
            // apenas diminuir a velocidade quando dirigir nessa área" —
            // this used to stop right here: it created a throwaway
            // SurfaceDataSO instance and never attached a SurfaceTrigger to
            // report it, so grass had exactly zero gameplay effect (the
            // grip loss request from round 1, 2026-08-19, was never
            // actually wired despite the underlying grip-multiplier system
            // already existing). SurfaceDataSO.Configure/SurfaceTrigger.Configure
            // are new runtime setters added specifically so a procedurally
            // generated surface can be filled in without a designer having
            // to author a .asset by hand.
            var surfaceData = ScriptableObject.CreateInstance<SurfaceDataSO>();
            surfaceData.Configure(name, name, gripMultiplier, instability, isOffTrack);
            var trigger = obj.AddComponent<SurfaceTrigger>();
            trigger.Configure(surfaceData);
        }

        /// <summary>
        /// Founder playtest feedback, 2026-08-20 (round 16): player used to
        /// always spawn at grid slot 0. <paramref name="shuffledGridSlotIndices"/>[0]
        /// (built once per race in <see cref="BuildShuffledGridSlotIndices"/>)
        /// is now the player's actual starting slot — a real random draw,
        /// not always pole position.
        /// </summary>
        private static KartDynamics CreatePlayerKart(TrackConfigurationSO trackConfiguration, List<int> shuffledGridSlotIndices,
            int playerRaceNumber)
        {
            var gridSlots = trackConfiguration != null ? trackConfiguration.GridSlots : null;
            Vector3 position;
            float yaw;
            if (gridSlots != null && gridSlots.Count > 0 && shuffledGridSlotIndices != null && shuffledGridSlotIndices.Count > 0)
            {
                var slot = gridSlots[shuffledGridSlotIndices[0]];
                position = slot.WorldPosition;
                yaw = slot.YawDegrees;
            }
            else
            {
                // Degraded fallback: same hardcoded spawn used before the
                // grid existed, so a missing/invalid TrackConfigurationSO
                // still produces a playable kart. Updated for the round-20
                // stadium track's new main straight (z=-14, was -22), and
                // again for the round-25 track expansion — mirrors grid
                // slot 1's position exactly (see
                // OvalMvpTrackConfiguration.asset).
                position = new Vector3(-41f, 0f, -14f);
                yaw = 90f;
            }
            position.y = 0.55f;

            var dynamics = CreateKartInstance("Prototype Kart", position, yaw,
                new Color(0.15f, 0.35f, 0.85f), playerRaceNumber);
            dynamics.gameObject.AddComponent<KartPrototypeInput>();
            dynamics.gameObject.AddComponent<KartAudioBridge>();
            return dynamics;
        }

        /// <summary>Fisher-Yates shuffle of the track's grid slot indices (0..GridSlots.Count-1) — one random starting order per race, shared by the player and every bot.</summary>
        private static List<int> BuildShuffledGridSlotIndices(TrackConfigurationSO trackConfiguration)
        {
            var count = trackConfiguration != null ? trackConfiguration.GridSlots.Count : 0;
            var indices = new List<int>(count);
            for (var i = 0; i < count; i++)
            {
                indices.Add(i);
            }

            for (var i = indices.Count - 1; i > 0; i--)
            {
                var j = UnityEngine.Random.Range(0, i + 1);
                (indices[i], indices[j]) = (indices[j], indices[i]);
            }

            return indices;
        }

        /// <summary>Fisher-Yates shuffle of 1..MaxRaceNumber — one random race-number order per race, shared by the player ([0]) and every bot ([1+i]).</summary>
        private static List<int> BuildShuffledRaceNumbers()
        {
            var numbers = new List<int>(MaxRaceNumber);
            for (var i = 1; i <= MaxRaceNumber; i++)
            {
                numbers.Add(i);
            }

            for (var i = numbers.Count - 1; i > 0; i--)
            {
                var j = UnityEngine.Random.Range(0, i + 1);
                (numbers[i], numbers[j]) = (numbers[j], numbers[i]);
            }

            return numbers;
        }

        // Founder request, 2026-08-24: "o fantasma" — pale ice-blue/white,
        // deliberately unlike the player's saturated blue (0.15, 0.35,
        // 0.85) and every color in BotTintPalette below, so the ghost kart
        // reads as "not a real rival" at a glance.
        private static readonly Color GhostTintColor = new Color(0.85f, 0.95f, 1f);

        private static readonly Color[] BotTintPalette =
        {
            new Color(0.85f, 0.4f, 0.05f),
            new Color(0.75f, 0.15f, 0.75f),
            new Color(0.15f, 0.75f, 0.65f),
            new Color(0.85f, 0.8f, 0.1f),
            new Color(0.5f, 0.2f, 0.85f),
            new Color(0.85f, 0.15f, 0.35f),
            new Color(0.3f, 0.6f, 0.15f),
            new Color(0.9f, 0.55f, 0.75f),
            new Color(0.35f, 0.35f, 0.85f),
        };

        // Founder playtest feedback, 2026-08-20: gave a list of names for the
        // bots instead of the generic "Bot Kart N" placeholder.
        private static readonly string[] BotNamePalette =
        {
            "Jonathan David",
            "Jean Zanotti",
            "Pety",
            "Fábio Mendes",
            "Baiacu",
            "Thiago",
            "César",
            "Babidi",
            "Marcus Oliveira",
            "Abner",
            "Caio",
            "Gabriel",
        };

        /// <summary>
        /// Founder playtest feedback, 2026-08-19: "poderia ter pelo menos 1
        /// bot competindo" and later "até 10 bots e o nível deles". Spawns
        /// <paramref name="count"/> bots on the track's remaining grid slots
        /// (<paramref name="shuffledGridSlotIndices"/>[0] went to the
        /// player in <see cref="CreatePlayerKart"/>, so bots use indices
        /// 1..MaxBotCount of that same shuffled order — see the round-16
        /// comment on <see cref="_shuffledGridSlotIndices"/>) and drives
        /// each one along <see cref="TrackConfigurationSO.BotPathPoints"/>
        /// via <see cref="KartBotController"/> at the requested
        /// <see cref="BotDifficulty"/>. Falls back to a single hardcoded
        /// spawn position if the track configuration failed to load, so a
        /// missing/invalid asset degrades gracefully instead of spawning no
        /// bots at all.
        /// </summary>
        // Note (2026-08-24): a "ghost rival bot" (bot 0 on Hard following
        // the player's recorded ghost path instead of the static one) was
        // tried here and shipped for one round, at the founder's explicit
        // request. He reported it back as "ficou perdidao" (got badly
        // lost/confused) and said it's probably not needed — removed. If
        // revisited later, it should probably reuse whatever the real
        // fix for Hard-bot competitiveness ends up being (see M4-T08 in
        // tasks.md) rather than repurposing GhostController's raw samples,
        // which were never meant to double as bot waypoints.
        private static List<KartDynamics> SpawnBots(
            TrackConfigurationSO trackConfiguration, int count, BotDifficulty difficulty,
            List<int> shuffledGridSlotIndices, List<int> shuffledRaceNumbers)
        {
            var bots = new List<KartDynamics>();
            var path = trackConfiguration != null
                ? trackConfiguration.BotPathPoints
                : Array.Empty<Vector3>();
            var gridSlots = trackConfiguration != null ? trackConfiguration.GridSlots : null;
            var clampedCount = Mathf.Clamp(count, 0, RaceSetupMenu.MaxBotCount);

            // Founder playtest feedback, 2026-08-20 (round 9): "os nomes
            // poderia pegar aleatório... quando colocamos 1 carrinho ele já
            // seleciona o Jonathan como se tivesse em sequência" —
            // `BotNamePalette[i % Length]` always started from index 0, so
            // a single bot was always named after whoever is first in the
            // array. Shuffle a copy once per race instead so a 1-bot race
            // gets a random name and, up to the palette's size, no name
            // repeats within the same race either.
            var shuffledNames = ShuffleNames(BotNamePalette);

            for (var i = 0; i < clampedCount; i++)
            {
                Vector3 position;
                float yaw;
                // Grid slot shuffledGridSlotIndices[0] went to the player,
                // so bots use shuffledGridSlotIndices[1..MaxBotCount].
                if (gridSlots != null && shuffledGridSlotIndices != null &&
                    i + 1 < shuffledGridSlotIndices.Count && i + 1 < gridSlots.Count)
                {
                    var slot = gridSlots[shuffledGridSlotIndices[i + 1]];
                    position = slot.WorldPosition;
                    yaw = slot.YawDegrees;
                }
                else
                {
                    // Degraded fallback: line the bots up behind the player's
                    // hardcoded spawn point instead of stacking them on top
                    // of each other. Updated for the round-20 stadium
                    // track's new main straight (was z=-22.8/-21.2), and
                    // again for the round-25 track expansion (was -21f,
                    // matching the old grid slot 2 — see the player
                    // fallback's comment above for the same pattern).
                    position = new Vector3(-43f - i * 2f, 0.55f, -14.8f - (i % 2) * 1.6f);
                    yaw = 90f;
                }
                position.y = 0.55f;

                var tint = BotTintPalette[i % BotTintPalette.Length];
                var botName = shuffledNames[i % shuffledNames.Count];
                // Round 27: the kart's own number-plate texture (see
                // CreateKartVisual) needs a race number before
                // CreateKartInstance runs, not after like
                // KartBotController.SetRaceNumber below — computed here
                // with the same "list missing/short" fallback
                // KartBotController.RaceNumber already implicitly has (0),
                // replaced with i+1 for the VISUAL number only so the
                // plate never shows blank; SetRaceNumber's own existing
                // condition below is untouched.
                var botRaceNumber = shuffledRaceNumbers != null && i + 1 < shuffledRaceNumbers.Count
                    ? shuffledRaceNumbers[i + 1]
                    : i + 1;
                var dynamics = CreateKartInstance(botName, position, yaw, tint, botRaceNumber);
                var bot = dynamics.gameObject.AddComponent<KartBotController>();
                bot.Configure(dynamics, path, difficulty);
                // Race number pool: shuffledRaceNumbers[0] went to the
                // player (see _playerRaceNumber in Awake), so bots use
                // shuffledRaceNumbers[1..MaxBotCount] — same pattern as the
                // grid-slot shuffle above.
                if (shuffledRaceNumbers != null && i + 1 < shuffledRaceNumbers.Count)
                {
                    bot.SetRaceNumber(shuffledRaceNumbers[i + 1]);
                }
                dynamics.gameObject.AddComponent<KartAudioBridge>();
                bots.Add(dynamics);
            }

            return bots;
        }

        /// <summary>Fisher-Yates shuffle of a copy of <paramref name="names"/> — never mutates the shared palette.</summary>
        private static List<string> ShuffleNames(IReadOnlyList<string> names)
        {
            var shuffled = new List<string>(names);
            for (var i = shuffled.Count - 1; i > 0; i--)
            {
                var j = UnityEngine.Random.Range(0, i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }
            return shuffled;
        }

        private static KartDynamics CreateKartInstance(string name, Vector3 position, float yawDegrees, Color tintColor,
            int raceNumber, string kartModelResourcePath = KartVisualResourcePath,
            string tuningResourcePath = TuningResourcePath)
        {
            var root = new GameObject(name);
            root.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yawDegrees, 0f));
            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(1.0f, 0.5f, 1.8f);
            collider.center = new Vector3(0f, 0.25f, 0f);
            collider.sharedMaterial = GetKartCollisionMaterial();
            var body = root.AddComponent<Rigidbody>();
            body.linearDamping = 0.02f;
            body.angularDamping = 0.6f;

            var visual = CreateKartVisual(root.transform, tintColor, raceNumber, kartModelResourcePath);

            var dynamics = root.AddComponent<KartDynamics>();
            var tuning = Resources.Load<KartCategorySO>(tuningResourcePath);
            dynamics.Configure(tuning, visual);

            // Round 27: wires the front-wheel/steering-wheel visual
            // rotation (see CreateKartVisual) to this kart's own physics
            // input + tuning, once both actually exist — KartSteeringVisual
            // is added to the visual inside CreateKartVisual, which runs
            // before KartDynamics exists yet.
            var steeringVisual = visual.GetComponent<KartSteeringVisual>();
            if (steeringVisual != null)
            {
                steeringVisual.Configure(dynamics, tuning);
            }

            // Round 28: same wiring, for the animated brake/throttle
            // pedals (see CreateKartVisual/KartPedalVisual).
            var pedalVisual = visual.GetComponent<KartPedalVisual>();
            if (pedalVisual != null)
            {
                pedalVisual.Configure(dynamics);
            }

            // Round 32: same wiring, for the wheel roll (forward-spin)
            // animation (see CreateKartVisual/KartWheelSpinVisual).
            var spinVisual = visual.GetComponent<KartWheelSpinVisual>();
            if (spinVisual != null)
            {
                spinVisual.Configure(dynamics);
            }

            return dynamics;
        }

        /// <summary>
        /// Round 32 (2026-08-24) founder request: a second, faster kart
        /// ("18 HP / 80 km/h") to compare against the existing one
        /// ("13 HP / 60 km/h" after this same round's retune — see
        /// docs/30-founder-playtest-log.md rodada 32). There is no proper
        /// pre-race category-selection SCREEN yet (a bigger, separate
        /// task) — see KartCategoryToggleButton, which calls this to tear
        /// down and rebuild an already-spawned kart's visual + tuning in
        /// place. Only ever called before the race actually starts (the
        /// kart is stationary at its grid slot then), so there is no live
        /// physics state to reconcile with a suddenly different tuning
        /// asset.
        /// </summary>
        internal static void RebuildKartVisual(KartDynamics dynamics, string kartModelResourcePath,
            string tuningResourcePath, Color tintColor, int raceNumber)
        {
            if (dynamics == null)
            {
                return;
            }

            var oldVisual = dynamics.VisualRoot;
            if (oldVisual != null)
            {
                Destroy(oldVisual.gameObject);
            }

            var newVisual = CreateKartVisual(dynamics.transform, tintColor, raceNumber, kartModelResourcePath);
            var tuning = Resources.Load<KartCategorySO>(tuningResourcePath);
            dynamics.Configure(tuning, newVisual);

            var newSteeringVisual = newVisual.GetComponent<KartSteeringVisual>();
            if (newSteeringVisual != null)
            {
                newSteeringVisual.Configure(dynamics, tuning);
            }

            var newPedalVisual = newVisual.GetComponent<KartPedalVisual>();
            if (newPedalVisual != null)
            {
                newPedalVisual.Configure(dynamics);
            }

            var newSpinVisual = newVisual.GetComponent<KartWheelSpinVisual>();
            if (newSpinVisual != null)
            {
                newSpinVisual.Configure(dynamics);
            }
        }

        // Round 26: the racing-kart OBJ's carbon floor/nose/side-pod panels
        // are the closest thing it has to "bodywork" (it's an open-wheel
        // kart with a tube chassis, not a car with painted body panels) —
        // Unity's OBJ importer names each imported Material after the
        // .mtl's own newmtl name (confirmed in racing-kart.mtl), so this is
        // matched by material name, not by a hardcoded slot index/renderer
        // name, so it's resilient to Unity's own import ordering.
        // Round 33 (2026-08-24) bug fix: kartv2 has no material named
        // "carbon" at all (it is a fully painted kart, not open-wheel
        // tube-frame like RacingKart), so every slot missed this check,
        // "appliedTeamColor" stayed false, and the code fell through to
        // its OTHER branch below — the one meant for models with no
        // paintable slot at all — which flat-tints EVERY material. That
        // is why kartv2 showed up solid blue in the founder's build
        // ("ficou todo azul feio... vc nao considerou os detalhes"): the
        // detailed 16-material paint job (chrome, tires, decals, seat...)
        // was correctly imported, then entirely overwritten by this bug.
        // Fix: try each kart model's own paintable-panel material name in
        // turn instead of a single hardcoded one. "body_primary" is
        // kartv2's own main paint-panel material (confirmed against its
        // .mtl); everything else on that model (chrome, rim_alloy,
        // decal_ink, seat_shell, tires, etc.) is left untouched, exactly
        // like "carbon" already does for RacingKart.
        private static readonly string[] TeamColorMaterialNameHints = { "carbon", "body_primary" };
        private const string NumberPlateMaterialNameHint = "number_plate";

        // Round 27 (2026-08-24) founder request: "Aplicar o novo design
        // enviado do volante e dos pedais... no cockpit 3D" — the founder
        // separately modeled a steering wheel and a brake/throttle pedal
        // box (via Claude in Cowork's 3D tool) and asked them applied
        // inside the kart's cockpit, not just the mobile UI buttons (that
        // half is handled by KartPrototypeInput's baked icon textures).
        // Imported the same way as RacingKart.obj (OBJ+MTL, no extra
        // package). See ReplaceCockpitProp below for how these get placed.
        private const string SteeringWheelResourcePath = "KartPhysics/Models/SteeringWheel";
        private const string PedalBoxResourcePath = "KartPhysics/Models/PedalBox";
        // Round 28 (2026-08-24): founder sent a new, more detailed pedal
        // model with separately hinged brake/throttle pedals ("pedais
        // animados de aceleração e freio") to replace the round-27
        // PedalBox — see ReplaceCockpitProp's sizeMultiplier param and
        // CreateHingePivot below for how the two pedals get sized up and
        // rigged to rotate on their own hinge pins.
        private const string PedalsResourcePath = "KartPhysics/Models/Pedals";

        /// <summary>
        /// Founder playtest feedback, 2026-08-19: "o carrinho tbm poderia já
        /// ser um kart" — swaps the placeholder box for a real kart mesh
        /// (Assets/RKW/Physics/Resources/KartPhysics/Models/RacingKart.obj,
        /// see KartVisualResourcePath's doc for provenance). Visual only:
        /// every collider that comes with the imported model is stripped
        /// immediately (this replaces the earlier "Kenney FBX causes
        /// MeshCollider stripping issues" TODO — the fix is to never attach
        /// a MeshCollider to it at all, since the root BoxCollider already
        /// handles all physics). Falls back to the original colored
        /// primitive if the model asset is missing, so a bad Resources path
        /// degrades gracefully instead of leaving the kart invisible.
        ///
        /// Round 26 performance note: this model is ~37K triangles across
        /// 149 parts/11 materials — multiplied across up to 10 simultaneous
        /// karts (player + 9 bots), that is well past the ~100K-triangle
        /// budget M3-T01 documents for the ENTIRE track on low-tier mobile.
        /// Founder explicitly chose to use it for every kart anyway,
        /// accepting that cost for now — see
        /// docs/30-founder-playtest-log.md rodada 26. A follow-up
        /// optimization pass (decimation, an LOD, or a lower-poly variant
        /// for bots) is flagged there as a known pendência, not attempted
        /// here — no mesh-simplification tooling is available in this
        /// environment to do it safely.
        /// </summary>
        private static Transform CreateKartVisual(Transform parent, Color tintColor, int raceNumber,
            string kartModelResourcePath = KartVisualResourcePath)
        {
            var kartModel = Resources.Load<GameObject>(kartModelResourcePath);
            if (kartModel == null)
            {
                return CreatePrimitiveKartVisual(parent, tintColor);
            }

            var instance = Instantiate(kartModel, parent, false);
            instance.name = "Kart Visual";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;

            foreach (var col in instance.GetComponentsInChildren<Collider>())
            {
                DestroyImmediate(col);
            }

            var renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                Destroy(instance);
                return CreatePrimitiveKartVisual(parent, tintColor);
            }

            // Founder playtest feedback, 2026-08-19: "teve cores que não era
            // pra ter em cada camada" — the old Kenney FBX had multiple
            // material slots per renderer (body/glass/chrome/etc.) with no
            // single "this is the paintable part" material, so every slot
            // got overwritten with the flat tint (leaving nothing of the
            // model's own materials — that was the whole fix at the time).
            //
            // Round 26: the new racing-kart model is the opposite situation
            // — it has real per-part materials worth keeping (chromed
            // steel, rubber, brass, carbon, translucent fuel tank...) that
            // a flat full-model tint would erase entirely. So this now only
            // overwrites the slot(s) whose ORIGINAL material name contains
            // TeamColorMaterialNameHint ("carbon" — the closest thing this
            // open-wheel kart has to bodywork), and leaves every other slot
            // exactly as imported. If no such material is found on this
            // model at all (e.g. KartVisualResourcePath ever points back at
            // the old Kenney FBX, which has no "carbon" slot), falls back
            // to the original flat-tint-every-slot behavior so that model
            // still gets a visible team color instead of showing no tint.
            var material = CreateMaterial("KartVisual", tintColor);
            var appliedTeamColor = false;
            foreach (var renderer in renderers)
            {
                var original = renderer.sharedMaterials;
                var slotCount = Mathf.Max(1, original.Length);
                var slots = new Material[slotCount];
                for (var i = 0; i < slotCount; i++)
                {
                    var originalMaterial = i < original.Length ? original[i] : null;
                    if (originalMaterial != null && MatchesAnyHint(originalMaterial.name, TeamColorMaterialNameHints))
                    {
                        slots[i] = material;
                        appliedTeamColor = true;
                    }
                    else
                    {
                        slots[i] = originalMaterial != null ? originalMaterial : material;
                    }
                }
                renderer.sharedMaterials = slots;
            }

            if (!appliedTeamColor)
            {
                foreach (var renderer in renderers)
                {
                    var slotCount = Mathf.Max(1, renderer.sharedMaterials.Length);
                    var slots = new Material[slotCount];
                    for (var i = 0; i < slotCount; i++)
                    {
                        slots[i] = material;
                    }
                    renderer.sharedMaterials = slots;
                }
            }

            // Round 27 founder request: "Numeração dos Karts: Incluir
            // numeração visível nas carenagens de todos os karts". Same
            // slot-matching technique as the "carbon" team-color loop
            // above, targeting the model's own "number_plate" material
            // instead — see KartNumberTexture for how the digit texture
            // itself is generated. Every kart gets its OWN material
            // instance here (CreateMaterial always returns a new Material),
            // so each kart's plate shows its own number even though they
            // all originally shared one "number_plate" material asset.
            var numberTexture = KartNumberTexture.CreateRaceNumberTexture(raceNumber);
            var numberMaterial = CreateMaterial("KartNumberPlate", Color.white, numberTexture);
            foreach (var renderer in renderers)
            {
                var original = renderer.sharedMaterials;
                var slotCount = original.Length;
                if (slotCount == 0)
                {
                    continue;
                }

                var slots = new Material[slotCount];
                var changed = false;
                for (var i = 0; i < slotCount; i++)
                {
                    if (original[i] != null &&
                        original[i].name.IndexOf(NumberPlateMaterialNameHint, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        slots[i] = numberMaterial;
                        changed = true;
                    }
                    else
                    {
                        slots[i] = original[i];
                    }
                }

                if (changed)
                {
                    renderer.sharedMaterials = slots;
                }
            }

            FitVisualScale(instance.transform, renderers, KartVisualTargetLengthMeters);

            // Round 27: front-wheel steering-turn visual + cockpit prop
            // upgrade (founder request: "Esterçamento Ativo... Aplicar o
            // novo design... no cockpit 3D"). See CreateWheelSteeringPivot
            // and ReplaceCockpitProp below for how each piece is derived
            // from the model's own geometry instead of a guessed offset.
            var frontLeftPivot = CreateWheelSteeringPivot(instance.transform, "wheel_front_left_", "FrontLeftSteeringPivot");
            var frontRightPivot = CreateWheelSteeringPivot(instance.transform, "wheel_front_right_", "FrontRightSteeringPivot");
            // Round 28 founder feedback: "o volante ficou ok poderia ser
            // um pouco maior tbm" — modest 20% bump on top of the
            // bounds-matched size from round 27.
            // Round 32: kartv2 has extra baked-in steering-wheel parts
            // this project's old external SteeringWheel.obj prop replaces
            // (spokes + hub boss, not just the rim/plate the OLD kart
            // needed) — extended the match list so BOTH karts get the
            // original wheel geometry fully swapped out, not partially
            // (a partial match would leave leftover baked geometry
            // showing through/around the external prop).
            // Round 33: kartv2 also has a "steering_column" part (the rod
            // between wheel and rack) that round 32's match list missed —
            // it was staying visible as leftover static baked geometry
            // right where the external, ANIMATED SteeringWheel prop gets
            // placed, which could read as "the wheel doesn't move" even
            // though the actual prop was rotating underneath/behind it.
            var steeringWheelProp = ReplaceCockpitProp(instance.transform,
                new[] { "steering_wheel", "wheel_spoke_", "steering_spoke_", "steering_hub_boss", "steering_column" },
                SteeringWheelResourcePath, "SteeringWheel (round 27)", 1.2f);

            // Round 28: swapped the round-27 static PedalBox for the
            // founder's new Pedals model (has real brake_hinge_pin /
            // throttle_hinge_pin geometry to rig for animation) — see
            // CreateHingePivot and KartPedalVisual. "achei eles bem
            // pequenos" -> 40% size bump, larger than the wheel's since
            // this was the stronger complaint.
            var pedalsProp = ReplaceCockpitProp(instance.transform,
                new[] { "pedal_brake", "pedal_throttle", "pedal_arm_" }, PedalsResourcePath, "Pedals (round 28)", 1.4f);
            var brakePivot = CreateHingePivot(instance.transform, "brake_", "brake_hinge_pin", "BrakePedalPivot");
            var throttlePivot = CreateHingePivot(instance.transform, "throttle_", "throttle_hinge_pin", "ThrottlePedalPivot");

            if (frontLeftPivot != null || frontRightPivot != null || steeringWheelProp != null)
            {
                var steeringVisual = instance.AddComponent<KartSteeringVisual>();
                steeringVisual.SetPivots(frontLeftPivot, frontRightPivot, steeringWheelProp);
            }

            if (pedalsProp != null && (brakePivot != null || throttlePivot != null))
            {
                var pedalVisual = instance.AddComponent<KartPedalVisual>();
                pedalVisual.SetPivots(brakePivot, throttlePivot);
            }

            // Round 32 (2026-08-24) founder feedback: "as rodas giram e
            // viram... porem percebi que pela roda ser preta parece que
            // nao esta girando" — checking KartSteeringVisual found the
            // real cause: only the steering TURN existed, there was no
            // forward ROLLING rotation at all on either kart. Adds that
            // for all 4 wheels (front wheels nested inside their existing
            // steering pivot, so a turned wheel still rolls correctly on
            // its own now-turned axle; rear wheels get their own pivot
            // directly, no steering involved there) plus a small bright
            // mark on each wheel's rim so the spin actually reads as
            // motion instead of a still, all-black disc.
            var wheelSpinVisual = instance.AddComponent<KartWheelSpinVisual>();
            var addedAnyWheelSpin = false;
            addedAnyWheelSpin |= AddWheelSpin(wheelSpinVisual,
                frontLeftPivot != null ? frontLeftPivot : instance.transform,
                "wheel_front_left_", "FrontLeftWheelSpinPivot");
            addedAnyWheelSpin |= AddWheelSpin(wheelSpinVisual,
                frontRightPivot != null ? frontRightPivot : instance.transform,
                "wheel_front_right_", "FrontRightWheelSpinPivot");
            addedAnyWheelSpin |= AddWheelSpin(wheelSpinVisual, instance.transform,
                "wheel_rear_left_", "RearLeftWheelSpinPivot");
            addedAnyWheelSpin |= AddWheelSpin(wheelSpinVisual, instance.transform,
                "wheel_rear_right_", "RearRightWheelSpinPivot");

            if (!addedAnyWheelSpin)
            {
                Destroy(wheelSpinVisual);
            }

            return instance.transform;
        }

        /// <summary>
        /// Round 32 helper for CreateKartVisual: creates one wheel's ROLL
        /// pivot (see CreateWheelSpinPivot) and registers it with
        /// <paramref name="spinVisual"/>, plus a small bright mark on the
        /// wheel's rim so the rotation is actually visible (the tire
        /// material on both kart models is near-black rubber — see
        /// docs/30-founder-playtest-log.md rodada 32). Returns false (no
        /// throw) if this wheel's named parts aren't found, so a future
        /// model swap using different names degrades gracefully.
        /// </summary>
        private static bool AddWheelSpin(KartWheelSpinVisual spinVisual, Transform parentTransform,
            string namePrefix, string pivotName)
        {
            var spinPivot = CreateWheelSpinPivot(parentTransform, namePrefix, pivotName, out var radiusMeters);
            if (spinPivot == null)
            {
                return false;
            }

            spinVisual.AddWheel(spinPivot, radiusMeters);
            AddWheelSpinMark(spinPivot, radiusMeters);
            return true;
        }

        /// <summary>
        /// Round 27 founder request: "Esterçamento Ativo: Sincronizar a
        /// rotação do volante com a animação de rotação das rodas
        /// dianteiras". OBJ import has no parent-child hierarchy (every "o"
        /// object in RacingKart.obj — tire/sidewall/rim/hub/bolts — imports
        /// as a flat sibling), so there is no single existing transform to
        /// rotate for a steering-turn effect. This groups one wheel's parts
        /// (matched by name prefix, e.g. "wheel_front_left_") under a new
        /// empty pivot GameObject placed at their own combined bounds
        /// center — bounds-based, like FitVisualScale above, rather than a
        /// hand-typed coordinate, so it is derived from the actual model
        /// instead of guessed. Returns null (caller skips rotation wiring
        /// for that wheel) if no matching named parts are found — e.g. a
        /// future model swap using different part names degrades
        /// gracefully instead of crashing.
        /// </summary>
        /// <summary>Round 33: true if <paramref name="materialName"/> contains any of <paramref name="hints"/> (case-insensitive) — see TeamColorMaterialNameHints above.</summary>
        private static bool MatchesAnyHint(string materialName, string[] hints)
        {
            foreach (var hint in hints)
            {
                if (materialName.IndexOf(hint, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static Transform CreateWheelSteeringPivot(Transform visualRoot, string namePrefix, string pivotName)
        {
            var parts = FindPartsByPrefixes(visualRoot, namePrefix);
            if (parts.Count == 0)
            {
                return null;
            }

            var bounds = ComputeRendererBounds(parts);
            if (bounds == null)
            {
                return null;
            }

            var pivotObject = new GameObject(pivotName);
            pivotObject.transform.SetParent(visualRoot, false);
            pivotObject.transform.position = bounds.Value.center;
            pivotObject.transform.rotation = visualRoot.rotation;

            foreach (var part in parts)
            {
                part.SetParent(pivotObject.transform, true);
            }

            return pivotObject.transform;
        }

        /// <summary>
        /// Round 32: like CreateWheelSteeringPivot above (same bounds-
        /// based pivot-at-center technique), but for the wheel's ROLLING
        /// axis instead of its steering axis. Rotating this pivot's local
        /// X axis spins the wheel forward — confirmed from the raw model
        /// geometry (both RacingKart.obj and the new KartV2.obj: a
        /// wheel's world-space bounding box is consistently smallest
        /// along X, the axle direction, once placed under a pivot whose
        /// own rotation matches the kart body — see
        /// KartWheelSpinVisual's class doc). <paramref name="parentTransform"/>
        /// is either an existing STEERING pivot (front wheels — so a
        /// turned wheel keeps rolling on its own now-turned axle) or the
        /// kart's own visual root directly (rear wheels, which never
        /// steer). Also returns an estimated wheel radius (half the
        /// matched parts' own vertical bounds) so the caller can convert
        /// road speed into a spin rate.
        /// </summary>
        private static Transform CreateWheelSpinPivot(Transform parentTransform, string namePrefix, string pivotName,
            out float radiusMeters)
        {
            radiusMeters = 0f;

            var parts = FindPartsByPrefixes(parentTransform, namePrefix);
            if (parts.Count == 0)
            {
                return null;
            }

            var bounds = ComputeRendererBounds(parts);
            if (bounds == null)
            {
                return null;
            }

            var pivotObject = new GameObject(pivotName);
            pivotObject.transform.SetParent(parentTransform, false);
            pivotObject.transform.position = bounds.Value.center;
            pivotObject.transform.rotation = parentTransform.rotation;

            foreach (var part in parts)
            {
                part.SetParent(pivotObject.transform, true);
            }

            radiusMeters = bounds.Value.size.y * 0.5f;
            return pivotObject.transform;
        }

        // Round 32: near-black tire rubber (both kart models) made the
        // new wheel-roll rotation effectively invisible — see
        // KartWheelSpinVisual's class doc — so each wheel gets one small
        // bright mark near its rim to make the spin actually read.
        private static readonly Color WheelSpinMarkColor = new Color(0.92f, 0.92f, 0.88f);

        /// <summary>Round 32: adds one small bright mark to a wheel's rim — see WheelSpinMarkColor's doc.</summary>
        private static void AddWheelSpinMark(Transform spinPivot, float radiusMeters)
        {
            if (spinPivot == null || radiusMeters <= 0.001f)
            {
                return;
            }

            var mark = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var markCollider = mark.GetComponent<Collider>();
            if (markCollider != null)
            {
                DestroyImmediate(markCollider);
            }

            mark.name = "SpinMark";
            mark.transform.SetParent(spinPivot, false);

            var depth = radiusMeters * 0.55f; // along the axle (X) -- sits on the tire's outer face
            var thickness = radiusMeters * 0.22f; // radial extent
            var length = radiusMeters * 0.6f; // tangential extent, so it reads as a dash sweeping around

            mark.transform.localScale = new Vector3(depth, thickness, length);
            mark.transform.localPosition = new Vector3(0f, radiusMeters * 0.68f, 0f);
            mark.transform.localRotation = Quaternion.identity;

            var renderer = mark.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreateMaterial("WheelSpinMark", WheelSpinMarkColor);
            }
        }

        /// <summary>
        /// Round 27 founder request: "Aplicar o novo design enviado do
        /// volante e dos pedais... no cockpit 3D". RacingKart.obj already
        /// has its own simple built-in steering_wheel/pedal_brake/
        /// pedal_throttle/pedal_arm geometry from the original kart model;
        /// this hides that original geometry (kept, not destroyed — cheap
        /// and reversible) and instantiates the founder's separately-
        /// modeled SteeringWheel.obj/PedalBox.obj in its place, positioned
        /// at and scaled to match the ORIGINAL geometry's own bounds
        /// (bounds-based, not a hand-typed coordinate) — so the new prop
        /// lands wherever the founder already placed the old one inside
        /// his own kart model, no guessed offset needed for this part.
        ///
        /// Approximation flagged: the new prop's ROTATION is set to match
        /// the kart's own forward-facing rotation (not derived from the
        /// original part's rotation, which OBJ import does not expose in a
        /// simple form). For the steering wheel this relies on the same
        /// "thin axis = spin axis" reasoning already used for its 2D UI
        /// icon this round (its bounding box is 0.43m x 0.33m x 0.086m —
        /// Z is by far the thinnest, so the wheel faces forward along the
        /// kart's own forward axis, a normal orientation for a cockpit
        /// steering wheel). Not verified visually — no Unity Editor is
        /// available in this environment — flagged for the founder to
        /// confirm in a playtest.
        /// </summary>
        private static Transform ReplaceCockpitProp(Transform visualRoot, string[] originalNamePrefixes,
            string resourcePath, string instanceName, float sizeMultiplier = 1f)
        {
            var originalParts = FindPartsByPrefixes(visualRoot, originalNamePrefixes);
            if (originalParts.Count == 0)
            {
                return null;
            }

            var bounds = ComputeRendererBounds(originalParts);
            if (bounds == null)
            {
                return null;
            }

            foreach (var part in originalParts)
            {
                part.gameObject.SetActive(false);
            }

            var newModel = Resources.Load<GameObject>(resourcePath);
            if (newModel == null)
            {
                return null;
            }

            var propInstance = Instantiate(newModel, visualRoot, false);
            propInstance.name = instanceName;
            propInstance.transform.position = bounds.Value.center;
            propInstance.transform.rotation = visualRoot.rotation;

            foreach (var col in propInstance.GetComponentsInChildren<Collider>())
            {
                DestroyImmediate(col);
            }

            var propRenderers = propInstance.GetComponentsInChildren<Renderer>();
            if (propRenderers.Length == 0)
            {
                return propInstance.transform;
            }

            var propBounds = propRenderers[0].bounds;
            for (var i = 1; i < propRenderers.Length; i++)
            {
                propBounds.Encapsulate(propRenderers[i].bounds);
            }

            var originalLongest = Mathf.Max(bounds.Value.size.x, Mathf.Max(bounds.Value.size.y, bounds.Value.size.z));
            var propLongest = Mathf.Max(propBounds.size.x, Mathf.Max(propBounds.size.y, propBounds.size.z));
            if (propLongest > 0.001f && originalLongest > 0.001f)
            {
                propInstance.transform.localScale = Vector3.one * (originalLongest / propLongest);

                var rescaledRenderers = propInstance.GetComponentsInChildren<Renderer>();
                var rescaledBounds = rescaledRenderers[0].bounds;
                for (var i = 1; i < rescaledRenderers.Length; i++)
                {
                    rescaledBounds.Encapsulate(rescaledRenderers[i].bounds);
                }
                propInstance.transform.position += bounds.Value.center - rescaledBounds.center;
            }

            // Round 28: extra founder-requested size bump on top of the
            // bounds-matched scale above (e.g. "achei eles bem pequenos" —
            // matching the OLD built-in geometry's size exactly made the
            // new, more detailed props read as smaller than intended).
            // Re-centers afterward the same way the block above does, so
            // growing the prop doesn't drift it off the original spot.
            if (!Mathf.Approximately(sizeMultiplier, 1f) && propLongest > 0.001f && originalLongest > 0.001f)
            {
                propInstance.transform.localScale *= sizeMultiplier;

                var boostedRenderers = propInstance.GetComponentsInChildren<Renderer>();
                if (boostedRenderers.Length > 0)
                {
                    var boostedBounds = boostedRenderers[0].bounds;
                    for (var i = 1; i < boostedRenderers.Length; i++)
                    {
                        boostedBounds.Encapsulate(boostedRenderers[i].bounds);
                    }
                    propInstance.transform.position += bounds.Value.center - boostedBounds.center;
                }
            }

            return propInstance.transform;
        }

        /// <summary>
        /// Round 28: like CreateWheelSteeringPivot, but the pivot POSITION
        /// comes from one specific named part (<paramref name="hingePartName"/>
        /// — e.g. "brake_hinge_pin", a small cylinder in the founder's
        /// Pedals.obj whose own bounds center IS the physical hinge axis)
        /// rather than the combined bounds of every matched part — a
        /// pedal's hinge sits near its BASE, far from the pedal assembly's
        /// overall bounding-box center, so reusing
        /// CreateWheelSteeringPivot's "center of everything" math would
        /// put the pivot in the wrong place and the pedal would swing from
        /// its middle instead of its actual hinge. Falls back to the
        /// combined-bounds center if the named hinge part isn't found, so
        /// a future model swap degrades gracefully instead of crashing.
        /// </summary>
        private static Transform CreateHingePivot(Transform visualRoot, string armPrefix, string hingePartName, string pivotName)
        {
            var parts = FindPartsByPrefixes(visualRoot, armPrefix);
            if (parts.Count == 0)
            {
                return null;
            }

            Transform hingePart = null;
            foreach (var part in parts)
            {
                if (part.name.Equals(hingePartName, StringComparison.OrdinalIgnoreCase))
                {
                    hingePart = part;
                    break;
                }
            }

            var hingeRenderer = hingePart != null ? hingePart.GetComponent<Renderer>() : null;
            Vector3? pivotPosition = hingeRenderer != null ? hingeRenderer.bounds.center : (Vector3?)null;
            if (pivotPosition == null)
            {
                var fallbackBounds = ComputeRendererBounds(parts);
                if (fallbackBounds == null)
                {
                    return null;
                }
                pivotPosition = fallbackBounds.Value.center;
            }

            var pivotObject = new GameObject(pivotName);
            pivotObject.transform.SetParent(visualRoot, false);
            pivotObject.transform.position = pivotPosition.Value;
            pivotObject.transform.rotation = visualRoot.rotation;

            foreach (var part in parts)
            {
                part.SetParent(pivotObject.transform, true);
            }

            return pivotObject.transform;
        }

        /// <summary>
        /// Shared by CreateWheelSteeringPivot/ReplaceCockpitProp: finds
        /// every descendant of <paramref name="root"/> whose name starts
        /// with any of <paramref name="prefixes"/> (case-insensitive) —
        /// OBJ-imported part names, matched by prefix since a single
        /// logical part (e.g. one wheel) is actually several separately
        /// named sub-meshes (tire/rim/hub/bolts...).
        /// </summary>
        private static List<Transform> FindPartsByPrefixes(Transform root, params string[] prefixes)
        {
            var matches = new List<Transform>();
            foreach (var child in root.GetComponentsInChildren<Transform>())
            {
                if (child == root)
                {
                    continue;
                }

                foreach (var prefix in prefixes)
                {
                    if (child.name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        matches.Add(child);
                        break;
                    }
                }
            }

            return matches;
        }

        /// <summary>Combined world-space Renderer bounds of every part in the list, or null if none has a Renderer.</summary>
        private static Bounds? ComputeRendererBounds(List<Transform> parts)
        {
            Bounds? bounds = null;
            foreach (var part in parts)
            {
                var renderer = part.GetComponent<Renderer>();
                if (renderer == null)
                {
                    continue;
                }

                if (bounds == null)
                {
                    bounds = renderer.bounds;
                }
                else
                {
                    var expanded = bounds.Value;
                    expanded.Encapsulate(renderer.bounds);
                    bounds = expanded;
                }
            }

            return bounds;
        }

        /// <summary>
        /// Scales the instantiated model so its longest horizontal
        /// dimension matches <paramref name="targetLengthMeters"/>, rather
        /// than assuming a fixed scale factor — the Kenney FBX's native
        /// units are not something this environment can inspect in the
        /// Editor, so a bounds-based fit is the robust option.
        /// </summary>
        private static void FitVisualScale(Transform visual, Renderer[] renderers, float targetLengthMeters)
        {
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            var longestDimension = Mathf.Max(bounds.size.x, bounds.size.z);
            if (longestDimension <= 0.001f)
            {
                return;
            }

            var scale = targetLengthMeters / longestDimension;
            visual.localScale = Vector3.one * scale;
        }

        private static Transform CreatePrimitiveKartVisual(Transform parent, Color tintColor)
        {
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Kart Visual (fallback primitive)";
            visual.transform.SetParent(parent, false);
            visual.transform.localScale = new Vector3(1.0f, 0.35f, 1.8f);
            visual.GetComponent<Renderer>().sharedMaterial = CreateMaterial("KartVisualFallback", tintColor);
            DestroyImmediate(visual.GetComponent<Collider>());

            // Add a nose piece for visual direction
            var nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nose.name = "Kart Nose";
            nose.transform.SetParent(visual.transform, false);
            nose.transform.localPosition = new Vector3(0f, 0.08f, 0.85f);
            nose.transform.localScale = new Vector3(0.55f, 0.5f, 0.35f);
            nose.GetComponent<Renderer>().sharedMaterial = CreateMaterial("KartNoseFallback", new Color(0.95f, 0.75f, 0.1f));
            DestroyImmediate(nose.GetComponent<Collider>());

            return visual.transform;
        }

        private static KartPrototypeCamera CreateCamera(Transform target)
        {
            foreach (var existing in FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                Destroy(existing.gameObject);
            }

            var cameraObject = new GameObject("Kart Follow Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<UniversalAdditionalCameraData>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.12f, 0.16f);
            camera.fieldOfView = 62f;
            cameraObject.AddComponent<AudioListener>();
            var prototypeCamera = cameraObject.AddComponent<KartPrototypeCamera>();
            prototypeCamera.Configure(target);
            return prototypeCamera;
        }

        private static void CreateWall(string name, Vector3 position, Vector3 scale, bool solidCollider = true,
            bool useFenceVisual = false)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.position = position;
            wall.transform.localScale = scale;
            var renderer = wall.GetComponent<Renderer>();
            renderer.sharedMaterial = CreateMaterial("Barrier", new Color(0.7f, 0.1f, 0.1f));
            var collider = wall.GetComponent<Collider>();
            if (solidCollider)
            {
                collider.sharedMaterial = GetLowFrictionMaterial();
            }
            else
            {
                // Visual boundary marker only — see the Inner barrier
                // callers above for why (raised geometry the kart's rigid
                // BoxCollider cannot climb, clipped at every corner).
                collider.isTrigger = true;
            }

            if (useFenceVisual)
            {
                // Collision stays exactly this cube's BoxCollider, untouched
                // — only the RENDERED look changes. Same collision/visual
                // split already used for the grass ground fix (round 9).
                renderer.enabled = false;
                CreateFenceVisual(name, position, scale);
            }
        }

        /// <summary>
        /// Rotation-aware counterpart of <see cref="CreateWall"/> — see
        /// <see cref="CreateTrackPieceOriented"/> for why this is a
        /// separate method rather than a new parameter on the existing one
        /// (zero regression risk on every already-confirmed straight
        /// wall). Round-20 track redesign: used for the curved sections of
        /// both barrier rings, with fence/tire decoration (round 12/19)
        /// now available on curves via <see cref="CreateFenceVisualOriented"/>/
        /// <see cref="CreateTireBarrierVisualOriented"/>.
        /// </summary>
        private static void CreateWallOriented(string name, Vector3 position, float yawDegrees,
            float lengthMeters, float thicknessMeters, float heightMeters,
            bool solidCollider = true, bool useFenceVisual = false, bool useTireVisual = false)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yawDegrees, 0f));
            wall.transform.localScale = new Vector3(lengthMeters, heightMeters, thicknessMeters);
            var renderer = wall.GetComponent<Renderer>();
            renderer.sharedMaterial = CreateMaterial("Barrier", new Color(0.7f, 0.1f, 0.1f));
            var collider = wall.GetComponent<Collider>();
            if (solidCollider)
            {
                collider.sharedMaterial = GetLowFrictionMaterial();
            }
            else
            {
                collider.isTrigger = true;
            }

            if (useFenceVisual)
            {
                renderer.enabled = false;
                CreateFenceVisualOriented(name, position, yawDegrees, lengthMeters, heightMeters);
            }

            if (useTireVisual)
            {
                CreateTireBarrierVisualOriented(name, position, yawDegrees, lengthMeters, heightMeters);
            }
        }

        /// <summary>
        /// Tiles a real fence model (Kenney Racing Kit, see the constants
        /// declared near the top of this class) along one outer barrier
        /// wall. Deliberately NOT parented under the wall cube itself: that
        /// cube's non-uniform localScale (e.g. 84×1×0.5) would multiply
        /// into every child's local position/scale and badly distort the
        /// tiles, so each tile's transform is set in world space instead.
        /// </summary>
        private static void CreateFenceVisual(string wallName, Vector3 wallPosition, Vector3 wallScale)
        {
            var fenceModel = Resources.Load<GameObject>(FenceModelResourcePath);
            if (fenceModel == null)
            {
                // No visual, but the wall's own (already-created) collider
                // still blocks the kart — fails safe, not silently broken.
                return;
            }

            var alongX = wallScale.x >= wallScale.z;
            var lengthMeters = alongX ? wallScale.x : wallScale.z;
            var desiredTileCount = Mathf.Max(1, Mathf.RoundToInt(lengthMeters / FenceTileLengthMeters));
            var tileCount = Mathf.Min(desiredTileCount, MaxFenceTilesPerWall);
            var actualTileLength = lengthMeters / tileCount;
            var heightScale = wallScale.y / FenceNativeHeightMeters;
            var axis = alongX ? Vector3.right : Vector3.forward;
            var rotation = Quaternion.Euler(0f, alongX ? 0f : 90f, 0f);
            var rowStart = wallPosition - axis * (lengthMeters * 0.5f) - Vector3.up * (wallScale.y * 0.5f);

            var container = new GameObject($"{wallName}_FenceVisual");
            container.transform.position = wallPosition;

            // Founder playtest feedback, 2026-08-20 (round 12 continuation):
            // "um rosa fechando a pista" — the .dae ships its own COLLADA/
            // phong material ("net", textured with net.png), and Unity's
            // importer maps that to a shader this URP/IL2CPP build doesn't
            // actually have compiled in, so every fence tile rendered as
            // Unity's missing-shader magenta fallback. This is the exact
            // failure CreateMaterial's own comment already warns about for
            // runtime-grabbed materials — same fix: force every renderer
            // onto the project's known-good URP material instead of
            // whatever the import produced.
            var fenceMaterial = CreateMaterial("Fence", new Color(0.82f, 0.82f, 0.82f));

            for (var i = 0; i < tileCount; i++)
            {
                var tile = Instantiate(fenceModel, container.transform);
                tile.name = $"FenceTile_{i}";
                tile.transform.position = rowStart + axis * (i * actualTileLength);
                tile.transform.rotation = rotation;
                // Native pivot is the piece's own corner (not centered), so
                // no extra position offset is needed for the scale below —
                // stretching along local X still starts from rowStart.
                tile.transform.localScale = new Vector3(actualTileLength / FenceTileLengthMeters, heightScale, 1f);
                foreach (var col in tile.GetComponentsInChildren<Collider>())
                {
                    Destroy(col); // visual only — the wall's own collider (created above) is unchanged.
                }

                foreach (var rend in tile.GetComponentsInChildren<Renderer>())
                {
                    var slots = new Material[rend.sharedMaterials.Length];
                    for (var s = 0; s < slots.Length; s++)
                    {
                        slots[s] = fenceMaterial;
                    }
                    rend.sharedMaterials = slots;
                }

                tile.isStatic = true;
            }
        }

        /// <summary>
        /// Rotation-aware counterpart of <see cref="CreateFenceVisual"/>,
        /// used by <see cref="CreateWallOriented"/> for the round-20
        /// curved barrier segments. Same tiling math, but the travel axis
        /// and tile rotation both come directly from the wall's own yaw
        /// instead of the old "pick whichever world axis is longer, snap
        /// rotation to 0 or 90" heuristic — which only ever worked because
        /// every wall before round 20 was exactly axis-aligned.
        /// </summary>
        private static void CreateFenceVisualOriented(string wallName, Vector3 wallPosition, float yawDegrees,
            float lengthMeters, float wallHeightMeters)
        {
            var fenceModel = Resources.Load<GameObject>(FenceModelResourcePath);
            if (fenceModel == null)
            {
                return;
            }

            var desiredTileCount = Mathf.Max(1, Mathf.RoundToInt(lengthMeters / FenceTileLengthMeters));
            var tileCount = Mathf.Min(desiredTileCount, MaxFenceTilesPerWall);
            var actualTileLength = lengthMeters / tileCount;
            var heightScale = wallHeightMeters / FenceNativeHeightMeters;
            var rotation = Quaternion.Euler(0f, yawDegrees, 0f);
            var axis = rotation * Vector3.right;
            var rowStart = wallPosition - axis * (lengthMeters * 0.5f) - Vector3.up * (wallHeightMeters * 0.5f);

            var container = new GameObject($"{wallName}_FenceVisual");
            container.transform.position = wallPosition;

            var fenceMaterial = CreateMaterial("Fence", new Color(0.82f, 0.82f, 0.82f));

            for (var i = 0; i < tileCount; i++)
            {
                var tile = Instantiate(fenceModel, container.transform);
                tile.name = $"FenceTile_{i}";
                tile.transform.position = rowStart + axis * (i * actualTileLength);
                tile.transform.rotation = rotation;
                tile.transform.localScale = new Vector3(actualTileLength / FenceTileLengthMeters, heightScale, 1f);
                foreach (var col in tile.GetComponentsInChildren<Collider>())
                {
                    Destroy(col);
                }

                foreach (var rend in tile.GetComponentsInChildren<Renderer>())
                {
                    var slots = new Material[rend.sharedMaterials.Length];
                    for (var s = 0; s < slots.Length; s++)
                    {
                        slots[s] = fenceMaterial;
                    }
                    rend.sharedMaterials = slots;
                }

                tile.isStatic = true;
            }
        }

        // Round-19 founder feedback: 5f spacing read as "poucos pneus"
        // (too few tires) against his real-track reference photos, where
        // stacks run almost continuously along the barrier. Tightened to
        // 2.5f (roughly doubles the stack count vs. the previous 5f) —
        // still short of "no gaps" because each stack is a real cylinder
        // mesh (heavier per-instance than the flat fence-tile quads, which
        // already tile at 1f/48 per wall with no reported cost issue), and
        // the mobile triangle budget (M3-T01: ≤100K triangles, tier low)
        // is a real constraint I can't verify live without device profiler
        // access. If this still reads sparse on device, next step is
        // either tighter spacing again or a lower-poly tire mesh (fewer
        // cylinder sides) to buy back triangle budget.
        private const float TireStackSpacingMeters = 2.5f;
        private const int MaxTireStacksPerWall = 40;

        /// <summary>
        /// Founder playtest feedback, 2026-08-20 (round 12/18): "o ideal
        /// seria pista de kart mesmo com pneus em volta". Kenney's Racing
        /// Kit has no dedicated tire-stack model, so — same "placeholder"
        /// precedent already used for the marshal flags and pit building —
        /// this tiles a simple procedural stack of 2 black cylinders along
        /// the wall, in front of the fence panels from
        /// <see cref="CreateFenceVisual"/> (not replacing them; real kart
        /// tracks commonly combine a tire wall with catch fencing above
        /// it). Purely decorative: every collider is stripped, exactly like
        /// the fence tiles — the wall cube's own BoxCollider (created by
        /// the CreateWall call this runs right after) is what actually
        /// stops the kart, completely unchanged by this.
        /// </summary>
        private static void CreateTireBarrierVisual(string wallName, Vector3 wallPosition, Vector3 wallScale)
        {
            var alongX = wallScale.x >= wallScale.z;
            var lengthMeters = alongX ? wallScale.x : wallScale.z;
            var desiredCount = Mathf.Max(1, Mathf.RoundToInt(lengthMeters / TireStackSpacingMeters));
            var tileCount = Mathf.Min(desiredCount, MaxTireStacksPerWall);
            var spacing = lengthMeters / tileCount;
            var axis = alongX ? Vector3.right : Vector3.forward;
            var rowStart = wallPosition - axis * (lengthMeters * 0.5f - spacing * 0.5f) - Vector3.up * (wallScale.y * 0.5f);

            var container = new GameObject($"{wallName}_TireBarrierVisual");
            container.transform.position = wallPosition;

            var tireMaterialDark = CreateMaterial("TireStackDark", new Color(0.05f, 0.05f, 0.05f));
            var tireMaterialMid = CreateMaterial("TireStackMid", new Color(0.12f, 0.12f, 0.12f));

            for (var i = 0; i < tileCount; i++)
            {
                var basePosition = rowStart + axis * (i * spacing);
                var stack = new GameObject($"TireStack_{i}");
                stack.transform.SetParent(container.transform);
                stack.transform.position = basePosition;

                for (var layer = 0; layer < 2; layer++)
                {
                    var tire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    tire.name = $"Tire_{layer}";
                    tire.transform.SetParent(stack.transform);
                    tire.transform.localPosition = new Vector3(0f, 0.15f + layer * 0.3f, 0f);
                    tire.transform.localScale = new Vector3(0.7f, 0.15f, 0.7f);
                    tire.GetComponent<Renderer>().sharedMaterial = layer % 2 == 0 ? tireMaterialDark : tireMaterialMid;
                    Destroy(tire.GetComponent<Collider>());
                    tire.isStatic = true;
                }
            }
        }

        /// <summary>
        /// Rotation-aware counterpart of <see cref="CreateTireBarrierVisual"/>,
        /// used by <see cref="CreateWallOriented"/> for the round-20 curved
        /// barrier segments — same tiling math, travel axis taken directly
        /// from the wall's own yaw instead of inferred from scale.
        /// </summary>
        private static void CreateTireBarrierVisualOriented(string wallName, Vector3 wallPosition,
            float yawDegrees, float lengthMeters, float wallHeightMeters)
        {
            var desiredCount = Mathf.Max(1, Mathf.RoundToInt(lengthMeters / TireStackSpacingMeters));
            var tileCount = Mathf.Min(desiredCount, MaxTireStacksPerWall);
            var spacing = lengthMeters / tileCount;
            var axis = Quaternion.Euler(0f, yawDegrees, 0f) * Vector3.right;
            var rowStart = wallPosition - axis * (lengthMeters * 0.5f - spacing * 0.5f) - Vector3.up * (wallHeightMeters * 0.5f);

            var container = new GameObject($"{wallName}_TireBarrierVisual");
            container.transform.position = wallPosition;

            var tireMaterialDark = CreateMaterial("TireStackDark", new Color(0.05f, 0.05f, 0.05f));
            var tireMaterialMid = CreateMaterial("TireStackMid", new Color(0.12f, 0.12f, 0.12f));

            for (var i = 0; i < tileCount; i++)
            {
                var basePosition = rowStart + axis * (i * spacing);
                var stack = new GameObject($"TireStack_{i}");
                stack.transform.SetParent(container.transform);
                stack.transform.position = basePosition;

                for (var layer = 0; layer < 2; layer++)
                {
                    var tire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    tire.name = $"Tire_{layer}";
                    tire.transform.SetParent(stack.transform);
                    tire.transform.localPosition = new Vector3(0f, 0.15f + layer * 0.3f, 0f);
                    tire.transform.localScale = new Vector3(0.7f, 0.15f, 0.7f);
                    tire.GetComponent<Renderer>().sharedMaterial = layer % 2 == 0 ? tireMaterialDark : tireMaterialMid;
                    Destroy(tire.GetComponent<Collider>());
                    tire.isStatic = true;
                }
            }
        }

        private static GameObject CreatePrimitive(
            string name,
            PrimitiveType primitiveType,
            Vector3 position,
            Vector3 scale,
            string materialResourcePath)
        {
            var primitive = GameObject.CreatePrimitive(primitiveType);
            primitive.name = name;
            primitive.transform.position = position;
            primitive.transform.localScale = scale;
            var renderer = primitive.GetComponent<Renderer>();
            var material = Resources.Load<Material>(materialResourcePath);
            if (material != null)
            {
                renderer.sharedMaterial = material;
            }
            var collider = primitive.GetComponent<Collider>();
            if (collider != null)
            {
                collider.sharedMaterial = GetLowFrictionMaterial();
            }
            return primitive;
        }

        private static PhysicsMaterial GetLowFrictionMaterial()
        {
            if (_lowFrictionMaterial != null)
            {
                return _lowFrictionMaterial;
            }

            _lowFrictionMaterial = new PhysicsMaterial("Prototype Low Friction")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounciness = 0f,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };
            return _lowFrictionMaterial;
        }

        /// <summary>
        /// Founder playtest feedback, 2026-08-20 (round 9): "meu carrinho
        /// continua travando antes de qualquer curva, impacto forte mesmo
        /// na pista normal" — with the grass/barrier geometry seams fixed
        /// (see CreateCourse), the remaining suspect is kart-vs-kart: bots
        /// brake in anticipation of every corner by design, and a player
        /// closing the gap right before a turn (encouraged by the round-8
        /// slipstream feature) rams the back of a decelerating bot. With
        /// the shared 0-bounciness material every kart used to use, that
        /// resolves as an instant dead stop — reads exactly like hitting a
        /// wall. This material is used ONLY by the kart's own body
        /// collider (see CreateKartInstance) and deliberately keeps
        /// bounceCombine at Minimum, same as every ground/track/wall
        /// material — so any contact against one of those (bounciness 0)
        /// still resolves to min(0.3, 0) = 0, completely unchanged from
        /// before. Only kart-vs-kart contacts, where both sides carry this
        /// same material, resolve to min(0.3, 0.3) = 0.3 — enough to turn
        /// that dead stop into a deflecting bump instead.
        /// </summary>
        private static PhysicsMaterial GetKartCollisionMaterial()
        {
            if (_kartCollisionMaterial != null)
            {
                return _kartCollisionMaterial;
            }

            _kartCollisionMaterial = new PhysicsMaterial("Prototype Kart Collision")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounciness = 0.3f,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };
            return _kartCollisionMaterial;
        }
    }
}
