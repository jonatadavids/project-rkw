using System;

namespace RKW.Core.Identity
{
    /// <summary>
    /// Identifies the complete competitive context of a session. Additional telemetry belongs to
    /// telemetry records until a concrete identity dimension has an approved consumer.
    /// </summary>
    public sealed class SessionContextKey : IEquatable<SessionContextKey>
    {
        private readonly LeaderboardKey leaderboardKey;

        public SessionContextKey(
            string trackId,
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
            TrackId = IdentityKeyValidation.RequiredId(trackId, nameof(trackId));
            leaderboardKey = new LeaderboardKey(
                trackConfigurationId,
                kartCategoryId,
                trackConditionId,
                environmentPresetId,
                gameMode,
                physicsVersion,
                trackVersion,
                rulesetVersion,
                assistClass);
        }

        public string TrackId { get; }

        public string TrackConfigurationId => leaderboardKey.TrackConfigurationId;

        public string KartCategoryId => leaderboardKey.KartCategoryId;

        public string TrackConditionId => leaderboardKey.TrackConditionId;

        public string EnvironmentPresetId => leaderboardKey.EnvironmentPresetId;

        public GameMode GameMode => leaderboardKey.GameMode;

        public int PhysicsVersion => leaderboardKey.PhysicsVersion;

        public int TrackVersion => leaderboardKey.TrackVersion;

        public int RulesetVersion => leaderboardKey.RulesetVersion;

        public AssistClass AssistClass => leaderboardKey.AssistClass;

        public LeaderboardKey ToLeaderboardKey()
        {
            return leaderboardKey;
        }

        public bool Equals(SessionContextKey other)
        {
            return other is not null
                && string.Equals(TrackId, other.TrackId, StringComparison.Ordinal)
                && leaderboardKey.Equals(other.leaderboardKey);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as SessionContextKey);
        }

        /// <summary>
        /// Produces a hash only for in-memory collections. It is not a persistent or canonical session ID.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                return (StringComparer.Ordinal.GetHashCode(TrackId) * 397) ^ leaderboardKey.GetHashCode();
            }
        }

        public static bool operator ==(SessionContextKey left, SessionContextKey right)
        {
            return ReferenceEquals(left, right) || (left is not null && left.Equals(right));
        }

        public static bool operator !=(SessionContextKey left, SessionContextKey right)
        {
            return !(left == right);
        }
    }
}
