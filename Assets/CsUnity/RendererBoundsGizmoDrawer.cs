using UnityEngine;
using uSource.Formats.Source.VBSP;

namespace CsUnity
{
    public class RendererBoundsGizmoDrawer : MonoBehaviour
    {
        public Color color = Color.red;
        public bool wireframe = true;
        public bool includeChildren = true;
        public bool drawSurroundingLeaves = true;


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
            var visibilitySystem = r.GetComponentInParent<VisibilitySystem>();

            Renderer[] renderers = this.includeChildren
                ? r.GetComponentsInChildren<Renderer>()
                : new Renderer[] { r };

            foreach (var renderer in renderers)
            {
                Gizmos.color = this.color;

                var bounds = renderer.bounds;
                if (this.wireframe)
                    Gizmos.DrawWireCube(bounds.center, bounds.size);
                else
                    Gizmos.DrawCube(bounds.center, bounds.size);

                if (this.drawSurroundingLeaves && visibilitySystem != null)
                {
                    Gizmos.color = Color.yellow;
                    foreach (int leaf in visibilitySystem.GetAllLeavesIntersectingBounds(bounds))
                        VisibilitySystem.GizmosDrawCube(
                            visibilitySystem.bspLeaves[leaf].common.Min,
                            visibilitySystem.bspLeaves[leaf].common.Max);
                }
            }
        }
    }
}
