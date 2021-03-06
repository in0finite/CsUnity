using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using uSource;
using uSource.Formats.Source.VBSP;
using System.Linq;

namespace CsUnity.Editor
{
    public class LightMenuTools
    {
        [MenuItem(EditorCore.MenuName + "/Select directional light")]
        static void SelectDirectionalLight()
        {
            var light = Object.FindObjectsOfType<Light>().FirstOrDefault(l => l.type == LightType.Directional);
            if (null == light)
                return;

            Selection.activeTransform = light.transform;
            EditorGUIUtility.PingObject(light);
        }
    }
}
