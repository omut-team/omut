using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Omut.EditorTools
{
    // Сборка игры в .exe для Windows. Меню Unity: Omut > Собрать .exe (Windows).
    // Результат: папка Builds/Omut рядом с папкой game (её целиком можно заархивировать и отдать).
    // Из командной строки:
    //   Unity.exe -batchmode -quit -projectPath game -executeMethod Omut.EditorTools.OmutBuild.BuildWindows
    public static class OmutBuild
    {
        public const string OutputFolder = "../Builds/Omut";
        public const string ExeName = "Omut.exe";

        [MenuItem("Omut/Собрать .exe (Windows)")]
        public static void BuildWindows()
        {
            ConfigurePlayer();

            string output = Path.GetFullPath(Path.Combine(OutputFolder, ExeName));
            var options = new BuildPlayerOptions
            {
                scenes = new[] { OmutSceneBuilder.MainScenePath },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[OmutBuild] Готово: {output} ({report.summary.totalSize / (1024 * 1024)} МБ)");
                if (!Application.isBatchMode)
                    EditorUtility.RevealInFinder(output);
            }
            else
            {
                Debug.LogError("[OmutBuild] Сборка не удалась: " + report.summary.result);
                if (Application.isBatchMode)
                    EditorApplication.Exit(1);
            }
        }

        // Название, версия и окно игры.
        public static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "omut-team";
            PlayerSettings.productName = "Омут";
            PlayerSettings.bundleVersion = "1.0";
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = false;
            AssetDatabase.SaveAssets();
        }
    }
}
