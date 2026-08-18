using NUnit.Framework;
using RKW.Physics;

namespace RKW.Physics.Tests.EditMode
{
    /// <summary>
    /// Property 11: Recovery Trigger Conditions
    /// For any collision event regardless of severity, the system shall NOT
    /// trigger automatic recovery. Recovery is ONLY for stuck/inverted/out-of-bounds.
    ///
    /// Also tests continuous severity calculation.
    /// Validates: Requirements 4.10, 4.11
    /// </summary>
    public sealed class CollisionSeverityTests
    {
        [Test]
        public void Severity_IsContinuous_NotBinary()
        {
            var random = new System.Random(1001);

            float previousSeverity = 0f;
            for (var step = 1; step <= 50; step++)
            {
                var speed = step * 0.5f; // 0.5 to 25 m/s
                var angle = 45f;
                var severity = CollisionHandler.CalculateSeverityFromParameters(speed, angle);

                Assert.That(severity, Is.GreaterThan(previousSeverity),
                    $"Severity should increase with speed: step={step}, " +
                    $"speed={speed:F1}, severity={severity:F4}");
                previousSeverity = severity;
            }
        }

        [Test]
        public void Severity_IncreasesWithAngle()
        {
            var speed = 10f; // fixed 10 m/s
            var previousSeverity = 0f;

            for (var angle = 0f; angle <= 90f; angle += 5f)
            {
                var severity = CollisionHandler.CalculateSeverityFromParameters(speed, angle);
                Assert.That(severity, Is.GreaterThanOrEqualTo(previousSeverity),
                    $"Severity should increase with angle: angle={angle:F0}, " +
                    $"severity={severity:F4}");
                previousSeverity = severity;
            }
        }

        [Test]
        public void Severity_IsProportionalToSpeed_200Iterations()
        {
            var random = new System.Random(1102);

            for (var iteration = 0; iteration < 200; iteration++)
            {
                var speed1 = RandomFloat(random, 1f, 10f);
                var speed2 = RandomFloat(random, speed1 + 0.1f, 25f);
                var angle = RandomFloat(random, 0f, 90f);

                var sev1 = CollisionHandler.CalculateSeverityFromParameters(speed1, angle);
                var sev2 = CollisionHandler.CalculateSeverityFromParameters(speed2, angle);

                Assert.That(sev2, Is.GreaterThan(sev1),
                    $"Higher speed should produce higher severity at iteration {iteration}: " +
                    $"speed1={speed1:F1} sev={sev1:F4}, speed2={speed2:F1} sev={sev2:F4}");
            }
        }

        [Test]
        public void Severity_IsNonNegative_ForAllInputs()
        {
            var random = new System.Random(1203);

            for (var iteration = 0; iteration < 200; iteration++)
            {
                var speed = RandomFloat(random, 0f, 30f);
                var angle = RandomFloat(random, 0f, 90f);

                var severity = CollisionHandler.CalculateSeverityFromParameters(speed, angle);
                Assert.That(severity, Is.GreaterThanOrEqualTo(0f));
            }
        }

        [Test]
        public void CollisionHandler_NeverTriggersRecovery_RegardlessOfSeverity()
        {
            // This is a design contract test: CollisionHandler only applies speed loss.
            // It has no reference to any recovery system.
            // Verify by checking the class has no recovery-related methods/fields.
            var type = typeof(CollisionHandler);
            var members = type.GetMembers(
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            foreach (var member in members)
            {
                Assert.That(member.Name.ToLowerInvariant(),
                    Does.Not.Contain("recovery").And.Not.Contain("reset").And.Not.Contain("reposition"),
                    $"CollisionHandler must not contain recovery logic. Found: {member.Name}");
            }
        }

        private static float RandomFloat(System.Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }
    }
}
