using System.Collections.Generic;
using NUnit.Framework;

namespace RKW.Physics.Tests.EditMode
{
    /// <summary>
    /// Rodada 46 (2026-09-01) founder request: "ele falar o melhor tempo
    /// nas 3 categorias 1 3 5 ... isso tudo no menu inicial" -- mirrors
    /// LapRecordMathTests' own structure/coverage, adapted for
    /// RaceRecordMath's extra "laps" filter (a race record is only
    /// comparable to another race of the SAME lap count).
    /// </summary>
    public sealed class RaceRecordMathTests
    {
        private const long DaySeconds = 24 * 60 * 60;

        [Test]
        public void FindBestRaceTimeSeconds_EmptyList_ReturnsNull()
        {
            var best = RaceRecordMath.FindBestRaceTimeSeconds(
                new List<RaceRecord>(), laps: 3, nowUnixSeconds: 1000, maxAgeSecondsOrNegativeForAllTime: -1);

            Assert.That(best, Is.Null);
        }

        [Test]
        public void FindBestRaceTimeSeconds_OnlyComparesRacesWithTheSameLapCount()
        {
            var records = new List<RaceRecord>
            {
                new RaceRecord(1, 30f, 0, "A", "239m", "rental-sport"),
                new RaceRecord(3, 95f, 0, "B", "239m", "rental-sport"),
                new RaceRecord(3, 88f, 0, "C", "239m", "rental-sport"),
                new RaceRecord(5, 150f, 0, "D", "239m", "rental-sport"),
            };

            var bestThreeLap = RaceRecordMath.FindBestRaceTimeSeconds(
                records, laps: 3, nowUnixSeconds: 0, maxAgeSecondsOrNegativeForAllTime: -1);
            var bestOneLap = RaceRecordMath.FindBestRaceTimeSeconds(
                records, laps: 1, nowUnixSeconds: 0, maxAgeSecondsOrNegativeForAllTime: -1);
            var bestTenLap = RaceRecordMath.FindBestRaceTimeSeconds(
                records, laps: 10, nowUnixSeconds: 0, maxAgeSecondsOrNegativeForAllTime: -1);

            Assert.That(bestThreeLap, Is.EqualTo(88f).Within(0.001f));
            Assert.That(bestOneLap, Is.EqualTo(30f).Within(0.001f));
            Assert.That(bestTenLap, Is.Null);
        }

        [Test]
        public void FindBestRaceTimeSeconds_DayWindow_ExcludesOlderRecords()
        {
            var now = 10 * DaySeconds;
            var records = new List<RaceRecord>
            {
                new RaceRecord(3, 90f, now - 2 * DaySeconds, "A", "239m", "rental-sport"), // too old
                new RaceRecord(3, 92f, now - 1000, "B", "239m", "rental-sport"), // within the last day
            };

            var best = RaceRecordMath.FindBestRaceTimeSeconds(records, laps: 3, now, maxAgeSecondsOrNegativeForAllTime: DaySeconds);

            Assert.That(best, Is.EqualTo(92f).Within(0.001f));
        }

        [Test]
        public void FindBestRaceTimeSeconds_IgnoresRecordsFromTheFuture()
        {
            var now = 10 * DaySeconds;
            var records = new List<RaceRecord>
            {
                new RaceRecord(3, 80f, now + DaySeconds, "A", "239m", "rental-sport"), // clock skew / bad data
            };

            var best = RaceRecordMath.FindBestRaceTimeSeconds(records, laps: 3, now, maxAgeSecondsOrNegativeForAllTime: DaySeconds);

            Assert.That(best, Is.Null);
        }

        [Test]
        public void RaceRecordPersistence_RoundTripPreservesAllFieldsExactly()
        {
            var source = new List<RaceRecord>
            {
                new RaceRecord(5, 268.75f, 123456L, "Piloto", "239m", "rental-sport"),
            };

            var decoded = RaceRecordStore.Decode(RaceRecordStore.Encode(source));

            Assert.That(decoded.Length, Is.EqualTo(1));
            Assert.That(decoded[0].Laps, Is.EqualTo(5));
            Assert.That(decoded[0].TotalTimeSeconds, Is.EqualTo(268.75f).Within(0.0001f));
            Assert.That(decoded[0].UnixTimestampSeconds, Is.EqualTo(123456L));
            Assert.That(decoded[0].PlayerName, Is.EqualTo("Piloto"));
            Assert.That(decoded[0].TrackSignature, Is.EqualTo("239m"));
            Assert.That(decoded[0].KartCategoryId, Is.EqualTo("rental-sport"));
        }

        [Test]
        public void RaceRecordPersistence_MalformedEntry_IsIgnored()
        {
            var decoded = RaceRecordStore.Decode("not-a-number,268.75,123456,239m,rental-sport,Piloto");

            Assert.That(decoded, Is.Empty);
        }

        [Test]
        public void RaceRecordPersistence_EmptyString_ReturnsEmpty()
        {
            Assert.That(RaceRecordStore.Decode(string.Empty), Is.Empty);
        }
    }
}
