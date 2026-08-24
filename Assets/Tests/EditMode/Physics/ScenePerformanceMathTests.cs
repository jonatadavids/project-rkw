using System.Collections.Generic;
using NUnit.Framework;

namespace RKW.Physics.Tests.EditMode
{
    /// <summary>
    /// M3-T01 "Evidência: Screenshot + profiler stats" pass — see
    /// ScenePerformanceMath's own XML doc for why this exists (auto-log a
    /// rough triangle/draw-call proxy on every build_deploy_verify.sh run
    /// instead of needing a manual profiler step).
    /// </summary>
    public sealed class ScenePerformanceMathTests
    {
        [Test]
        public void Summarize_SumsTrianglesAndCountsRenderers()
        {
            var summary = ScenePerformanceMath.Summarize(new List<int> { 100, 250, 50 });

            Assert.That(summary.TriangleCount, Is.EqualTo(400));
            Assert.That(summary.RendererCount, Is.EqualTo(3));
        }

        [Test]
        public void Summarize_EmptyList_IsZero()
        {
            var summary = ScenePerformanceMath.Summarize(new List<int>());

            Assert.That(summary.TriangleCount, Is.EqualTo(0));
            Assert.That(summary.RendererCount, Is.EqualTo(0));
        }

        [Test]
        public void Summarize_NullList_IsZero()
        {
            var summary = ScenePerformanceMath.Summarize(null);

            Assert.That(summary.TriangleCount, Is.EqualTo(0));
            Assert.That(summary.RendererCount, Is.EqualTo(0));
        }

        [Test]
        public void Summarize_NegativeEntry_ClampsToZeroInsteadOfReducingTotal()
        {
            var summary = ScenePerformanceMath.Summarize(new List<int> { 100, -50, 20 });

            Assert.That(summary.TriangleCount, Is.EqualTo(120));
            Assert.That(summary.RendererCount, Is.EqualTo(3));
        }

        [Test]
        public void IsWithinBudget_UnderBothLimits_IsTrue()
        {
            var summary = new ScenePerformanceMath.Summary(50000, 40);

            var result = ScenePerformanceMath.IsWithinBudget(summary, maxTriangles: 100000, maxRenderersAsDrawCallProxy: 100);

            Assert.That(result, Is.True);
        }

        [Test]
        public void IsWithinBudget_ExactlyAtBothLimits_IsTrue()
        {
            var summary = new ScenePerformanceMath.Summary(100000, 100);

            var result = ScenePerformanceMath.IsWithinBudget(summary, maxTriangles: 100000, maxRenderersAsDrawCallProxy: 100);

            Assert.That(result, Is.True);
        }

        [Test]
        public void IsWithinBudget_OverTriangleLimit_IsFalse()
        {
            var summary = new ScenePerformanceMath.Summary(100001, 10);

            var result = ScenePerformanceMath.IsWithinBudget(summary, maxTriangles: 100000, maxRenderersAsDrawCallProxy: 100);

            Assert.That(result, Is.False);
        }

        [Test]
        public void IsWithinBudget_OverRendererLimit_IsFalse()
        {
            var summary = new ScenePerformanceMath.Summary(10, 101);

            var result = ScenePerformanceMath.IsWithinBudget(summary, maxTriangles: 100000, maxRenderersAsDrawCallProxy: 100);

            Assert.That(result, Is.False);
        }
    }
}
