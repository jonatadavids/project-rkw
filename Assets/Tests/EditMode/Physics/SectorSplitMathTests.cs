using NUnit.Framework;
using RKW.Physics;

namespace RKW.Physics.Tests.EditMode
{
    /// <summary>
    /// Round 44 (2026-09-01): validates the checkpoint-to-sector bucketing
    /// used by CheckpointSplitHud, for BOTH tracks this project has today
    /// (Oval: 3 raw checkpoints; Circuit2: 16) -- see SectorSplitMath's own
    /// doc comment for why a fixed "3 checkpoints" assumption would break
    /// on Circuit2.
    /// </summary>
    public sealed class SectorSplitMathTests
    {
        [Test]
        public void OvalTrack_ThreeCheckpointsThreeSectors_IsIdentityMapping()
        {
            // Every raw checkpoint on the Oval already IS its own sector.
            Assert.That(SectorSplitMath.ComputeSectorIndex(0, totalCheckpoints: 3, sectorCount: 3), Is.EqualTo(0));
            Assert.That(SectorSplitMath.ComputeSectorIndex(1, totalCheckpoints: 3, sectorCount: 3), Is.EqualTo(1));
            Assert.That(SectorSplitMath.ComputeSectorIndex(2, totalCheckpoints: 3, sectorCount: 3), Is.EqualTo(2));
        }

        [Test]
        public void Circuit2Track_SixteenCheckpoints_GroupIntoThreeEvenSectors()
        {
            // 16 checkpoints -> 3 sectors: index*3/16 (integer division).
            // Expected buckets: 0-5 -> sector 0, 6-10 -> sector 1, 11-15 -> sector 2.
            for (var i = 0; i <= 5; i++)
            {
                Assert.That(SectorSplitMath.ComputeSectorIndex(i, 16, 3), Is.EqualTo(0), $"checkpoint {i}");
            }
            for (var i = 6; i <= 10; i++)
            {
                Assert.That(SectorSplitMath.ComputeSectorIndex(i, 16, 3), Is.EqualTo(1), $"checkpoint {i}");
            }
            for (var i = 11; i <= 15; i++)
            {
                Assert.That(SectorSplitMath.ComputeSectorIndex(i, 16, 3), Is.EqualTo(2), $"checkpoint {i}");
            }
        }

        [Test]
        public void ResultIsNeverNegativeAndNeverExceedsSectorCount()
        {
            for (var total = 1; total <= 20; total++)
            {
                for (var i = 0; i < total; i++)
                {
                    var sector = SectorSplitMath.ComputeSectorIndex(i, total, 3);
                    Assert.That(sector, Is.InRange(0, 2), $"total={total}, checkpoint={i}");
                }
            }
        }

        [Test]
        public void OutOfRangeCheckpointIndex_ClampsInsteadOfThrowing()
        {
            Assert.That(SectorSplitMath.ComputeSectorIndex(-5, 16, 3), Is.EqualTo(0));
            Assert.That(SectorSplitMath.ComputeSectorIndex(999, 16, 3), Is.EqualTo(2));
        }

        [Test]
        public void ZeroCheckpointsOrSectors_ReturnsZeroInsteadOfDividingByZero()
        {
            Assert.That(SectorSplitMath.ComputeSectorIndex(0, 0, 3), Is.EqualTo(0));
            Assert.That(SectorSplitMath.ComputeSectorIndex(0, 16, 0), Is.EqualTo(0));
        }
    }
}
