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

        private IBspLeaf[] m_leaves = System.Array.Empty<IBspLeaf>();
        private LeafAmbientLighting[] m_leafAmbientLightings = System.Array.Empty<LeafAmbientLighting>();
        private LeafAmbientIndex[] m_leafAmbientIndexes = System.Array.Empty<LeafAmbientIndex>();

        public bool drawAmbientLeaves = false;


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
            this.SetupAmbientLights(bspFile);
            
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

        void SetupAmbientLights(ValveBspFile bspFile)
        {
            m_leafAmbientIndexes = bspFile.LeafAmbientIndicesHdr.ToArray();
            m_leafAmbientLightings = bspFile.LeafAmbientLightingHdr.ToArray();
            m_leaves = bspFile.Leaves.ToArray();

            Debug.Log($"num leaf ambient indexes {m_leafAmbientIndexes.Length}, " +
                $"num leav ambient lightings {m_leafAmbientLightings.Length}, " +
                $"num leaves {m_leaves.Length}, " +
                $"num valid lightings {m_leafAmbientLightings.Count(_ => _.X != 0 || _.Y != 0 || _.Z != 0 )}, " +
                $"num non-black lightings {m_leafAmbientLightings.Count(_ => GetAllFaces(_.Cube).Any(c => c.R != 0 || c.G != 0 || c.B != 0 || c.Exponent != 0))}");
        }

        private void OnDrawGizmosSelected()
        {
            // draw ambient cubes in currently visible leaves
            if (this.drawAmbientLeaves)
            {
                var occlusionManager = FindObjectOfType<OcclusionManager>();
                if (null == occlusionManager)
                    return;

                var currentLeaf = Application.isPlaying ? OcclusionManager.LastLeaf : OcclusionManager.CalculateCurrentLeaf();

                if (null == currentLeaf)
                    return;

                var pvs = OcclusionManager.GetPvsListForCluster(currentLeaf.Info.Cluster);
                foreach (int cluster in pvs)
                {
                    if (!OcclusionManager.LeafsPerCluster.TryGetValue(cluster, out var leaves))
                        continue;

                    foreach (var leaf in leaves)
                    {
                        LeafAmbientIndex ambientIndex = m_leafAmbientIndexes[leaf.Index];
                        for (int i = 0; i < ambientIndex.AmbientSampleCount; i++)
                        {
                            LeafAmbientLighting leafAmbientLigthing = m_leafAmbientLightings[ambientIndex.FirstAmbientSample + i];
                            
                            var posPerc = OcclusionManager.Convert(new Vector3S(leafAmbientLigthing.X, leafAmbientLigthing.Y, leafAmbientLigthing.Z)) / Settings.UnitScale / 255f;
                            DrawAmbientCube(leafAmbientLigthing.Cube, leaf, posPerc);
                        }
                    }
                }
                
            }

        }

        private void DrawAmbientCube(CompressedLightCube cube, BspTree.Leaf leaf, UnityEngine.Vector3 posPerc)
        {
            var min = OcclusionManager.Convert(leaf.Info.Min);
            var max = OcclusionManager.Convert(leaf.Info.Max);
            var size = max - min;

            var cubePos = min + UnityEngine.Vector3.Scale(size, posPerc);

            for (int i = 0; i < 3; i++)
            {
                var dir = new UnityEngine.Vector3();
                dir[i] = 1f;
                //dir = OcclusionManager.Convert(new Vector3S((short)dir.x, (short)dir.y, (short)dir.z));
                DrawAmbientCubeSide(cubePos, dir, cube[i]);

                // opposite direction
                dir = new UnityEngine.Vector3();
                dir[i] = -1f;
                //dir = OcclusionManager.Convert(new Vector3S((short)dir.x, (short)dir.y, (short)dir.z));
                DrawAmbientCubeSide(cubePos, dir, cube[i + 3]);
            }
        }

        private void DrawAmbientCubeSide(
            UnityEngine.Vector3 cubePos, UnityEngine.Vector3 sideDirection, ColorRGBExp32 colorRGBExp32)
        {
            var convertedColor = new VBSPStruct.ColorRGBExp32 { r = colorRGBExp32.R, g = colorRGBExp32.G, b = colorRGBExp32.B, exponent = colorRGBExp32.Exponent };
            var gizmoColor = ConvertColor(convertedColor);
            Gizmos.color = gizmoColor;

            float boxSize = 0.6f;
            var sizeVec = (UnityEngine.Vector3.one - sideDirection.Absolute()) * boxSize;
            sizeVec = UnityEngine.Vector3.Max(sizeVec, UnityEngine.Vector3.one * 0.05f);
            Gizmos.DrawWireCube(cubePos + sideDirection * (boxSize / 2f + 0.05f), sizeVec);
        }

        private static IEnumerable<ColorRGBExp32> GetAllFaces(CompressedLightCube cube)
        {
            for (int i = 0; i < 6; i++)
                yield return cube[i];
        }

        private static Color ConvertColor(VBSPStruct.ColorRGBExp32 colorRGBExp32)
        {
            /*var color = VBSPFile.ConvertColorToGamma(colorRGBExp32);
            color.a = 255;
            return color;*/

            float pow = Mathf.Pow(2f, colorRGBExp32.exponent);
            var color = new UnityEngine.Vector4(colorRGBExp32.r, colorRGBExp32.g, colorRGBExp32.b) * pow;
            color.w = 1f;
            return ((Color)color).gamma;
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
