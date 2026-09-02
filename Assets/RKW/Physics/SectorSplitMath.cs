using UnityEngine;

namespace RKW.Physics
{
    /// <summary>
    /// Round 44 (2026-09-01) founder feedback: "vc pode colocar checkpoint
    /// sei la dividir a pista em 3". The two tracks in this project have a
    /// very different number of internal lap-validity checkpoints (the
    /// Oval has exactly 3 -- CP1/CP2/CP3 -- but Circuit2 has 16, one per
    /// filleted vertex, see KartPhysicsPrototypeBootstrap.SetupTiming), and
    /// that raw count is tuned for validating the lap, not for what a
    /// player wants to see on screen. Showing 16 rows of split times on
    /// Circuit2 would be unreadable clutter, and would not match what the
    /// founder actually asked for ("dividir a pista em 3").
    ///
    /// This groups whatever number of raw checkpoints a track has into
    /// exactly <paramref name="sectorCount"/> even buckets by index order
    /// (checkpoints are always hit in increasing index order within a lap,
    /// enforced by TimingManagerLite's own validity check, so "last
    /// checkpoint seen in a bucket" is always that bucket's final/most
    /// complete split). On the Oval (3 checkpoints, 3 sectors) this is the
    /// identity mapping -- each checkpoint IS a sector, exactly as before.
    /// </summary>
    public static class SectorSplitMath
    {
        /// <summary>
        /// Which of <paramref name="sectorCount"/> display sectors a raw
        /// <paramref name="checkpointIndex"/> (0-based, out of
        /// <paramref name="totalCheckpoints"/>) belongs to.
        /// </summary>
        public static int ComputeSectorIndex(int checkpointIndex, int totalCheckpoints, int sectorCount)
        {
            if (totalCheckpoints <= 0 || sectorCount <= 0)
            {
                return 0;
            }

            var clampedIndex = Mathf.Clamp(checkpointIndex, 0, totalCheckpoints - 1);
            var sector = clampedIndex * sectorCount / totalCheckpoints; // integer division -> floor
            return Mathf.Clamp(sector, 0, sectorCount - 1);
        }
    }
}
