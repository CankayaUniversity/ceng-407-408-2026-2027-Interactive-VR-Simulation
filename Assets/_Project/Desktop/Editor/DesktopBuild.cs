#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using WorkplaceStudio.Core;

namespace WorkplaceStudio.Desktop.Editor
{
    public static class DesktopBuild
    {
        const string ScenePath = "Assets/_Project/Desktop/Scenes/WorkplaceDesktop.unity";
        const string OutputPath = "Builds/WorkplaceStudio-Windows/WorkplaceStudio.exe";

        [MenuItem("Workplace Studio/Open Desktop Application Scene")]
        public static void OpenScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            PrepareContent();
            EditorSceneManager.OpenScene(ScenePath);
            Debug.Log("Desktop scene ready. Enter Play mode to open Workplace Studio.");
        }

        public static void PrepareContent()
        {
            var text = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Desktop/Resources/DesktopCatalogue.json");
            if (text == null) throw new InvalidDataException("Desktop content catalogue is missing.");
            var catalogue = TrainingJson.Read<Catalogue>(text.text);
            var root = Directory.GetParent(Application.dataPath).FullName;
            var contentRoot = Path.Combine(Application.streamingAssetsPath, "WorkplaceStudio");
            foreach (var project in catalogue.environments)
            {
                TrainingValidation.Project(project);
                var source = Path.Combine(root, "web-editor", "public", project.editor.modelUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                var target = LocalPaths.Resolve(contentRoot, project.modelFile);
                CopyVerified(source, target, project.modelSha256);
            }
            CopyVerified(Path.Combine(root, "web-editor", "public", "guides", "piko", "piko.glb"),
                Path.Combine(contentRoot, "guides", "piko.glb"), "fd2338c12af77d767f769fdf0c06beba1b3279df78ceb6c36dbda2c5f82383ac");
            AssetDatabase.Refresh();
        }
        static void CopyVerified(string source, string target, string expected)
        {
            GlbFiles.Verify(source, expected);
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            if (File.Exists(target)) GlbFiles.Verify(target, expected);
            else File.Copy(source, target);
        }

        [MenuItem("Workplace Studio/Build Windows Application")]
        public static void BuildWindows()
        {
            PrepareContent();
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
                throw new InvalidOperationException("Unity Windows build support is not installed.");
            var defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone);
            if (!defines.Contains("GLTFAST_KEEP_MESH_DATA") || !defines.Contains("GLTFAST_SAFE"))
                throw new InvalidOperationException("Standalone must define GLTFAST_KEEP_MESH_DATA and GLTFAST_SAFE before compilation.");
            string root = Directory.GetParent(Application.dataPath).FullName;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(root, OutputPath)));
            var oldPipeline = GraphicsSettings.defaultRenderPipeline;
            var oldQualityPipeline = QualitySettings.renderPipeline;
            var oldProduct = PlayerSettings.productName;
            var oldCompany = PlayerSettings.companyName;
            var oldWindowMode = PlayerSettings.fullScreenMode;
            var oldResizable = PlayerSettings.resizableWindow;
            var oldNativeResolution = PlayerSettings.defaultIsNativeResolution;
            int oldWidth = PlayerSettings.defaultScreenWidth, oldHeight = PlayerSettings.defaultScreenHeight;
            var graphics = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            var shaders = graphics.FindProperty("m_AlwaysIncludedShaders");
            var previousShaders = Enumerable.Range(0, shaders.arraySize).Select(i => shaders.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
            try
            {
                GraphicsSettings.defaultRenderPipeline = Resources.Load<RenderPipelineAsset>("DesktopPipeline");
                QualitySettings.renderPipeline = GraphicsSettings.defaultRenderPipeline;
                PlayerSettings.productName = "Workplace Studio"; PlayerSettings.companyName = "Cengaverler";
                PlayerSettings.fullScreenMode = FullScreenMode.Windowed; PlayerSettings.resizableWindow = true;
                PlayerSettings.defaultIsNativeResolution = false; PlayerSettings.defaultScreenWidth = 1440; PlayerSettings.defaultScreenHeight = 900;
                // Keep glTF runtime shader variants in the standalone player. Editor play alone cannot verify stripping.
                foreach (string guid in AssetDatabase.FindAssets("t:Shader", new[] { "Packages/com.unity.cloud.gltfast/Runtime/Shader" }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.Contains("/HDRP/") || path.Contains("/Built-In/")) continue;
                    var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                    if (shader == null || previousShaders.Contains(shader)) continue;
                    shaders.InsertArrayElementAtIndex(shaders.arraySize); shaders.GetArrayElementAtIndex(shaders.arraySize - 1).objectReferenceValue = shader;
                }
                graphics.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.SaveAssets();
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath }, locationPathName = Path.Combine(root, OutputPath),
                    target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
                });
                if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Windows build failed. Read the Unity build log.");
                File.WriteAllText(Path.Combine(root, "Builds/WorkplaceStudio-Windows/README.txt"),
                    "Workplace Studio - Windows desktop\r\nRun WorkplaceStudio.exe. Keep the complete folder together.\r\nModels and runtime content are bundled; no Node.js or web server is required.\r\nRight mouse: capture, WASD: move, Esc: release.\r\nSpectator: Shift up, Ctrl down. Walking: Shift faster.\r\n");
                Debug.Log("Windows application built: " + Path.Combine(root, OutputPath));
            }
            finally
            {
                GraphicsSettings.defaultRenderPipeline = oldPipeline; QualitySettings.renderPipeline = oldQualityPipeline;
                PlayerSettings.productName = oldProduct; PlayerSettings.companyName = oldCompany;
                PlayerSettings.fullScreenMode = oldWindowMode; PlayerSettings.resizableWindow = oldResizable;
                PlayerSettings.defaultIsNativeResolution = oldNativeResolution;
                PlayerSettings.defaultScreenWidth = oldWidth; PlayerSettings.defaultScreenHeight = oldHeight;
                graphics.Update(); shaders = graphics.FindProperty("m_AlwaysIncludedShaders"); shaders.arraySize = previousShaders.Length;
                for (int i = 0; i < previousShaders.Length; i++) shaders.GetArrayElementAtIndex(i).objectReferenceValue = previousShaders[i];
                graphics.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.SaveAssets();
            }
        }
    }
}
#endif
