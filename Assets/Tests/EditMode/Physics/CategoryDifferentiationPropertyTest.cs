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
    }
}
