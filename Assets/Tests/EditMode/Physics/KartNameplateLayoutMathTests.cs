using NUnit.Framework;
using RKW.Physics;
using UnityEngine;

namespace RKW.Physics.Tests.EditMode
{
    /// <summary>
    /// Round 46 (2026-09-01): checks the pure math behind
    /// KartNameplateHud's screen-space label placement (see
    /// KartNameplateLayoutMath's own doc comment for why
    /// Camera.WorldToScreenPoint itself is left out of scope here).
    /// </summary>
    public sealed class KartNameplateLayoutMathTests
    {
        [Test]
        public void IsBehindCamera_TrueForNonPositiveZ()
        {
            Assert.That(KartNameplateLayoutMath.IsBehindCamera(new Vector3(0f, 0f, 0f)), Is.True);
            Assert.That(KartNameplateLayoutMath.IsBehindCamera(new Vector3(0f, 0f, -5f)), Is.True);
            Assert.That(KartNameplateLayoutMath.IsBehindCamera(new Vector3(0f, 0f, 5f)), Is.False);
        }

        [Test]
        public void ComputeLabelRect_CentersOnScreenPoint_AndFlipsYAxis()
        {
            // Unity screen space: Y=100 out of a 720-tall screen means
            // near the BOTTOM of the screen.
            var screenPoint = new Vector3(400f, 100f, 10f);
            var rect = KartNameplateLayoutMath.ComputeLabelRect(screenPoint, screenHeight: 720f, scale: 1f);

            const float expectedCenterX = 400f;
            // OnGUI space has the opposite Y direction, so the same
            // "near the bottom" point should land at 720-100=620, still
            // near the bottom of the OnGUI Rect coordinate space.
            const float expectedCenterY = 720f - 100f;
            Assert.That(rect.x + rect.width * 0.5f, Is.EqualTo(expectedCenterX).Within(0.01f));
            Assert.That(rect.y + rect.height * 0.5f, Is.EqualTo(expectedCenterY).Within(0.01f));
        }

        [Test]
        public void ComputeLabelRect_ScalesWidthAndHeight()
        {
            var rect = KartNameplateLayoutMath.ComputeLabelRect(new Vector3(0f, 0f, 10f), screenHeight: 720f, scale: 2f);
            Assert.That(rect.width, Is.EqualTo(KartNameplateLayoutMath.LabelWidthRaw * 2f).Within(0.01f));
            Assert.That(rect.height, Is.EqualTo(KartNameplateLayoutMath.LabelHeightRaw * 2f).Within(0.01f));
        }
    }
}
