#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace RKW.Editor
{
    public static class BuildHelper
    {
        private const string OutputPath = "/tmp/rkw-dev.apk";

        /// <summary>
        /// Round 25 (2026-08-24) founder feedback: "tirar aquele erro que
        /// fica no canto inferior direito de console development meio que
        /// registrando logs nao faz sentido ter ele" — that's not an app
        /// error at all, it's Unity's own built-in on-device indicator/log
        /// console, which ANY Android build made with
        /// <c>BuildOptions.Development</c> shows automatically (confirmed
        /// via rkw_logcat.txt: "Build type 'Development'"). Dropped both
        /// <c>BuildOptions.Development</c> and <c>AllowDebugging</c> (the
        /// latter requires the former) so future builds are ordinary
        /// (non-development) Android builds with no on-device overlay.
        /// <see cref="RKW.Physics.ScenePerformanceLogger"/>'s perf-summary
        /// logging (M3-T01 evidence) still works either way — it's a plain
        /// <c>Debug.Log</c> line, captured by
        /// <c>adb logcat -s Unity:V</c> regardless of the Development
        /// flag, not a Profiler-connection feature. Method name kept as
        /// <see cref="BuildAndroidDevelopment"/> unchanged (despite no
        /// longer building a Development Build) only because
        /// scripts/build_deploy_verify.sh calls it by exact name via
        /// -executeMethod; renaming both together felt riskier to do
        /// unsupervised than living with a slightly stale method name.
        /// </summary>
        public static void BuildAndroidDevelopment()
        {
            var scenes = new[]
            {
                "Assets/Scenes/KartPhysicsPrototype.unity"
            };

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            EditorUserBuildSettings.buildAppBundle = false;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            var report = BuildPipeline.BuildPlayer(options);

            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log($"BUILD SUCCEEDED: {OutputPath} ({report.summary.totalSize / 1024 / 1024} MB)");
            }
            else
            {
                Debug.LogError($"BUILD FAILED: {report.summary.result}");
                EditorApplication.Exit(1);
            }
        }
    }
}
#endif
