using UnityEditor;

namespace CsUnity.Editor
{
    public class MapMenuTools
    {
        [MenuItem(EditorCore.MenuName + "/Reload map")]
        static void ReloadMap()
        {
            CsGameManager.ReloadMap();
        }
    }
}
