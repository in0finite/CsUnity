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

            int numRenderers = 0;
            int numVisibleRenderers = 0;            
            for (int i = 0; i < OcclusionManager.NumClusters; i++)
            {
                var renderers = OcclusionManager.GetRenderersInCluster(i);
                numRenderers += renderers.Count;
                if (currentLeaf != null && OcclusionManager.IsClusterVisible(currentLeaf, i))
                    numVisibleRenderers += renderers.Count;
            }

            GUILayout.Space(20);
            EditorGUILayout.LabelField($"num leaves: {numLeaves}");
            EditorGUILayout.LabelField($"num visible leaves: {numVisibleLeaves}");
            EditorGUILayout.LabelField($"num clusters: {OcclusionManager.NumClusters}");
            EditorGUILayout.LabelField($"num visible clusters: {numVisibleClusters}");
            EditorGUILayout.LabelField($"num renderers: {numRenderers}");
            EditorGUILayout.LabelField($"num visible renderers: {numVisibleRenderers}");
            EditorGUILayout.LabelField($"average renderers per cluster: {numRenderers / (float)OcclusionManager.NumClusters}");

            GUILayout.Space(20);

            if (GUILayout.Button("Enable all renderers"))
                OcclusionManager.EnableAllRenderers(true);
            if (GUILayout.Button("Disable all renderers"))
                OcclusionManager.EnableAllRenderers(false);
        }
    }
}
