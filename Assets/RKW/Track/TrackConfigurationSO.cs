using System;
using System.Collections.Generic;
using UnityEngine;

namespace RKW.Track
{
    /// <summary>One of the 10 MVP grid start positions.</summary>
    [Serializable]
    public struct GridSlot
    {
        [SerializeField] private int position;
        [SerializeField] private Vector3 worldPosition;
        [SerializeField] private float yawDegrees;

        public GridSlot(int position, Vector3 worldPosition, float yawDegrees)
        {
            this.position = position;
            this.worldPosition = worldPosition;
            this.yawDegrees = yawDegrees;
        }

        public int Position => position;
        public Vector3 WorldPosition => worldPosition;
        public float YawDegrees => yawDegrees;
    }

    /// <summary>A timing checkpoint, including the start/finish checkpoint (index 0).</summary>
    [Serializable]
    public struct TrackCheckpointDefinition
    {
        [SerializeField] private string checkpointId;
        [SerializeField] private int sectorIndex;
        [SerializeField] private bool isStartFinish;
        [SerializeField] private Vector3 worldPosition;
        [SerializeField] private Vector3 size;

        public TrackCheckpointDefinition(string checkpointId, int sectorIndex, bool isStartFinish,
            Vector3 worldPosition, Vector3 size)
        {
            this.checkpointId = checkpointId;
            this.sectorIndex = sectorIndex;
            this.isStartFinish = isStartFinish;
            this.worldPosition = worldPosition;
            this.size = size;
        }

        public string CheckpointId => checkpointId;
        public int SectorIndex => sectorIndex;
        public bool IsStartFinish => isStartFinish;
        public Vector3 WorldPosition => worldPosition;
        public Vector3 Size => size;
    }

    /// <summary>A single named, positioned reference point (braking point, marshal post,
    /// signal post, recovery point, etc.) — anything that is just "an ID + a place".</summary>
    [Serializable]
    public struct NamedPoint
    {
        [SerializeField] private string pointId;
        [SerializeField] private Vector3 worldPosition;

        public NamedPoint(string pointId, Vector3 worldPosition)
        {
            this.pointId = pointId;
            this.worldPosition = worldPosition;
        }

        public string PointId => pointId;
        public Vector3 WorldPosition => worldPosition;
    }

    /// <summary>An axis-aligned area (escape road, recovery zone) defined by center + size.</summary>
    [Serializable]
    public struct TrackAreaBounds
    {
        [SerializeField] private string areaId;
        [SerializeField] private Vector3 center;
        [SerializeField] private Vector3 size;

        public TrackAreaBounds(string areaId, Vector3 center, Vector3 size)
        {
            this.areaId = areaId;
            this.center = center;
            this.size = size;
        }

        public string AreaId => areaId;
        public Vector3 Center => center;
        public Vector3 Size => size;
    }

    /// <summary>
    /// Requirement 16 (Track Configuration): an independent, versionable layout for a
    /// track — full/short/technical, clockwise/counter-clockwise are each their own
    /// TrackConfiguration with a stable ID, never derived by flipping another one at
    /// runtime. Referenced everywhere by <see cref="TrackConfigurationId"/> (stable
    /// string), never by direct Transform/scene reference — bindings to actual scene
    /// objects are resolved at runtime by whatever system consumes this data.
    /// MVP (Requirement 16.7): exactly one track, one configuration (clockwise).
    /// </summary>
    [CreateAssetMenu(fileName = "NewTrackConfiguration", menuName = "RKW/Track/Track Configuration")]
    public sealed class TrackConfigurationSO : ScriptableObject
    {
        /// <summary>Fixed MVP grid size (Requirement 16.2: "posições de grid").</summary>
        public const int RequiredGridSlotCount = 10;

        [Header("Identity")]
        [SerializeField] private string trackConfigurationId = "";
        [SerializeField] private string trackId = "";
        [SerializeField] private string displayName = "";
        [SerializeField] private TrackDirection direction = TrackDirection.Clockwise;

        [Header("Grid")]
        [SerializeField] private GridSlot[] gridSlots = Array.Empty<GridSlot>();

        [Header("Start / finish / pit")]
        [SerializeField] private string startLineId = "";
        [SerializeField] private string finishLineId = "";
        [SerializeField] private string pitEntryId = "";
        [SerializeField] private string pitExitId = "";

        [Header("Checkpoints and sectors")]
        [SerializeField] private TrackCheckpointDefinition[] checkpoints = Array.Empty<TrackCheckpointDefinition>();
        [Min(1)] [SerializeField] private int timingSectorCount = 3;

        [Header("Racing line")]
        [SerializeField] private Vector3[] racingSplinePoints = Array.Empty<Vector3>();
        [SerializeField] private Vector3[] idealLinePoints = Array.Empty<Vector3>();
        [SerializeField] private NamedPoint[] brakingPoints = Array.Empty<NamedPoint>();
        [SerializeField] private Vector3[] botPathPoints = Array.Empty<Vector3>();

