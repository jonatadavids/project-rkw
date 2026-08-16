using System;
using NUnit.Framework;
using RKW.Core.Identity;

namespace RKW.Core.Tests.EditMode
{
    public sealed class LeaderboardKeyPropertyTests
    {
        private const string SeedEnvironmentVariable = "RKW_PROPERTY_28_SEED";
        private const uint DefaultSeed = 0x28C0FFEEu;
        private const int CaseCount = 128;

        [Test]
        public void Property28_StrictEquality_HoldsForEveryGeneratedCase()
        {
            var seed = ResolveSeed();
            var random = new DeterministicGenerator(seed);
            TestContext.Out.WriteLine($"Property 28 seed={seed}, cases={CaseCount}");

            for (var caseIndex = 0; caseIndex < CaseCount; caseIndex++)
            {
                var original = GenerateKey(ref random, caseIndex);
                var equal = Copy(original);
                var thirdEqual = Copy(original);
                var context = Describe(seed, caseIndex, original);

                Assert.That(original.Equals(original), Is.True, $"Reflexivity failed. {context}");
                Assert.That(original.Equals(equal), Is.True, $"Equal fields were unequal. {context}");
                Assert.That(equal.Equals(original), Is.True, $"Symmetry failed. {context}");
                Assert.That(equal.Equals(thirdEqual), Is.True, $"Transitivity premise failed. {context}");
                Assert.That(original.Equals(thirdEqual), Is.True, $"Transitivity failed. {context}");
                Assert.That(original == equal, Is.True, $"Equality operator disagreed. {context}");
                Assert.That(original.GetHashCode(), Is.EqualTo(equal.GetHashCode()), $"Equal hash codes differed. {context}");

                AssertUnequal(original, WithTrackConfigurationId(original), "TrackConfigurationId", context);
                AssertUnequal(original, WithKartCategoryId(original), "KartCategoryId", context);
                AssertUnequal(original, WithTrackConditionId(original), "TrackConditionId", context);
                AssertUnequal(original, WithEnvironmentPresetId(original), "EnvironmentPresetId", context);
                AssertUnequal(original, WithGameMode(original), "GameMode", context);
                AssertUnequal(original, WithPhysicsVersion(original), "PhysicsVersion", context);
                AssertUnequal(original, WithTrackVersion(original), "TrackVersion", context);
                AssertUnequal(original, WithRulesetVersion(original), "RulesetVersion", context);
                AssertUnequal(original, WithAssistClass(original), "AssistClass", context);
            }
        }

