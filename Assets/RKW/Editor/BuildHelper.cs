#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace RKW.Editor
{
    public static class BuildHelper
    {
        private const string OutputPath = "/tmp/rkw-dev.apk";

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
                options = BuildOptions.Development | BuildOptions.AllowDebugging
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
