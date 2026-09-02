using NUnit.Framework;
using UnityEngine;

namespace RKW.Physics.Tests.EditMode
{
    /// <summary>
    /// Property 12: Category Differentiation
    /// For any pair of distinct kart categories, at least maxSpeed, acceleration
    /// time, and lateral grip parameters SHALL have different values.
    /// Validates: Requirements 5.3
    /// </summary>
    public sealed class CategoryDifferentiationPropertyTest
    {
        [Test]
        public void SchoolAndRentalSport_DifferInAllKeyParameters()
        {
            var school = Resources.Load<KartCategorySO>("KartPhysics/PrototypeSchoolTuning");
            var rentalSport = Resources.Load<KartCategorySO>("KartPhysics/PrototypeRentalSportTuning");

            Assert.That(school, Is.Not.Null, "School tuning asset must exist");
            Assert.That(rentalSport, Is.Not.Null, "Rental Sport tuning asset must exist");

            // Must differ in at least these three parameters
            Assert.That(rentalSport.MaxSpeedKph, Is.Not.EqualTo(school.MaxSpeedKph),
                "MaxSpeed must differ between categories");
            Assert.That(rentalSport.ZeroToMaxSeconds, Is.Not.EqualTo(school.ZeroToMaxSeconds),
                "Acceleration time must differ between categories");
            Assert.That(rentalSport.LateralGripG, Is.Not.EqualTo(school.LateralGripG),
                "Lateral grip must differ between categories");
        }

        [Test]
        public void RentalSport_IsFasterThanSchool()
        {
            var school = Resources.Load<KartCategorySO>("KartPhysics/PrototypeSchoolTuning");
            var rentalSport = Resources.Load<KartCategorySO>("KartPhysics/PrototypeRentalSportTuning");

            Assert.That(school, Is.Not.Null);
            Assert.That(rentalSport, Is.Not.Null);

            Assert.That(rentalSport.MaxSpeedKph, Is.GreaterThan(school.MaxSpeedKph));
            Assert.That(rentalSport.ZeroToMaxSeconds, Is.LessThan(school.ZeroToMaxSeconds));
            Assert.That(rentalSport.LateralGripG, Is.GreaterThan(school.LateralGripG));
        }

        [Test]
        public void BothCategories_AreValid()
        {
            var school = Resources.Load<KartCategorySO>("KartPhysics/PrototypeSchoolTuning");
            var rentalSport = Resources.Load<KartCategorySO>("KartPhysics/PrototypeRentalSportTuning");

            Assert.That(school, Is.Not.Null);
            Assert.That(rentalSport, Is.Not.Null);

            Assert.That(school.IsValid(out var schoolReason), Is.True, schoolReason);
            Assert.That(rentalSport.IsValid(out var sportReason), Is.True, sportReason);
        }

        [Test]
        public void Categories_HaveDistinctIds()
        {
            var school = Resources.Load<KartCategorySO>("KartPhysics/PrototypeSchoolTuning");
            var rentalSport = Resources.Load<KartCategorySO>("KartPhysics/PrototypeRentalSportTuning");

            Assert.That(school, Is.Not.Null);
            Assert.That(rentalSport, Is.Not.Null);

            Assert.That(school.CategoryId, Is.Not.EqualTo(rentalSport.CategoryId));
            Assert.That(school.CategoryId, Is.Not.Empty);
            Assert.That(rentalSport.CategoryId, Is.Not.Empty);
        }

        // Etapa 11 (2026-08-31): 6.5 HP (PrototypeSchoolTuning) vs 13 HP
        // (PrototypeRentalSportTuning) MVP category differentiation. The
        // three tests above already establish (and this session's retune
        // preserves) that RentalSport is faster/grippier/quicker to
        // accelerate than School. These tests check the ADDITIONAL
        // dimensions the Etapa 11 spec explicitly required so the
        // difference is not just "13 HP = 6.5 HP x a single top-speed
        // multiplier": grip curve WIDTH (how forgiving a mistake is, not
        // just how much grip there is), minimum grip floor, combined-grip
        // (friction ellipse) tolerance, throttle response, braking power,
        // engine braking, and weight-transfer character.

        [Test]
        public void ThirteenHp_HasNarrowerGripCurve_ThanSixPointFiveHp()
        {
            // A narrower peak/full-loss slip angle spread means mistakes
            // are punished sooner and harder -- the "higher skill ceiling,
            // less forgiving" character the spec asked for, independent of
            // raw grip magnitude (LateralGripG).
            var sixPointFiveHp = Resources.Load<KartCategorySO>("KartPhysics/PrototypeSchoolTuning");
            var thirteenHp = Resources.Load<KartCategorySO>("KartPhysics/PrototypeRentalSportTuning");

            Assert.That(thirteenHp.PeakSlipAngleDegrees, Is.LessThan(sixPointFiveHp.PeakSlipAngleDegrees));
            Assert.That(thirteenHp.FullLossSlipAngleDegrees, Is.LessThan(sixPointFiveHp.FullLossSlipAngleDegrees));
            Assert.That(thirteenHp.MinimumGripRatio, Is.LessThan(sixPointFiveHp.MinimumGripRatio),
                "13 HP should have a lower grip floor once past peak slip -- mistakes cost more.");
        }

