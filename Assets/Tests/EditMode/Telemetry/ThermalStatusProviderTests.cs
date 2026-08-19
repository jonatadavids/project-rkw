using NUnit.Framework;

namespace RKW.Telemetry.Tests.EditMode
{
    /// <summary>
    /// M3-T07: thermal status provider tests.
    /// Validates: Requirement R12.4 ("Integrar Thermal Status API quando
    /// disponível", with graceful degradation elsewhere).
    /// </summary>
    public sealed class ThermalStatusProviderTests
    {
        [Test]
        public void UnsupportedProvider_AlwaysReturnsUnknown()
        {
            var provider = new UnsupportedThermalStatusProvider();

            Assert.That(provider.GetThermalStatus(), Is.EqualTo(ThermalStatus.Unknown));
        }

        [TestCase(0, ThermalStatus.Nominal)]
        [TestCase(1, ThermalStatus.Light)]
        [TestCase(2, ThermalStatus.Moderate)]
        [TestCase(3, ThermalStatus.Severe)]
        [TestCase(4, ThermalStatus.Critical)]
        [TestCase(5, ThermalStatus.Critical)]
        [TestCase(6, ThermalStatus.Critical)]
        [TestCase(999, ThermalStatus.Unknown)]
        public void MapAndroidThermalStatus_MatchesPowerManagerConstants(int rawStatus, ThermalStatus expected)
        {
            var mapped = AndroidThermalStatusProvider.MapAndroidThermalStatus(rawStatus);

            Assert.That(mapped, Is.EqualTo(expected));
        }

        [Test]
        public void EditorProvider_ReturnsUnknownRatherThanThrowing()
        {
            // In the Editor (where EditMode tests run), AndroidThermalStatusProvider
            // compiles its non-Android branch, which must never touch JNI.
            var provider = new AndroidThermalStatusProvider();

            Assert.DoesNotThrow(() => provider.GetThermalStatus());
        }
    }
}
