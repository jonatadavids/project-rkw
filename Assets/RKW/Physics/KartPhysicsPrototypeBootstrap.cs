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
        // Round 38 founder feedback: "ao lancar o carro fantasma ele
        // pega o carro de 60" -- the ghost's best-lap DATA was already
        // correctly scoped per kart category (see
        // PrototypeCompetitiveScope/kartCategoryId in
        // OnRaceSetupConfirmed), but its VISUAL MESH was hardcoded to
        // KartVisualResourcePath regardless of which kart the player is
        // actually driving. Tracked here, updated by RebuildKartVisual
        // (the only place the player's kart model ever changes), and
        // read when the ghost visual is created so it always matches.
        private static string _currentPlayerKartModelResourcePath = KartVisualResourcePath;
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

            // Round 36: track choice now comes from TrackSelectMenu (shown
            // FIRST, before any track/kart geometry exists) instead of the
            // old build-time UseTechnicalCircuit2 constant — see that
            // field's own comment above and OnTrackSelected/BeginRace
            // below. Everything that used to run directly in Awake() (track
            // build, kart spawn, camera, timing, the other pre-race
            // buttons, RaceSetupMenu) now runs inside BeginRace(), once
            // this first screen confirms.
            var trackMenuObject = new GameObject("TrackSelectMenu");
            var trackMenu = trackMenuObject.AddComponent<TrackSelectMenu>();
            trackMenu.Configure(OnTrackSelected);
        }

        // internal (was private): KartPhysicsPrototypeTests (PlayMode)
        // calls this directly to simulate the TrackSelectMenu tap a real
        // player would make -- see AssemblyInfo.cs's round-36 note.
        internal void OnTrackSelected(bool useTechnicalCircuit2)
        {
            UseTechnicalCircuit2 = useTechnicalCircuit2;
            BeginRace();
        }

        /// <summary>
        /// Builds the track/kart/camera/timing and every other pre-race
        /// object — everything Awake() used to do unconditionally before
        /// round 36 made track choice a runtime decision (see
        /// TrackSelectMenu's class doc). Runs once, right after
        /// <see cref="OnTrackSelected"/> sets
        /// <see cref="UseTechnicalCircuit2"/> from the player's pick.
        /// </summary>
        private void BeginRace()
        {
            _trackConfiguration = LoadTrackConfiguration();
            CreateLighting();
            if (UseTechnicalCircuit2)
            {
                CreateCourseTechnicalCircuit2();
            }
            else
            {
                CreateCourse();
            }
            // Round 40: see CombineStaticTrackGeometryForBatching's own
            // doc comment above for the full root-cause writeup. Runs
            // right after track geometry exists and before any kart is
            // spawned, so only the track's own isStatic-flagged pieces are
            // ever in scope.
            CombineStaticTrackGeometryForBatching();
            // Round 37 founder feedback: "falta nas 2 pistas o desenho da
            // largada no posicionamento do kart" -- the start/finish LINE
            // already existed, but the individual grid BOXES (one per
            // starting slot) were never drawn, so a player had no visual
            // cue for exactly where each of the 10 karts should line up.
            CreateGridSlotMarkers(_trackConfiguration);
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
            // Round 37 founder feedback: "quando jogar sozinho a ideia que
            // o kart sempre saia no primeiro grid de largada, sem ficar
            // alternando quando for com bot pode alternar" -- the player's
            // grid slot is normally a genuine random draw (round 16,
            // shared with bots so nobody is always P1), but in SOZINHO
            // mode (botCount == 0, forced by RaceSetupMenu's own
            // SelectMode) there are no bots to be fair to, so reposition
            // to pole position (grid slot 0) instead of whatever slot the
            // shared shuffle happened to give the player.
            // Round 38 founder feedback: "o modo sozinho continua largando
            // em qualquer posicao... nao em primeiro" -- re-reviewed this
            // whole block plus KartRecoveryController, RaceStartController,
            // RaceManager and SpawnBots line by line and found no logic
            // fault (recovery only ever monitors once InputEnabled is
            // true, which this block runs before; RaceStartController
            // never touches position; RaceManager never repositions
            // karts; SpawnBots does nothing when botCount is 0). Since
            // static reading alone could not find the bug, logging every
            // input to this decision plus the exact position actually
            // applied, so a real device logcat can show whether this
            // branch is even being entered, with what data, and whether
            // the position sticks -- see the matching log in
            // RaceStartController.Update() below for whether anything
            // moves the kart afterward.
            var kartPosBeforeLabel = SpawnedKart != null ? SpawnedKart.transform.position.ToString("F2") : "null";
            Debug.Log("KartPhysicsPrototypeBootstrap: OnRaceSetupConfirmed " +
                $"botCount={botCount}, gridSlotsCount={(_trackConfiguration != null ? _trackConfiguration.GridSlots.Count : -1)}, " +
                $"kartPosBefore={kartPosBeforeLabel}.");
            if (botCount == 0 && _trackConfiguration != null && _trackConfiguration.GridSlots.Count > 0)
            {
                var poleSlot = _trackConfiguration.GridSlots[0];
                var polePosition = poleSlot.WorldPosition;
                polePosition.y = 0.55f; // same vertical offset CreatePlayerKart uses
                var poleRotation = Quaternion.Euler(0f, poleSlot.YawDegrees, 0f);
                var poleBody = SpawnedKart.GetComponent<Rigidbody>();
                if (poleBody != null)
                {
                    // Round 39 (continuation 6) fix: teleport through the
                    // Rigidbody, not the Transform -- see this block's
                    // doc comment above (KartDynamics' Rigidbody has
                    // interpolation enabled, and a Transform-only move
                    // gets silently reconciled back to the Rigidbody's
                    // real physics pose a few steps later, which is
                    // exactly the "modo sozinho não larga na pole" bug).
                    poleBody.position = polePosition;
                    poleBody.rotation = poleRotation;
                    poleBody.linearVelocity = Vector3.zero;
                    poleBody.angularVelocity = Vector3.zero;
                }
                else
                {
                    SpawnedKart.transform.SetPositionAndRotation(polePosition, poleRotation);
                }
                var poleTargetLabel = polePosition.ToString("F2");
                var kartPosAfterLabel = SpawnedKart.transform.position.ToString("F2");
                Debug.Log("KartPhysicsPrototypeBootstrap: solo-mode pole reposition applied -- " +
                    $"poleSlot.Position={poleSlot.Position}, target={poleTargetLabel}, " +
                    $"kartPosAfter={kartPosAfterLabel}.");
            }

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
            var ghostVisual = CreateKartVisual(ghostRoot.transform, GhostTintColor, _playerRaceNumber,
                _currentPlayerKartModelResourcePath);
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
            // Round 34: load whichever track's data matches the
            // UseTechnicalCircuit2 toggle above (Awake() picks the
            // matching GEOMETRY the same way — the two must stay in sync
            // or the grid/bot-path data won't match what's actually built).
            var trackConfigurationResourcePath = UseTechnicalCircuit2
                ? "Track/TechnicalCircuit2Configuration"
                : "Track/OvalMvpTrackConfiguration";
            var trackConfiguration = Resources.Load<TrackConfigurationSO>(trackConfigurationResourcePath);
            if (trackConfiguration == null)
            {
                Debug.LogWarning("KartPhysicsPrototypeBootstrap: no TrackConfigurationSO found at " +
                    $"Resources/{trackConfigurationResourcePath}.");
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
            // Round 34: the oval has 3 non-start/finish checkpoints (CP1-3);
            // Circuit2 has 11 (one per filleted vertex, CP0-CP10 — see the
            // Circuit2 constants/CreateCourseTechnicalCircuit2 below).
            timing.Configure(UseTechnicalCircuit2 ? 16 : 3);
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

        /// <summary>
        /// See the round-40 comment above this method's call site in
        /// BeginRace for the full root-cause writeup (isStatic alone does
        /// nothing for procedurally-created-at-runtime geometry -- Unity's
        /// automatic static batcher only runs at build time). Finds every
        /// renderer in the scene whose GameObject is already marked
        /// isStatic (by construction, only the track-building code above
        /// ever sets that flag -- kart visuals, wheel/steering pivots, and
        /// HUD are never marked static) and merges them via Unity's own
        /// documented runtime API for this exact situation.
        /// </summary>
        private static void CombineStaticTrackGeometryForBatching()
        {
            var allRenderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            var staticObjects = new List<GameObject>();
            foreach (var renderer in allRenderers)
            {
                if (renderer.gameObject.isStatic)
                {
                    staticObjects.Add(renderer.gameObject);
                }
            }

            if (staticObjects.Count == 0)
            {
                return;
            }

            var batchRoot = new GameObject("StaticTrackGeometry_CombinedBatchRoot");
            StaticBatchingUtility.Combine(staticObjects.ToArray(), batchRoot);
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
            groundVisual.transform.position = new Vector3(0f, 0.058f, 0f); // round 37: closer to pavement top (0.06) to remove the visible "ranhura" step at the grass/asphalt seam
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
            CreateRibbon("Pavement", pavementCenterline, TrackWidthMeters, 0.12f, asphaltMat, StadiumArcSegmentsPerEnd,
                solidCollider: false);
            // Round 39 (continuation 4): same seamless mesh floor fix as
            // the Carrera Kart (see CreatePavementMeshFloor's doc comment)
            // -- confirmed by founder testing to fully resolve the
            // catch/jump physics feel, so applying it here too even
            // though the Oval was never reported as badly affected, per
            // founder request to make this the standard pattern for every
            // track. Oval's width is constant (unlike Circuit2's
            // per-point width), so this just fills a same-length array
            // with TrackWidthMeters before reusing the identical method.
            var ovalWidths = new float[pavementCenterline.Count];
            for (var ovalWidthIndex = 0; ovalWidthIndex < ovalWidths.Length; ovalWidthIndex++)
            {
                ovalWidths[ovalWidthIndex] = TrackWidthMeters;
            }
            CreatePavementMeshFloor("Pavement_MeshFloor", pavementCenterline.ToArray(), ovalWidths, 0.06f);

            // --- CURBS (zebras) ---
            // Round 36 founder feedback: "a zebra ideal... fica bem
            // próxima as curvas... não aquele bloco grande e travado" —
            // replaced the two static 4x4m apex blocks with a continuous
            // checkered strip that hugs the INSIDE (infield) edge of both
            // semicircle ends the whole way round, built from the same
            // GenerateStadiumCenterline formula as the pavement/barriers
            // above (just a smaller radius, so it stays perfectly
            // concentric with zero extra bookkeeping — same trick already
            // used for outerBarrierCenterline/innerBarrierCenterline).
            // Non-solid for the same reason as round 18 (raised geometry a
            // wheel-less rigid-BoxCollider kart can't climb).
            const float CurbWidthMeters = 1.2f;
            var curbCenterline = GenerateStadiumCenterline(
                StadiumHalfStraightMeters, StadiumRadiusMeters - (TrackWidthMeters * 0.5f - CurbWidthMeters * 0.5f),
                StadiumArcSegmentsPerEnd);
            CreateAlternatingCurbRibbon("Curb", curbCenterline, StadiumArcSegmentsPerEnd, CurbWidthMeters, 0.08f);

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

        // ============================================================
        // PISTA 2 — CIRCUITO TÉCNICO (round 34, 2026-08-25)
        // ============================================================
        // Founder-approved-for-greybox second track: an irregular ~1001m
        // closed loop (NOT a stadium — 11 waypoints, each rounded with its
        // own circular-arc fillet radius), with a technical S-sector
        // (vertices 7-8-9) and a tight hairpin (vertex 10) near the grid.
        // Every array below (pavement centerline, outer/inner barrier
        // rings, per-point width, which edges are arc chords) was computed
        // and validated — zero self-intersections, minimum real clearance
        // between non-adjacent sections, cornering-speed/braking-distance
        // sanity-checked against the REAL kart tuning constants
        // (wheelbaseMeters/maxSteeringAngleDegrees/lateralGripG/
        // brakeDeceleration in the Resources/KartPhysics/*.asset files) —
        // in a standalone Python model before any of this was written,
        // the same practice this project already established for the
        // first track (see GenerateStadiumCenterline's own comment).
        // See docs/30-founder-playtest-log.md round 34 for the full
        // derivation and claude/status-pista2-planejamento.md (project
        // docs) for the founder-facing summary, sign-off, and the 6-point
        // review (grid/width/chicane/kart-radius/bots/checkpoints) this
        // build responds to.
        //
        // Deliberately minimal decoration in this first greybox (see
        // round 34 log): hairpin-specific local widening beyond the grid
        // straight, dedicated escape/runoff areas at curves 3/5/7/8/10,
        // marshal posts/fences/pit building.
        //
        // Round 36 (2026-08-25) founder request: "o circuito oval pode
        // dar o nome de Circuito Oval e deixar selecionável" — this used
        // to be a build-time `const bool` (flip and rebuild to change
        // track, the same "pre-race toggle, not a full selection screen"
        // scope choice made for kart category in round 32). Now a mutable
        // static field instead, set once per session by
        // TrackSelectMenu's confirm callback (see
        // OnTrackSelected/BeginRace below) BEFORE any track/kart geometry
        // is built — every method below that reads this (LoadTrackConfiguration,
        // SetupTiming, Awake/BeginRace's own course-building branch) still
        // reads the exact same field, just no longer compile-time fixed.
        // Default (true) only matters for the brief window before
        // TrackSelectMenu's first OnGUI paints.
        private static bool UseTechnicalCircuit2 = true;

        // Ground plane sized to Circuit2's actual bounding box (it does
        // NOT sit at the world origin like the oval — the two tracks'
        // footprints currently overlap in world space, which is fine only
        // because just one of them is ever built per build via the toggle
        // above; a real track-select flow will need to either offset one
        // track far away or destroy/rebuild the scene on selection).
        private const float Circuit2GroundCenterX = 169.4f;
        private const float Circuit2GroundCenterZ = -84.6f;
        private const float Circuit2GroundSizeX = 336.6f;
        private const float Circuit2GroundSizeZ = 188.3f;

        private static readonly Vector3[] Circuit2Centerline = {new Vector3(88.012f, 0.000f, -134.091f), new Vector3(84.238f, 0.000f, -130.930f), new Vector3(80.481f, 0.000f, -127.751f), new Vector3(77.449f, 0.000f, -124.991f), new Vector3(76.125f, 0.000f, -124.021f), new Vector3(74.762f, 0.000f, -123.107f), new Vector3(73.364f, 0.000f, -122.249f), new Vector3(71.912f, 0.000f, -121.483f), new Vector3(67.372f, 0.000f, -119.581f), new Vector3(62.797f, 0.000f, -117.762f), new Vector3(58.137f, 0.000f, -116.176f), new Vector3(53.335f, 0.000f, -115.101f), new Vector3(48.467f, 0.000f, -114.385f), new Vector3(44.410f, 0.000f, -113.793f), new Vector3(42.802f, 0.000f, -113.465f), new Vector3(41.204f, 0.000f, -113.088f), new Vector3(39.636f, 0.000f, -112.608f), new Vector3(38.122f, 0.000f, -111.976f), new Vector3(36.706f, 0.000f, -111.150f), new Vector3(35.431f, 0.000f, -110.119f), new Vector3(34.335f, 0.000f, -108.899f), new Vector3(33.386f, 0.000f, -107.561f), new Vector3(32.523f, 0.000f, -106.166f), new Vector3(29.997f, 0.000f, -101.939f), new Vector3(29.162f, 0.000f, -100.526f), new Vector3(28.350f, 0.000f, -99.101f), new Vector3(27.588f, 0.000f, -97.647f), new Vector3(26.948f, 0.000f, -96.137f), new Vector3(26.566f, 0.000f, -94.545f), new Vector3(26.655f, 0.000f, -92.914f), new Vector3(27.182f, 0.000f, -91.362f), new Vector3(27.844f, 0.000f, -89.860f), new Vector3(28.488f, 0.000f, -88.351f), new Vector3(30.476f, 0.000f, -83.849f), new Vector3(31.265f, 0.000f, -82.410f), new Vector3(32.110f, 0.000f, -81.003f), new Vector3(32.992f, 0.000f, -79.619f), new Vector3(33.932f, 0.000f, -78.274f), new Vector3(34.984f, 0.000f, -77.016f), new Vector3(36.161f, 0.000f, -75.873f), new Vector3(37.415f, 0.000f, -74.815f), new Vector3(41.241f, 0.000f, -71.716f), new Vector3(45.124f, 0.000f, -68.689f), new Vector3(48.646f, 0.000f, -65.258f), new Vector3(51.895f, 0.000f, -61.558f), new Vector3(53.006f, 0.000f, -60.350f), new Vector3(54.177f, 0.000f, -59.201f), new Vector3(55.420f, 0.000f, -58.129f), new Vector3(56.724f, 0.000f, -57.134f), new Vector3(58.088f, 0.000f, -56.221f), new Vector3(59.518f, 0.000f, -55.418f), new Vector3(61.027f, 0.000f, -54.773f), new Vector3(62.592f, 0.000f, -54.282f), new Vector3(64.191f, 0.000f, -53.916f), new Vector3(65.810f, 0.000f, -53.647f), new Vector3(67.441f, 0.000f, -53.463f), new Vector3(69.078f, 0.000f, -53.359f), new Vector3(70.719f, 0.000f, -53.337f), new Vector3(75.630f, 0.000f, -53.670f), new Vector3(80.536f, 0.000f, -54.080f), new Vector3(84.634f, 0.000f, -54.281f), new Vector3(86.274f, 0.000f, -54.330f), new Vector3(87.912f, 0.000f, -54.247f), new Vector3(89.537f, 0.000f, -54.015f), new Vector3(91.137f, 0.000f, -53.655f), new Vector3(92.706f, 0.000f, -53.173f), new Vector3(94.238f, 0.000f, -52.586f), new Vector3(95.733f, 0.000f, -51.909f), new Vector3(99.799f, 0.000f, -49.156f), new Vector3(101.135f, 0.000f, -48.205f), new Vector3(102.614f, 0.000f, -47.501f), new Vector3(103.397f, 0.000f, -47.256f), new Vector3(105.008f, 0.000f, -46.948f), new Vector3(106.643f, 0.000f, -46.805f), new Vector3(108.283f, 0.000f, -46.762f), new Vector3(113.206f, 0.000f, -46.750f), new Vector3(114.847f, 0.000f, -46.706f), new Vector3(116.482f, 0.000f, -46.564f), new Vector3(118.097f, 0.000f, -46.278f), new Vector3(119.675f, 0.000f, -45.830f), new Vector3(121.207f, 0.000f, -45.242f), new Vector3(122.713f, 0.000f, -44.590f), new Vector3(124.222f, 0.000f, -43.945f), new Vector3(125.739f, 0.000f, -43.319f), new Vector3(127.227f, 0.000f, -42.627f), new Vector3(128.633f, 0.000f, -41.783f), new Vector3(132.418f, 0.000f, -38.639f), new Vector3(136.381f, 0.000f, -35.722f), new Vector3(140.480f, 0.000f, -32.994f), new Vector3(144.335f, 0.000f, -29.937f), new Vector3(148.154f, 0.000f, -26.831f), new Vector3(151.942f, 0.000f, -23.691f), new Vector3(155.490f, 0.000f, -20.280f), new Vector3(156.738f, 0.000f, -19.213f), new Vector3(158.020f, 0.000f, -18.189f), new Vector3(159.373f, 0.000f, -17.263f), new Vector3(160.847f, 0.000f, -16.545f), new Vector3(162.414f, 0.000f, -16.061f), new Vector3(164.019f, 0.000f, -15.723f), new Vector3(165.647f, 0.000f, -15.514f), new Vector3(167.287f, 0.000f, -15.489f), new Vector3(168.922f, 0.000f, -15.621f), new Vector3(170.548f, 0.000f, -15.844f), new Vector3(172.158f, 0.000f, -16.160f), new Vector3(173.729f, 0.000f, -16.630f), new Vector3(175.203f, 0.000f, -17.345f), new Vector3(176.481f, 0.000f, -18.367f), new Vector3(177.427f, 0.000f, -19.701f), new Vector3(178.021f, 0.000f, -21.229f), new Vector3(178.402f, 0.000f, -22.824f), new Vector3(178.663f, 0.000f, -24.444f), new Vector3(178.815f, 0.000f, -26.078f), new Vector3(179.231f, 0.000f, -30.983f), new Vector3(179.991f, 0.000f, -35.847f), new Vector3(180.293f, 0.000f, -37.460f), new Vector3(180.692f, 0.000f, -39.051f), new Vector3(181.207f, 0.000f, -40.609f), new Vector3(181.817f, 0.000f, -42.132f), new Vector3(182.508f, 0.000f, -43.621f), new Vector3(183.236f, 0.000f, -45.092f), new Vector3(183.980f, 0.000f, -46.554f), new Vector3(184.807f, 0.000f, -47.971f), new Vector3(185.804f, 0.000f, -49.273f), new Vector3(186.969f, 0.000f, -50.428f), new Vector3(188.257f, 0.000f, -51.443f), new Vector3(189.630f, 0.000f, -52.341f), new Vector3(191.045f, 0.000f, -53.173f), new Vector3(192.460f, 0.000f, -54.004f), new Vector3(193.870f, 0.000f, -54.843f), new Vector3(195.322f, 0.000f, -55.607f), new Vector3(196.858f, 0.000f, -56.180f), new Vector3(198.463f, 0.000f, -56.518f), new Vector3(200.091f, 0.000f, -56.727f), new Vector3(201.725f, 0.000f, -56.882f), new Vector3(203.363f, 0.000f, -56.973f), new Vector3(208.278f, 0.000f, -56.760f), new Vector3(213.197f, 0.000f, -56.599f), new Vector3(218.121f, 0.000f, -56.597f), new Vector3(222.223f, 0.000f, -56.534f), new Vector3(223.864f, 0.000f, -56.548f), new Vector3(225.502f, 0.000f, -56.646f), new Vector3(227.126f, 0.000f, -56.881f), new Vector3(227.924f, 0.000f, -57.071f), new Vector3(229.470f, 0.000f, -57.617f), new Vector3(230.933f, 0.000f, -58.359f), new Vector3(232.318f, 0.000f, -59.238f), new Vector3(236.460f, 0.000f, -61.895f), new Vector3(237.889f, 0.000f, -62.700f), new Vector3(239.245f, 0.000f, -63.623f), new Vector3(240.470f, 0.000f, -64.713f), new Vector3(243.874f, 0.000f, -68.269f), new Vector3(247.563f, 0.000f, -71.529f), new Vector3(251.475f, 0.000f, -74.512f), new Vector3(255.718f, 0.000f, -77.008f), new Vector3(259.863f, 0.000f, -79.663f), new Vector3(263.965f, 0.000f, -82.386f), new Vector3(267.845f, 0.000f, -85.414f), new Vector3(271.863f, 0.000f, -88.256f), new Vector3(273.779f, 0.000f, -89.800f), new Vector3(277.184f, 0.000f, -93.352f), new Vector3(280.617f, 0.000f, -96.879f), new Vector3(284.224f, 0.000f, -100.230f), new Vector3(286.082f, 0.000f, -101.844f), new Vector3(287.377f, 0.000f, -102.852f), new Vector3(288.732f, 0.000f, -103.776f), new Vector3(293.073f, 0.000f, -106.096f), new Vector3(297.434f, 0.000f, -108.381f), new Vector3(298.829f, 0.000f, -109.244f), new Vector3(300.161f, 0.000f, -110.202f), new Vector3(301.424f, 0.000f, -111.249f), new Vector3(302.629f, 0.000f, -112.364f), new Vector3(303.784f, 0.000f, -113.529f), new Vector3(304.339f, 0.000f, -114.134f), new Vector3(305.373f, 0.000f, -115.407f), new Vector3(306.248f, 0.000f, -116.794f), new Vector3(306.947f, 0.000f, -118.278f), new Vector3(308.878f, 0.000f, -122.807f), new Vector3(310.977f, 0.000f, -127.260f), new Vector3(311.593f, 0.000f, -128.781f), new Vector3(312.074f, 0.000f, -130.349f), new Vector3(312.412f, 0.000f, -131.955f), new Vector3(312.631f, 0.000f, -133.581f), new Vector3(312.725f, 0.000f, -135.219f), new Vector3(312.622f, 0.000f, -136.855f), new Vector3(312.329f, 0.000f, -138.470f), new Vector3(311.947f, 0.000f, -140.065f), new Vector3(311.502f, 0.000f, -141.645f), new Vector3(310.937f, 0.000f, -143.185f), new Vector3(310.202f, 0.000f, -144.651f), new Vector3(309.284f, 0.000f, -146.009f), new Vector3(308.750f, 0.000f, -146.632f), new Vector3(307.557f, 0.000f, -147.757f), new Vector3(306.234f, 0.000f, -148.727f), new Vector3(304.820f, 0.000f, -149.558f), new Vector3(303.325f, 0.000f, -150.234f), new Vector3(301.776f, 0.000f, -150.773f), new Vector3(300.191f, 0.000f, -151.198f), new Vector3(298.579f, 0.000f, -151.505f), new Vector3(293.667f, 0.000f, -151.772f), new Vector3(288.812f, 0.000f, -152.499f), new Vector3(283.985f, 0.000f, -153.433f), new Vector3(279.076f, 0.000f, -153.788f), new Vector3(274.152f, 0.000f, -153.803f), new Vector3(269.229f, 0.000f, -153.803f), new Vector3(264.305f, 0.000f, -153.803f), new Vector3(259.382f, 0.000f, -153.801f), new Vector3(254.466f, 0.000f, -153.561f), new Vector3(249.556f, 0.000f, -153.205f), new Vector3(244.674f, 0.000f, -152.569f), new Vector3(239.835f, 0.000f, -151.671f), new Vector3(235.030f, 0.000f, -150.597f), new Vector3(230.257f, 0.000f, -149.391f), new Vector3(228.686f, 0.000f, -148.917f), new Vector3(227.129f, 0.000f, -148.398f), new Vector3(225.605f, 0.000f, -147.790f), new Vector3(224.154f, 0.000f, -147.026f), new Vector3(222.760f, 0.000f, -146.160f), new Vector3(221.395f, 0.000f, -145.249f), new Vector3(220.099f, 0.000f, -144.243f), new Vector3(218.963f, 0.000f, -143.062f), new Vector3(218.072f, 0.000f, -141.687f), new Vector3(217.465f, 0.000f, -140.166f), new Vector3(217.137f, 0.000f, -138.560f), new Vector3(217.077f, 0.000f, -136.921f), new Vector3(217.248f, 0.000f, -135.290f), new Vector3(217.617f, 0.000f, -133.692f), new Vector3(218.185f, 0.000f, -132.153f), new Vector3(218.940f, 0.000f, -130.698f), new Vector3(219.837f, 0.000f, -129.324f), new Vector3(220.829f, 0.000f, -128.017f), new Vector3(221.939f, 0.000f, -126.810f), new Vector3(223.214f, 0.000f, -125.780f), new Vector3(224.667f, 0.000f, -125.021f), new Vector3(226.224f, 0.000f, -124.507f), new Vector3(227.820f, 0.000f, -124.126f), new Vector3(232.628f, 0.000f, -123.069f), new Vector3(234.225f, 0.000f, -122.690f), new Vector3(235.783f, 0.000f, -122.175f), new Vector3(237.291f, 0.000f, -121.529f), new Vector3(238.774f, 0.000f, -120.825f), new Vector3(240.246f, 0.000f, -120.101f), new Vector3(241.690f, 0.000f, -119.322f), new Vector3(243.054f, 0.000f, -118.410f), new Vector3(244.239f, 0.000f, -117.281f), new Vector3(245.108f, 0.000f, -115.895f), new Vector3(245.590f, 0.000f, -114.330f), new Vector3(245.794f, 0.000f, -112.703f), new Vector3(245.797f, 0.000f, -111.063f), new Vector3(245.600f, 0.000f, -109.435f), new Vector3(245.185f, 0.000f, -107.849f), new Vector3(244.446f, 0.000f, -106.391f), new Vector3(243.281f, 0.000f, -105.247f), new Vector3(241.822f, 0.000f, -104.504f), new Vector3(240.252f, 0.000f, -104.033f), new Vector3(238.644f, 0.000f, -103.702f), new Vector3(237.024f, 0.000f, -103.442f), new Vector3(232.168f, 0.000f, -102.631f), new Vector3(227.316f, 0.000f, -101.799f), new Vector3(222.507f, 0.000f, -100.745f), new Vector3(217.716f, 0.000f, -99.613f), new Vector3(212.841f, 0.000f, -98.927f), new Vector3(207.995f, 0.000f, -98.064f), new Vector3(205.596f, 0.000f, -97.511f), new Vector3(204.018f, 0.000f, -97.063f), new Vector3(202.471f, 0.000f, -96.516f), new Vector3(200.960f, 0.000f, -95.875f), new Vector3(199.471f, 0.000f, -95.186f), new Vector3(197.987f, 0.000f, -94.483f), new Vector3(196.533f, 0.000f, -93.723f), new Vector3(195.151f, 0.000f, -92.840f), new Vector3(193.875f, 0.000f, -91.809f), new Vector3(192.705f, 0.000f, -90.660f), new Vector3(191.618f, 0.000f, -89.430f), new Vector3(190.582f, 0.000f, -88.157f), new Vector3(189.593f, 0.000f, -86.848f), new Vector3(187.080f, 0.000f, -82.621f), new Vector3(184.600f, 0.000f, -78.370f), new Vector3(183.726f, 0.000f, -76.981f), new Vector3(182.844f, 0.000f, -75.597f), new Vector3(181.884f, 0.000f, -74.266f), new Vector3(180.757f, 0.000f, -73.077f), new Vector3(180.127f, 0.000f, -72.551f), new Vector3(178.778f, 0.000f, -71.618f), new Vector3(177.367f, 0.000f, -70.780f), new Vector3(175.919f, 0.000f, -70.008f), new Vector3(174.426f, 0.000f, -69.328f), new Vector3(172.892f, 0.000f, -68.744f), new Vector3(169.803f, 0.000f, -67.634f), new Vector3(168.241f, 0.000f, -67.131f), new Vector3(166.652f, 0.000f, -66.721f), new Vector3(165.047f, 0.000f, -66.380f), new Vector3(163.431f, 0.000f, -66.096f), new Vector3(161.797f, 0.000f, -65.948f), new Vector3(160.158f, 0.000f, -65.995f), new Vector3(158.526f, 0.000f, -66.168f), new Vector3(153.634f, 0.000f, -66.722f), new Vector3(151.210f, 0.000f, -67.148f), new Vector3(149.604f, 0.000f, -67.489f), new Vector3(148.018f, 0.000f, -67.908f), new Vector3(146.460f, 0.000f, -68.421f), new Vector3(144.947f, 0.000f, -69.057f), new Vector3(143.505f, 0.000f, -69.839f), new Vector3(142.134f, 0.000f, -70.741f), new Vector3(138.131f, 0.000f, -73.607f), new Vector3(136.146f, 0.000f, -75.063f), new Vector3(134.854f, 0.000f, -76.074f), new Vector3(133.628f, 0.000f, -77.165f), new Vector3(132.497f, 0.000f, -78.353f), new Vector3(131.475f, 0.000f, -79.637f), new Vector3(130.572f, 0.000f, -81.006f), new Vector3(129.804f, 0.000f, -82.455f), new Vector3(129.186f, 0.000f, -83.975f), new Vector3(128.728f, 0.000f, -85.550f), new Vector3(128.425f, 0.000f, -87.163f), new Vector3(128.220f, 0.000f, -88.791f), new Vector3(128.072f, 0.000f, -90.425f), new Vector3(127.990f, 0.000f, -92.064f), new Vector3(127.965f, 0.000f, -93.705f), new Vector3(127.972f, 0.000f, -95.346f), new Vector3(128.036f, 0.000f, -96.986f), new Vector3(128.240f, 0.000f, -98.613f), new Vector3(128.679f, 0.000f, -100.192f), new Vector3(129.430f, 0.000f, -101.647f), new Vector3(130.471f, 0.000f, -102.913f), new Vector3(131.692f, 0.000f, -104.008f), new Vector3(133.006f, 0.000f, -104.991f), new Vector3(134.383f, 0.000f, -105.883f), new Vector3(135.840f, 0.000f, -106.635f), new Vector3(137.385f, 0.000f, -107.187f), new Vector3(138.977f, 0.000f, -107.585f), new Vector3(140.581f, 0.000f, -107.932f), new Vector3(145.424f, 0.000f, -108.811f), new Vector3(150.254f, 0.000f, -109.766f), new Vector3(154.920f, 0.000f, -111.317f), new Vector3(159.525f, 0.000f, -113.060f), new Vector3(164.269f, 0.000f, -114.351f), new Vector3(168.841f, 0.000f, -116.143f), new Vector3(173.390f, 0.000f, -118.019f), new Vector3(176.394f, 0.000f, -119.337f), new Vector3(177.844f, 0.000f, -120.107f), new Vector3(179.276f, 0.000f, -120.908f), new Vector3(180.676f, 0.000f, -121.764f), new Vector3(181.993f, 0.000f, -122.740f), new Vector3(183.130f, 0.000f, -123.920f), new Vector3(183.970f, 0.000f, -125.325f), new Vector3(184.261f, 0.000f, -126.091f), new Vector3(184.643f, 0.000f, -127.686f), new Vector3(184.833f, 0.000f, -129.315f), new Vector3(184.827f, 0.000f, -130.955f), new Vector3(184.569f, 0.000f, -132.574f), new Vector3(184.048f, 0.000f, -134.128f), new Vector3(183.276f, 0.000f, -135.574f), new Vector3(182.293f, 0.000f, -136.886f), new Vector3(181.160f, 0.000f, -138.072f), new Vector3(179.900f, 0.000f, -139.122f), new Vector3(178.499f, 0.000f, -139.973f), new Vector3(176.967f, 0.000f, -140.554f), new Vector3(175.359f, 0.000f, -140.876f), new Vector3(173.727f, 0.000f, -141.043f), new Vector3(172.090f, 0.000f, -141.157f), new Vector3(167.178f, 0.000f, -141.482f), new Vector3(162.255f, 0.000f, -141.498f), new Vector3(157.331f, 0.000f, -141.498f), new Vector3(152.410f, 0.000f, -141.394f), new Vector3(147.505f, 0.000f, -140.970f), new Vector3(142.652f, 0.000f, -140.147f), new Vector3(137.801f, 0.000f, -139.309f), new Vector3(136.169f, 0.000f, -139.136f), new Vector3(134.530f, 0.000f, -139.057f), new Vector3(132.889f, 0.000f, -139.044f), new Vector3(127.998f, 0.000f, -139.544f), new Vector3(123.112f, 0.000f, -140.138f), new Vector3(118.225f, 0.000f, -140.740f), new Vector3(113.327f, 0.000f, -141.232f), new Vector3(108.407f, 0.000f, -141.403f), new Vector3(104.321f, 0.000f, -141.774f), new Vector3(102.683f, 0.000f, -141.862f), new Vector3(101.042f, 0.000f, -141.828f), new Vector3(99.414f, 0.000f, -141.630f), new Vector3(97.816f, 0.000f, -141.261f), new Vector3(97.033f, 0.000f, -141.016f), new Vector3(95.511f, 0.000f, -140.405f), new Vector3(94.082f, 0.000f, -139.601f), new Vector3(92.786f, 0.000f, -138.596f), new Vector3(91.587f, 0.000f, -137.476f)};

        private static readonly bool[] Circuit2IsArcEdge = {false, false, true, true, true, true, true, true, false, false, false, false, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, false, false, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, false, false, false, false, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, false, true, true, true, true, true, true, true, true, true, true, true, true, true, false, false, false, false, false, false, false, false, false, false, false, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, false, false, false, false, false, false, false, false, false, false, false, false, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, false, false, false, false, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, false, false, false, false, false, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, false, false, false, false, false, true, true, true, true, false, false, false, false, true, true, true, true, true, true, true, true, true, true, true};

        private static readonly float[] Circuit2Widths = {8.500f, 8.500f, 8.500f, 8.500f, 8.500f, 8.500f, 8.500f, 8.500f, 8.500f, 8.500f, 8.500f, 8.500f, 8.500f, 8.500f, 8.500f, 8.500f, 8.500f, 8.500f, 8.500f, 8.500f, 8.500f, 8.500f, 8.500f, 8.500f, 8.460f, 8.340f, 8.210f, 8.090f, 7.970f, 7.840f, 7.720f, 7.600f, 7.480f, 7.110f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 6.000f, 6.000f, 6.000f, 6.000f, 6.000f, 6.000f, 6.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 6.000f, 6.000f, 6.000f, 6.000f, 6.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 6.000f, 6.000f, 6.000f, 6.000f, 6.000f, 6.000f, 6.000f, 6.000f, 6.000f, 6.000f, 6.000f, 6.000f, 6.000f, 6.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 6.000f, 6.000f, 6.000f, 6.000f, 6.000f, 6.000f, 6.000f, 6.000f, 6.000f, 6.000f, 6.000f, 6.000f, 6.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 6.000f, 6.000f, 6.000f, 6.000f, 6.000f, 6.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 6.000f, 6.000f, 6.000f, 6.000f, 6.000f, 6.000f, 6.000f, 6.000f, 6.000f, 6.000f, 6.000f, 6.000f, 6.000f, 6.000f, 6.000f, 6.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f, 7.000f};

        private static readonly Vector3[] Circuit2OuterBarrier = {new Vector3(84.188f, 0.000f, -138.384f), new Vector3(80.536f, 0.000f, -135.329f), new Vector3(76.695f, 0.000f, -132.079f), new Vector3(73.709f, 0.000f, -129.358f), new Vector3(72.824f, 0.000f, -128.729f), new Vector3(71.656f, 0.000f, -127.946f), new Vector3(70.517f, 0.000f, -127.244f), new Vector3(69.574f, 0.000f, -126.736f), new Vector3(65.199f, 0.000f, -124.905f), new Vector3(60.808f, 0.000f, -123.156f), new Vector3(56.581f, 0.000f, -121.711f), new Vector3(52.288f, 0.000f, -120.755f), new Vector3(47.634f, 0.000f, -120.074f), new Vector3(43.488f, 0.000f, -119.468f), new Vector3(41.567f, 0.000f, -119.081f), new Vector3(39.702f, 0.000f, -118.638f), new Vector3(37.685f, 0.000f, -118.017f), new Vector3(35.561f, 0.000f, -117.124f), new Vector3(33.439f, 0.000f, -115.882f), new Vector3(31.472f, 0.000f, -114.289f), new Vector3(29.844f, 0.000f, -112.490f), new Vector3(28.594f, 0.000f, -110.738f), new Vector3(27.598f, 0.000f, -109.134f), new Vector3(25.058f, 0.000f, -104.882f), new Vector3(24.207f, 0.000f, -103.402f), new Vector3(23.376f, 0.000f, -101.819f), new Vector3(22.520f, 0.000f, -100.043f), new Vector3(21.682f, 0.000f, -97.873f), new Vector3(21.105f, 0.000f, -95.043f), new Vector3(21.331f, 0.000f, -91.884f), new Vector3(22.186f, 0.000f, -89.417f), new Vector3(22.982f, 0.000f, -87.751f), new Vector3(23.690f, 0.000f, -86.250f), new Vector3(25.898f, 0.000f, -81.709f), new Vector3(26.929f, 0.000f, -79.921f), new Vector3(27.857f, 0.000f, -78.373f), new Vector3(28.834f, 0.000f, -76.843f), new Vector3(29.961f, 0.000f, -75.235f), new Vector3(31.320f, 0.000f, -73.614f), new Vector3(32.805f, 0.000f, -72.166f), new Vector3(34.249f, 0.000f, -70.946f), new Vector3(38.130f, 0.000f, -67.802f), new Vector3(41.837f, 0.000f, -64.920f), new Vector3(45.021f, 0.000f, -61.815f), new Vector3(48.157f, 0.000f, -58.237f), new Vector3(49.413f, 0.000f, -56.872f), new Vector3(50.792f, 0.000f, -55.521f), new Vector3(52.269f, 0.000f, -54.247f), new Vector3(53.815f, 0.000f, -53.067f), new Vector3(55.471f, 0.000f, -51.960f), new Vector3(57.309f, 0.000f, -50.933f), new Vector3(59.294f, 0.000f, -50.083f), new Vector3(61.285f, 0.000f, -49.456f), new Vector3(63.224f, 0.000f, -49.011f), new Vector3(65.119f, 0.000f, -48.695f), new Vector3(67.001f, 0.000f, -48.482f), new Vector3(68.887f, 0.000f, -48.363f), new Vector3(70.956f, 0.000f, -48.343f), new Vector3(76.007f, 0.000f, -48.684f), new Vector3(80.874f, 0.000f, -49.091f), new Vector3(84.852f, 0.000f, -49.286f), new Vector3(86.222f, 0.000f, -49.331f), new Vector3(87.431f, 0.000f, -49.270f), new Vector3(88.634f, 0.000f, -49.097f), new Vector3(89.853f, 0.000f, -48.823f), new Vector3(91.075f, 0.000f, -48.446f), new Vector3(92.312f, 0.000f, -47.971f), new Vector3(93.109f, 0.000f, -47.653f), new Vector3(96.972f, 0.000f, -45.033f), new Vector3(98.600f, 0.000f, -43.895f), new Vector3(100.680f, 0.000f, -42.890f), new Vector3(102.273f, 0.000f, -42.384f), new Vector3(104.320f, 0.000f, -41.996f), new Vector3(106.358f, 0.000f, -41.813f), new Vector3(108.241f, 0.000f, -41.762f), new Vector3(113.164f, 0.000f, -41.750f), new Vector3(114.563f, 0.000f, -41.714f), new Vector3(115.828f, 0.000f, -41.607f), new Vector3(116.976f, 0.000f, -41.405f), new Vector3(118.095f, 0.000f, -41.086f), new Vector3(119.318f, 0.000f, -40.613f), new Vector3(120.737f, 0.000f, -39.997f), new Vector3(122.286f, 0.000f, -39.335f), new Vector3(123.730f, 0.000f, -38.741f), new Vector3(124.881f, 0.000f, -38.211f), new Vector3(125.587f, 0.000f, -37.817f), new Vector3(129.338f, 0.000f, -34.701f), new Vector3(133.513f, 0.000f, -31.626f), new Vector3(137.539f, 0.000f, -28.951f), new Vector3(141.204f, 0.000f, -26.038f), new Vector3(144.981f, 0.000f, -22.966f), new Vector3(148.611f, 0.000f, -19.962f), new Vector3(152.078f, 0.000f, -16.625f), new Vector3(153.553f, 0.000f, -15.359f), new Vector3(155.045f, 0.000f, -14.170f), new Vector3(156.859f, 0.000f, -12.941f), new Vector3(159.009f, 0.000f, -11.895f), new Vector3(161.160f, 0.000f, -11.221f), new Vector3(163.184f, 0.000f, -10.793f), new Vector3(165.290f, 0.000f, -10.526f), new Vector3(167.451f, 0.000f, -10.491f), new Vector3(169.464f, 0.000f, -10.651f), new Vector3(171.369f, 0.000f, -10.912f), new Vector3(173.237f, 0.000f, -11.791f), new Vector3(175.360f, 0.000f, -12.436f), new Vector3(177.605f, 0.000f, -13.539f), new Vector3(179.754f, 0.000f, -15.278f), new Vector3(181.390f, 0.000f, -17.569f), new Vector3(182.316f, 0.000f, -19.888f), new Vector3(182.815f, 0.000f, -21.943f), new Vector3(183.623f, 0.000f, -23.814f), new Vector3(183.796f, 0.000f, -25.645f), new Vector3(184.195f, 0.000f, -30.386f), new Vector3(184.926f, 0.000f, -35.037f), new Vector3(185.178f, 0.000f, -36.393f), new Vector3(185.493f, 0.000f, -37.657f), new Vector3(185.904f, 0.000f, -38.893f), new Vector3(186.408f, 0.000f, -40.150f), new Vector3(187.017f, 0.000f, -41.460f), new Vector3(187.705f, 0.000f, -42.849f), new Vector3(188.369f, 0.000f, -44.159f), new Vector3(188.960f, 0.000f, -45.186f), new Vector3(189.558f, 0.000f, -45.970f), new Vector3(190.281f, 0.000f, -46.683f), new Vector3(191.176f, 0.000f, -47.383f), new Vector3(192.266f, 0.000f, -48.093f), new Vector3(193.579f, 0.000f, -48.862f), new Vector3(195.005f, 0.000f, -49.701f), new Vector3(196.314f, 0.000f, -50.481f), new Vector3(197.364f, 0.000f, -51.043f), new Vector3(198.251f, 0.000f, -51.378f), new Vector3(199.297f, 0.000f, -51.588f), new Vector3(200.644f, 0.000f, -51.758f), new Vector3(202.100f, 0.000f, -51.896f), new Vector3(203.270f, 0.000f, -51.974f), new Vector3(208.088f, 0.000f, -51.763f), new Vector3(213.115f, 0.000f, -51.600f), new Vector3(218.085f, 0.000f, -51.598f), new Vector3(222.180f, 0.000f, -51.535f), new Vector3(224.035f, 0.000f, -51.550f), new Vector3(226.011f, 0.000f, -51.672f), new Vector3(227.990f, 0.000f, -51.956f), new Vector3(229.422f, 0.000f, -52.301f), new Vector3(231.436f, 0.000f, -53.020f), new Vector3(233.405f, 0.000f, -54.013f), new Vector3(235.013f, 0.000f, -55.026f), new Vector3(239.099f, 0.000f, -57.648f), new Vector3(240.525f, 0.000f, -58.451f), new Vector3(242.320f, 0.000f, -59.680f), new Vector3(244.012f, 0.000f, -61.184f), new Vector3(247.339f, 0.000f, -64.663f), new Vector3(250.736f, 0.000f, -67.665f), new Vector3(254.264f, 0.000f, -70.362f), new Vector3(258.334f, 0.000f, -72.748f), new Vector3(262.594f, 0.000f, -75.475f), new Vector3(266.888f, 0.000f, -78.329f), new Vector3(270.828f, 0.000f, -81.401f), new Vector3(274.836f, 0.000f, -84.235f), new Vector3(277.237f, 0.000f, -86.189f), new Vector3(280.781f, 0.000f, -89.878f), new Vector3(284.111f, 0.000f, -93.302f), new Vector3(287.586f, 0.000f, -96.529f), new Vector3(289.280f, 0.000f, -98.000f), new Vector3(290.322f, 0.000f, -98.812f), new Vector3(291.206f, 0.000f, -99.431f), new Vector3(295.412f, 0.000f, -101.676f), new Vector3(299.833f, 0.000f, -103.994f), new Vector3(301.606f, 0.000f, -105.086f), new Vector3(303.218f, 0.000f, -106.246f), new Vector3(304.719f, 0.000f, -107.488f), new Vector3(306.103f, 0.000f, -108.768f), new Vector3(307.381f, 0.000f, -110.056f), new Vector3(308.156f, 0.000f, -110.905f), new Vector3(309.435f, 0.000f, -112.492f), new Vector3(310.632f, 0.000f, -114.390f), new Vector3(311.527f, 0.000f, -116.274f), new Vector3(313.440f, 0.000f, -120.760f), new Vector3(315.529f, 0.000f, -125.192f), new Vector3(316.305f, 0.000f, -127.107f), new Vector3(316.916f, 0.000f, -129.100f), new Vector3(317.340f, 0.000f, -131.106f), new Vector3(317.608f, 0.000f, -133.104f), new Vector3(317.725f, 0.000f, -135.233f), new Vector3(317.585f, 0.000f, -137.460f), new Vector3(317.222f, 0.000f, -139.499f), new Vector3(316.785f, 0.000f, -141.326f), new Vector3(316.259f, 0.000f, -143.185f), new Vector3(315.526f, 0.000f, -145.170f), new Vector3(314.086f, 0.000f, -146.924f), new Vector3(312.914f, 0.000f, -148.669f), new Vector3(311.952f, 0.000f, -149.795f), new Vector3(310.436f, 0.000f, -151.216f), new Vector3(308.707f, 0.000f, -152.487f), new Vector3(307.120f, 0.000f, -153.997f), new Vector3(305.180f, 0.000f, -154.878f), new Vector3(303.245f, 0.000f, -155.553f), new Vector3(301.307f, 0.000f, -156.072f), new Vector3(299.017f, 0.000f, -156.486f), new Vector3(294.173f, 0.000f, -156.746f), new Vector3(289.657f, 0.000f, -157.427f), new Vector3(284.641f, 0.000f, -158.390f), new Vector3(279.264f, 0.000f, -158.784f), new Vector3(274.160f, 0.000f, -158.803f), new Vector3(269.229f, 0.000f, -158.803f), new Vector3(264.305f, 0.000f, -158.803f), new Vector3(259.259f, 0.000f, -158.800f), new Vector3(254.163f, 0.000f, -158.552f), new Vector3(249.052f, 0.000f, -158.179f), new Vector3(243.895f, 0.000f, -157.508f), new Vector3(238.833f, 0.000f, -156.569f), new Vector3(233.873f, 0.000f, -155.461f), new Vector3(228.977f, 0.000f, -154.224f), new Vector3(227.174f, 0.000f, -153.683f), new Vector3(225.413f, 0.000f, -153.094f), new Vector3(223.511f, 0.000f, -152.331f), new Vector3(221.669f, 0.000f, -151.364f), new Vector3(220.052f, 0.000f, -150.364f), new Vector3(218.472f, 0.000f, -149.305f), new Vector3(217.090f, 0.000f, -147.590f), new Vector3(215.437f, 0.000f, -145.858f), new Vector3(214.075f, 0.000f, -143.755f), new Vector3(213.153f, 0.000f, -141.454f), new Vector3(212.669f, 0.000f, -139.093f), new Vector3(212.580f, 0.000f, -136.769f), new Vector3(212.809f, 0.000f, -134.549f), new Vector3(213.305f, 0.000f, -132.404f), new Vector3(214.069f, 0.000f, -130.334f), new Vector3(215.054f, 0.000f, -128.429f), new Vector3(216.158f, 0.000f, -126.732f), new Vector3(217.377f, 0.000f, -125.130f), new Vector3(218.861f, 0.000f, -123.527f), new Vector3(220.747f, 0.000f, -122.017f), new Vector3(222.719f, 0.000f, -120.416f), new Vector3(224.858f, 0.000f, -119.697f), new Vector3(226.724f, 0.000f, -119.248f), new Vector3(231.534f, 0.000f, -118.190f), new Vector3(232.862f, 0.000f, -117.879f), new Vector3(234.013f, 0.000f, -117.499f), new Vector3(235.234f, 0.000f, -116.972f), new Vector3(236.597f, 0.000f, -116.324f), new Vector3(237.955f, 0.000f, -115.657f), new Vector3(239.369f, 0.000f, -115.467f), new Vector3(240.241f, 0.000f, -114.898f), new Vector3(240.754f, 0.000f, -114.434f), new Vector3(241.016f, 0.000f, -114.022f), new Vector3(241.190f, 0.000f, -113.385f), new Vector3(241.303f, 0.000f, -112.418f), new Vector3(241.305f, 0.000f, -111.331f), new Vector3(241.179f, 0.000f, -110.277f), new Vector3(240.977f, 0.000f, -109.444f), new Vector3(240.814f, 0.000f, -109.048f), new Vector3(240.653f, 0.000f, -108.900f), new Vector3(240.148f, 0.000f, -108.681f), new Vector3(239.151f, 0.000f, -108.396f), new Vector3(237.744f, 0.000f, -108.620f), new Vector3(236.208f, 0.000f, -108.375f), new Vector3(231.334f, 0.000f, -107.561f), new Vector3(226.358f, 0.000f, -106.706f), new Vector3(221.397f, 0.000f, -105.620f), new Vector3(216.792f, 0.000f, -104.527f), new Vector3(212.055f, 0.000f, -103.865f), new Vector3(207.036f, 0.000f, -102.972f), new Vector3(204.376f, 0.000f, -102.360f), new Vector3(202.501f, 0.000f, -101.828f), new Vector3(200.659f, 0.000f, -101.177f), new Vector3(198.934f, 0.000f, -100.446f), new Vector3(197.351f, 0.000f, -99.715f), new Vector3(195.758f, 0.000f, -98.959f), new Vector3(194.026f, 0.000f, -98.049f), new Vector3(192.230f, 0.000f, -96.897f), new Vector3(190.549f, 0.000f, -95.542f), new Vector3(189.077f, 0.000f, -94.101f), new Vector3(187.805f, 0.000f, -92.664f), new Vector3(186.648f, 0.000f, -91.242f), new Vector3(185.368f, 0.000f, -89.521f), new Vector3(182.771f, 0.000f, -85.159f), new Vector3(180.302f, 0.000f, -80.926f), new Vector3(179.501f, 0.000f, -79.656f), new Vector3(178.706f, 0.000f, -78.404f), new Vector3(178.034f, 0.000f, -77.456f), new Vector3(177.265f, 0.000f, -76.655f), new Vector3(177.161f, 0.000f, -76.577f), new Vector3(176.077f, 0.000f, -75.826f), new Vector3(174.913f, 0.000f, -75.137f), new Vector3(173.705f, 0.000f, -74.491f), new Vector3(172.499f, 0.000f, -73.942f), new Vector3(171.172f, 0.000f, -73.439f), new Vector3(168.164f, 0.000f, -72.358f), new Vector3(166.849f, 0.000f, -71.933f), new Vector3(165.508f, 0.000f, -71.589f), new Vector3(164.094f, 0.000f, -71.288f), new Vector3(162.772f, 0.000f, -71.052f), new Vector3(161.644f, 0.000f, -70.945f), new Vector3(160.493f, 0.000f, -70.984f), new Vector3(159.080f, 0.000f, -71.137f), new Vector3(154.298f, 0.000f, -71.678f), new Vector3(152.144f, 0.000f, -72.060f), new Vector3(150.762f, 0.000f, -72.353f), new Vector3(149.440f, 0.000f, -72.701f), new Vector3(148.212f, 0.000f, -73.104f), new Vector3(147.111f, 0.000f, -73.564f), new Vector3(146.074f, 0.000f, -74.129f), new Vector3(145.004f, 0.000f, -74.835f), new Vector3(141.057f, 0.000f, -77.661f), new Vector3(139.153f, 0.000f, -79.057f), new Vector3(138.058f, 0.000f, -79.913f), new Vector3(137.104f, 0.000f, -80.759f), new Vector3(136.268f, 0.000f, -81.637f), new Vector3(135.522f, 0.000f, -82.573f), new Vector3(134.872f, 0.000f, -83.557f), new Vector3(134.334f, 0.000f, -84.571f), new Vector3(133.909f, 0.000f, -85.616f), new Vector3(133.592f, 0.000f, -86.710f), new Vector3(133.365f, 0.000f, -87.938f), new Vector3(133.191f, 0.000f, -89.329f), new Vector3(133.060f, 0.000f, -90.777f), new Vector3(132.987f, 0.000f, -92.227f), new Vector3(132.965f, 0.000f, -93.732f), new Vector3(132.971f, 0.000f, -95.238f), new Vector3(133.019f, 0.000f, -96.578f), new Vector3(132.652f, 0.000f, -97.728f), new Vector3(132.869f, 0.000f, -98.549f), new Vector3(133.188f, 0.000f, -99.172f), new Vector3(133.720f, 0.000f, -99.798f), new Vector3(134.546f, 0.000f, -100.528f), new Vector3(135.578f, 0.000f, -101.299f), new Vector3(136.892f, 0.000f, -101.557f), new Vector3(137.834f, 0.000f, -102.050f), new Vector3(138.834f, 0.000f, -102.402f), new Vector3(140.112f, 0.000f, -102.716f), new Vector3(141.515f, 0.000f, -103.020f), new Vector3(146.356f, 0.000f, -103.899f), new Vector3(151.530f, 0.000f, -104.931f), new Vector3(156.595f, 0.000f, -106.606f), new Vector3(161.068f, 0.000f, -108.304f), new Vector3(165.840f, 0.000f, -109.604f), new Vector3(170.706f, 0.000f, -111.504f), new Vector3(175.337f, 0.000f, -113.414f), new Vector3(178.516f, 0.000f, -114.810f), new Vector3(180.236f, 0.000f, -115.716f), new Vector3(181.801f, 0.000f, -116.592f), new Vector3(183.192f, 0.000f, -118.033f), new Vector3(184.964f, 0.000f, -119.361f), new Vector3(186.704f, 0.000f, -121.186f), new Vector3(187.961f, 0.000f, -123.245f), new Vector3(188.589f, 0.000f, -124.858f), new Vector3(189.074f, 0.000f, -126.900f), new Vector3(189.326f, 0.000f, -129.063f), new Vector3(189.312f, 0.000f, -131.318f), new Vector3(188.939f, 0.000f, -133.646f), new Vector3(188.181f, 0.000f, -135.909f), new Vector3(187.072f, 0.000f, -137.989f), new Vector3(185.727f, 0.000f, -139.795f), new Vector3(184.232f, 0.000f, -141.360f), new Vector3(182.517f, 0.000f, -142.783f), new Vector3(180.473f, 0.000f, -144.017f), new Vector3(178.210f, 0.000f, -144.879f), new Vector3(176.105f, 0.000f, -145.820f), new Vector3(174.156f, 0.000f, -146.024f), new Vector3(172.425f, 0.000f, -146.146f), new Vector3(167.351f, 0.000f, -146.479f), new Vector3(162.263f, 0.000f, -146.498f), new Vector3(157.278f, 0.000f, -146.498f), new Vector3(152.142f, 0.000f, -146.387f), new Vector3(146.871f, 0.000f, -145.930f), new Vector3(141.808f, 0.000f, -145.075f), new Vector3(137.031f, 0.000f, -144.249f), new Vector3(135.787f, 0.000f, -144.122f), new Vector3(134.390f, 0.000f, -144.055f), new Vector3(133.261f, 0.000f, -144.030f), new Vector3(128.554f, 0.000f, -144.513f), new Vector3(123.719f, 0.000f, -145.100f), new Vector3(118.781f, 0.000f, -145.709f), new Vector3(113.664f, 0.000f, -146.221f), new Vector3(108.707f, 0.000f, -146.394f), new Vector3(104.720f, 0.000f, -146.758f), new Vector3(102.765f, 0.000f, -146.861f), new Vector3(100.688f, 0.000f, -146.815f), new Vector3(98.549f, 0.000f, -146.554f), new Vector3(96.568f, 0.000f, -146.103f), new Vector3(95.293f, 0.000f, -145.704f), new Vector3(93.349f, 0.000f, -144.914f), new Vector3(91.315f, 0.000f, -143.766f), new Vector3(89.543f, 0.000f, -142.402f), new Vector3(88.155f, 0.000f, -141.112f)};

        private static readonly Vector3[] Circuit2InnerBarrier = {new Vector3(91.837f, 0.000f, -129.797f), new Vector3(87.941f, 0.000f, -126.531f), new Vector3(84.267f, 0.000f, -123.424f), new Vector3(81.189f, 0.000f, -120.623f), new Vector3(79.427f, 0.000f, -119.313f), new Vector3(77.868f, 0.000f, -118.268f), new Vector3(76.210f, 0.000f, -117.253f), new Vector3(74.251f, 0.000f, -116.230f), new Vector3(69.546f, 0.000f, -114.258f), new Vector3(64.787f, 0.000f, -112.367f), new Vector3(59.694f, 0.000f, -110.640f), new Vector3(54.382f, 0.000f, -109.447f), new Vector3(49.301f, 0.000f, -108.695f), new Vector3(45.331f, 0.000f, -108.117f), new Vector3(44.036f, 0.000f, -107.849f), new Vector3(42.707f, 0.000f, -107.538f), new Vector3(41.587f, 0.000f, -107.199f), new Vector3(40.683f, 0.000f, -106.828f), new Vector3(39.972f, 0.000f, -106.418f), new Vector3(39.390f, 0.000f, -105.949f), new Vector3(38.826f, 0.000f, -105.308f), new Vector3(38.179f, 0.000f, -104.384f), new Vector3(37.447f, 0.000f, -103.197f), new Vector3(34.937f, 0.000f, -98.996f), new Vector3(34.118f, 0.000f, -97.650f), new Vector3(33.323f, 0.000f, -96.382f), new Vector3(32.657f, 0.000f, -95.251f), new Vector3(32.214f, 0.000f, -94.400f), new Vector3(32.026f, 0.000f, -94.047f), new Vector3(31.978f, 0.000f, -93.944f), new Vector3(32.177f, 0.000f, -93.307f), new Vector3(32.705f, 0.000f, -91.970f), new Vector3(33.286f, 0.000f, -90.452f), new Vector3(35.054f, 0.000f, -85.989f), new Vector3(35.601f, 0.000f, -84.900f), new Vector3(36.362f, 0.000f, -83.633f), new Vector3(37.150f, 0.000f, -82.396f), new Vector3(37.902f, 0.000f, -81.313f), new Vector3(38.649f, 0.000f, -80.418f), new Vector3(39.516f, 0.000f, -79.580f), new Vector3(40.582f, 0.000f, -78.685f), new Vector3(44.352f, 0.000f, -75.630f), new Vector3(48.410f, 0.000f, -72.457f), new Vector3(52.272f, 0.000f, -68.701f), new Vector3(55.633f, 0.000f, -64.879f), new Vector3(56.598f, 0.000f, -63.828f), new Vector3(57.562f, 0.000f, -62.881f), new Vector3(58.570f, 0.000f, -62.012f), new Vector3(59.633f, 0.000f, -61.201f), new Vector3(60.704f, 0.000f, -60.482f), new Vector3(61.728f, 0.000f, -59.903f), new Vector3(62.759f, 0.000f, -59.464f), new Vector3(63.899f, 0.000f, -59.109f), new Vector3(65.159f, 0.000f, -58.822f), new Vector3(66.501f, 0.000f, -58.600f), new Vector3(67.880f, 0.000f, -58.444f), new Vector3(69.270f, 0.000f, -58.355f), new Vector3(70.482f, 0.000f, -58.332f), new Vector3(75.253f, 0.000f, -58.656f), new Vector3(80.198f, 0.000f, -59.068f), new Vector3(84.416f, 0.000f, -59.276f), new Vector3(86.326f, 0.000f, -59.330f), new Vector3(88.394f, 0.000f, -59.224f), new Vector3(90.439f, 0.000f, -58.933f), new Vector3(92.422f, 0.000f, -58.487f), new Vector3(94.336f, 0.000f, -57.899f), new Vector3(96.163f, 0.000f, -57.200f), new Vector3(98.357f, 0.000f, -56.165f), new Vector3(102.627f, 0.000f, -53.280f), new Vector3(103.670f, 0.000f, -52.515f), new Vector3(104.548f, 0.000f, -52.112f), new Vector3(104.522f, 0.000f, -52.128f), new Vector3(105.696f, 0.000f, -51.901f), new Vector3(106.927f, 0.000f, -51.797f), new Vector3(108.325f, 0.000f, -51.761f), new Vector3(113.249f, 0.000f, -51.750f), new Vector3(115.131f, 0.000f, -51.698f), new Vector3(117.135f, 0.000f, -51.521f), new Vector3(119.217f, 0.000f, -51.151f), new Vector3(121.254f, 0.000f, -50.574f), new Vector3(123.096f, 0.000f, -49.872f), new Vector3(124.689f, 0.000f, -49.183f), new Vector3(126.158f, 0.000f, -48.555f), new Vector3(127.748f, 0.000f, -47.898f), new Vector3(129.572f, 0.000f, -47.043f), new Vector3(131.678f, 0.000f, -45.748f), new Vector3(135.499f, 0.000f, -42.577f), new Vector3(139.249f, 0.000f, -39.818f), new Vector3(143.421f, 0.000f, -37.038f), new Vector3(147.466f, 0.000f, -33.835f), new Vector3(151.326f, 0.000f, -30.695f), new Vector3(155.272f, 0.000f, -27.421f), new Vector3(158.902f, 0.000f, -23.934f), new Vector3(159.923f, 0.000f, -23.068f), new Vector3(160.994f, 0.000f, -22.208f), new Vector3(161.887f, 0.000f, -21.585f), new Vector3(162.684f, 0.000f, -21.195f), new Vector3(163.668f, 0.000f, -20.902f), new Vector3(164.855f, 0.000f, -20.652f), new Vector3(166.003f, 0.000f, -20.501f), new Vector3(167.122f, 0.000f, -20.486f), new Vector3(168.380f, 0.000f, -20.592f), new Vector3(169.727f, 0.000f, -20.776f), new Vector3(171.079f, 0.000f, -20.529f), new Vector3(172.098f, 0.000f, -20.824f), new Vector3(172.802f, 0.000f, -21.150f), new Vector3(173.209f, 0.000f, -21.456f), new Vector3(173.465f, 0.000f, -21.833f), new Vector3(173.725f, 0.000f, -22.569f), new Vector3(173.989f, 0.000f, -23.705f), new Vector3(173.702f, 0.000f, -25.074f), new Vector3(173.834f, 0.000f, -26.511f), new Vector3(174.267f, 0.000f, -31.581f), new Vector3(175.057f, 0.000f, -36.656f), new Vector3(175.408f, 0.000f, -38.527f), new Vector3(175.890f, 0.000f, -40.446f), new Vector3(176.511f, 0.000f, -42.325f), new Vector3(177.227f, 0.000f, -44.115f), new Vector3(177.999f, 0.000f, -45.782f), new Vector3(178.767f, 0.000f, -47.334f), new Vector3(179.592f, 0.000f, -48.950f), new Vector3(180.655f, 0.000f, -50.756f), new Vector3(182.050f, 0.000f, -52.575f), new Vector3(183.656f, 0.000f, -54.173f), new Vector3(185.338f, 0.000f, -55.502f), new Vector3(186.994f, 0.000f, -56.590f), new Vector3(188.512f, 0.000f, -57.483f), new Vector3(189.915f, 0.000f, -58.308f), new Vector3(191.427f, 0.000f, -59.206f), new Vector3(193.280f, 0.000f, -60.171f), new Vector3(195.466f, 0.000f, -60.982f), new Vector3(197.629f, 0.000f, -61.448f), new Vector3(199.537f, 0.000f, -61.696f), new Vector3(201.349f, 0.000f, -61.867f), new Vector3(203.456f, 0.000f, -61.973f), new Vector3(208.468f, 0.000f, -61.756f), new Vector3(213.280f, 0.000f, -61.599f), new Vector3(218.157f, 0.000f, -61.597f), new Vector3(222.267f, 0.000f, -61.534f), new Vector3(223.694f, 0.000f, -61.545f), new Vector3(224.994f, 0.000f, -61.620f), new Vector3(226.261f, 0.000f, -61.806f), new Vector3(226.425f, 0.000f, -61.842f), new Vector3(227.503f, 0.000f, -62.214f), new Vector3(228.460f, 0.000f, -62.705f), new Vector3(229.624f, 0.000f, -63.449f), new Vector3(233.820f, 0.000f, -66.141f), new Vector3(235.253f, 0.000f, -66.949f), new Vector3(236.170f, 0.000f, -67.565f), new Vector3(236.928f, 0.000f, -68.242f), new Vector3(240.410f, 0.000f, -71.874f), new Vector3(244.389f, 0.000f, -75.392f), new Vector3(248.687f, 0.000f, -78.662f), new Vector3(253.102f, 0.000f, -81.269f), new Vector3(257.132f, 0.000f, -83.851f), new Vector3(261.042f, 0.000f, -86.443f), new Vector3(264.863f, 0.000f, -89.427f), new Vector3(268.891f, 0.000f, -92.277f), new Vector3(270.320f, 0.000f, -93.411f), new Vector3(273.588f, 0.000f, -96.826f), new Vector3(277.122f, 0.000f, -100.455f), new Vector3(280.862f, 0.000f, -103.930f), new Vector3(282.884f, 0.000f, -105.688f), new Vector3(284.431f, 0.000f, -106.892f), new Vector3(286.258f, 0.000f, -108.122f), new Vector3(290.735f, 0.000f, -110.515f), new Vector3(295.034f, 0.000f, -112.768f), new Vector3(296.052f, 0.000f, -113.402f), new Vector3(297.104f, 0.000f, -114.159f), new Vector3(298.130f, 0.000f, -115.011f), new Vector3(299.154f, 0.000f, -115.959f), new Vector3(300.187f, 0.000f, -117.003f), new Vector3(300.521f, 0.000f, -117.363f), new Vector3(301.310f, 0.000f, -118.323f), new Vector3(301.864f, 0.000f, -119.198f), new Vector3(302.366f, 0.000f, -120.282f), new Vector3(304.317f, 0.000f, -124.854f), new Vector3(306.425f, 0.000f, -129.329f), new Vector3(306.882f, 0.000f, -130.454f), new Vector3(307.232f, 0.000f, -131.598f), new Vector3(307.485f, 0.000f, -132.804f), new Vector3(307.654f, 0.000f, -134.058f), new Vector3(307.725f, 0.000f, -135.205f), new Vector3(307.658f, 0.000f, -136.251f), new Vector3(307.436f, 0.000f, -137.440f), new Vector3(307.108f, 0.000f, -138.805f), new Vector3(306.745f, 0.000f, -140.105f), new Vector3(306.348f, 0.000f, -141.200f), new Vector3(306.318f, 0.000f, -142.378f), new Vector3(305.654f, 0.000f, -143.350f), new Vector3(305.549f, 0.000f, -143.470f), new Vector3(304.678f, 0.000f, -144.299f), new Vector3(303.761f, 0.000f, -144.968f), new Vector3(302.519f, 0.000f, -145.118f), new Vector3(301.471f, 0.000f, -145.591f), new Vector3(300.307f, 0.000f, -145.994f), new Vector3(299.075f, 0.000f, -146.324f), new Vector3(298.141f, 0.000f, -146.525f), new Vector3(293.161f, 0.000f, -146.798f), new Vector3(287.966f, 0.000f, -147.571f), new Vector3(283.329f, 0.000f, -148.476f), new Vector3(278.888f, 0.000f, -148.791f), new Vector3(274.145f, 0.000f, -148.803f), new Vector3(269.229f, 0.000f, -148.803f), new Vector3(264.306f, 0.000f, -148.803f), new Vector3(259.505f, 0.000f, -148.803f), new Vector3(254.769f, 0.000f, -148.571f), new Vector3(250.060f, 0.000f, -148.230f), new Vector3(245.454f, 0.000f, -147.630f), new Vector3(240.837f, 0.000f, -146.772f), new Vector3(236.188f, 0.000f, -145.732f), new Vector3(231.537f, 0.000f, -144.557f), new Vector3(230.198f, 0.000f, -144.151f), new Vector3(228.846f, 0.000f, -143.702f), new Vector3(227.700f, 0.000f, -143.250f), new Vector3(226.639f, 0.000f, -142.687f), new Vector3(225.467f, 0.000f, -141.957f), new Vector3(224.318f, 0.000f, -141.192f), new Vector3(223.108f, 0.000f, -140.897f), new Vector3(222.489f, 0.000f, -140.266f), new Vector3(222.069f, 0.000f, -139.619f), new Vector3(221.776f, 0.000f, -138.877f), new Vector3(221.605f, 0.000f, -138.026f), new Vector3(221.575f, 0.000f, -137.073f), new Vector3(221.686f, 0.000f, -136.032f), new Vector3(221.929f, 0.000f, -134.979f), new Vector3(222.300f, 0.000f, -133.973f), new Vector3(222.826f, 0.000f, -132.967f), new Vector3(223.515f, 0.000f, -131.916f), new Vector3(224.281f, 0.000f, -130.904f), new Vector3(225.017f, 0.000f, -130.092f), new Vector3(225.681f, 0.000f, -129.543f), new Vector3(226.614f, 0.000f, -129.627f), new Vector3(227.589f, 0.000f, -129.317f), new Vector3(228.916f, 0.000f, -129.005f), new Vector3(233.723f, 0.000f, -127.948f), new Vector3(235.588f, 0.000f, -127.501f), new Vector3(237.553f, 0.000f, -126.852f), new Vector3(239.348f, 0.000f, -126.087f), new Vector3(240.950f, 0.000f, -125.327f), new Vector3(242.537f, 0.000f, -124.545f), new Vector3(244.011f, 0.000f, -123.177f), new Vector3(245.867f, 0.000f, -121.923f), new Vector3(247.724f, 0.000f, -120.128f), new Vector3(249.200f, 0.000f, -117.769f), new Vector3(249.989f, 0.000f, -115.276f), new Vector3(250.285f, 0.000f, -112.989f), new Vector3(250.289f, 0.000f, -110.796f), new Vector3(250.020f, 0.000f, -108.594f), new Vector3(249.393f, 0.000f, -106.254f), new Vector3(248.077f, 0.000f, -103.733f), new Vector3(245.908f, 0.000f, -101.594f), new Vector3(243.497f, 0.000f, -100.327f), new Vector3(241.353f, 0.000f, -99.670f), new Vector3(239.545f, 0.000f, -98.784f), new Vector3(237.840f, 0.000f, -98.509f), new Vector3(233.003f, 0.000f, -97.701f), new Vector3(228.274f, 0.000f, -96.892f), new Vector3(223.617f, 0.000f, -95.870f), new Vector3(218.640f, 0.000f, -94.700f), new Vector3(213.628f, 0.000f, -93.989f), new Vector3(208.954f, 0.000f, -93.157f), new Vector3(206.817f, 0.000f, -92.662f), new Vector3(205.534f, 0.000f, -92.299f), new Vector3(204.282f, 0.000f, -91.856f), new Vector3(202.987f, 0.000f, -91.304f), new Vector3(201.590f, 0.000f, -90.658f), new Vector3(200.216f, 0.000f, -90.008f), new Vector3(199.040f, 0.000f, -89.397f), new Vector3(198.073f, 0.000f, -88.782f), new Vector3(197.202f, 0.000f, -88.077f), new Vector3(196.332f, 0.000f, -87.219f), new Vector3(195.431f, 0.000f, -86.196f), new Vector3(194.517f, 0.000f, -85.072f), new Vector3(193.819f, 0.000f, -84.174f), new Vector3(191.388f, 0.000f, -80.083f), new Vector3(188.897f, 0.000f, -75.814f), new Vector3(187.950f, 0.000f, -74.306f), new Vector3(186.982f, 0.000f, -72.790f), new Vector3(185.735f, 0.000f, -71.077f), new Vector3(184.249f, 0.000f, -69.498f), new Vector3(183.092f, 0.000f, -68.526f), new Vector3(181.478f, 0.000f, -67.410f), new Vector3(179.821f, 0.000f, -66.424f), new Vector3(178.133f, 0.000f, -65.525f), new Vector3(176.352f, 0.000f, -64.714f), new Vector3(174.612f, 0.000f, -64.049f), new Vector3(171.441f, 0.000f, -62.910f), new Vector3(169.633f, 0.000f, -62.329f), new Vector3(167.797f, 0.000f, -61.854f), new Vector3(166.000f, 0.000f, -61.471f), new Vector3(164.090f, 0.000f, -61.140f), new Vector3(161.951f, 0.000f, -60.950f), new Vector3(159.823f, 0.000f, -61.007f), new Vector3(157.972f, 0.000f, -61.198f), new Vector3(152.970f, 0.000f, -61.767f), new Vector3(150.276f, 0.000f, -62.236f), new Vector3(148.447f, 0.000f, -62.624f), new Vector3(146.596f, 0.000f, -63.114f), new Vector3(144.708f, 0.000f, -63.738f), new Vector3(142.783f, 0.000f, -64.549f), new Vector3(140.937f, 0.000f, -65.550f), new Vector3(139.264f, 0.000f, -66.647f), new Vector3(135.205f, 0.000f, -69.553f), new Vector3(133.139f, 0.000f, -71.068f), new Vector3(131.650f, 0.000f, -72.236f), new Vector3(130.152f, 0.000f, -73.571f), new Vector3(128.727f, 0.000f, -75.070f), new Vector3(127.429f, 0.000f, -76.700f), new Vector3(126.271f, 0.000f, -78.456f), new Vector3(125.273f, 0.000f, -80.340f), new Vector3(124.462f, 0.000f, -82.334f), new Vector3(123.865f, 0.000f, -84.390f), new Vector3(123.486f, 0.000f, -86.388f), new Vector3(123.249f, 0.000f, -88.253f), new Vector3(123.085f, 0.000f, -90.074f), new Vector3(122.992f, 0.000f, -91.901f), new Vector3(122.965f, 0.000f, -93.679f), new Vector3(122.973f, 0.000f, -95.454f), new Vector3(123.053f, 0.000f, -97.394f), new Vector3(123.828f, 0.000f, -99.499f), new Vector3(124.490f, 0.000f, -101.835f), new Vector3(125.672f, 0.000f, -104.123f), new Vector3(127.223f, 0.000f, -106.027f), new Vector3(128.839f, 0.000f, -107.487f), new Vector3(130.433f, 0.000f, -108.683f), new Vector3(131.875f, 0.000f, -110.208f), new Vector3(133.847f, 0.000f, -111.220f), new Vector3(135.935f, 0.000f, -111.973f), new Vector3(137.842f, 0.000f, -112.455f), new Vector3(139.647f, 0.000f, -112.844f), new Vector3(144.493f, 0.000f, -113.724f), new Vector3(148.978f, 0.000f, -114.600f), new Vector3(153.246f, 0.000f, -116.029f), new Vector3(157.982f, 0.000f, -117.816f), new Vector3(162.698f, 0.000f, -119.098f), new Vector3(166.975f, 0.000f, -120.782f), new Vector3(171.443f, 0.000f, -122.625f), new Vector3(174.272f, 0.000f, -123.865f), new Vector3(175.451f, 0.000f, -124.497f), new Vector3(176.752f, 0.000f, -125.223f), new Vector3(178.159f, 0.000f, -125.494f), new Vector3(179.022f, 0.000f, -126.120f), new Vector3(179.556f, 0.000f, -126.655f), new Vector3(179.980f, 0.000f, -127.404f), new Vector3(179.933f, 0.000f, -127.325f), new Vector3(180.213f, 0.000f, -128.472f), new Vector3(180.340f, 0.000f, -129.567f), new Vector3(180.341f, 0.000f, -130.591f), new Vector3(180.198f, 0.000f, -131.501f), new Vector3(179.916f, 0.000f, -132.347f), new Vector3(179.479f, 0.000f, -133.158f), new Vector3(178.859f, 0.000f, -133.978f), new Vector3(178.087f, 0.000f, -134.784f), new Vector3(177.283f, 0.000f, -135.461f), new Vector3(176.525f, 0.000f, -135.929f), new Vector3(175.724f, 0.000f, -136.229f), new Vector3(174.614f, 0.000f, -135.932f), new Vector3(173.298f, 0.000f, -136.061f), new Vector3(171.755f, 0.000f, -136.169f), new Vector3(167.005f, 0.000f, -136.485f), new Vector3(162.246f, 0.000f, -136.498f), new Vector3(157.384f, 0.000f, -136.499f), new Vector3(152.678f, 0.000f, -136.401f), new Vector3(148.139f, 0.000f, -136.011f), new Vector3(143.496f, 0.000f, -135.219f), new Vector3(138.571f, 0.000f, -134.368f), new Vector3(136.552f, 0.000f, -134.151f), new Vector3(134.670f, 0.000f, -134.059f), new Vector3(132.517f, 0.000f, -134.058f), new Vector3(127.443f, 0.000f, -134.575f), new Vector3(122.504f, 0.000f, -135.175f), new Vector3(117.669f, 0.000f, -135.771f), new Vector3(112.990f, 0.000f, -136.244f), new Vector3(108.107f, 0.000f, -136.412f), new Vector3(103.922f, 0.000f, -136.790f), new Vector3(102.600f, 0.000f, -136.862f), new Vector3(101.396f, 0.000f, -136.840f), new Vector3(100.280f, 0.000f, -136.705f), new Vector3(99.063f, 0.000f, -136.419f), new Vector3(98.772f, 0.000f, -136.329f), new Vector3(97.672f, 0.000f, -135.897f), new Vector3(96.848f, 0.000f, -135.436f), new Vector3(96.029f, 0.000f, -134.790f), new Vector3(95.019f, 0.000f, -133.839f)};

        /// <summary>
        /// Round-36 founder feedback ("zebra ideal ... fica bem
        /// próxima as curvas ... não aquele bloco grande e travado"):
        /// a continuous curb centerline hugging the INSIDE (apex) edge
        /// of every genuinely tight corner, instead of a fixed static
        /// square block. Computed in Python from the same 385-point
        /// centerline as everything else above: local radius of
        /// curvature per point (smoothed, small gaps closed so one real
        /// corner doesn't fragment into several tiny curb patches),
        /// corners tighter than 20m flagged as curb zone, each point
        /// offset toward the inside of ITS OWN turn direction (left or
        /// right, detected per point via the sign of the turn) by
        /// (localWidth/2 - curbWidth/2) so the curb sits right at the
        /// pavement edge. 16 separate contiguous curb runs came out of
        /// this — one per named corner, confirming the smoothing found
        /// the real corners and not curvature noise from the photo
        /// extraction. Same length (385) and same per-edge indexing as
        /// <see cref="Circuit2Centerline"/>/<see cref="Circuit2IsArcEdge"/>.
        /// </summary>
        private static readonly Vector3[] Circuit2CurbPoints = {
new Vector3(85.585f, 0.13f, -136.816f), new Vector3(86.589f, 0.13f, -128.137f), new Vector3(82.884f, 0.13f, -125.004f), new Vector3(75.075f, 0.13f, -127.763f), new Vector3(74.029f, 0.13f, -127.009f), new Vector3(72.791f, 0.13f, -126.178f), new Vector3(71.557f, 0.13f, -125.420f), new Vector3(70.428f, 0.13f, -124.817f), new Vector3(65.993f, 0.13f, -122.960f), new Vector3(61.535f, 0.13f, -121.186f), new Vector3(57.149f, 0.13f, -119.689f), new Vector3(52.670f, 0.13f, -118.690f), new Vector3(47.938f, 0.13f, -117.996f), new Vector3(44.995f, 0.13f, -110.190f), new Vector3(43.585f, 0.13f, -109.900f), new Vector3(42.158f, 0.13f, -109.565f), new Vector3(40.874f, 0.13f, -109.174f), new Vector3(39.748f, 0.13f, -108.708f), new Vector3(38.779f, 0.13f, -108.146f), new Vector3(37.944f, 0.13f, -107.472f), new Vector3(37.186f, 0.13f, -106.620f), new Vector3(36.428f, 0.13f, -105.544f), new Vector3(35.649f, 0.13f, -104.281f), new Vector3(33.133f, 0.13f, -100.071f), new Vector3(32.302f, 0.13f, -98.704f), new Vector3(31.480f, 0.13f, -97.389f), new Vector3(30.758f, 0.13f, -96.148f), new Vector3(30.220f, 0.13f, -95.058f), new Vector3(29.935f, 0.13f, -94.238f), new Vector3(29.916f, 0.13f, -93.545f), new Vector3(30.220f, 0.13f, -92.545f), new Vector3(24.909f, 0.13f, -88.587f), new Vector3(31.362f, 0.13f, -89.610f), new Vector3(33.151f, 0.13f, -85.100f), new Vector3(33.780f, 0.13f, -83.854f), new Vector3(34.576f, 0.13f, -82.529f), new Vector3(35.403f, 0.13f, -81.230f), new Vector3(36.235f, 0.13f, -80.037f), new Vector3(37.110f, 0.13f, -78.989f), new Vector3(38.107f, 0.13f, -78.023f), new Vector3(39.252f, 0.13f, -77.059f), new Vector3(43.045f, 0.13f, -73.986f), new Vector3(43.218f, 0.13f, -66.503f), new Vector3(46.544f, 0.13f, -63.261f), new Vector3(54.063f, 0.13f, -63.484f), new Vector3(55.090f, 0.13f, -62.367f), new Vector3(56.140f, 0.13f, -61.335f), new Vector3(57.247f, 0.13f, -60.381f), new Vector3(58.411f, 0.13f, -59.492f), new Vector3(59.605f, 0.13f, -58.692f), new Vector3(60.800f, 0.13f, -58.019f), new Vector3(62.032f, 0.13f, -57.494f), new Vector3(63.350f, 0.13f, -57.082f), new Vector3(64.753f, 0.13f, -56.761f), new Vector3(66.211f, 0.13f, -56.520f), new Vector3(67.696f, 0.13f, -56.352f), new Vector3(69.190f, 0.13f, -56.257f), new Vector3(70.582f, 0.13f, -56.234f), new Vector3(75.411f, 0.13f, -56.562f), new Vector3(80.732f, 0.13f, -51.186f), new Vector3(84.761f, 0.13f, -51.384f), new Vector3(86.244f, 0.13f, -51.430f), new Vector3(87.633f, 0.13f, -51.360f), new Vector3(89.013f, 0.13f, -51.162f), new Vector3(90.392f, 0.13f, -50.852f), new Vector3(91.760f, 0.13f, -50.431f), new Vector3(93.121f, 0.13f, -49.909f), new Vector3(94.211f, 0.13f, -49.441f), new Vector3(98.159f, 0.13f, -46.765f), new Vector3(102.605f, 0.13f, -50.705f), new Vector3(103.736f, 0.13f, -50.175f), new Vector3(104.050f, 0.13f, -50.082f), new Vector3(105.407f, 0.13f, -49.820f), new Vector3(106.807f, 0.13f, -49.701f), new Vector3(108.307f, 0.13f, -49.661f), new Vector3(113.182f, 0.13f, -43.850f), new Vector3(114.682f, 0.13f, -43.811f), new Vector3(116.103f, 0.13f, -43.689f), new Vector3(117.447f, 0.13f, -43.452f), new Vector3(118.759f, 0.13f, -43.078f), new Vector3(120.111f, 0.13f, -42.557f), new Vector3(123.859f, 0.13f, -47.254f), new Vector3(125.345f, 0.13f, -46.619f), new Vector3(124.574f, 0.13f, -40.664f), new Vector3(125.866f, 0.13f, -40.066f), new Vector3(126.866f, 0.13f, -39.483f), new Vector3(134.205f, 0.13f, -40.923f), new Vector3(138.045f, 0.13f, -38.097f), new Vector3(138.774f, 0.13f, -30.649f), new Vector3(142.519f, 0.13f, -27.676f), new Vector3(146.313f, 0.13f, -24.589f), new Vector3(150.010f, 0.13f, -21.528f), new Vector3(157.469f, 0.13f, -22.399f), new Vector3(158.585f, 0.13f, -21.449f), new Vector3(159.745f, 0.13f, -20.520f), new Vector3(160.831f, 0.13f, -19.770f), new Vector3(161.912f, 0.13f, -19.242f), new Vector3(163.142f, 0.13f, -18.869f), new Vector3(164.504f, 0.13f, -18.582f), new Vector3(165.854f, 0.13f, -18.406f), new Vector3(167.191f, 0.13f, -18.387f), new Vector3(168.608f, 0.13f, -18.504f), new Vector3(170.072f, 0.13f, -18.705f), new Vector3(171.582f, 0.13f, -18.490f), new Vector3(172.859f, 0.13f, -18.867f), new Vector3(173.922f, 0.13f, -19.374f), new Vector3(174.736f, 0.13f, -20.014f), new Vector3(175.314f, 0.13f, -20.838f), new Vector3(175.730f, 0.13f, -21.944f), new Vector3(176.049f, 0.13f, -23.294f), new Vector3(175.786f, 0.13f, -24.809f), new Vector3(175.926f, 0.13f, -26.329f), new Vector3(182.110f, 0.13f, -30.637f), new Vector3(182.853f, 0.13f, -35.377f), new Vector3(183.126f, 0.13f, -36.841f), new Vector3(183.477f, 0.13f, -38.242f), new Vector3(183.931f, 0.13f, -39.614f), new Vector3(184.480f, 0.13f, -40.982f), new Vector3(185.123f, 0.13f, -42.367f), new Vector3(185.828f, 0.13f, -43.791f), new Vector3(186.526f, 0.13f, -45.165f), new Vector3(187.216f, 0.13f, -46.355f), new Vector3(187.982f, 0.13f, -47.357f), new Vector3(188.890f, 0.13f, -48.256f), new Vector3(189.950f, 0.13f, -49.088f), new Vector3(191.159f, 0.13f, -49.877f), new Vector3(189.576f, 0.13f, -55.673f), new Vector3(190.984f, 0.13f, -56.501f), new Vector3(195.287f, 0.13f, -52.313f), new Vector3(196.506f, 0.13f, -52.960f), new Vector3(197.666f, 0.13f, -53.395f), new Vector3(198.947f, 0.13f, -53.659f), new Vector3(200.412f, 0.13f, -53.845f), new Vector3(201.942f, 0.13f, -53.990f), new Vector3(203.309f, 0.13f, -54.074f), new Vector3(208.389f, 0.13f, -59.657f), new Vector3(213.245f, 0.13f, -59.499f), new Vector3(218.100f, 0.13f, -53.697f), new Vector3(222.248f, 0.13f, -59.434f), new Vector3(223.765f, 0.13f, -59.446f), new Vector3(225.207f, 0.13f, -59.531f), new Vector3(226.624f, 0.13f, -59.737f), new Vector3(227.055f, 0.13f, -59.838f), new Vector3(228.329f, 0.13f, -60.283f), new Vector3(229.499f, 0.13f, -60.879f), new Vector3(230.755f, 0.13f, -61.680f), new Vector3(237.991f, 0.13f, -59.432f), new Vector3(236.361f, 0.13f, -65.165f), new Vector3(237.462f, 0.13f, -65.909f), new Vector3(238.416f, 0.13f, -66.760f), new Vector3(245.883f, 0.13f, -66.178f), new Vector3(249.403f, 0.13f, -69.288f), new Vector3(253.093f, 0.13f, -72.105f), new Vector3(254.200f, 0.13f, -79.480f), new Vector3(258.279f, 0.13f, -82.092f), new Vector3(262.270f, 0.13f, -84.739f), new Vector3(269.575f, 0.13f, -83.086f), new Vector3(270.139f, 0.13f, -90.588f), new Vector3(271.773f, 0.13f, -91.895f), new Vector3(279.270f, 0.13f, -91.337f), new Vector3(282.643f, 0.13f, -94.804f), new Vector3(286.174f, 0.13f, -98.083f), new Vector3(287.937f, 0.13f, -99.614f), new Vector3(289.085f, 0.13f, -100.509f), new Vector3(290.167f, 0.13f, -101.256f), new Vector3(294.430f, 0.13f, -103.532f), new Vector3(296.042f, 0.13f, -110.925f), new Vector3(297.219f, 0.13f, -111.656f), new Vector3(298.388f, 0.13f, -112.497f), new Vector3(299.514f, 0.13f, -113.431f), new Vector3(300.614f, 0.13f, -114.449f), new Vector3(301.698f, 0.13f, -115.544f), new Vector3(302.124f, 0.13f, -116.007f), new Vector3(303.017f, 0.13f, -117.098f), new Vector3(303.705f, 0.13f, -118.188f), new Vector3(304.290f, 0.13f, -119.441f), new Vector3(311.524f, 0.13f, -121.620f), new Vector3(308.337f, 0.13f, -128.460f), new Vector3(308.861f, 0.13f, -129.751f), new Vector3(309.266f, 0.13f, -131.074f), new Vector3(309.554f, 0.13f, -132.447f), new Vector3(309.744f, 0.13f, -133.858f), new Vector3(309.825f, 0.13f, -135.211f), new Vector3(309.743f, 0.13f, -136.505f), new Vector3(309.492f, 0.13f, -137.873f), new Vector3(309.140f, 0.13f, -139.334f), new Vector3(308.743f, 0.13f, -140.752f), new Vector3(308.275f, 0.13f, -142.034f), new Vector3(308.131f, 0.13f, -143.439f), new Vector3(307.348f, 0.13f, -144.591f), new Vector3(307.043f, 0.13f, -144.946f), new Vector3(306.022f, 0.13f, -145.913f), new Vector3(304.915f, 0.13f, -146.722f), new Vector3(303.486f, 0.13f, -146.983f), new Vector3(302.250f, 0.13f, -147.541f), new Vector3(300.924f, 0.13f, -148.001f), new Vector3(299.544f, 0.13f, -148.371f), new Vector3(298.325f, 0.13f, -148.616f), new Vector3(293.960f, 0.13f, -154.657f), new Vector3(289.302f, 0.13f, -155.357f), new Vector3(283.604f, 0.13f, -150.558f), new Vector3(278.967f, 0.13f, -150.890f), new Vector3(274.148f, 0.13f, -150.903f), new Vector3(269.229f, 0.13f, -150.903f), new Vector3(264.306f, 0.13f, -150.903f), new Vector3(259.453f, 0.13f, -150.902f), new Vector3(254.642f, 0.13f, -150.667f), new Vector3(249.849f, 0.13f, -150.319f), new Vector3(245.126f, 0.13f, -149.704f), new Vector3(240.416f, 0.13f, -148.829f), new Vector3(235.702f, 0.13f, -147.775f), new Vector3(231.000f, 0.13f, -146.587f), new Vector3(229.563f, 0.13f, -146.152f), new Vector3(228.125f, 0.13f, -145.675f), new Vector3(226.820f, 0.13f, -145.157f), new Vector3(225.595f, 0.13f, -144.509f), new Vector3(224.330f, 0.13f, -143.722f), new Vector3(223.090f, 0.13f, -142.896f), new Vector3(221.704f, 0.13f, -142.459f), new Vector3(220.844f, 0.13f, -141.571f), new Vector3(220.204f, 0.13f, -140.584f), new Vector3(219.764f, 0.13f, -139.478f), new Vector3(219.520f, 0.13f, -138.275f), new Vector3(219.476f, 0.13f, -137.002f), new Vector3(219.615f, 0.13f, -135.686f), new Vector3(219.916f, 0.13f, -134.379f), new Vector3(220.380f, 0.13f, -133.124f), new Vector3(221.013f, 0.13f, -131.908f), new Vector3(221.799f, 0.13f, -130.706f), new Vector3(222.670f, 0.13f, -129.556f), new Vector3(223.581f, 0.13f, -128.560f), new Vector3(224.530f, 0.13f, -127.787f), new Vector3(225.796f, 0.13f, -127.692f), new Vector3(227.016f, 0.13f, -127.297f), new Vector3(228.455f, 0.13f, -126.956f), new Vector3(231.994f, 0.13f, -120.239f), new Vector3(233.435f, 0.13f, -119.900f), new Vector3(234.756f, 0.13f, -119.463f), new Vector3(236.098f, 0.13f, -118.886f), new Vector3(237.511f, 0.13f, -118.215f), new Vector3(238.917f, 0.13f, -117.523f), new Vector3(240.452f, 0.13f, -117.266f), new Vector3(241.553f, 0.13f, -116.537f), new Vector3(242.380f, 0.13f, -115.762f), new Vector3(242.926f, 0.13f, -114.897f), new Vector3(243.243f, 0.13f, -113.826f), new Vector3(243.399f, 0.13f, -112.551f), new Vector3(243.402f, 0.13f, -111.206f), new Vector3(243.242f, 0.13f, -109.884f), new Vector3(242.941f, 0.13f, -108.700f), new Vector3(242.509f, 0.13f, -107.808f), new Vector3(241.879f, 0.13f, -107.196f), new Vector3(240.929f, 0.13f, -106.732f), new Vector3(239.664f, 0.13f, -106.360f), new Vector3(238.122f, 0.13f, -106.555f), new Vector3(237.497f, 0.13f, -100.581f), new Vector3(232.652f, 0.13f, -99.772f), new Vector3(227.871f, 0.13f, -98.953f), new Vector3(223.151f, 0.13f, -97.917f), new Vector3(217.180f, 0.13f, -102.463f), new Vector3(213.298f, 0.13f, -96.063f), new Vector3(208.551f, 0.13f, -95.218f), new Vector3(206.304f, 0.13f, -94.699f), new Vector3(204.897f, 0.13f, -94.300f), new Vector3(203.521f, 0.13f, -93.813f), new Vector3(202.136f, 0.13f, -93.224f), new Vector3(200.700f, 0.13f, -92.560f), new Vector3(199.280f, 0.13f, -91.888f), new Vector3(197.987f, 0.13f, -91.214f), new Vector3(196.846f, 0.13f, -90.486f), new Vector3(195.805f, 0.13f, -89.644f), new Vector3(194.809f, 0.13f, -88.664f), new Vector3(193.830f, 0.13f, -87.554f), new Vector3(192.865f, 0.13f, -86.368f), new Vector3(192.044f, 0.13f, -85.297f), new Vector3(189.578f, 0.13f, -81.149f), new Vector3(182.107f, 0.13f, -79.852f), new Vector3(181.276f, 0.13f, -78.532f), new Vector3(180.444f, 0.13f, -77.225f), new Vector3(179.651f, 0.13f, -76.116f), new Vector3(178.731f, 0.13f, -75.152f), new Vector3(178.407f, 0.13f, -74.886f), new Vector3(177.211f, 0.13f, -74.059f), new Vector3(175.943f, 0.13f, -73.307f), new Vector3(174.635f, 0.13f, -72.608f), new Vector3(173.308f, 0.13f, -72.004f), new Vector3(171.894f, 0.13f, -71.467f), new Vector3(168.853f, 0.13f, -70.374f), new Vector3(167.434f, 0.13f, -69.916f), new Vector3(165.988f, 0.13f, -69.544f), new Vector3(164.494f, 0.13f, -69.227f), new Vector3(163.049f, 0.13f, -68.971f), new Vector3(161.708f, 0.13f, -68.846f), new Vector3(160.352f, 0.13f, -68.889f), new Vector3(158.847f, 0.13f, -69.050f), new Vector3(154.019f, 0.13f, -69.597f), new Vector3(151.751f, 0.13f, -69.997f), new Vector3(150.276f, 0.13f, -70.310f), new Vector3(148.843f, 0.13f, -70.688f), new Vector3(147.476f, 0.13f, -71.137f), new Vector3(146.202f, 0.13f, -71.671f), new Vector3(144.995f, 0.13f, -72.327f), new Vector3(143.799f, 0.13f, -73.115f), new Vector3(139.828f, 0.13f, -75.958f), new Vector3(137.890f, 0.13f, -77.380f), new Vector3(136.712f, 0.13f, -78.300f), new Vector3(135.644f, 0.13f, -79.249f), new Vector3(134.684f, 0.13f, -80.258f), new Vector3(133.822f, 0.13f, -81.340f), new Vector3(133.066f, 0.13f, -82.486f), new Vector3(132.431f, 0.13f, -83.682f), new Vector3(131.925f, 0.13f, -84.927f), new Vector3(131.549f, 0.13f, -86.223f), new Vector3(131.290f, 0.13f, -87.612f), new Vector3(131.103f, 0.13f, -89.103f), new Vector3(130.965f, 0.13f, -90.629f), new Vector3(130.888f, 0.13f, -92.159f), new Vector3(130.865f, 0.13f, -93.721f), new Vector3(130.872f, 0.13f, -95.284f), new Vector3(130.926f, 0.13f, -96.749f), new Vector3(130.593f, 0.13f, -98.141f), new Vector3(130.914f, 0.13f, -99.316f), new Vector3(131.434f, 0.13f, -100.327f), new Vector3(132.204f, 0.13f, -101.252f), new Vector3(133.214f, 0.13f, -102.152f), new Vector3(134.378f, 0.13f, -103.022f), new Vector3(135.838f, 0.13f, -103.374f), new Vector3(136.997f, 0.13f, -103.975f), new Vector3(138.225f, 0.13f, -104.412f), new Vector3(139.635f, 0.13f, -104.761f), new Vector3(141.122f, 0.13f, -105.083f), new Vector3(144.884f, 0.13f, -111.661f), new Vector3(149.514f, 0.13f, -112.570f), new Vector3(153.949f, 0.13f, -114.050f), new Vector3(160.420f, 0.13f, -110.302f), new Vector3(163.358f, 0.13f, -117.104f), new Vector3(167.759f, 0.13f, -118.834f), new Vector3(172.260f, 0.13f, -120.690f), new Vector3(175.163f, 0.13f, -121.963f), new Vector3(176.456f, 0.13f, -122.653f), new Vector3(177.812f, 0.13f, -123.411f), new Vector3(179.334f, 0.13f, -123.753f), new Vector3(180.409f, 0.13f, -124.543f), new Vector3(181.223f, 0.13f, -125.379f), new Vector3(181.842f, 0.13f, -126.434f), new Vector3(181.953f, 0.13f, -126.749f), new Vector3(182.280f, 0.13f, -128.105f), new Vector3(182.437f, 0.13f, -129.450f), new Vector3(182.435f, 0.13f, -130.761f), new Vector3(182.238f, 0.13f, -132.002f), new Vector3(181.844f, 0.13f, -133.178f), new Vector3(181.251f, 0.13f, -134.285f), new Vector3(180.462f, 0.13f, -135.335f), new Vector3(179.521f, 0.13f, -136.318f), new Vector3(178.504f, 0.13f, -137.170f), new Vector3(177.447f, 0.13f, -137.817f), new Vector3(176.304f, 0.13f, -138.247f), new Vector3(174.927f, 0.13f, -138.008f), new Vector3(173.478f, 0.13f, -138.153f), new Vector3(171.896f, 0.13f, -138.264f), new Vector3(167.077f, 0.13f, -138.584f), new Vector3(162.250f, 0.13f, -138.598f), new Vector3(157.362f, 0.13f, -138.599f), new Vector3(152.566f, 0.13f, -138.498f), new Vector3(147.873f, 0.13f, -138.094f), new Vector3(143.142f, 0.13f, -137.289f), new Vector3(137.355f, 0.13f, -142.174f), new Vector3(135.947f, 0.13f, -142.028f), new Vector3(134.449f, 0.13f, -141.956f), new Vector3(133.105f, 0.13f, -141.936f), new Vector3(128.320f, 0.13f, -142.426f), new Vector3(123.464f, 0.13f, -143.016f), new Vector3(117.903f, 0.13f, -137.858f), new Vector3(113.131f, 0.13f, -138.339f), new Vector3(108.581f, 0.13f, -144.298f), new Vector3(104.090f, 0.13f, -138.883f), new Vector3(102.635f, 0.13f, -138.962f), new Vector3(101.248f, 0.13f, -138.935f), new Vector3(99.916f, 0.13f, -138.774f), new Vector3(98.539f, 0.13f, -138.452f), new Vector3(98.042f, 0.13f, -138.297f), new Vector3(96.764f, 0.13f, -137.790f), new Vector3(95.686f, 0.13f, -137.186f), new Vector3(94.667f, 0.13f, -136.389f), new Vector3(93.577f, 0.13f, -135.367f)
        };

        /// <summary>
        /// Per-EDGE (same indexing as <see cref="Circuit2IsArcEdge"/>):
        /// true if a curb piece should be built between point i and
        /// i+1 (either endpoint inside the curb zone — see
        /// <see cref="Circuit2CurbPoints"/>'s comment). 118 of 385
        /// edges are active, covering 16 separate corner-hugging runs.
        /// </summary>
        private static readonly bool[] Circuit2CurbActive = {
false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, true, true, true, false, false, false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, true, true, true, false, false, false, false, false, false, false, false, false, false, true, true, false, false, false, false, false, false, true, true, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, false, false, false, false, false, false, false, false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, false, false, false, false, false, false, false, false, true, true, true, true, true, true, true, true, true, true, true, true, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, true, true, true, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, true, true, true, true, true, false, false, false, false, false, false, false, true, true, true, true, true, true, true, true, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false
        };

        /// <summary>
        /// Builds Pista 2's greybox: asphalt, outer/inner barriers,
        /// start/finish line and 12 checkpoint gates (start/finish + 11,
        /// one per filleted vertex — see the round-34 comment block above).
        /// Deliberately does not touch <see cref="CreateCourse"/> (the
        /// already verified-on-device oval) at all — this is a fully
        /// separate, additive method, gated by <see cref="UseTechnicalCircuit2"/>.
        /// Grid slots and the bot path come from the
        /// TechnicalCircuit2Configuration TrackConfigurationSO (loaded by
        /// <see cref="LoadTrackConfiguration"/>), not from here.
        /// </summary>
        private static void CreateCourseTechnicalCircuit2()
        {
            var groundCollision = new GameObject("Grass Ground Collision (Circuit2)");
            groundCollision.transform.position = new Vector3(Circuit2GroundCenterX, 0.06f, Circuit2GroundCenterZ);
            var groundCollider = groundCollision.AddComponent<BoxCollider>();
            groundCollider.size = new Vector3(Circuit2GroundSizeX, 0.02f, Circuit2GroundSizeZ);
            groundCollider.sharedMaterial = GetLowFrictionMaterial();

            var groundVisual = GameObject.CreatePrimitive(PrimitiveType.Plane);
            groundVisual.name = "Grass Ground (Circuit2)";
            groundVisual.transform.position = new Vector3(Circuit2GroundCenterX, 0.058f, Circuit2GroundCenterZ); // round 37: same grass-height fix as the oval's ground plane
            groundVisual.transform.localScale = new Vector3(Circuit2GroundSizeX / 10f, 1f, Circuit2GroundSizeZ / 10f);
            groundVisual.GetComponent<Renderer>().sharedMaterial = CreateMaterial("Grass2", new Color(0.25f, 0.55f, 0.18f));
            Destroy(groundVisual.GetComponent<Collider>());

            var asphaltMat = CreateMaterial("Asphalt2", new Color(0.22f, 0.22f, 0.25f));
            var whiteMat = CreateMaterial("White2", new Color(0.95f, 0.95f, 0.95f));

            // Pavement: locally 8.5m wide from the hairpin exit through the
            // grid zone (founder/reviewer request: "8-9m na largada"),
            // tapering back to the nominal 7m (same as the oval) before
            // turn 1 — see Circuit2Widths' round-34 Python derivation.
            CreateVariableRibbon("Pavement2", Circuit2Centerline, Circuit2IsArcEdge, Circuit2Widths, 0.12f, asphaltMat, solidCollider: false);
            // Round 39 (continuation 3): the 385 boxes above are now
            // visual-only (non-solid) -- see CreatePavementMeshFloor's own
            // doc comment for why -- and this single seamless mesh is the
            // ONLY physical driving surface for the Carrera Kart pavement.
            CreatePavementMeshFloor("Pavement2_MeshFloor", Circuit2Centerline, Circuit2Widths, 0.06f);

            CreateWallRingFromPoints("Barrier2_Outer", Circuit2OuterBarrier, Circuit2IsArcEdge,
                thicknessMeters: 0.5f, heightMeters: 1f, solidCollider: true, useFenceVisual: true, useTireVisual: true);
            CreateWallRingFromPoints("Barrier2_Inner", Circuit2InnerBarrier, Circuit2IsArcEdge,
                thicknessMeters: 0.4f, heightMeters: 0.8f, solidCollider: true, useFenceVisual: false, useTireVisual: false);

            // --- GRASS SHOULDER (round 39 continuation 4: founder asked
            // for a bit of speed loss on grass on BOTH tracks -- Circuit2
            // had no grass grip zone at all before this) ---
            var circuit2GrassData = ScriptableObject.CreateInstance<SurfaceDataSO>();
            circuit2GrassData.Configure("grass_shoulder_circuit2", "Grass (Shoulder)", 0.5f, 0f, false);
            CreateShoulderGrassZone("GrassShoulder2_Outer", Circuit2Centerline, Circuit2Widths,
                sign: 1f, shoulderWidthMeters: 1.5f, topHeight: 0.06f, surfaceData: circuit2GrassData);
            CreateShoulderGrassZone("GrassShoulder2_Inner", Circuit2Centerline, Circuit2Widths,
                sign: -1f, shoulderWidthMeters: 1.5f, topHeight: 0.06f, surfaceData: circuit2GrassData);

            // --- CURBS (zebras) ---
            // Round 36 founder feedback: continuous checkered strip along
            // the inside (apex) edge of every genuinely tight corner — see
            // Circuit2CurbPoints'/Circuit2CurbActive's own comments for how
            // these were computed. Non-solid, same reasoning as the oval's.
            CreateAlternatingCurbRibbon("Curb2", Circuit2CurbPoints, Circuit2CurbActive, Circuit2IsArcEdge,
                curbWidthMeters: 1.2f, heightMeters: 0.08f);

            // --- START/FINISH LINE (non-solid, same reasoning as the oval's) ---
            CreateTrackPieceOriented("StartFinish2_Line", new Vector3(88.012f, 0.13f, -134.091f), -138.31f, 0.3f, 8.5f, 0.02f, whiteMat, solidCollider: false);

            // --- CHECKPOINTS (12 gates: start/finish + 11, one per vertex) ---
            // Unlike the oval (whose 4 gates all happen to be axis-aligned
            // because every stadium checkpoint sits on a straight or an
            // arc tip), Circuit2's corners are at arbitrary angles, so
            // these gates are ROTATED to the local track direction via
            // CreateCheckpointOriented (new this round) instead of the
            // oval's plain CreateCheckpoint.
            CreateCheckpointOriented("StartFinish", new Vector3(88.012f, 1f, -134.091f), -138.31f, 0.5f, 8.5f, 0, true, Vector3.right);
CreateCheckpointOriented("CP0", new Vector3(26.566f, 1f, -94.545f), -95.21f, 0.5f, 8.0f, 0, false, Vector3.zero);
CreateCheckpointOriented("CP1", new Vector3(59.518f, 1f, -55.418f), -26.23f, 0.5f, 7.0f, 1, false, Vector3.zero);
CreateCheckpointOriented("CP2", new Vector3(103.397f, 1f, -47.256f), -13.00f, 0.5f, 7.0f, 2, false, Vector3.zero);
CreateCheckpointOriented("CP3", new Vector3(176.481f, 1f, -18.367f), 46.65f, 0.5f, 7.0f, 3, false, Vector3.zero);
CreateCheckpointOriented("CP4", new Vector3(185.804f, 1f, -49.273f), 48.66f, 0.5f, 7.0f, 4, false, Vector3.zero);
CreateCheckpointOriented("CP5", new Vector3(227.924f, 1f, -57.071f), 17.44f, 0.5f, 7.0f, 5, false, Vector3.zero);
CreateCheckpointOriented("CP6", new Vector3(273.779f, 1f, -89.800f), 43.76f, 0.5f, 7.0f, 6, false, Vector3.zero);
CreateCheckpointOriented("CP7", new Vector3(304.339f, 1f, -114.134f), 49.77f, 0.5f, 7.0f, 7, false, Vector3.zero);
CreateCheckpointOriented("CP8", new Vector3(308.750f, 1f, -146.632f), 134.65f, 0.5f, 7.0f, 8, false, Vector3.zero);
CreateCheckpointOriented("CP9", new Vector3(217.465f, 1f, -140.166f), -106.64f, 0.5f, 7.0f, 9, false, Vector3.zero);
CreateCheckpointOriented("CP10", new Vector3(244.446f, 1f, -106.391f), -126.20f, 0.5f, 7.0f, 10, false, Vector3.zero);
CreateCheckpointOriented("CP11", new Vector3(180.127f, 1f, -72.551f), -143.62f, 0.5f, 7.0f, 11, false, Vector3.zero);
CreateCheckpointOriented("CP12", new Vector3(129.430f, 1f, -101.647f), 56.63f, 0.5f, 7.0f, 12, false, Vector3.zero);
CreateCheckpointOriented("CP13", new Vector3(184.261f, 1f, -126.091f), 74.09f, 0.5f, 7.0f, 13, false, Vector3.zero);
CreateCheckpointOriented("CP14", new Vector3(134.530f, 1f, -139.057f), -178.39f, 0.5f, 7.0f, 14, false, Vector3.zero);
CreateCheckpointOriented("CP15", new Vector3(97.033f, 1f, -141.016f), -159.64f, 0.5f, 7.0f, 15, false, Vector3.zero);
        }

        /// <summary>
        /// Pista 2 counterpart of <see cref="CreateRibbon"/>: same
        /// overlap-extension technique (stretch arc-chord edges so
        /// neighbors overlap and cover the kink — see that method's
        /// comment for the full round-21 rationale this reuses), but
        /// generalized two ways: (a) <paramref name="isArcEdge"/> replaces
        /// <see cref="IsStadiumStraightEdge"/>'s hardcoded "edge 0 or
        /// arcSegmentsPerEnd+1" rule with real per-edge data computed in
        /// the round-34 Python model (Circuit2 is an irregular polygon,
        /// not a 2-straight stadium), and (b) a per-point width
        /// (<paramref name="widthAtPoint"/>) so the grid straight can be
        /// locally wider than the rest of the lap without a second,
        /// separate ribbon call.
        /// </summary>
        private static void CreateVariableRibbon(string prefix, Vector3[] centerline, bool[] isArcEdge,
            float[] widthAtPoint, float heightMeters, Material material, bool solidCollider = true)
        {
            var count = centerline.Length;
            for (var i = 0; i < count; i++)
            {
                var p0 = centerline[i];
                var p1 = centerline[(i + 1) % count];
                var delta = p1 - p0;
                var length = delta.magnitude;
                var mid = (p0 + p1) * 0.5f;
                var yaw = YawDegreesForDirection(delta.x / length, delta.z / length);
                var pieceLength = length * ArcSegmentOverlapFactor; // round 37: always stretch (see CreateVariableRibbon's comment)
                var width = (widthAtPoint[i] + widthAtPoint[(i + 1) % count]) * 0.5f;
                CreateTrackPieceOriented($"{prefix}_Seg_{i:000}", mid, yaw, pieceLength, width, heightMeters, material,
                    solidCollider: solidCollider);
            }
        }

        /// <summary>
        /// Round 39 (continuation 3, 2026-08-25): builds ONE continuous,
        /// seamless mesh collider for the Circuit2 pavement's driving
        /// surface, used alongside (not instead of) the existing
        /// per-segment visual boxes from <see cref="CreateVariableRibbon"/>
        /// (which are now created non-solid for this call -- see that call
        /// site -- so they no longer provide physical collision, only the
        /// unchanged visual).
        ///
        /// Why: founder playtest feedback across rounds 37-39 kept
        /// reporting "trava"/"pulos" (catches/jumps) while driving the
        /// Carrera Kart, even on straights, worse near curves/zebra --
        /// present even on the known-good pre-round-39 geometry, so no
        /// single round's data change caused it. The kart's Rigidbody has
        /// X/Z rotation FROZEN (see KartDynamics.ConfigureRigidbody), so it
        /// cannot physically "tip" over a bump -- which points specifically
        /// at a well-known Unity/PhysX phenomenon: a driving surface built
        /// from many separate BoxColliders (385 of them here, one per
        /// pavement segment) can generate spurious, non-vertical contact
        /// normals at the internal seams between adjacent boxes even when
        /// the boxes are perfectly flush/coplanar, because PhysX resolves
        /// each box independently with no notion that segment N's edge is
        /// meant to continue smoothly into segment N+1. This is exactly
        /// why production racing games build the drivable surface as ONE
        /// continuous mesh collider instead of tiled primitives -- a
        /// single mesh has no internal seams for PhysX to catch on.
        ///
        /// This method rebuilds the SAME pavement footprint (same
        /// centerline, same per-point width) as a single triangle strip
        /// instead of N boxes, so the physical floor is geometrically
        /// identical in shape/position to before, just seamless. Left/right
        /// offsets use the same centered-difference tangent + perpendicular
        /// normal formula already calibrated and verified for the round-39
        /// barrier work. Validated offline (Python) before writing this: 0
        /// degenerate triangles, 0 wrong-winding triangles (both checked
        /// against every one of the 770 triangles), and the tightest
        /// corner's turn radius is 1.42x the local half-width (safely above
        /// 1.0, so the inner edge never folds/self-intersects anywhere
        /// around the 940m lap).
        ///
        /// This is a hypothesis-driven fix for a physics feel problem, not
        /// a provable one from static analysis alone -- I cannot run Unity
        /// from here to confirm it removes the jumps/catches. If it
        /// doesn't help, CreateVariableRibbon's Circuit2 call can go back
        /// to solidCollider: true (its default) and this call removed;
        /// nothing else references this object.
        /// </summary>
        private static void CreatePavementMeshFloor(string name, Vector3[] centerline, float[] widthAtPoint,
            float topHeight)
        {
            var count = centerline.Length;
            var vertices = new Vector3[count * 2];
            for (var i = 0; i < count; i++)
            {
                var prev = centerline[(i - 1 + count) % count];
                var next = centerline[(i + 1) % count];
                var tangent = new Vector2(next.x - prev.x, next.z - prev.z).normalized;
                var leftNormal = new Vector2(-tangent.y, tangent.x);
                var rightNormal = new Vector2(tangent.y, -tangent.x);
                var halfWidth = widthAtPoint[i] * 0.5f;
                var center = centerline[i];
                vertices[i * 2] = new Vector3(center.x + leftNormal.x * halfWidth, topHeight,
                    center.z + leftNormal.y * halfWidth);
                vertices[i * 2 + 1] = new Vector3(center.x + rightNormal.x * halfWidth, topHeight,
                    center.z + rightNormal.y * halfWidth);
            }

            var triangles = new int[count * 6];
            for (var i = 0; i < count; i++)
            {
                var l0 = i * 2;
                var r0 = i * 2 + 1;
                var next = (i + 1) % count;
                var l1 = next * 2;
                var r1 = next * 2 + 1;
                var t = i * 6;
                // Winding validated offline (Python) to face up (+Y).
                triangles[t] = l0;
                triangles[t + 1] = r1;
                triangles[t + 2] = r0;
                triangles[t + 3] = l0;
                triangles[t + 4] = l1;
                triangles[t + 5] = r1;
            }

            var mesh = new Mesh { name = name };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var obj = new GameObject(name);
            var collider = obj.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            collider.convex = false;
            collider.sharedMaterial = GetLowFrictionMaterial();
        }

        /// <summary>
        /// Round 39 (continuation 4): a 1.5m-wide grass "shoulder" trigger
        /// strip running alongside the pavement, on one side only
        /// (<paramref name="sign"/> = +1 for the outer side, -1 for the
        /// inner side -- same left/right normal convention already
        /// calibrated and validated for Circuit2OuterBarrier/InnerBarrier).
        /// Reuses the per-point width array so the shoulder tracks the
        /// pavement's own varying width exactly like the barriers do.
        /// See this method's call site for why this is built from many
        /// small trigger boxes instead of one continuous mesh.
        /// </summary>
        private static void CreateShoulderGrassZone(string prefix, Vector3[] centerline, float[] widthAtPoint,
            float sign, float shoulderWidthMeters, float topHeight, SurfaceDataSO surfaceData)
        {
            var count = centerline.Length;
            for (var i = 0; i < count; i++)
            {
                var p0 = centerline[i];
                var p1 = centerline[(i + 1) % count];
                var delta = p1 - p0;
                var length = delta.magnitude;
                var yaw = YawDegreesForDirection(delta.x / length, delta.z / length);
                var pieceLength = length * ArcSegmentOverlapFactor;
                var halfWidth0 = widthAtPoint[i] * 0.5f;
                var halfWidth1 = widthAtPoint[(i + 1) % count] * 0.5f;
                var avgHalfWidth = (halfWidth0 + halfWidth1) * 0.5f;
                var mid = (p0 + p1) * 0.5f;
                var tangentDir = delta / length;
                var sideDir = new Vector3(-tangentDir.z, 0f, tangentDir.x) * sign;
                var shoulderCenterOffset = avgHalfWidth + shoulderWidthMeters * 0.5f;
                var shoulderMid = mid + sideDir * shoulderCenterOffset;
                shoulderMid.y = topHeight;

                var obj = new GameObject($"{prefix}_Seg_{i:000}");
                obj.transform.SetPositionAndRotation(shoulderMid, Quaternion.Euler(0f, yaw, 0f));
                var col = obj.AddComponent<BoxCollider>();
                col.size = new Vector3(pieceLength, 0.2f, shoulderWidthMeters);
                col.isTrigger = true;
                var trigger = obj.AddComponent<SurfaceTrigger>();
                trigger.Configure(surfaceData);
            }
        }

        /// <summary>
        /// Wall-ring counterpart of <see cref="CreateVariableRibbon"/>, for
        /// a centerline that is ALREADY the offset ring itself (Circuit2's
        /// outer/inner barrier arrays are pre-offset per-point in the
        /// round-34 Python model — unlike the oval, which regenerates a
        /// second true-circle centerline via GenerateStadiumCenterline at
        /// a different radius, an irregular filleted polygon's offset was
        /// computed directly, point by point, along each pavement point's
        /// own normal — see docs/30-founder-playtest-log.md round 34).
        /// </summary>
        private static void CreateWallRingFromPoints(string prefix, Vector3[] points, bool[] isArcEdge,
            float thicknessMeters, float heightMeters, bool solidCollider, bool useFenceVisual, bool useTireVisual)
        {
            var count = points.Length;
            var verticalOffset = Vector3.up * (heightMeters * 0.5f);
            for (var i = 0; i < count; i++)
            {
                var p0 = points[i];
                var p1 = points[(i + 1) % count];
                var delta = p1 - p0;
                var length = delta.magnitude;
                var mid = (p0 + p1) * 0.5f + verticalOffset;
                var yaw = YawDegreesForDirection(delta.x / length, delta.z / length);
                var pieceLength = length * ArcSegmentOverlapFactor; // round 37: always stretch (see CreateVariableRibbon's comment)
                CreateWallOriented($"{prefix}_Seg_{i:000}", mid, yaw, pieceLength, thicknessMeters, heightMeters,
                    solidCollider, useFenceVisual, useTireVisual);
            }
        }

        /// <summary>
        /// Rotated counterpart of <see cref="CreateCheckpoint"/> — needed
        /// because Circuit2's corners sit at arbitrary angles (the oval's 4
        /// checkpoints are all axis-aligned by construction; see
        /// <see cref="CreateCourse"/>'s own comment on that). Local +X
        /// (after the yaw rotation) is "along the track direction"
        /// (<paramref name="thicknessAlongTrack"/>), local Z is "across the
        /// track" (<paramref name="gateWidthAcrossTrack"/>) — same axis
        /// convention as <see cref="CreateTrackPieceOriented"/>/
        /// <see cref="CreateWallOriented"/>.
        /// </summary>
        private static void CreateCheckpointOriented(string name, Vector3 position, float yawDegrees,
            float thicknessAlongTrack, float gateWidthAcrossTrack, int index, bool isStartFinish,
            Vector3 crossingDirection)
        {
            var obj = new GameObject(name);
            obj.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yawDegrees, 0f));
            var col = obj.AddComponent<BoxCollider>();
            col.size = new Vector3(thicknessAlongTrack, 3f, gateWidthAcrossTrack);
            col.isTrigger = true;
            var cp = obj.AddComponent<CheckpointTrigger>();
            // Round 39 (continuation) fix: every StartFinish call site here
            // passed the constant Vector3.right as the required forward-
            // crossing direction, in WORLD space, ignoring this
            // checkpoint's own yawDegrees. That constant was correct for
            // the Oval's separate (older, unrotated) CreateCheckpoint
            // helper -- its comment literally says "the clockwise
            // prototype crosses start/finish toward +X" -- but Circuit2's
            // start/finish line sits at an arbitrary track angle
            // (yawDegrees=-138.31 here), so the real direction of travel
            // there is roughly (-0.77, 0, 0.64), not (1, 0, 0). Dot product
            // of those two is NEGATIVE, so CheckpointTrigger.IsCrossingForward
            // returned false on every single correct lap -- and because
            // RegisterCheckpointHit's isStartFinish branch just does
            // "if (!isCrossingForward) return;" with no side effect, this
            // produced exactly the reported symptom: no lap completes, no
            // "invalidated" message either, nothing happens at all.
            // Fix: derive the forward direction from this checkpoint's own
            // rotation instead of trusting a passed-in world-space
            // constant, so it is correct regardless of the angle this
            // particular checkpoint happens to sit at.
            var effectiveCrossingDirection = isStartFinish
                ? obj.transform.rotation * Vector3.right
                : crossingDirection;
            cp.Configure(index, isStartFinish, effectiveCrossingDirection);
        }

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
        private static GameObject CreateTrackPieceOriented(string name, Vector3 position, float yawDegrees,
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
            // Round 38 founder feedback: "a zebra poderia dar uma sensacao
            // as vezes vibrar o celular... mas nao é pra tirar velocidade
            // nem travar" -- returning the piece (was void) so curb
            // segments specifically can be tagged with CurbZoneMarker
            // right after creation (see both CreateAlternatingCurbRibbon
            // overloads below) without touching this method's own
            // physics/visual behavior for any of its other callers, all of
            // which still simply ignore the return value.
            return piece;
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
            float heightMeters, Material material, int arcSegmentsPerEnd, bool solidCollider = true)
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
                CreateTrackPieceOriented($"{prefix}_Seg_{i:00}", mid, yaw, pieceLength, widthMeters, heightMeters, material,
                    solidCollider: solidCollider);
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

        /// <summary>
        /// Round-36 founder feedback ("zebra ideal ... fica bem próxima
        /// as curvas ... não aquele bloco grande e travado"): replaces
        /// the old static square curb block with a continuous checkered
        /// strip that follows the curve itself, built with the same
        /// overlapping-segment technique as <see cref="CreateRibbon"/>/
        /// <see cref="CreateWallRibbon"/> (see those methods' comments
        /// for the round-21 "no joint squares" rationale) — but only
        /// along the curved (non-straight) portion of the centerline,
        /// alternating red/white per segment for the classic curb look.
        /// Oval overload: uses <see cref="IsStadiumStraightEdge"/> to
        /// skip the two straights (every arc edge gets a curb piece —
        /// the whole semicircle end, not a narrow apex slice, since the
        /// stadium's curve is uniformly tight along its whole length).
        /// </summary>
        private static void CreateAlternatingCurbRibbon(string prefix, List<Vector3> centerline,
            int arcSegmentsPerEnd, float curbWidthMeters, float heightMeters)
        {
            var count = centerline.Count;
            var matRed = CreateMaterial($"{prefix}_Red", new Color(0.85f, 0.12f, 0.12f));
            var matWhite = CreateMaterial($"{prefix}_White", new Color(0.92f, 0.92f, 0.92f));
            for (var i = 0; i < count; i++)
            {
                if (IsStadiumStraightEdge(i, arcSegmentsPerEnd))
                {
                    continue;
                }

                var p0 = centerline[i];
                var p1 = centerline[(i + 1) % count];
                var delta = p1 - p0;
                var length = delta.magnitude;
                var mid = (p0 + p1) * 0.5f + Vector3.up * 0.13f;
                var yaw = YawDegreesForDirection(delta.x / length, delta.z / length);
                var pieceLength = length; // round 37: no stretch -- overlap caused red/white z-fighting flicker
                var isRed = i % 2 == 0;
                var curbPiece = CreateTrackPieceOriented($"{prefix}_Seg_{i:00}", mid, yaw, pieceLength, curbWidthMeters,
                    heightMeters, isRed ? matRed : matWhite, solidCollider: false);
                curbPiece.AddComponent<CurbZoneMarker>();
            }
        }

        /// <summary>
        /// Circuit2 counterpart of the <see cref="List{Vector3}"/>
        /// overload above: same overlapping-segment/checkered technique,
        /// but driven by an explicit per-edge active mask
        /// (<paramref name="edgeActive"/>, see
        /// <see cref="Circuit2CurbActive"/>'s comment) instead of the
        /// stadium's straight/arc rule, since Circuit2 is an irregular
        /// polygon extracted from a real photo (not a 2-straight
        /// stadium) with only SOME of its curves tight enough to need a
        /// curb. <paramref name="centerline"/> points already carry
        /// their own y (see <see cref="Circuit2CurbPoints"/>), so no
        /// extra vertical offset is added here.
        /// </summary>
        private static void CreateAlternatingCurbRibbon(string prefix, Vector3[] centerline, bool[] edgeActive,
            bool[] isArcEdge, float curbWidthMeters, float heightMeters)
        {
            var count = centerline.Length;
            var matRed = CreateMaterial($"{prefix}_Red", new Color(0.85f, 0.12f, 0.12f));
            var matWhite = CreateMaterial($"{prefix}_White", new Color(0.92f, 0.92f, 0.92f));
            for (var i = 0; i < count; i++)
            {
                if (!edgeActive[i])
                {
                    continue;
                }

                var p0 = centerline[i];
                var p1 = centerline[(i + 1) % count];
                var delta = p1 - p0;
                var length = delta.magnitude;
                var mid = (p0 + p1) * 0.5f;
                var yaw = YawDegreesForDirection(delta.x / length, delta.z / length);
                var pieceLength = length; // round 37: no stretch -- overlap caused red/white z-fighting flicker
                var isRed = i % 2 == 0;
                var curbPiece = CreateTrackPieceOriented($"{prefix}_Seg_{i:000}", mid, yaw, pieceLength, curbWidthMeters,
                    heightMeters, isRed ? matRed : matWhite, solidCollider: false);
                curbPiece.AddComponent<CurbZoneMarker>();
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
            // Round 38 founder feedback: "a zebra poderia dar uma sensacao
            // as vezes vibrar o celular" -- player-only (a phone vibrates,
            // bots don't need to "feel" anything), see
            // KartCurbHapticsController's own class doc.
            dynamics.gameObject.AddComponent<KartCurbHapticsController>();
            // Round 39 (continuation 4): founder asked for vibration when
            // hitting a wall or another kart, mirroring the curb-vibration
            // pattern just above. See KartImpactHapticsController's own
            // class doc for why it does NOT reuse CollisionHandler as a
            // component (only its math helper).
            dynamics.gameObject.AddComponent<KartImpactHapticsController>();
            return dynamics;
        }

        /// <summary>
        /// Round 37 founder feedback: "falta nas 2 pistas o desenho da
        /// largada no posicionamento do kart" -- draws a marking at every
        /// grid slot so the player can see exactly where each of the 10
        /// karts should line up (the start/finish LINE already existed;
        /// the individual grid BOXES never did).
        ///
        /// Round 38 founder feedback: "colocou um bloco amarelo no chao
        /// achei bem grosseiro no kart real e somente 3 linhas brancas
        /// envolta do carrinho quase um quadrado aberto na traseira" --
        /// replaced the single solid yellow block with 3 thin white lines
        /// (left, right, front) forming an open-backed box, matching how a
        /// real kart track paints its grid boxes (the kart enters from
        /// behind, so that side is left open). While rebuilding this, also
        /// fixed a real orientation bug the old block quietly had: it
        /// passed slot.YawDegrees (the KART yaw convention, local +Z =
        /// forward -- see the round-36 grid-slot-yaw comment where this
        /// convention was first pinned down) straight into
        /// CreateTrackPieceOriented, which expects the TRACK-PIECE
        /// convention (local +X = length direction, off by a fixed +90
        /// degrees from the kart convention). That made the old block's
        /// long side sit crosswise to the kart instead of running the same
        /// way it faces -- part of why it read as "grosseiro" even before
        /// considering the color/shape. This version computes each line's
        /// own forward/right directions from slot.YawDegrees via
        /// Quaternion.Euler (the same math Unity itself uses to orient the
        /// kart), so every line is guaranteed aligned with the kart that
        /// actually spawns there.
        ///
        /// Still non-solid, same reasoning as the curbs/start-finish line:
        /// a wheel-less rigid-box kart has no suspension to "climb" a
        /// raised marking anyway, so there is nothing gained by making
        /// this solid.
        /// </summary>
        private static void CreateGridSlotMarkers(TrackConfigurationSO trackConfiguration)
        {
            if (trackConfiguration == null)
            {
                return;
            }

            const float boxLengthMeters = 2.6f;
            const float boxWidthMeters = 1.9f;
            const float lineThicknessMeters = 0.12f;
            const float heightMeters = 0.02f;
            var halfLength = boxLengthMeters * 0.5f;
            var halfWidth = boxWidthMeters * 0.5f;

            var markMat = CreateMaterial("GridSlotMark", Color.white);
            var slots = trackConfiguration.GridSlots;
            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                var rotation = Quaternion.Euler(0f, slot.YawDegrees, 0f);
                var forward = rotation * Vector3.forward;
                var right = rotation * Vector3.right;
                var center = slot.WorldPosition;
                center.y = 0.11f;

                // Side lines run the full box length along the kart's own
                // forward direction, one on each side.
                var sideYaw = YawDegreesForDirection(forward.x, forward.z);
                CreateTrackPieceOriented($"GridMark_{slot.Position:00}_L", center - right * halfWidth, sideYaw,
                    boxLengthMeters, lineThicknessMeters, heightMeters, markMat, solidCollider: false);
                CreateTrackPieceOriented($"GridMark_{slot.Position:00}_R", center + right * halfWidth, sideYaw,
                    boxLengthMeters, lineThicknessMeters, heightMeters, markMat, solidCollider: false);

                // Front line closes the box ahead of the kart (the
                // direction it will drive off toward). No line behind --
                // that is the "aberto na traseira" opening the kart enters
                // through.
                var frontYaw = YawDegreesForDirection(right.x, right.z);
                CreateTrackPieceOriented($"GridMark_{slot.Position:00}_F", center + forward * halfLength, frontYaw,
                    boxWidthMeters, lineThicknessMeters, heightMeters, markMat, solidCollider: false);
            }
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

            // Round 40: same wiring, for the exhaust smoke puffs (see
            // CreateKartVisual/KartExhaustSmokeController).
            var smokeVisual = visual.GetComponent<KartExhaustSmokeController>();
            if (smokeVisual != null)
            {
                smokeVisual.Configure(dynamics);
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

            _currentPlayerKartModelResourcePath = kartModelResourcePath;

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

            var newSmokeVisual = newVisual.GetComponent<KartExhaustSmokeController>();
            if (newSmokeVisual != null)
            {
                newSmokeVisual.Configure(dynamics);
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

            // Round 40 founder request: "nao da pra ver a fumacinha
            // saindo do kart novo" -- see KartExhaustSmokeController's
            // class doc for the full reasoning.
            var smokeParts = FindPartsByPrefixes(instance.transform, "smoke_puff");
            if (smokeParts.Count > 0)
            {
                var smokeBounds = ComputeRendererBounds(smokeParts);
                foreach (var smokePart in smokeParts)
                {
                    smokePart.gameObject.SetActive(false);
                }

                if (smokeBounds != null)
                {
                    var smokeAnchor = new GameObject("ExhaustSmokeAnchor");
                    smokeAnchor.transform.SetParent(instance.transform, false);
                    smokeAnchor.transform.position = smokeBounds.Value.center;
                    smokeAnchor.transform.rotation = instance.transform.rotation;

                    var smokeController = instance.AddComponent<KartExhaustSmokeController>();
                    var smokeMaterial = CreateMaterial("ExhaustSmokePuff", new Color(0.55f, 0.55f, 0.55f));
                    smokeController.SetEmissionPoint(smokeAnchor.transform, smokeMaterial);
                }
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
