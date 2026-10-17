#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Simulation.Editor
{
    public static class WebGLBuildTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/02_TrainingLab.unity";
        private const string OutputRelativeFolder = "Builds/WebGL";

        [MenuItem("Simulation/Build WebGL (Development)")]
        public static void BuildWebGLDevelopment()
        {
            string projectRoot = Directory.GetCurrentDirectory();
            string buildPath = Path.Combine(projectRoot, OutputRelativeFolder);

            if (!Directory.Exists(buildPath))
            {
                Directory.CreateDirectory(buildPath);
            }

            string[] scenes = new string[] { ScenePath };

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = buildPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.Development
            };

            Debug.Log($"[WebGLBuildTool] WebGL Development Build başlatılıyor... Hedef: {buildPath}");

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[WebGLBuildTool] WebGL Build başarıyla tamamlandı! Boyut: {summary.totalSize / (1024 * 1024):F2} MB, Süre: {summary.totalTime.TotalSeconds:F1}s");
                EditorUtility.RevealInFinder(buildPath);
            }
            else if (summary.result == BuildResult.Failed)
            {
                Debug.LogError($"[WebGLBuildTool] WebGL Build başarısız oldu! Hata Sayısı: {summary.totalErrors}");
            }
        }
    }
}
#endif