        [Header("Track limits and safety")]
        [Min(0.1f)] [SerializeField] private float trackWidthMeters = 6f;
        [SerializeField] private NamedPoint[] marshalPosts = Array.Empty<NamedPoint>();
        [SerializeField] private NamedPoint[] signalPosts = Array.Empty<NamedPoint>();
        [SerializeField] private TrackAreaBounds[] escapeAreas = Array.Empty<TrackAreaBounds>();
        [SerializeField] private NamedPoint[] recoveryPoints = Array.Empty<NamedPoint>();

        [Header("Player recovery")]
        [Min(0.1f)] [SerializeField] private float recoveryStuckSeconds = 4f;
        [Min(0f)] [SerializeField] private float recoveryStoppedSpeedMetersPerSecond = 0.2f;
        [Range(1f, 180f)] [SerializeField] private float recoveryInvertedDegrees = 85f;
        [Min(0.1f)] [SerializeField] private float recoverySafetyHeightMeters = 2f;
        [Min(0f)] [SerializeField] private float recoveryCollisionGraceSeconds = 3f;
        [Min(1f)] [SerializeField] private float recoveryPerimeterMultiplier = 3f;

        public string TrackConfigurationId => trackConfigurationId;
        public string TrackId => trackId;
        public string DisplayName => displayName;
        public TrackDirection Direction => direction;
        public IReadOnlyList<GridSlot> GridSlots => gridSlots;
        public string StartLineId => startLineId;
        public string FinishLineId => finishLineId;
        public string PitEntryId => pitEntryId;
        public string PitExitId => pitExitId;
        public IReadOnlyList<TrackCheckpointDefinition> Checkpoints => checkpoints;
        public int TimingSectorCount => timingSectorCount;
        public IReadOnlyList<Vector3> RacingSplinePoints => racingSplinePoints;
        public IReadOnlyList<Vector3> IdealLinePoints => idealLinePoints;
        public IReadOnlyList<NamedPoint> BrakingPoints => brakingPoints;
        public IReadOnlyList<Vector3> BotPathPoints => botPathPoints;
        public float TrackWidthMeters => trackWidthMeters;
        public IReadOnlyList<NamedPoint> MarshalPosts => marshalPosts;
        public IReadOnlyList<NamedPoint> SignalPosts => signalPosts;
        public IReadOnlyList<TrackAreaBounds> EscapeAreas => escapeAreas;
        public IReadOnlyList<NamedPoint> RecoveryPoints => recoveryPoints;
        public float RecoveryStuckSeconds => recoveryStuckSeconds;
        public float RecoveryStoppedSpeedMetersPerSecond => recoveryStoppedSpeedMetersPerSecond;
        public float RecoveryInvertedDegrees => recoveryInvertedDegrees;
        public float RecoverySafetyHeightMeters => recoverySafetyHeightMeters;
        public float RecoveryCollisionGraceSeconds => recoveryCollisionGraceSeconds;
        public float RecoveryPerimeterMultiplier => recoveryPerimeterMultiplier;

        /// <summary>
        /// Checks that every field required by Requirement 16.2 is actually
        /// populated. Used by EditMode tests and can be called at load time by
        /// whatever system consumes the configuration.
        /// </summary>
        public bool IsValid(out string reason)
        {
            if (string.IsNullOrWhiteSpace(trackConfigurationId))
            {
                reason = "Track configuration ID is required.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(trackId))
            {
                reason = "Track ID is required.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                reason = "Display name is required.";
                return false;
            }

            if (gridSlots == null || gridSlots.Length != RequiredGridSlotCount)
            {
                reason = $"Grid must have exactly {RequiredGridSlotCount} positions (has {gridSlots?.Length ?? 0}).";
                return false;
            }

            if (string.IsNullOrWhiteSpace(startLineId))
            {
                reason = "Start line ID is required.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(finishLineId))
            {
                reason = "Finish line ID is required.";
                return false;
            }

            if (checkpoints == null || checkpoints.Length == 0)
            {
                reason = "At least one checkpoint is required.";
                return false;
            }

            var hasStartFinishCheckpoint = false;
            foreach (var checkpoint in checkpoints)
            {
                if (checkpoint.IsStartFinish)
                {
                    hasStartFinishCheckpoint = true;
                    break;
                }
            }

            if (!hasStartFinishCheckpoint)
            {
                reason = "Checkpoints must include the start/finish checkpoint.";
                return false;
            }

            if (timingSectorCount <= 0)
            {
                reason = "Timing sector count must be positive.";
                return false;
            }

            if (racingSplinePoints == null || racingSplinePoints.Length < 2)
            {
                reason = "Racing spline requires at least 2 points.";
                return false;
            }

            if (idealLinePoints == null || idealLinePoints.Length < 2)
            {
                reason = "Ideal line requires at least 2 points.";
                return false;
            }

            if (botPathPoints == null || botPathPoints.Length < 2)
            {
                reason = "Bot path requires at least 2 points.";
                return false;
            }

            if (trackWidthMeters <= 0f)
            {
                reason = "Track width must be positive.";
                return false;
            }

            reason = string.Empty;
            return true;
        }
    }
}
