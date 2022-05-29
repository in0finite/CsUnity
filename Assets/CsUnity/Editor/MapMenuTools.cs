using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using uSource;
using uSource.Formats.Source.VBSP;
using System.Linq;

namespace CsUnity.Editor
{
    public class MapMenuTools
    {
        [MenuItem(EditorCore.MenuName + "/Reload map")]
        static void ReloadMap()
        {
            var loader = Object.FindObjectOfType<uLoader>();
            if (null == loader)
            {
                Debug.LogError("Loader object not found");
                return;
            }

            DestroyWorlds();

            if (!uLoader.PresetLoaded)
                uLoader.LoadPreset();

            uLoaderEditor.OnLoadBspPressed();
        }

        static void DestroyWorlds()
        {
            var worlds = Object.FindObjectsOfType<WorldRoot>();
            foreach (var world in worlds)
                Object.DestroyImmediate(world.gameObject);
        }
    }
}
