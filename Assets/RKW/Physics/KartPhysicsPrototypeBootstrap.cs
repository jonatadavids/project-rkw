using RKW.Track;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace RKW.Physics
{
    [DisallowMultipleComponent]
    public sealed class KartPhysicsPrototypeBootstrap : MonoBehaviour
    {
        private const string TuningResourcePath = "KartPhysics/PrototypeSchoolTuning";
        private static PhysicsMaterial _lowFrictionMaterial;

        public KartDynamics SpawnedKart { get; private set; }

        private void Awake()
        {
            if (FindFirstObjectByType<KartDynamics>() != null)
            {
                return;
            }

            UnityEngine.Physics.gravity = new Vector3(0f, -9.81f, 0f);
            LoadTrackConfiguration();
            CreateLighting();
            CreateCourse();
            SpawnedKart = CreateKart();
            CreateCamera(SpawnedKart.transform);
            SetupTiming(SpawnedKart);
        }

        /// <summary>
        /// M3-T02: loads the TrackConfigurationSO at runtime so it is exercised
        /// outside of EditMode tests too. Read-only for now — the greybox oval
        /// geometry below is still generated procedurally, not yet driven by
        /// this configuration's grid/checkpoint/spline data. Wiring that up is
        /// a separate, deliberate follow-up so it does not risk the already
        /// verified-on-device track generation in CreateCourse().
        /// </summary>
        private static void LoadTrackConfiguration()
        {
            var trackConfiguration = Resources.Load<TrackConfigurationSO>("Track/OvalMvpTrackConfiguration");
            if (trackConfiguration == null)
            {
                Debug.LogWarning("KartPhysicsPrototypeBootstrap: no TrackConfigurationSO found at " +
                    "Resources/Track/OvalMvpTrackConfiguration.");
                return;
            }

            if (!trackConfiguration.IsValid(out var reason))
            {
                Debug.LogWarning("KartPhysicsPrototypeBootstrap: TrackConfigurationSO " +
                    $"'{trackConfiguration.TrackConfigurationId}' failed validation: {reason}");
                return;
            }

            Debug.Log("KartPhysicsPrototypeBootstrap: loaded track configuration " +
                $"'{trackConfiguration.TrackConfigurationId}' ({trackConfiguration.DisplayName}), " +
                $"direction={trackConfiguration.Direction}, grid slots={trackConfiguration.GridSlots.Count}.");
        }

        private void SetupTiming(KartDynamics kart)
        {
            var timingObject = new GameObject("TimingManager");
            var timing = timingObject.AddComponent<TimingManagerLite>();
            timing.Configure(3); // 3 checkpoints (not counting start/finish)
            timingObject.AddComponent<TimingHUD>();

            var detector = kart.gameObject.AddComponent<KartCheckpointDetector>();
            detector.Configure(timing);
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
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Grass Ground";
            ground.transform.position = new Vector3(0f, -0.05f, 0f);
            ground.transform.localScale = new Vector3(15f, 1f, 12f);
            ground.GetComponent<Renderer>().sharedMaterial = CreateMaterial("Grass", new Color(0.25f, 0.55f, 0.18f));
            ground.GetComponent<Collider>().sharedMaterial = GetLowFrictionMaterial();

            // --- TRACK SURFACE ---
            // Simple oval: two straights connected by semicircles
            // Layout: ~80m x 40m oval, track width 6m
            var asphaltMat = CreateMaterial("Asphalt", new Color(0.22f, 0.22f, 0.25f));
            var curbMat = CreateMaterial("Curb", new Color(0.9f, 0.15f, 0.15f));
            var whiteMat = CreateMaterial("White", new Color(0.95f, 0.95f, 0.95f));

            // Main straight (south side)
            CreateTrackPiece("Straight_Main", new Vector3(0f, 0f, -15f), new Vector3(70f, 0.12f, 7f), asphaltMat);
            // Back straight (north side)
            CreateTrackPiece("Straight_Back", new Vector3(0f, 0f, 15f), new Vector3(70f, 0.12f, 7f), asphaltMat);
            // Left connection
            CreateTrackPiece("Straight_Left", new Vector3(-35f, 0f, 0f), new Vector3(7f, 0.12f, 37f), asphaltMat);
            // Right connection
            CreateTrackPiece("Straight_Right", new Vector3(35f, 0f, 0f), new Vector3(7f, 0.12f, 37f), asphaltMat);

            // Corner fills (approximate circular corners with angled pieces)
            CreateTrackPiece("Corner_NE", new Vector3(30f, 0f, 12f), new Vector3(17f, 0.12f, 13f), asphaltMat);
            CreateTrackPiece("Corner_NW", new Vector3(-30f, 0f, 12f), new Vector3(17f, 0.12f, 13f), asphaltMat);
            CreateTrackPiece("Corner_SE", new Vector3(30f, 0f, -12f), new Vector3(17f, 0.12f, 13f), asphaltMat);
            CreateTrackPiece("Corner_SW", new Vector3(-30f, 0f, -12f), new Vector3(17f, 0.12f, 13f), asphaltMat);

            // --- CURBS (zebras) ---
            // Inner curbs at corners. Non-solid: this is a single rigid BoxCollider
            // kart with no wheel/suspension simulation, so any raised geometry
            // (top surface above the main track's) acts as a physical wall it
            // cannot climb. Curbs here are visual only until proper wheel colliders
            // exist.
            CreateTrackPiece("Curb_NE_Inner", new Vector3(28f, 0.13f, 10f), new Vector3(3f, 0.08f, 3f), curbMat, solidCollider: false);
            CreateTrackPiece("Curb_NW_Inner", new Vector3(-28f, 0.13f, 10f), new Vector3(3f, 0.08f, 3f), curbMat, solidCollider: false);
            CreateTrackPiece("Curb_SE_Inner", new Vector3(28f, 0.13f, -10f), new Vector3(3f, 0.08f, 3f), curbMat, solidCollider: false);
            CreateTrackPiece("Curb_SW_Inner", new Vector3(-28f, 0.13f, -10f), new Vector3(3f, 0.08f, 3f), curbMat, solidCollider: false);

            // --- CHICANE (on back straight) ---
            CreateWall("Chicane_L", new Vector3(-5f, 0.4f, 14f), new Vector3(1.5f, 0.8f, 4f));
            CreateWall("Chicane_R", new Vector3(5f, 0.4f, 16f), new Vector3(1.5f, 0.8f, 4f));

            // --- BARRIERS (outer walls) ---
            // Outer barriers
            CreateWall("Barrier_S_Outer", new Vector3(0f, 0.5f, -19.5f), new Vector3(72f, 1f, 0.5f));
            CreateWall("Barrier_N_Outer", new Vector3(0f, 0.5f, 19.5f), new Vector3(72f, 1f, 0.5f));
            CreateWall("Barrier_E_Outer", new Vector3(39f, 0.5f, 0f), new Vector3(0.5f, 1f, 40f));
            CreateWall("Barrier_W_Outer", new Vector3(-39f, 0.5f, 0f), new Vector3(0.5f, 1f, 40f));

            // Inner barriers (oval center)
            CreateWall("Barrier_Inner_N", new Vector3(0f, 0.4f, 11f), new Vector3(52f, 0.8f, 0.4f));
            CreateWall("Barrier_Inner_S", new Vector3(0f, 0.4f, -11f), new Vector3(52f, 0.8f, 0.4f));
            CreateWall("Barrier_Inner_E", new Vector3(26f, 0.4f, 0f), new Vector3(0.4f, 0.8f, 22f));
            CreateWall("Barrier_Inner_W", new Vector3(-26f, 0.4f, 0f), new Vector3(0.4f, 0.8f, 22f));

            // --- START/FINISH LINE ---
            // Non-solid for the same reason as the curbs above: it sits proud of
            // the asphalt surface and was blocking the kart right after spawn.
            CreateTrackPiece("StartFinish_Line", new Vector3(-10f, 0.13f, -15f), new Vector3(0.3f, 0.02f, 7f), whiteMat, solidCollider: false);

            // --- GRASS SURFACE TRIGGERS (inside and outside) ---
            CreateSurface("Grass_Inner", new Vector3(0f, -0.01f, 0f), new Vector3(50f, 0.5f, 20f), 0.5f, 0f, true);

            // --- CHECKPOINTS (triggers spanning track width) ---
            // Start/Finish
            CreateCheckpoint("StartFinish", new Vector3(-10f, 1f, -15f), new Vector3(0.5f, 3f, 8f), 0, true);
            // Checkpoint 1: end of main straight (before turn 1)
            CreateCheckpoint("CP1", new Vector3(30f, 1f, -15f), new Vector3(0.5f, 3f, 8f), 0, false);
            // Checkpoint 2: back straight
            CreateCheckpoint("CP2", new Vector3(0f, 1f, 15f), new Vector3(0.5f, 3f, 8f), 1, false);
            // Checkpoint 3: before turn 3 (west side)
            CreateCheckpoint("CP3", new Vector3(-30f, 1f, 0f), new Vector3(8f, 3f, 0.5f), 2, false);
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

        private const string BaseMaterialResourcePath = "KartPhysics/BaseURPLit";
        private static Material _baseMaterial;

        private static Material CreateMaterial(string name, Color color)
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
            cp.Configure(index, isStartFinish);
        }

        private static void CreateSurface(string name, Vector3 position, Vector3 size,
            float gripMultiplier, float instability, bool isOffTrack)
        {
            var obj = new GameObject(name);
            obj.transform.position = position;
            var col = obj.AddComponent<BoxCollider>();
            col.size = size;
            col.isTrigger = true;
            // Note: SurfaceTrigger requires a SurfaceDataSO asset.
            // For the greybox, we create a runtime instance.
            var surfaceData = ScriptableObject.CreateInstance<SurfaceDataSO>();
            // We can't set private fields at runtime easily, so we use reflection-free approach:
            // The SurfaceTrigger will be added but without a configured SO for now.
            // The grip system uses OnTriggerEnter which checks for null SurfaceData.
            // For the vertical slice, the kart already defaults to grip=1.0 on asphalt.
            // Grass reduction will be validated in M3 when we have proper SurfaceDataSO assets.
        }

        private static KartDynamics CreateKart()
        {
            var root = new GameObject("Prototype Kart");
            root.transform.SetPositionAndRotation(new Vector3(-15f, 0.55f, -15f), Quaternion.Euler(0f, 90f, 0f));
            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(1.0f, 0.5f, 1.8f);
            collider.center = new Vector3(0f, 0.25f, 0f);
            collider.sharedMaterial = GetLowFrictionMaterial();
            var body = root.AddComponent<Rigidbody>();
            body.linearDamping = 0.02f;
            body.angularDamping = 0.6f;

            // Use simple colored primitive for now (Kenney FBX causes MeshCollider stripping issues)
            // TODO: Create proper prefab from FBX in Editor with colliders removed for M3-T01 final
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Kart Visual";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = new Vector3(1.0f, 0.35f, 1.8f);
            visual.GetComponent<Renderer>().sharedMaterial = CreateMaterial("KartBlue", new Color(0.15f, 0.35f, 0.85f));
            DestroyImmediate(visual.GetComponent<Collider>());

            // Add a nose piece for visual direction
            var nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nose.name = "Kart Nose";
            nose.transform.SetParent(visual.transform, false);
            nose.transform.localPosition = new Vector3(0f, 0.08f, 0.85f);
            nose.transform.localScale = new Vector3(0.55f, 0.5f, 0.35f);
            nose.GetComponent<Renderer>().sharedMaterial = CreateMaterial("KartYellow", new Color(0.95f, 0.75f, 0.1f));
            DestroyImmediate(nose.GetComponent<Collider>());

            var dynamics = root.AddComponent<KartDynamics>();
            var tuning = Resources.Load<KartCategorySO>(TuningResourcePath);
            dynamics.Configure(tuning, visual.transform);
            root.AddComponent<KartPrototypeInput>();
            return dynamics;
        }

        private static void CreateCamera(Transform target)
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
            cameraObject.AddComponent<KartPrototypeCamera>().Configure(target);
        }

        private static void CreateWall(string name, Vector3 position, Vector3 scale)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.position = position;
            wall.transform.localScale = scale;
            wall.GetComponent<Renderer>().sharedMaterial = CreateMaterial("Barrier", new Color(0.7f, 0.1f, 0.1f));
            wall.GetComponent<Collider>().sharedMaterial = GetLowFrictionMaterial();
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
    }
}
