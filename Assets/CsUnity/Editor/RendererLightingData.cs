using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CsUnity.Editor
{
    [System.Serializable]
    public struct RendererLightingData
    {
        public byte type;
        public string path;
        public int index;

        public int lightmapIndex;
        public Vector4 lightmapScaleOffset;
    }
}
