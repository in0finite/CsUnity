using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UGameCore.Utilities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using uSource;

namespace CsUnity.Editor
{
    public class LightingDataGenerator
    {
        private static void RunFromCommandLine()
        {
            string[] args = CmdLineUtils.GetCmdLineArgs();

            // we need to check this before starting coroutine, or otherwise Editor may exit
            if (args.Contains("-quit"))
            {
                throw new ArgumentException("Lighting data generation from command line can not be used with '-quit' argument. " +
                    "Remove the argument, and Editor will be closed when lighting data generation is finished.");
            }

            string[] maps = CmdLineUtils.GetStringArgument("csMaps").Split(',');
            if (maps.Length == 0)
                throw new ArgumentException("You must provide maps, eg: -csMaps:de_dust2,de_inferno");

            CmdLineUtils.TryGetStringArgument("csGameDir", out string overridenGameDir);

            CoroutineManager.Start(RunCoroutine(maps, overridenGameDir), OnFinishWithSuccess, OnFinishWithError);
        }

        static void OnFinishWithSuccess()
        {
            // exit Editor with success code
            EditorApplication.Exit(0);
        }

        static void OnFinishWithError(Exception exception)
        {
            // exit Editor with error code
            EditorApplication.Exit(1);
        }

        [MenuItem(EditorCore.MenuName + "/Generate lighting")]
        static void RunFromMenu()
        {
            if (null == uSettings.Instance)
                throw new InvalidOperationException($"Failed to find {nameof(uSettings)} script in scene");

            CoroutineManager.Start(RunCoroutine(new string[] { uSettings.Instance.MapName }, null), null, OnFinishFromMenuItemWithError);
        }

        static void OnFinishFromMenuItemWithError(Exception exception)
        {
            Lightmapping.Cancel();
        }

        private static IEnumerator RunCoroutine(string[] maps, string overridenGameDir)
        {
            if (maps.Length == 0)
                throw new ArgumentException("No maps provided");

            if (Lightmapping.isRunning)
                throw new InvalidOperationException("Lightmapping process is already running");

            var totalTimeStopwatch = System.Diagnostics.Stopwatch.StartNew();

            Debug.Log("Started lighting data generation ...");

            yield return null;

            foreach (string map in maps)
            {
                foreach (var obj in GenerateForMap(map, overridenGameDir))
                    yield return obj;
            }

            Debug.Log($"Finished generation of lighting data - elapsed {totalTimeStopwatch.Elapsed}");

            yield return null;
            yield return null;
        }

        static IEnumerable GenerateForMap(string mapName, string overridenGameDir)
        {
            var totalTimeStopwatch = System.Diagnostics.Stopwatch.StartNew();

            Debug.Log($"Starting for map {mapName}");

            // close all scenes
            /*for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                var scene = EditorSceneManager.GetSceneAt(i);
                EditorSceneManager.CloseScene(scene, true);
            }*/
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            yield return null;
            yield return null;

            Debug.Log("Opening scene");
            EditorSceneManager.OpenScene(EditorBuildSettings.scenes[0].path, OpenSceneMode.Single);

            yield return null;
            yield return null;

            if (null == uSettings.Instance)
                throw new InvalidOperationException($"Failed to find {nameof(uSettings)} script in scene");

            if (!string.IsNullOrWhiteSpace(overridenGameDir))
                uSettings.Instance.RootPath = overridenGameDir;

            var availableMaps = CsGameManager.EnumerateMaps();
            if (availableMaps.Length == 0)
                throw new InvalidOperationException($"Failed to find maps - is path to game directory assigned ?");

            if (!availableMaps.Contains(mapName))
                throw new ArgumentException($"Provided map '{mapName}' not found");

            var profiler = new LoggingProfiler($"Loading map {mapName}");
            uSettings.Instance.MapName = mapName;
            uLoader.OnLoadBspPressed();
            profiler.LogElapsed();

            yield return null;
            yield return null;
            yield return null;

            // now fire up lighting generator

            profiler.Restart($"Baking for map {mapName}");

            Lightmapping.Cancel();
            Lightmapping.Clear();

            yield return null;
            yield return null;

            if (!Lightmapping.BakeAsync())
                throw new Exception("Failed to starting baking");

            var etaMeasurer = new ETAMeasurer(1f);
            var logStopwatch = System.Diagnostics.Stopwatch.StartNew();

            while (Lightmapping.isRunning)
            {
                yield return null;

                etaMeasurer.UpdateETA(Lightmapping.buildProgress);

                if (logStopwatch.Elapsed.TotalSeconds > 10)
                {
                    logStopwatch.Restart();
                    Debug.Log($"Baking - progress {Lightmapping.buildProgress}, ETA {etaMeasurer.ETA}, elapsed {totalTimeStopwatch.Elapsed}");
                }
            }

            profiler.LogElapsed();

            yield return null;
            yield return null;
            yield return null;

            if (null == Lightmapping.lightingDataAsset)
                throw new Exception("Failed to generate lighting data asset");

            profiler.Restart("Saving lighting data");
            SaveData(mapName);
            profiler.LogElapsed();

            yield return null;

            /*profiler.Restart("Unloading scene");

            var operation = EditorSceneManager.UnloadSceneAsync(EditorSceneManager.GetActiveScene());
            while (!operation.isDone)
                yield return null;

            profiler.LogElapsed();*/

            Debug.Log($"\n\nFinished for map {mapName}\n\n");

            yield return null;
        }

        private static void SaveData(string mapName)
        {
            AssetDatabase.CreateAsset(Lightmapping.lightingDataAsset, $"Assets/CsUnity/LightingData/{mapName}_lightingData.asset");
            
            // save renderers' data
            
            //var renderers = UnityEngine.Object.FindObjectOfType<Renderer>();

        }
    }
}
