using NUnit.Framework;
using RKW.Physics;
using UnityEngine;

namespace RKW.Physics.Tests.EditMode
{
    /// <summary>
    /// Property 9: Surface Grip Reduction.
    /// For any kart state, the grip coefficient on grass/dirt surfaces
    /// SHALL be at most 60% of the grip coefficient on dry asphalt.
    ///
    /// Rewritten (2026-08-31, rigid rear axle/friction ellipse round): the
    /// previous version of this file asserted properties of hardcoded local
    /// float literals (0.5f, 0.6f, etc.) that never touched SurfaceDataSO
    /// or KartDynamics at all -- it would have kept passing even if
    /// SurfaceDataSO.Configure's clamp broke entirely. This version drives
    /// the REAL production SurfaceDataSO.Configure with the SAME grip value
    /// KartPhysicsPrototypeBootstrap actually uses for grass
    /// (see CreateSurface("Grass_Inner", ...) and circuit2GrassData.Configure(...),
    /// both 0.5f), and reads the result back through the real GripMultiplier
    /// property. See KartSurfaceGripIntegrationTests (PlayMode) for the
    /// further end-to-end check that this real multiplier actually reduces
    /// a live kart's cornering capacity through OnTriggerEnter.
    /// </summary>
    public sealed class SurfaceGripPropertyTest
    {
        // The actual value KartPhysicsPrototypeBootstrap configures for
        // every grass surface in the game today.
        private const float RealGrassGripMultiplier = 0.5f;

        [Test]
        public void RealGrassConfiguration_GripMultiplier_IsAtMost60PercentOfAsphalt()
        {
            var grass = ScriptableObject.CreateInstance<SurfaceDataSO>();
            grass.Configure("grass_test", "Grass (Test)", RealGrassGripMultiplier, 0f, true);

            Assert.That(grass.GripMultiplier, Is.LessThanOrEqualTo(0.6f),
                $"Grass GripMultiplier ({grass.GripMultiplier:F3}) exceeds the 60%-of-asphalt contract.");

            Object.DestroyImmediate(grass);
        }

        [Test]
        public void Configure_ClampsGripMultiplier_ToDocumentedRange()
        {
            var surface = ScriptableObject.CreateInstance<SurfaceDataSO>();

            surface.Configure("over", "Over", 5f, 0f, false);
            Assert.That(surface.GripMultiplier, Is.EqualTo(1.5f).Within(0.0001f),
                "Configure should clamp an over-range grip multiplier to the documented maximum (1.5).");

            surface.Configure("under", "Under", -2f, 0f, false);
            Assert.That(surface.GripMultiplier, Is.EqualTo(0f).Within(0.0001f),
                "Configure should clamp a negative grip multiplier to 0.");

            Object.DestroyImmediate(surface);
        }

        [Test]
        public void Configure_ClampsInstabilityFactor_To01()
        {
            var surface = ScriptableObject.CreateInstance<SurfaceDataSO>();

            surface.Configure("curb", "Curb", 0.8f, 3f, false);
            Assert.That(surface.InstabilityFactor, Is.EqualTo(1f).Within(0.0001f));

            surface.Configure("curb2", "Curb2", 0.8f, -1f, false);
            Assert.That(surface.InstabilityFactor, Is.EqualTo(0f).Within(0.0001f));

            Object.DestroyImmediate(surface);
        }

        [Test]
        public void Configure_AsphaltBaseline_HasFullGripAndNoInstability()
        {
            var asphalt = ScriptableObject.CreateInstance<SurfaceDataSO>();
            asphalt.Configure("asphalt_dry", "Asphalt (Dry)", 1f, 0f, false);

            Assert.That(asphalt.GripMultiplier, Is.EqualTo(1f));
            Assert.That(asphalt.InstabilityFactor, Is.EqualTo(0f));
            Assert.That(asphalt.IsOffTrack, Is.False);

            Object.DestroyImmediate(asphalt);
        }

        [Test]
        public void RollingResistanceMultiplier_DefaultsToOne_ForEveryExistingSurface()
        {
            // Etapa 5 (2026-08-31): new field, must default to a pure
            // no-op (1.0) so every asset/procedural surface predating this
            // etapa keeps its exact old rolling-resistance behavior.
            var surface = ScriptableObject.CreateInstance<SurfaceDataSO>();
            Assert.That(surface.RollingResistanceMultiplier, Is.EqualTo(1f));
            Object.DestroyImmediate(surface);
        }
    }
}
