using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace RKW.Physics.Tests.EditMode
{
    /// <summary>
    /// Post-race leaderboard selection logic. Round 8 founder feedback
    /// (2026-08-20) replaced the original all-time/day/week idea with a
    /// named top-5 ("talvez pegar os 5 + o restante só descartar").
    /// </summary>
    public sealed class LapRecordMathTests
    {
        private const long DaySeconds = 24 * 60 * 60;
        private const long WeekSeconds = 7 * DaySeconds;

        [Test]
        public void FindBestLapTimeSeconds_EmptyList_ReturnsNull()
        {
            var best = LapRecordMath.FindBestLapTimeSeconds(new List<LapRecord>(), nowUnixSeconds: 1000, maxAgeSecondsOrNegativeForAllTime: -1);

            Assert.That(best, Is.Null);
        }

        [Test]
        public void FindBestLapTimeSeconds_AllTime_ReturnsFastestRegardlessOfAge()
        {
            var records = new List<LapRecord>
            {
                new LapRecord(45.2f, unixTimestampSeconds: 0, playerName: "A"),
                new LapRecord(38.7f, unixTimestampSeconds: 1_000_000, playerName: "B"),
                new LapRecord(52.1f, unixTimestampSeconds: 2_000_000, playerName: "C"),
            };

            var best = LapRecordMath.FindBestLapTimeSeconds(records, nowUnixSeconds: 2_000_000, maxAgeSecondsOrNegativeForAllTime: -1);

            Assert.That(best, Is.EqualTo(38.7f).Within(0.001f));
        }

        [Test]
        public void FindBestLapTimeSeconds_DayWindow_ExcludesOlderRecords()
        {
            var now = 10 * DaySeconds;
            var records = new List<LapRecord>
            {
                new LapRecord(30f, unixTimestampSeconds: now - 2 * DaySeconds, playerName: "A"), // too old for "today"
                new LapRecord(40f, unixTimestampSeconds: now - 1000, playerName: "B"), // within the last day
            };

            var best = LapRecordMath.FindBestLapTimeSeconds(records, now, maxAgeSecondsOrNegativeForAllTime: DaySeconds);

            Assert.That(best, Is.EqualTo(40f).Within(0.001f));
        }

        [Test]
        public void FindBestLapTimeSeconds_WeekWindow_IncludesRecordFromFourDaysAgo()
        {
            var now = 10 * DaySeconds;
            var records = new List<LapRecord>
            {
                new LapRecord(33f, unixTimestampSeconds: now - 4 * DaySeconds, playerName: "A"),
            };

            var best = LapRecordMath.FindBestLapTimeSeconds(records, now, maxAgeSecondsOrNegativeForAllTime: WeekSeconds);

            Assert.That(best, Is.EqualTo(33f).Within(0.001f));
        }

        [Test]
        public void FindBestLapTimeSeconds_NoRecordWithinWindow_ReturnsNull()
        {
            var now = 10 * DaySeconds;
            var records = new List<LapRecord>
            {
                new LapRecord(33f, unixTimestampSeconds: now - 3 * WeekSeconds, playerName: "A"),
            };

            var best = LapRecordMath.FindBestLapTimeSeconds(records, now, maxAgeSecondsOrNegativeForAllTime: DaySeconds);

            Assert.That(best, Is.Null);
        }

        [Test]
        public void FindBestLapTimeSeconds_IgnoresRecordsFromTheFuture()
        {
            var now = 10 * DaySeconds;
            var records = new List<LapRecord>
            {
                new LapRecord(20f, unixTimestampSeconds: now + DaySeconds, playerName: "A"), // clock skew / bad data
            };

            var best = LapRecordMath.FindBestLapTimeSeconds(records, now, maxAgeSecondsOrNegativeForAllTime: WeekSeconds);

            Assert.That(best, Is.Null);
        }

        [Test]
        public void FindTopRecords_ReturnsFastestFirst()
        {
            var records = new List<LapRecord>
            {
                new LapRecord(45.2f, 0, "Devagar"),
                new LapRecord(38.7f, 0, "Rapido"),
                new LapRecord(41.0f, 0, "Meio"),
            };

            var top = LapRecordMath.FindTopRecords(records, 5);

            Assert.That(top.Count, Is.EqualTo(3));
            Assert.That(top[0].PlayerName, Is.EqualTo("Rapido"));
            Assert.That(top[1].PlayerName, Is.EqualTo("Meio"));
            Assert.That(top[2].PlayerName, Is.EqualTo("Devagar"));
        }

        [Test]
        public void FindTopRecords_DiscardsBeyondCount()
        {
            var records = new List<LapRecord>();
            for (var i = 0; i < 12; i++)
            {
                records.Add(new LapRecord(10f + i, 0, $"P{i}"));
            }

            var top = LapRecordMath.FindTopRecords(records, 5);

            Assert.That(top.Count, Is.EqualTo(5));
            Assert.That(top[4].LapTimeSeconds, Is.EqualTo(14f).Within(0.001f));
        }

        [Test]
        public void FindTopRecords_FewerRecordsThanCount_ReturnsAllOfThem()
        {
            var records = new List<LapRecord> { new LapRecord(30f, 0, "Sozinho") };

            var top = LapRecordMath.FindTopRecords(records, 5);

            Assert.That(top.Count, Is.EqualTo(1));
        }

        [Test]
        public void FindTopRecords_EmptyList_ReturnsEmpty()
        {
            var top = LapRecordMath.FindTopRecords(new List<LapRecord>(), 5);

            Assert.That(top, Is.Empty);
        }

        [Test]
        public void FindTopRecords_ZeroCount_ReturnsEmpty()
        {
            var records = new List<LapRecord> { new LapRecord(30f, 0, "A") };

            var top = LapRecordMath.FindTopRecords(records, 0);

            Assert.That(top, Is.Empty);
        }

        // Round 23 follow-up (2026-08-24): "depois que alteramos a pista a
        // melhor volta ficou travada na ultima deveria ser reiniciada toda
        // vez que a gente colocar uma pista nova" — track-signature tests.
        [Test]
        public void CalculateClosedPathLengthMeters_Square_ReturnsPerimeter()
        {
            var path = new List<Vector3>
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(10f, 0f, 0f),
                new Vector3(10f, 0f, 10f),
                new Vector3(0f, 0f, 10f),
            };

            var length = LapRecordMath.CalculateClosedPathLengthMeters(path);

            // 4 sides of 10m, including the closing segment back to the
            // start (this is a LOOP, not an open polyline).
            Assert.That(length, Is.EqualTo(40f).Within(0.001f));
        }

        [Test]
        public void CalculateClosedPathLengthMeters_FewerThanTwoPoints_ReturnsZero()
        {
            Assert.That(LapRecordMath.CalculateClosedPathLengthMeters(null), Is.EqualTo(0f));
            Assert.That(LapRecordMath.CalculateClosedPathLengthMeters(new List<Vector3>()), Is.EqualTo(0f));
            Assert.That(LapRecordMath.CalculateClosedPathLengthMeters(new List<Vector3> { Vector3.zero }), Is.EqualTo(0f));
        }

        [Test]
        public void FormatTrackSignature_RoundsToNearestMeter()
        {
            Assert.That(LapRecordMath.FormatTrackSignature(238.6f), Is.EqualTo("239m"));
            Assert.That(LapRecordMath.FormatTrackSignature(238.4f), Is.EqualTo("238m"));
        }

        [Test]
        public void FilterByTrackSignature_OnlyKeepsMatchingSignature()
        {
            var records = new List<LapRecord>
            {
                new LapRecord(40f, 0, "A", "239m"),
                new LapRecord(35f, 0, "B", "276m"),
                new LapRecord(41f, 0, "C", "239m"),
            };

            var filtered = LapRecordMath.FilterByTrackSignature(records, "239m");

            Assert.That(filtered.Count, Is.EqualTo(2));
            Assert.That(filtered[0].PlayerName, Is.EqualTo("A"));
            Assert.That(filtered[1].PlayerName, Is.EqualTo("C"));
        }

        [Test]
        public void FilterByTrackSignature_EmptySignature_ReturnsEmpty()
        {
            var records = new List<LapRecord> { new LapRecord(40f, 0, "A", "239m") };

            var filtered = LapRecordMath.FilterByTrackSignature(records, "");

            Assert.That(filtered, Is.Empty);
        }

        [Test]
        public void FilterByTrackSignature_NullRecords_ReturnsEmpty()
        {
            var filtered = LapRecordMath.FilterByTrackSignature(null, "239m");

            Assert.That(filtered, Is.Empty);
        }

        [Test]
        public void FilterByComparisonScope_SeparatesKartCategoriesOnSameTrack()
        {
            var records = new List<LapRecord>
            {
                new LapRecord(41f, 1, "Rental", "239m", "rental-sport"),
                new LapRecord(35f, 2, "Plus", "239m", "sport-plus"),
                new LapRecord(40f, 3, "Rental 2", "239m", "rental-sport"),
                new LapRecord(30f, 4, "Outra pista", "300m", "rental-sport"),
            };

            var filtered = LapRecordMath.FilterByComparisonScope(
                records, new PrototypeCompetitiveScope("239m", "rental-sport"));

            Assert.That(filtered.Count, Is.EqualTo(2));
            Assert.That(filtered[0].PlayerName, Is.EqualTo("Rental"));
            Assert.That(filtered[1].PlayerName, Is.EqualTo("Rental 2"));
        }

        [Test]
        public void PrototypeCompetitiveScope_UsesOrdinalIdentityAndDistinctStorageKeys()
        {
            var rental = new PrototypeCompetitiveScope("239m", "rental-sport");
            var rentalAgain = new PrototypeCompetitiveScope("239m", "rental-sport");
            var caseVariant = new PrototypeCompetitiveScope("239m", "Rental-Sport");
            var plus = new PrototypeCompetitiveScope("239m", "sport-plus");

            Assert.That(rental, Is.EqualTo(rentalAgain));
            Assert.That(rental, Is.Not.EqualTo(caseVariant));
            Assert.That(GhostRecordStore.BuildStorageKey(rental, 3),
                Is.Not.EqualTo(GhostRecordStore.BuildStorageKey(plus, 3)));
            Assert.That(GhostRecordStore.BuildStorageKey(rental, 3),
                Is.Not.EqualTo(GhostRecordStore.BuildStorageKey(rental, 5)));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase(" rental-sport")]
        [TestCase("rental-sport ")]
        [TestCase("rental\tsport")]
        public void PrototypeCompetitiveScope_InvalidCategory_Throws(string categoryId)
        {
            Assert.That(() => new PrototypeCompetitiveScope("239m", categoryId),
                Throws.ArgumentException);
        }

        [Test]
        public void LapRecordPersistence_RoundTripPreservesCategoryExactly()
        {
            var source = new List<LapRecord>
            {
                new LapRecord(42.125f, 123456L, "Piloto", "239m", "rental-sport"),
            };

            var decoded = LapRecordStore.Decode(LapRecordStore.Encode(source));

            Assert.That(decoded.Length, Is.EqualTo(1));
            Assert.That(decoded[0].LapTimeSeconds, Is.EqualTo(42.125f).Within(0.0001f));
            Assert.That(decoded[0].UnixTimestampSeconds, Is.EqualTo(123456L));
            Assert.That(decoded[0].PlayerName, Is.EqualTo("Piloto"));
            Assert.That(decoded[0].TrackSignature, Is.EqualTo("239m"));
            Assert.That(decoded[0].KartCategoryId, Is.EqualTo("rental-sport"));
        }

        [Test]
        public void LapRecordPersistence_CategorylessLegacyEntry_IsIgnored()
        {
            var decoded = LapRecordStore.Decode("42.125,123456,239m,Piloto");

            Assert.That(decoded, Is.Empty);
        }
    }
}
