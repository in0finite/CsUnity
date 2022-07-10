using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using uSource.Formats.Source.VBSP;

namespace CsUnity.Editor
{
    [CustomEditor(typeof(VisibilitySystem))]
    public class OcclusionManagerInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.DrawDefaultInspector();

            var visibilitySystem = (VisibilitySystem)this.target;

            int currentLeaf = visibilitySystem.LastLeaf;

            int numLeaves = 0;
            int numVisibleLeaves = 0;
            foreach (int leaf in visibilitySystem.GetAllLeaves())
            {
                numLeaves++;
                if (currentLeaf >= 0 && visibilitySystem.IsLeafVisible(currentLeaf, leaf))
                    numVisibleLeaves++;
            }

            int numVisibleClusters = currentLeaf >= 0
                ? visibilitySystem.GetPvsListForCluster(visibilitySystem.GetClusterOfLeaf(currentLeaf)).Count
                : 0;

            int numRenderers = visibilitySystem.NumRenderers;
            int numVisibleRenderers = 0;
            long numReferences = 0;
            for (int i = 0; i < visibilitySystem.NumRenderers; i++)
            {
                var rendererInfo = visibilitySystem.GetRenderer(i);
                if (rendererInfo.numClustersReferencing > 0)
                    numVisibleRenderers++;
                numReferences += rendererInfo.numClustersReferencing;
            }

            GUILayout.Space(20);
            EditorGUILayout.LabelField($"num leaves: {numLeaves}");
            EditorGUILayout.LabelField($"num visible leaves: {numVisibleLeaves}");
            EditorGUILayout.LabelField($"num clusters: {visibilitySystem.NumClusters}");
            EditorGUILayout.LabelField($"num visible clusters: {numVisibleClusters}");
            EditorGUILayout.LabelField($"num renderers: {numRenderers}");
            EditorGUILayout.LabelField($"num renderers in culling system: {visibilitySystem.NumRenderersInCullingSystem}");
            EditorGUILayout.LabelField($"num visible renderers: {numVisibleRenderers}");
            EditorGUILayout.LabelField($"average renderers per cluster: {numRenderers / (float)visibilitySystem.NumClusters}");
            EditorGUILayout.LabelField($"average references per visible renderer: {numReferences / (float)numVisibleRenderers}");

            GUILayout.Space(20);

            if (GUILayout.Button("Enable all renderers"))
                visibilitySystem.EnableAllRenderers(true);
            if (GUILayout.Button("Disable all renderers"))
                visibilitySystem.EnableAllRenderers(false);
        }
    }
}
