using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CometaChampions.Editor
{
    public static class AndroidBuildMenuItems
    {
        [MenuItem("Cometa Champions/Build Android Release APK")]
        public static void BuildReleaseApk()
        {
            PlayerSettings.Android.keystorePass = "00000000";
            PlayerSettings.Android.keyaliasPass = "00000000";

            var scenes = new[]
            {
                "Assets/_Project/Scenes/Home.unity",
                "Assets/_Project/Scenes/Battle.unity",
                "Assets/_Project/Scenes/CardShowcase.unity"
            };

            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = "/Users/balasanyana/Desktop/Proyectos/Deploy/tcg_cometa_release.apk",
                target = BuildTarget.Android,
                options = BuildOptions.CompressWithLz4
            };

            Debug.Log("[AndroidBuild] Starting Android build...");
            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            var summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[AndroidBuild] BUILD SUCCESSFUL! Output: {summary.outputPath} ({summary.totalSize} bytes) in {summary.totalTime.TotalSeconds:F1}s");
            }
            else
            {
                Debug.LogError($"[AndroidBuild] BUILD FAILED! Result: {summary.result}, Errors: {summary.totalErrors}");
            }
        }

        [MenuItem("Cometa Champions/Log Game Version")]
        public static void LogGameVersion()
        {
            Debug.Log($"[VersionCheck] Current Formatted Version: {CometaChampions.Runtime.Services.GameVersionService.GetFormattedVersion()}");
        }

        [MenuItem("Cometa Champions/Log Adaptive Quality Diagnostic Report")]
        public static void LogAdaptiveQualityReport()
        {
            Debug.Log(CometaChampions.Runtime.Services.AdaptiveQualityManager.Instance.GenerateDiagnosticReport());
        }
    }
}
