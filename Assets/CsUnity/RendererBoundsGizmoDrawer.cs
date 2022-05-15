using UnityEngine;

namespace CsUnity
{
    public class RendererBoundsGizmoDrawer : MonoBehaviour
    {
        public Color color = Color.red;
        public bool wireframe = true;
        public bool includeChildren = true;


#if UNITY_EDITOR
        [UnityEditor.DrawGizmo(UnityEditor.GizmoType.Selected)]
        private static void DrawGizmoForRenderer(MeshRenderer renderer, UnityEditor.GizmoType gizmoType)
        {
            var instance = FindObjectOfType<RendererBoundsGizmoDrawer>();
            if (instance != null)
                instance.Draw(renderer);
        }
#endif

        private void Draw(Renderer r)
        {
            Gizmos.color = this.color;

            Renderer[] renderers = this.includeChildren
                ? r.GetComponentsInChildren<Renderer>()
                : new Renderer[] { r };

            foreach (var renderer in renderers)
            {
                var bounds = renderer.bounds;
                if (this.wireframe)
                    Gizmos.DrawWireCube(bounds.center, bounds.size);
                else
                    Gizmos.DrawCube(bounds.center, bounds.size);
            }
        }
    }
}
