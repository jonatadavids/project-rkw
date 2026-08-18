using UnityEngine;

namespace RKW.Physics
{
    [CreateAssetMenu(fileName = "KartPrototypeTuning", menuName = "RKW/Physics/Kart Prototype Tuning")]
    public sealed class KartCategorySO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string categoryId = "prototype-school";

        [Header("Body")]
        [Min(1f)] [SerializeField] private float massKilograms = 165f;
        [Min(0.01f)] [SerializeField] private float centerOfMassHeightMeters = 0.22f;
        [Min(0.1f)] [SerializeField] private float wheelbaseMeters = 1.05f;
        [Min(0.1f)] [SerializeField] private float rearTrackWidthMeters = 1.05f;

        [Header("Longitudinal")]
        [Min(1f)] [SerializeField] private float maxSpeedKph = 55f;
        [Min(0.1f)] [SerializeField] private float zeroToMaxSeconds = 8f;
        [Min(0f)] [SerializeField] private float brakeDeceleration = 10f;
        [Range(0.5f, 1f)] [SerializeField] private float rearBrakeDistribution = 0.7f;
        [Min(0f)] [SerializeField] private float brakeOversteerGain = 1.2f;
        [Min(0f)] [SerializeField] private float reverseAcceleration = 2.5f;
        [Min(1f)] [SerializeField] private float reverseMaxSpeedKph = 12f;
        [Min(0f)] [SerializeField] private float coastingDeceleration = 1.6f;
        [Min(0f)] [SerializeField] private float aerodynamicDrag = 0.012f;
        [Min(0f)] [SerializeField] private float steeringLossAcceleration = 2.5f;
        [Min(0.15f)] [SerializeField] private float throttleRampSeconds = 0.2f;

        [Header("Lateral grip")]
        [Min(0.1f)] [SerializeField] private float lateralGripG = 1f;
        [Range(1f, 30f)] [SerializeField] private float peakSlipAngleDegrees = 8f;
        [Range(2f, 60f)] [SerializeField] private float fullLossSlipAngleDegrees = 28f;
        [Range(0f, 1f)] [SerializeField] private float minimumGripRatio = 0.32f;
        [Min(0.1f)] [SerializeField] private float lateralResponse = 7f;
        [Min(0.1f)] [SerializeField] private float gripLossRate = 5f;
        [Min(0.1f)] [SerializeField] private float gripRecoveryRate = 2.5f;

        [Header("Steering and rigid axle")]
        [Range(1f, 60f)] [SerializeField] private float maxSteeringAngleDegrees = 28f;
        [Min(1f)] [SerializeField] private float maximumYawRateDegrees = 105f;
        [Min(0.1f)] [SerializeField] private float yawResponse = 7f;
        [Min(0f)] [SerializeField] private float yawDamping = 2.5f;
        [Min(0f)] [SerializeField] private float weightTransferGain = 3.4f;
        [Range(0f, 1f)] [SerializeField] private float innerRearLiftThreshold = 0.62f;
        [Range(0f, 1f)] [SerializeField] private float rigidAxleGripInfluence = 0.22f;
        [Range(0f, 12f)] [SerializeField] private float visualWeightTransferDegrees = 4f;

        public string CategoryId => categoryId;
        public float MassKilograms => massKilograms;
        public float CenterOfMassHeightMeters => centerOfMassHeightMeters;
        public float WheelbaseMeters => wheelbaseMeters;
        public float RearTrackWidthMeters => rearTrackWidthMeters;
        public float MaxSpeedKph => maxSpeedKph;
        public float MaxSpeedMetersPerSecond => maxSpeedKph / 3.6f;
        public float ZeroToMaxSeconds => zeroToMaxSeconds;
        public float BrakeDeceleration => brakeDeceleration;
        public float RearBrakeDistribution => rearBrakeDistribution;
        public float BrakeOversteerGain => brakeOversteerGain;
        public float ReverseAcceleration => reverseAcceleration;
        public float ReverseMaxSpeedMetersPerSecond => reverseMaxSpeedKph / 3.6f;
        public float CoastingDeceleration => coastingDeceleration;
        public float AerodynamicDrag => aerodynamicDrag;
        public float SteeringLossAcceleration => steeringLossAcceleration;
        public float ThrottleRampSeconds => throttleRampSeconds;
        public float LateralGripG => lateralGripG;
        public float PeakSlipAngleDegrees => peakSlipAngleDegrees;
        public float FullLossSlipAngleDegrees => fullLossSlipAngleDegrees;
        public float MinimumGripRatio => minimumGripRatio;
        public float LateralResponse => lateralResponse;
        public float GripLossRate => gripLossRate;
        public float GripRecoveryRate => gripRecoveryRate;
        public float MaxSteeringAngleDegrees => maxSteeringAngleDegrees;
        public float MaximumYawRateDegrees => maximumYawRateDegrees;
        public float YawResponse => yawResponse;
        public float YawDamping => yawDamping;
        public float WeightTransferGain => weightTransferGain;
        public float InnerRearLiftThreshold => innerRearLiftThreshold;
        public float RigidAxleGripInfluence => rigidAxleGripInfluence;
        public float VisualWeightTransferDegrees => visualWeightTransferDegrees;

        public bool IsValid(out string reason)
        {
            if (string.IsNullOrWhiteSpace(categoryId))
            {
                reason = "Category ID is required.";
                return false;
            }

            if (fullLossSlipAngleDegrees <= peakSlipAngleDegrees)
            {
                reason = "Full-loss slip angle must exceed peak slip angle.";
                return false;
            }

            reason = string.Empty;
            return true;
        }
    }
}
