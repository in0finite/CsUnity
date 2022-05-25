using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using uSource.Formats.Source.VBSP;
using System.IO;
using SourceUtils;

namespace CsUnity
{
    public class CsGameManager : MonoBehaviour
    {
        public static event System.Action<ValveBspFile> OnMapLoaded = delegate { };

        public static CsGameManager Instance { get; private set; }

        public LayerMask dynamicLightsLayerMask = 0;
        public LightShadowCasterMode lightShadowCasterMode = LightShadowCasterMode.Default;

        public bool setEnvironmentLightingSourceToSkybox = true;


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

            // notify others
            OnMapLoaded(bspFile);

            bspFile.Dispose();
        }

        void SetupLights()
        {
            if (this.setEnvironmentLightingSourceToSkybox)
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;

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
    }
}
