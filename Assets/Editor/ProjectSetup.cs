using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HeroesBattle.EditorTools
{
    /// <summary>Создание сцены/настроек проекта и сборка Windows-билда из командной строки.</summary>
    public static class ProjectSetup
    {
        public const string ScenePath = "Assets/Scenes/Battle.unity";

        [MenuItem("HeroesBattle/Setup Project")]
        public static void Configure()
        {
            CreateScene();
            ApplyPlayerSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[HeroesBattle] Проект настроен. Сцена: " + ScenePath);
        }

        static void CreateScene()
        {
            Directory.CreateDirectory("Assets/Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 9f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.035f, 0.03f);
            cam.transform.position = new Vector3(8.6f, -5.2f, -10f);
            camGo.AddComponent<AudioListener>();

            var game = new GameObject("Game");
            game.AddComponent<GameRoot>();

            EditorSceneManager.SaveScene(scene, ScenePath);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };
        }

        static void ApplyPlayerSettings()
        {
            PlayerSettings.companyName = "HeroesBattle";
            PlayerSettings.productName = "Heroes of Battle";
            PlayerSettings.bundleVersion = "1.0";
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            PlayerSettings.usePlayerLog = true;
            PlayerSettings.visibleInBackground = true;
        }

        public static void BuildWindows()
        {
            if (!File.Exists(ScenePath)) CreateScene();
            ApplyPlayerSettings();

            Directory.CreateDirectory("Builds");

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/HeroesBattle.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result != BuildResult.Succeeded)
            {
                throw new Exception("[HeroesBattle] Сборка не удалась: " + summary.result + ", ошибок: " + summary.totalErrors);
            }

            Debug.Log("[HeroesBattle] Сборка успешна: " + Path.GetFullPath(summary.outputPath) +
                      " (" + (summary.totalSize / (1024 * 1024)) + " МБ)");
        }
    }
}