        [Test]
        public void Constructors_RejectInvalidIdentityValues()
        {
            Assert.Throws<ArgumentException>(() => Create(trackConfigurationId: null));
            Assert.Throws<ArgumentException>(() => Create(kartCategoryId: string.Empty));
            Assert.Throws<ArgumentException>(() => Create(trackConditionId: "   "));
            Assert.Throws<ArgumentException>(() => Create(environmentPresetId: null));
            Assert.Throws<ArgumentOutOfRangeException>(() => Create(gameMode: (GameMode)int.MaxValue));
            Assert.Throws<ArgumentOutOfRangeException>(() => Create(physicsVersion: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => Create(trackVersion: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Create(rulesetVersion: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => Create(assistClass: (AssistClass)int.MaxValue));
            Assert.Throws<ArgumentException>(() => new SessionContextKey(
                string.Empty,
                "track-config",
                "kart-category",
                "dry",
                "day",
                GameMode.TimeTrial,
                1,
                1,
                1,
                AssistClass.Standardized));
        }

        [Test]
        public void SessionContextKey_UsesTrackIdAndLeaderboardDimensionsForEquality()
        {
            var first = CreateSession("track-a");
            var equal = CreateSession("track-a");
            var differentTrack = CreateSession("track-b");

            Assert.That(first, Is.EqualTo(equal));
            Assert.That(first.GetHashCode(), Is.EqualTo(equal.GetHashCode()));
            Assert.That(first, Is.Not.EqualTo(differentTrack));
            Assert.That(first.ToLeaderboardKey(), Is.EqualTo(equal.ToLeaderboardKey()));
        }

        private static uint ResolveSeed()
        {
            var configured = Environment.GetEnvironmentVariable(SeedEnvironmentVariable);
            if (string.IsNullOrEmpty(configured))
            {
                return DefaultSeed;
            }

            if (uint.TryParse(configured, out var seed))
            {
                return seed;
            }

            Assert.Fail($"{SeedEnvironmentVariable} must be an unsigned integer; received '{configured}'.");
            return DefaultSeed;
        }

        private static LeaderboardKey GenerateKey(ref DeterministicGenerator random, int caseIndex)
        {
            return new LeaderboardKey(
                RandomId(ref random, "configuration", caseIndex),
                RandomId(ref random, "category", caseIndex),
                RandomId(ref random, "condition", caseIndex),
                RandomId(ref random, "environment", caseIndex),
                (GameMode)random.Next(0, 7),
                random.Next(1, 1000),
                random.Next(1, 1000),
                random.Next(1, 1000),
                (AssistClass)random.Next(0, 2));
        }

        private static string RandomId(ref DeterministicGenerator random, string prefix, int caseIndex)
        {
            return $"{prefix}-{caseIndex}-{random.NextUInt():X8}";
        }

        private static LeaderboardKey Copy(LeaderboardKey key)
        {
            return NewKey(key);
        }

        private static LeaderboardKey WithTrackConfigurationId(LeaderboardKey key) =>
            NewKey(key, trackConfigurationId: key.TrackConfigurationId + "-changed");

        private static LeaderboardKey WithKartCategoryId(LeaderboardKey key) =>
            NewKey(key, kartCategoryId: key.KartCategoryId + "-changed");

        private static LeaderboardKey WithTrackConditionId(LeaderboardKey key) =>
            NewKey(key, trackConditionId: key.TrackConditionId + "-changed");

        private static LeaderboardKey WithEnvironmentPresetId(LeaderboardKey key) =>
            NewKey(key, environmentPresetId: key.EnvironmentPresetId + "-changed");

        private static LeaderboardKey WithGameMode(LeaderboardKey key) =>
            NewKey(key, gameMode: key.GameMode == GameMode.DrivingSchool ? GameMode.FreePractice : GameMode.DrivingSchool);

        private static LeaderboardKey WithPhysicsVersion(LeaderboardKey key) =>
            NewKey(key, physicsVersion: key.PhysicsVersion + 1);

        private static LeaderboardKey WithTrackVersion(LeaderboardKey key) =>
            NewKey(key, trackVersion: key.TrackVersion + 1);

        private static LeaderboardKey WithRulesetVersion(LeaderboardKey key) =>
            NewKey(key, rulesetVersion: key.RulesetVersion + 1);

        private static LeaderboardKey WithAssistClass(LeaderboardKey key) =>
            NewKey(key, assistClass: key.AssistClass == AssistClass.Standardized ? AssistClass.Open : AssistClass.Standardized);

        private static LeaderboardKey NewKey(
            LeaderboardKey source,
            string trackConfigurationId = null,
            string kartCategoryId = null,
            string trackConditionId = null,
            string environmentPresetId = null,
            GameMode? gameMode = null,
            int? physicsVersion = null,
            int? trackVersion = null,
            int? rulesetVersion = null,
            AssistClass? assistClass = null)
        {
            return new LeaderboardKey(
                trackConfigurationId ?? source.TrackConfigurationId,
                kartCategoryId ?? source.KartCategoryId,
                trackConditionId ?? source.TrackConditionId,
                environmentPresetId ?? source.EnvironmentPresetId,
                gameMode ?? source.GameMode,
                physicsVersion ?? source.PhysicsVersion,
                trackVersion ?? source.TrackVersion,
                rulesetVersion ?? source.RulesetVersion,
                assistClass ?? source.AssistClass);
        }

        private static void AssertUnequal(
            LeaderboardKey original,
            LeaderboardKey changed,
            string changedField,
            string context)
        {
            Assert.That(original.Equals(changed), Is.False, $"Changing {changedField} preserved equality. {context}");
            Assert.That(changed.Equals(original), Is.False, $"Changing {changedField} broke symmetric inequality. {context}");
            Assert.That(original != changed, Is.True, $"Inequality operator ignored {changedField}. {context}");
        }

        private static string Describe(uint seed, int caseIndex, LeaderboardKey key)
        {
            return $"seed={seed}, case={caseIndex}, generated="
                + $"[{key.TrackConfigurationId}|{key.KartCategoryId}|{key.TrackConditionId}|"
                + $"{key.EnvironmentPresetId}|{key.GameMode}|{key.PhysicsVersion}|{key.TrackVersion}|"
                + $"{key.RulesetVersion}|{key.AssistClass}]";
        }

        private static LeaderboardKey Create(
            string trackConfigurationId = "track-config",
            string kartCategoryId = "kart-category",
            string trackConditionId = "dry",
            string environmentPresetId = "day",
            GameMode gameMode = GameMode.TimeTrial,
            int physicsVersion = 1,
            int trackVersion = 1,
            int rulesetVersion = 1,
            AssistClass assistClass = AssistClass.Standardized)
        {
            return new LeaderboardKey(
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

        private static SessionContextKey CreateSession(string trackId)
        {
            return new SessionContextKey(
                trackId,
                "track-config",
                "kart-category",
                "dry",
                "day",
                GameMode.TimeTrial,
                1,
                1,
                1,
                AssistClass.Standardized);
        }

        private struct DeterministicGenerator
        {
            private uint state;

            public DeterministicGenerator(uint seed)
            {
                state = seed == 0 ? 0x6D2B79F5u : seed;
            }

            public uint NextUInt()
            {
                var value = state;
                value ^= value << 13;
                value ^= value >> 17;
                value ^= value << 5;
                state = value;
                return value;
            }

            public int Next(int minimumInclusive, int maximumExclusive)
            {
                return minimumInclusive + (int)(NextUInt() % (uint)(maximumExclusive - minimumInclusive));
            }
        }
    }
}
