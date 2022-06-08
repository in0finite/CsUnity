using UnityEngine;

namespace CsUnity.Editor
{
    [CreateAssetMenu(fileName = "LightingData", menuName = "Lighting/Lighting data")]
    public class LightingData : ScriptableObject
    {
        public long version;
        public long flags;
        /*public Texture2D[] lightmaps;
        public Texture2D[] lightmapsDir;
        public Texture2D[] shadowMasks;*/
        public LightmapsMode lightmapsMode;
        public SphericalHarmonicsSerializable[] lightProbes;
        public RendererLightingData[] renderers;
    }

    [System.Serializable]
    public struct SphericalHarmonicsSerializable
    {
        public float[] coefficients;

        public SphericalHarmonicsSerializable(UnityEngine.Rendering.SphericalHarmonicsL2 probe)
        {
            this.coefficients = new float[27];
            for (int j = 0; j < 3; j++)
            {
                for (int k = 0; k < 9; k++)
                    this.coefficients[j * 9 + k] = probe[j, k];
            }
        }

        public UnityEngine.Rendering.SphericalHarmonicsL2 ToUnity()
        {
            var sphericalHarmonics = new UnityEngine.Rendering.SphericalHarmonicsL2();
            for (int j = 0; j < 3; j++)
            {
                for (int k = 0; k < 9; k++)
                    sphericalHarmonics[j, k] = this.coefficients[j * 9 + k];
            }
            return sphericalHarmonics;
        }
    }
}
