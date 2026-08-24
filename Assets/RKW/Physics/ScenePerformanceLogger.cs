using System.Collections.Generic;
using UnityEngine;

namespace RKW.Physics
{
    /// <summary>
    /// M3-T01 requires "Evidência: Screenshot + profiler stats", but
    /// scripts/build_deploy_verify.sh only ever automated the screenshot
    /// half — the profiler numbers always needed a separate manual step
    /// nobody had actually done. This logs one summary line
    /// (triangle count + renderer count, see <see cref="ScenePerformanceMath"/>
    /// for why a renderer count is a reasonable draw-call proxy without a
    /// live Profiler connection) a few seconds after the race scene loads,
    /// tagged so it lands in the same "Unity:V" logcat filter the script
    /// already captures — so real numbers show up automatically in
    /// rkw_logcat.txt on every future run, no extra step required.
    ///
    /// Deliberately display-only, same as RaceStandingsHud: never read by
    /// any gameplay system, never blocks anything if it's missing from a
    /// scene. Created once by KartPhysicsPrototypeBootstrap alongside
    /// RaceManager/RaceStandingsHud.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ScenePerformanceLogger : MonoBehaviour
    {
        // M3-T01's stated budget: "≤ 100K triângulos, ≤ 100 draw calls
        // (tier low)".
        private const int MaxTriangles = 100000;
        private const int MaxRenderersAsDrawCallProxy = 100;

        // Long enough that every procedurally-generated piece
        // (KartPhysicsPrototypeBootstrap runs its whole track/kart/bot
        // build in Awake, so this is a generous margin, not a requirement)
        // and every spawned bot/kart has finished appearing, short enough
        // to land well inside the ~5s window build_deploy_verify.sh waits
        // after launch before it dumps logcat.
        private const float LogDelaySeconds = 2f;

        private readonly List<int> _scratchTriangleCounts = new List<int>();

        private void Start()
        {
            Invoke(nameof(LogSummary), LogDelaySeconds);
        }

        private void LogSummary()
        {
            _scratchTriangleCounts.Clear();

            var meshFilters = FindObjectsOfType<MeshFilter>();
            for (var i = 0; i < meshFilters.Length; i++)
            {
                var mesh = meshFilters[i] != null ? meshFilters[i].sharedMesh : null;
                if (mesh == null)
                {
                    continue;
                }

                // triangles.Length is 3 ints per triangle (a flat index
                // buffer), matching Unity's own definition of "triangle
                // count" everywhere else (Profiler, Stats window, etc.).
                _scratchTriangleCounts.Add(mesh.triangles.Length / 3);
            }

            var summary = ScenePerformanceMath.Summarize(_scratchTriangleCounts);
            var withinBudget = ScenePerformanceMath.IsWithinBudget(summary, MaxTriangles, MaxRenderersAsDrawCallProxy);
            var status = withinBudget ? "OK" : "ACIMA DO BUDGET";

            Debug.Log($"[RKW-PERF] {status} — triângulos: {summary.TriangleCount} (limite {MaxTriangles}), " +
                $"renderers (proxy de draw calls): {summary.RendererCount} (limite {MaxRenderersAsDrawCallProxy})");
        }

        private void OnDestroy()
        {
            CancelInvoke(nameof(LogSummary));
        }
    }
}
