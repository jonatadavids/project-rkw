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
            CreateLighting();
            CreateCourse();
            SpawnedKart = CreateKart();
            CreateCamera(SpawnedKart.transform);
            SetupTiming(SpawnedKart);
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
            CreatePrimitive("Asphalt", PrimitiveType.Cube, new Vector3(0f, -0.3f, 0f),
                new Vector3(58f, 0.5f, 38f), "KartPhysics/Materials/Asphalt");

            CreateWall("North Boundary", new Vector3(0f, 0.75f, 19f), new Vector3(60f, 1.5f, 0.5f));
            CreateWall("South Boundary", new Vector3(0f, 0.75f, -19f), new Vector3(60f, 1.5f, 0.5f));
            CreateWall("East Boundary", new Vector3(29f, 0.75f, 0f), new Vector3(0.5f, 1.5f, 38f));
            CreateWall("West Boundary", new Vector3(-29f, 0.75f, 0f), new Vector3(0.5f, 1.5f, 38f));

            CreateWall("Chicane A", new Vector3(-5f, 0.55f, 3f), new Vector3(1.2f, 1.1f, 10f));
            CreateWall("Chicane B", new Vector3(5f, 0.55f, -3f), new Vector3(1.2f, 1.1f, 10f));
            CreateWall("Turn Marker Left", new Vector3(-14f, 0.3f, 10f), new Vector3(4f, 0.6f, 0.6f));
            CreateWall("Turn Marker Right", new Vector3(20f, 0.3f, -10f), new Vector3(4f, 0.6f, 0.6f));

            // Start/Finish line checkpoint
            CreateCheckpoint("StartFinish", new Vector3(-20f, 0.5f, -12f),
                new Vector3(8f, 2f, 0.5f), 0, true);

            // Intermediate checkpoints (placed around the circuit)
            CreateCheckpoint("Checkpoint1", new Vector3(20f, 0.5f, -12f),
                new Vector3(0.5f, 2f, 8f), 0, false);
            CreateCheckpoint("Checkpoint2", new Vector3(20f, 0.5f, 12f),
                new Vector3(0.5f, 2f, 8f), 1, false);
            CreateCheckpoint("Checkpoint3", new Vector3(-20f, 0.5f, 12f),
                new Vector3(8f, 2f, 0.5f), 2, false);

            // Grass surfaces on outer edges
            CreateSurface("Grass North", new Vector3(0f, -0.28f, 17.5f),
                new Vector3(56f, 0.5f, 3f), 0.5f, 0f, true);
            CreateSurface("Grass South", new Vector3(0f, -0.28f, -17.5f),
                new Vector3(56f, 0.5f, 3f), 0.5f, 0f, true);
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
            root.transform.SetPositionAndRotation(new Vector3(-20f, 0.55f, -8f), Quaternion.identity);
            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(1.25f, 0.5f, 2f);
            collider.sharedMaterial = GetLowFrictionMaterial();
            var body = root.AddComponent<Rigidbody>();
            body.linearDamping = 0.02f;
            body.angularDamping = 0.6f;

            var visual = CreatePrimitive("Kart Visual", PrimitiveType.Cube, Vector3.zero,
                new Vector3(1.2f, 0.45f, 1.9f), "KartPhysics/Materials/KartBlue");
            Destroy(visual.GetComponent<Collider>());
            visual.transform.SetParent(root.transform, false);

            var nose = CreatePrimitive("Kart Nose", PrimitiveType.Cube, Vector3.zero,
                new Vector3(0.75f, 0.25f, 0.7f), "KartPhysics/Materials/KartYellow");
            Destroy(nose.GetComponent<Collider>());
            nose.transform.SetParent(visual.transform, false);
            nose.transform.localPosition = new Vector3(0f, 0.1f, 1.05f);

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
            CreatePrimitive(name, PrimitiveType.Cube, position, scale, "KartPhysics/Materials/Barrier");
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
