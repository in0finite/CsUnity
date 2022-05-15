using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CsUnity.Editor
{
    [CustomEditor(typeof(OcclusionManager))]
    public class OcclusionManagerInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.DrawDefaultInspector();

            var currentLeaf = OcclusionManager.LastLeaf;

            int numLeaves = 0;
            int numVisibleLeaves = 0;
            foreach (var leaf in OcclusionManager.GetAllLeaves())
            {
                numLeaves++;
                if (currentLeaf != null && OcclusionManager.IsLeafVisible(currentLeaf, leaf))
                    numVisibleLeaves++;
            }

            int numVisibleClusters = currentLeaf != null
                ? OcclusionManager.GetPvsListForCluster(currentLeaf.Info.Cluster).Count
                : 0;

            int numRenderers = OcclusionManager.NumRenderers;
            int numVisibleRenderers = 0;
            long numReferences = 0;
            for (int i = 0; i < OcclusionManager.NumRenderers; i++)
            {
                var rendererInfo = OcclusionManager.GetRenderer(i);
                if (rendererInfo.numClustersReferencing > 0)
                    numVisibleRenderers++;
                numReferences += rendererInfo.numClustersReferencing;
            }

            GUILayout.Space(20);
            EditorGUILayout.LabelField($"num leaves: {numLeaves}");
            EditorGUILayout.LabelField($"num visible leaves: {numVisibleLeaves}");
            EditorGUILayout.LabelField($"num clusters: {OcclusionManager.NumClusters}");
            EditorGUILayout.LabelField($"num visible clusters: {numVisibleClusters}");
            EditorGUILayout.LabelField($"num renderers: {numRenderers}");
            EditorGUILayout.LabelField($"num renderers in culling system: {OcclusionManager.NumRenderersInCullingSystem}");
            EditorGUILayout.LabelField($"num visible renderers: {numVisibleRenderers}");
            EditorGUILayout.LabelField($"average renderers per cluster: {numRenderers / (float)OcclusionManager.NumClusters}");
            EditorGUILayout.LabelField($"average references per visible renderer: {numReferences / (float)numVisibleRenderers}");

            GUILayout.Space(20);

            if (GUILayout.Button("Enable all renderers"))
                OcclusionManager.EnableAllRenderers(true);
            if (GUILayout.Button("Disable all renderers"))
                OcclusionManager.EnableAllRenderers(false);
        }
    }
}