        [Test]
        public void ThirteenHp_HasTighterFrictionEllipseBias_MoreDemandingOnCornerExit()
        {
            var sixPointFiveHp = Resources.Load<KartCategorySO>("KartPhysics/PrototypeSchoolTuning");
            var thirteenHp = Resources.Load<KartCategorySO>("KartPhysics/PrototypeRentalSportTuning");

            Assert.That(thirteenHp.RearFrictionEllipseBias, Is.LessThan(sixPointFiveHp.RearFrictionEllipseBias),
                "13 HP's extra power should more readily eat into combined rear grip on corner exit.");
            Assert.That(thirteenHp.FrontFrictionEllipseBias, Is.LessThan(sixPointFiveHp.FrontFrictionEllipseBias));
        }

        [Test]
        public void ThirteenHp_HasSnappierThrottleResponse_ThanSixPointFiveHp()
        {
            var sixPointFiveHp = Resources.Load<KartCategorySO>("KartPhysics/PrototypeSchoolTuning");
            var thirteenHp = Resources.Load<KartCategorySO>("KartPhysics/PrototypeRentalSportTuning");

            Assert.That(thirteenHp.ThrottleRampSeconds, Is.LessThan(sixPointFiveHp.ThrottleRampSeconds),
                "13 HP should feel more throttle-sensitive (faster ramp), per the Etapa 11 spec.");
        }

        [Test]
        public void ThirteenHp_BrakesHarderAndHasRealEngineBraking()
        {
            var sixPointFiveHp = Resources.Load<KartCategorySO>("KartPhysics/PrototypeSchoolTuning");
            var thirteenHp = Resources.Load<KartCategorySO>("KartPhysics/PrototypeRentalSportTuning");

            Assert.That(thirteenHp.BrakeDeceleration, Is.GreaterThan(sixPointFiveHp.BrakeDeceleration));
            Assert.That(thirteenHp.EngineBrakingDeceleration, Is.GreaterThan(sixPointFiveHp.EngineBrakingDeceleration),
                "The bigger 13 HP engine should have noticeably more engine braking than the small 6.5 HP single.");
        }

        [Test]
        public void ThirteenHp_HasMorePronouncedWeightTransferCharacter()
        {
            var sixPointFiveHp = Resources.Load<KartCategorySO>("KartPhysics/PrototypeSchoolTuning");
            var thirteenHp = Resources.Load<KartCategorySO>("KartPhysics/PrototypeRentalSportTuning");

            Assert.That(thirteenHp.WeightTransferGain, Is.GreaterThan(sixPointFiveHp.WeightTransferGain));
        }

        [Test]
        public void DifferentiationIsNotAFlatMultiplier_AcrossAllDimensions()
        {
            // The Etapa 11 spec's explicit anti-pattern: "13 HP = 6.5 HP x
            // a single constant, applied everywhere". Collects the ratio of
            // every differentiated dimension and asserts they are NOT all
            // (approximately) equal -- a real multi-dimensional
            // differentiation should show a spread of different ratios.
            var sixPointFiveHp = Resources.Load<KartCategorySO>("KartPhysics/PrototypeSchoolTuning");
            var thirteenHp = Resources.Load<KartCategorySO>("KartPhysics/PrototypeRentalSportTuning");

            var ratios = new[]
            {
                thirteenHp.MaxSpeedKph / sixPointFiveHp.MaxSpeedKph,
                thirteenHp.ZeroToMaxSeconds / sixPointFiveHp.ZeroToMaxSeconds,
                thirteenHp.LateralGripG / sixPointFiveHp.LateralGripG,
                thirteenHp.PeakSlipAngleDegrees / sixPointFiveHp.PeakSlipAngleDegrees,
                thirteenHp.ThrottleRampSeconds / sixPointFiveHp.ThrottleRampSeconds,
                thirteenHp.RearFrictionEllipseBias / sixPointFiveHp.RearFrictionEllipseBias,
            };

            var minRatio = ratios[0];
            var maxRatio = ratios[0];
            foreach (var r in ratios)
            {
                if (r < minRatio) minRatio = r;
                if (r > maxRatio) maxRatio = r;
            }

            Assert.That(maxRatio - minRatio, Is.GreaterThan(0.15f),
                $"Ratios across dimensions should spread out meaningfully (min={minRatio:F3}, max={maxRatio:F3}) -- " +
                "a near-uniform ratio across every dimension would indicate a flat multiplier, not real differentiation.");
        }
    }
}
