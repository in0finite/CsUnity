using UnityEditor;
using uSource;

namespace CsUnity.Editor
{
    public class MapMenuTools
    {
        private const string LoadMapMenuName = "/Load map";


        [MenuItem(EditorCore.MenuName + "/Reload map")]
        static void ReloadMap()
        {
            CsGameManager.ReloadMap();
        }

        private static void LoadMap(string mapName)
        {
            if (null == uSettings.Instance)
                throw new System.Exception($"Failed to find {nameof(uSettings)} object in scene");

            uSettings.Instance.MapName = mapName;
            CsGameManager.ReloadMap();
        }

        [MenuItem(EditorCore.MenuName + LoadMapMenuName + "/de_dust2")]
        static void de_dust2()
        {
            LoadMap("de_dust2");
        }

        [MenuItem(EditorCore.MenuName + LoadMapMenuName + "/de_inferno")]
        static void de_inferno()
        {
            LoadMap("de_inferno");
        }

        [MenuItem(EditorCore.MenuName + LoadMapMenuName + "/de_nuke")]
        static void de_nuke()
        {
            LoadMap("de_nuke");
        }

        [MenuItem(EditorCore.MenuName + LoadMapMenuName + "/de_overpass")]
        static void de_overpass()
        {
            LoadMap("de_overpass");
        }

        [MenuItem(EditorCore.MenuName + LoadMapMenuName + "/de_vertigo")]
        static void de_vertigo()
        {
            LoadMap("de_vertigo");
        }

        [MenuItem(EditorCore.MenuName + LoadMapMenuName + "/de_ancient")]
        static void de_ancient()
        {
            LoadMap("de_ancient");
        }

        [MenuItem(EditorCore.MenuName + LoadMapMenuName + "/de_mirage")]
        static void de_mirage()
        {
            LoadMap("de_mirage");
        }
    }
}
