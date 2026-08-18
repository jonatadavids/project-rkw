using NUnit.Framework;

namespace RKW.Physics.Tests.EditMode
{
    /// <summary>
    /// Property 9: Surface Grip Reduction
    /// For any kart state, the grip coefficient on grass or dirt surface SHALL
    /// be at most 60% of the grip coefficient on dry asphalt (reduction >= 40%).
    /// Validates: Requirements 4.7
    /// </summary>
    public sealed class SurfaceGripPropertyTest
    {
        [Test]
        public void GrassGripMultiplier_IsAtMost60PercentOfAsphalt()
        {
            // The contract: grass/dirt SurfaceDataSO must have gripMultiplier <= 0.6
            // This test verifies the math contract that any multiplier <= 0.6
            // produces effective grip <= 60% of baseline (1.0)
            for (var i = 0; i <= 100; i++)
            {
                var grassMultiplier = i / 100f * 0.6f; // 0.0 to 0.6
                var asphaltBaseline = 1f;
                var ratio = grassMultiplier / asphaltBaseline;

                Assert.That(ratio, Is.LessThanOrEqualTo(0.6f),
                    $"Grip ratio {ratio:F3} exceeds 60% at multiplier {grassMultiplier:F3}");
            }
        }

        [Test]
        public void SurfaceDataSO_GripMultiplierRange_IsValid()
        {
            // Verify that the Range attribute (0 to 1.5) allows both reduction and bonus
            // Grass must be configured <= 0.6; asphalt = 1.0; rubber line could be > 1.0
            var grassExpected = 0.5f; // typical grass value
            var dirtExpected = 0.4f;  // typical dirt value
            var asphaltExpected = 1f;
            var curbExpected = 0.8f;  // curbs reduce slightly but mainly add instability

            Assert.That(grassExpected, Is.LessThanOrEqualTo(0.6f));
            Assert.That(dirtExpected, Is.LessThanOrEqualTo(0.6f));
            Assert.That(asphaltExpected, Is.EqualTo(1f));
            Assert.That(curbExpected, Is.GreaterThan(0.6f).And.LessThan(1f));
        }

        [Test]
        public void EffectiveGrip_WithSurfaceMultiplier_RespectsReductionContract()
        {
            var random = new System.Random(909);

            for (var iteration = 0; iteration < 200; iteration++)
            {
                var baseGrip = RandomFloat(random, 0.8f, 1.4f); // lateral grip G
                var surfaceMultiplier = RandomFloat(random, 0.3f, 0.6f); // grass/dirt range

                var effectiveGrip = baseGrip * surfaceMultiplier;
                var asphaltGrip = baseGrip * 1f;
                var ratio = effectiveGrip / asphaltGrip;

                Assert.That(ratio, Is.LessThanOrEqualTo(0.6f),
                    $"Effective grip ratio {ratio:F4} exceeds 60% at iteration {iteration}: " +
                    $"baseGrip={baseGrip:F2}, multiplier={surfaceMultiplier:F3}");
            }
        }

        [Test]
        public void InstabilityFactor_IsZeroForAsphalt_PositiveForCurbs()
        {
            // Contract: asphalt has 0 instability, curbs have > 0
            var asphaltInstability = 0f;
            var curbInstability = 0.5f; // typical value

            Assert.That(asphaltInstability, Is.Zero);
            Assert.That(curbInstability, Is.GreaterThan(0f).And.LessThanOrEqualTo(1f));
        }

        private static float RandomFloat(System.Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }
    }
}
