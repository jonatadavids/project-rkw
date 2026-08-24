using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace RKW.Physics.Tests.EditMode
{
    /// <summary>Live standings ranking proxy (founder playtest feedback, 2026-08-20: "quero ver classificação durante a corrida").</summary>
    public sealed class RaceProgressMathTests
    {
        private static readonly List<Vector3> Path = new List<Vector3>
        {
            new Vector3(0f, 0f, 0f),
            new Vector3(10f, 0f, 0f),
            new Vector3(10f, 0f, 10f),
            new Vector3(0f, 0f, 10f),
        };

        [Test]
        public void FindNearestWaypointIndex_ExactMatch_ReturnsThatIndex()
        {
            var index = RaceProgressMath.FindNearestWaypointIndex(new Vector3(10f, 0f, 10f), Path);

            Assert.That(index, Is.EqualTo(2));
        }

        [Test]
        public void FindNearestWaypointIndex_ClosePosition_ReturnsClosestIndex()
        {
            var index = RaceProgressMath.FindNearestWaypointIndex(new Vector3(9f, 0f, 0.5f), Path);

            Assert.That(index, Is.EqualTo(1));
        }

        [Test]
        public void FindNearestWaypointIndex_IgnoresHeight()
        {
            var index = RaceProgressMath.FindNearestWaypointIndex(new Vector3(0f, 50f, 0f), Path);

            Assert.That(index, Is.EqualTo(0));
        }

        [Test]
        public void FindNearestWaypointIndex_EmptyPath_ReturnsZero()
        {
            var index = RaceProgressMath.FindNearestWaypointIndex(Vector3.zero, new List<Vector3>());

            Assert.That(index, Is.EqualTo(0));
        }

        [Test]
        public void FindNearestWaypointIndex_NullPath_ReturnsZero()
        {
            var index = RaceProgressMath.FindNearestWaypointIndex(Vector3.zero, null);

            Assert.That(index, Is.EqualTo(0));
        }

        [Test]
        public void IsAheadOf_MoreLapsWinsRegardlessOfWaypoint()
        {
            Assert.That(RaceProgressMath.IsAheadOf(2, 0, 1, 7), Is.True);
        }

        [Test]
        public void IsAheadOf_SameLap_HigherWaypointIndexWins()
        {
            Assert.That(RaceProgressMath.IsAheadOf(1, 5, 1, 2), Is.True);
            Assert.That(RaceProgressMath.IsAheadOf(1, 2, 1, 5), Is.False);
        }

        [Test]
        public void IsAheadOf_ExactTie_IsFalse()
        {
            Assert.That(RaceProgressMath.IsAheadOf(1, 3, 1, 3), Is.False);
        }

        [Test]
        public void IsBetterComparisonCandidate_MoreLapsWinsRegardlessOfTime()
        {
            Assert.That(RaceProgressMath.IsBetterComparisonCandidate(3, 999f, 2, 1f), Is.True);
        }

        [Test]
        public void IsBetterComparisonCandidate_FewerLapsLosesRegardlessOfTime()
        {
            Assert.That(RaceProgressMath.IsBetterComparisonCandidate(1, 1f, 2, 999f), Is.False);
        }

        [Test]
        public void IsBetterComparisonCandidate_SameLaps_LowerTotalTimeWins()
        {
            Assert.That(RaceProgressMath.IsBetterComparisonCandidate(2, 30f, 2, 40f), Is.True);
            Assert.That(RaceProgressMath.IsBetterComparisonCandidate(2, 40f, 2, 30f), Is.False);
        }

        [Test]
        public void IsBetterComparisonCandidate_ExactTie_IsFalse()
        {
            Assert.That(RaceProgressMath.IsBetterComparisonCandidate(2, 30f, 2, 30f), Is.False);
        }

        [Test]
        public void IsBetterComparisonCandidate_FirstCandidateAgainstSentinel_IsTrue()
        {
            // RaceManager.SelectComparisonBot seeds bestLaps=-1,
            // bestTotalTime=float.MaxValue so the very first bot examined
            // always wins the comparison and becomes the initial "best".
            Assert.That(RaceProgressMath.IsBetterComparisonCandidate(0, 5f, -1, float.MaxValue), Is.True);
        }
    }
}
