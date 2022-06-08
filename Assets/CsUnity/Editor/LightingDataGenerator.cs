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
        public const string LightingDataFolderPath = "Assets/CsUnity/LightingData";


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
            Debug.LogError("\n\nAn error occured during lighting generation process, exiting Unity... \n\n");
            // exit Editor with error code
            EditorApplication.Exit(1);
        }

        [MenuItem(EditorCore.MenuName + "/Lighting/Generate lighting")]
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

            if (EditorBuildSettings.scenes.Length == 0)
                throw new InvalidOperationException("There are no scenes configured in build settings");

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
            SaveAllData(mapName);
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

        private static void SaveAllData(string mapName)
        {
            SaveCustomLightingData(mapName);

            SaveTextures(mapName);

            if (LightmapSettings.lightProbes != null)
                SaveAsset(LightmapSettings.lightProbes, $"{mapName}_lightProbes.asset");

            if (Lightmapping.lightingDataAsset != null)
                SaveAsset(Lightmapping.lightingDataAsset, $"{mapName}_lightingData.asset");
        }

        private static void SaveCustomLightingData(string mapName)
        {
            LightingData lightingData = ScriptableObject.CreateInstance<LightingData>();

            lightingData.renderers = GetRenderersData();
            lightingData.lightmapsMode = LightmapSettings.lightmapsMode;
            lightingData.lightProbes = LightmapSettings.lightProbes?.bakedProbes?
                .Select(_ => new SphericalHarmonicsSerializable(_))
                .ToArray()
                ?? Array.Empty<SphericalHarmonicsSerializable>();

            SaveAsset(lightingData, $"{mapName}_customLightingData.asset");
            AssetDatabase.SaveAssets();
        }

        private static void SaveTextures(string mapName)
        {
            // save textures
            var lightmapDatas = LightmapSettings.lightmaps;
            for (int i = 0; i < lightmapDatas.Length; i++)
            {
                SaveAsset(lightmapDatas[i].lightmapColor, $"{mapName}_lightmapColor_{i}.asset");
                SaveAsset(lightmapDatas[i].lightmapDir, $"{mapName}_lightmapDir_{i}.asset");
                SaveAsset(lightmapDatas[i].shadowMask, $"{mapName}_shadowMask_{i}.asset");
            }

            AssetDatabase.SaveAssets();
        }

        private static void SaveAsset(UnityEngine.Object obj, string name)
        {
            AssetDatabase.CreateAsset(obj, LightingDataFolderPath + "/" + name);
        }

        private static T LoadAssetIfExists<T>(string name)
            where T : UnityEngine.Object
        {
            string path = LightingDataFolderPath + "/" + name;
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        private static T LoadAsset<T>(string name)
            where T : UnityEngine.Object
        {
            string path = LightingDataFolderPath + "/" + name;
            return AssetDatabase.LoadAssetAtPath<T>(path)
                ?? throw new System.IO.FileNotFoundException($"Failed to load asset at path {path}");
        }

        private static RendererLightingData[] GetRenderersData()
        {
            var renderers = UnityEngine.Object.FindObjectsOfType<Renderer>();

            var rendererLightingDatas = new RendererLightingData[renderers.Length];

            for (int i = 0; i < rendererLightingDatas.Length; i++)
            {
                Renderer r = renderers[i];
                var data = new RendererLightingData();
                data.lightmapIndex = r.lightmapIndex;
                data.lightmapScaleOffset = r.lightmapScaleOffset;
                data.path = r.gameObject.GetGameObjectPath();
                rendererLightingDatas[i] = data;
            }

            return rendererLightingDatas;
        }

        private static void RestoreAllData(string mapName)
        {
            var lightingData = LoadCustomLightingData(mapName);
            LightmapSettings.lightmapsMode = lightingData.lightmapsMode; // need to set this before restoring textures
            RestoreTextures(mapName);
            RestoreCustomLightingData(lightingData);

            var lightProbes = LoadAssetIfExists<LightProbes>($"{mapName}_lightProbes.asset");
            if (lightProbes != null)
                LightmapSettings.lightProbes = lightProbes;
        }

        private static LightingData LoadCustomLightingData(string mapName)
        {
            return LoadAsset<LightingData>($"{mapName}_customLightingData.asset");
        }

        private static void RestoreCustomLightingData(LightingData lightingData)
        {
            LightmapSettings.lightmapsMode = lightingData.lightmapsMode;
            RestoreRenderersData(lightingData.renderers);
        }

        private static void RestoreRenderersData(RendererLightingData[] rendererLightingDatas)
        {
            var renderers = UnityEngine.Object.FindObjectsOfType<Renderer>();

            if (renderers.Length != rendererLightingDatas.Length)
            {
                Debug.LogError($"num renderers {renderers.Length} not equal to num datas {rendererLightingDatas.Length}");
                return;
            }

            for (int i = 0; i < rendererLightingDatas.Length; i++)
            {
                renderers[i].lightmapIndex = rendererLightingDatas[i].lightmapIndex;
                renderers[i].lightmapScaleOffset = rendererLightingDatas[i].lightmapScaleOffset;
            }
        }

        private static void RestoreTextures(string mapName)
        {
            var colors = ReadTextures($"{mapName}_lightmapColor_");
            var dirs = ReadTextures($"{mapName}_lightmapDir_");
            var shadowMasks = ReadTextures($"{mapName}_shadowMask_");

            var lightmapDatas = new LightmapData[Mathf.Max(colors.Length, dirs.Length, shadowMasks.Length)];
            for (int i = 0; i < lightmapDatas.Length; i++)
            {
                lightmapDatas[i] = new LightmapData
                {
                    lightmapColor = colors.ElementAtOrDefault(i),
                    lightmapDir = dirs.ElementAtOrDefault(i),
                    shadowMask = shadowMasks.ElementAtOrDefault(i),
                };
            }

            LightmapSettings.lightmaps = lightmapDatas;
        }

        private static Texture2D[] ReadTextures(string prefix)
        {
            var list = new List<Texture2D>();
            for (int i = 0; ; i++)
            {
                var texture = LoadAssetIfExists<Texture2D>($"{prefix}{i}.asset");
                if (null == texture)
                    break;
                list.Add(texture);
            }
            return list.ToArray();
        }

        private static string GetMapName()
        {
            if (null == uSettings.Instance)
                throw new InvalidOperationException($"Failed to find {nameof(uSettings)} script in scene");

            if (string.IsNullOrWhiteSpace(uSettings.Instance.MapName))
                throw new InvalidOperationException($"Map name is not assigned on {nameof(uSettings)} script");

            return uSettings.Instance.MapName;
        }

        [MenuItem(EditorCore.MenuName + "/Lighting/Save all lighting data for current map")]
        static void SaveAllLightingDataForCurrentMap()
        {
            SaveAllData(GetMapName());
        }

        [MenuItem(EditorCore.MenuName + "/Lighting/Restore all lighting data for current map")]
        static void RestoreAllLightingDataForCurrentMap()
        {
            RestoreAllData(GetMapName());
        }

        [MenuItem(EditorCore.MenuName + "/Lighting/Save custom lighting data for current map")]
        static void SaveCustomLightingDataForCurrentMap()
        {
            SaveCustomLightingData(GetMapName());
        }

        [MenuItem(EditorCore.MenuName + "/Lighting/Restore custom lighting data for current map")]
        static void RestoreCustomLightingDataForCurrentMap()
        {
            RestoreCustomLightingData(LoadCustomLightingData(GetMapName()));
        }

        [MenuItem(EditorCore.MenuName + "/Lighting/Save textures for current map")]
        static void SaveTexturesForCurrentMap()
        {
            SaveTextures(GetMapName());
        }

        [MenuItem(EditorCore.MenuName + "/Lighting/Restore textures for current map")]
        static void RestoreTexturesForCurrentMap()
        {
            RestoreTextures(GetMapName());
        }
    }
}
