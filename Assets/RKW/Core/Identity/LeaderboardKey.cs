using System;

namespace RKW.Core.Identity
{
    /// <summary>
    /// Immutable domain value object that identifies every dimension used to compare competitive times.
    /// Direction belongs to TrackConfigurationId and is intentionally not duplicated.
    /// </summary>
    /// <remarks>
    /// Do not serialize this domain object directly with Unity JsonUtility. Transport or persistence DTOs
    /// will be introduced with their first real consumer. Their persistent format must encode enums by
    /// stable names rather than ordinal values and must pass a round-trip test before Cloud Save or Cloud
    /// Code integration.
    /// </remarks>
    public sealed class LeaderboardKey : IEquatable<LeaderboardKey>
    {
        public LeaderboardKey(
            string trackConfigurationId,
            string kartCategoryId,
            string trackConditionId,
            string environmentPresetId,
            GameMode gameMode,
            int physicsVersion,
            int trackVersion,
            int rulesetVersion,
            AssistClass assistClass)
        {
            TrackConfigurationId = IdentityKeyValidation.RequiredId(trackConfigurationId, nameof(trackConfigurationId));
            KartCategoryId = IdentityKeyValidation.RequiredId(kartCategoryId, nameof(kartCategoryId));
            TrackConditionId = IdentityKeyValidation.RequiredId(trackConditionId, nameof(trackConditionId));
            EnvironmentPresetId = IdentityKeyValidation.RequiredId(environmentPresetId, nameof(environmentPresetId));
            GameMode = IdentityKeyValidation.DefinedEnum(gameMode, nameof(gameMode));
            PhysicsVersion = IdentityKeyValidation.PositiveVersion(physicsVersion, nameof(physicsVersion));
            TrackVersion = IdentityKeyValidation.PositiveVersion(trackVersion, nameof(trackVersion));
            RulesetVersion = IdentityKeyValidation.PositiveVersion(rulesetVersion, nameof(rulesetVersion));
            AssistClass = IdentityKeyValidation.DefinedEnum(assistClass, nameof(assistClass));
        }

        public string TrackConfigurationId { get; }

        public string KartCategoryId { get; }

        public string TrackConditionId { get; }

        public string EnvironmentPresetId { get; }

        public GameMode GameMode { get; }

        public int PhysicsVersion { get; }

        public int TrackVersion { get; }

        public int RulesetVersion { get; }

        public AssistClass AssistClass { get; }

        public bool Equals(LeaderboardKey other)
        {
            return other is not null
                && string.Equals(TrackConfigurationId, other.TrackConfigurationId, StringComparison.Ordinal)
                && string.Equals(KartCategoryId, other.KartCategoryId, StringComparison.Ordinal)
                && string.Equals(TrackConditionId, other.TrackConditionId, StringComparison.Ordinal)
                && string.Equals(EnvironmentPresetId, other.EnvironmentPresetId, StringComparison.Ordinal)
                && GameMode == other.GameMode
                && PhysicsVersion == other.PhysicsVersion
                && TrackVersion == other.TrackVersion
                && RulesetVersion == other.RulesetVersion
                && AssistClass == other.AssistClass;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as LeaderboardKey);
        }

        /// <summary>
        /// Produces a hash only for in-memory collections. It is not a persistent or canonical leaderboard ID.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = StringComparer.Ordinal.GetHashCode(TrackConfigurationId);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(KartCategoryId);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(TrackConditionId);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(EnvironmentPresetId);
                hashCode = (hashCode * 397) ^ (int)GameMode;
                hashCode = (hashCode * 397) ^ PhysicsVersion;
                hashCode = (hashCode * 397) ^ TrackVersion;
                hashCode = (hashCode * 397) ^ RulesetVersion;
                return (hashCode * 397) ^ (int)AssistClass;
            }
        }

        public static bool operator ==(LeaderboardKey left, LeaderboardKey right)
        {
            return ReferenceEquals(left, right) || (left is not null && left.Equals(right));
        }

        public static bool operator !=(LeaderboardKey left, LeaderboardKey right)
        {
            return !(left == right);
        }
    }
}
