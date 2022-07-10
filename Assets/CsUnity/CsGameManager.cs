using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using uSource.Formats.Source.VBSP;
using System.IO;
using SourceUtils;
using SourceUtils.ValveBsp;
using System.Linq;
using uSource;

namespace CsUnity
{
    public class CsGameManager : MonoBehaviour
    {
        public static uSettings Settings => uSettings.Instance;

        public static event System.Action<ValveBspFile> OnMapLoaded = delegate { };

        public static CsGameManager Instance { get; private set; }

        public LayerMask dynamicLightsLayerMask = 0;
        public LightShadowCasterMode lightShadowCasterMode = LightShadowCasterMode.Default;

        public bool setEnvironmentLightingSourceToSkybox = true;
        public float skyboxIntensity = 0.75f;



        static CsGameManager()
        {
            VBSPFile.OnLoaded -= OnBspLoadedStatic;
            VBSPFile.OnLoaded += OnBspLoadedStatic;
        }

        private void Awake()
        {
            if (null != Instance)
                return;

            Instance = this;
        }

        static void OnBspLoadedStatic(Stream stream)
        {
            if (null == Instance)
                Instance = FindObjectOfType<CsGameManager>();

            if (Instance != null)
                Instance.OnBspLoaded(stream);
        }

        void OnBspLoaded(Stream stream)
        {
            // initialize SourceUtils
            stream.Position = 0;
            using ValveBspFile bspFile = new ValveBspFile(((FileStream)stream).Name);

            this.SetupLights();
            this.SetupCamera();
            
            // notify others
            OnMapLoaded(bspFile);

            bspFile.Dispose();
        }

        void SetupLights()
        {
            if (this.setEnvironmentLightingSourceToSkybox)
            {
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
                RenderSettings.ambientIntensity = this.skyboxIntensity;
            }

            if (null == VBSPFile.WorldLightsGroup)
                return;

            var lights = VBSPFile.WorldLightsGroup.GetComponentsInChildren<Light>();
            foreach (Light light in lights)
            {
                if (light.type != LightType.Directional)
                {
                    light.renderingLayerMask = this.dynamicLightsLayerMask.value; // this is for receiving shadows ?
                    light.cullingMask = this.dynamicLightsLayerMask.value; // this is for receiving light ?
                }

                light.lightShadowCasterMode = this.lightShadowCasterMode;
            }
        }

        private void OnValidate()
        {
            this.SetupLights();
        }

        void SetupCamera()
        {
            if (null == VBSPFile.EntitiesGroup)
                return;

            var spawn = VBSPFile.EntitiesGroup.Find("info_player_counterterrorist");
            if (null == spawn)
                return;

            var cameras = Camera.allCameras;
            foreach (var camera in cameras)
                camera.transform.SetPositionAndRotation(spawn.transform.position, spawn.transform.rotation);
        }

        public static void ReloadMap()
        {
            var loader = Object.FindObjectOfType<uLoader>();
            if (null == loader)
            {
                Debug.LogError("Loader object not found");
                return;
            }

            DestroyWorlds();

            uLoader.OnLoadBspPressed();
        }

        private static void DestroyWorlds()
        {
            var worlds = Object.FindObjectsOfType<WorldRoot>();
            foreach (var world in worlds)
                Object.DestroyImmediate(world.gameObject);
        }

        public static string[] EnumerateMapsWithFullPaths()
        {
            if (null == Settings)
                return System.Array.Empty<string>();

            if (Settings.ModFolders == null || Settings.ModFolders.Length == 0)
                return System.Array.Empty<string>();

            string mapsFolder = Path.Combine(Settings.RootPath, Settings.ModFolders[0], "maps");
            if (!Directory.Exists(mapsFolder))
                return System.Array.Empty<string>();

            return Directory.EnumerateFiles(mapsFolder, "*.bsp", SearchOption.TopDirectoryOnly).ToArray();
        }

        public static string[] EnumerateMaps()
        {
            return EnumerateMapsWithFullPaths()
                .Select(Path.GetFileNameWithoutExtension)
                .ToArray();
        }

        private static void ListFilesInPAKLump()
        {
            PAKProvider pakProvider = uResourceManager.Providers?.OfType<PAKProvider>().FirstOrDefault();
            if (null == pakProvider)
                return;
            
            Debug.Log($"Files found in PAK lump [{pakProvider.files.Count}]:\r\n{string.Join("\r\n", pakProvider.files.Select(_ => $"[{_.Value}]: {_.Key}"))}");
        }
    }
}
