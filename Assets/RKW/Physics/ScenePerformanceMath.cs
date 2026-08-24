using System.Collections.Generic;
using UnityEngine;

namespace RKW.Physics
{
    /// <summary>
    /// Pure aggregation/budget-check logic for a lightweight in-scene
    /// performance snapshot. Founder playtest feedback context (M3-T01,
    /// "Manter dentro de budget: ≤ 100K triângulos, ≤ 100 draw calls (tier
    /// low)"): the project's build/deploy pipeline
    /// (scripts/build_deploy_verify.sh) already auto-captures a device
    /// screenshot + logcat dump on every run, but never captured any actual
    /// performance numbers — "evidência formal (screenshot + profiler
    /// stats)" always needed a separate manual step nobody had done yet.
    /// <see cref="ScenePerformanceLogger"/> uses this pure logic to log a
    /// one-line summary a few seconds after the scene loads, which lands in
    /// the same logcat dump the script already pulls — so evidence is
    /// produced automatically on the next run, no extra step required.
    ///
    /// Deliberately a rough proxy, not a real GPU profiler connection: a
    /// renderer count is an upper bound on draw calls (it ignores Unity's
    /// automatic static/dynamic batching, which can only ever reduce the
    /// real number), and a triangle sum from shared meshes is the same
    /// geometry Unity itself would submit. Good enough to catch a real
    /// budget blowout without needing a live Profiler window attached to a
    /// device build, which this headless pipeline has no way to drive.
    /// </summary>
    public static class ScenePerformanceMath
    {
        public readonly struct Summary
        {
            public readonly int TriangleCount;
            public readonly int RendererCount;

            public Summary(int triangleCount, int rendererCount)
            {
                TriangleCount = triangleCount;
                RendererCount = rendererCount;
            }
        }

        /// <summary>
        /// Sums per-renderer triangle counts and counts how many renderers
        /// contributed. Negative entries (should never happen for a real
        /// mesh, but a defensive floor costs nothing) are clamped to 0
        /// rather than allowed to reduce the running total.
        /// </summary>
        public static Summary Summarize(IReadOnlyList<int> trianglesPerRenderer)
        {
            if (trianglesPerRenderer == null)
            {
                return new Summary(0, 0);
            }

            var total = 0;
            for (var i = 0; i < trianglesPerRenderer.Count; i++)
            {
                total += Mathf.Max(0, trianglesPerRenderer[i]);
            }

            return new Summary(total, trianglesPerRenderer.Count);
        }

        /// <summary>True if both the triangle sum and the renderer-count draw-call proxy are within budget.</summary>
        public static bool IsWithinBudget(Summary summary, int maxTriangles, int maxRenderersAsDrawCallProxy)
        {
            return summary.TriangleCount <= maxTriangles && summary.RendererCount <= maxRenderersAsDrawCallProxy;
        }
    }
}
