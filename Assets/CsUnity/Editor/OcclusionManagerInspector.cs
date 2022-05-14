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

            int numLeaves = 0;
            int numVisibleLeaves = 0;
            foreach (var leaf in OcclusionManager.GetAllLeaves())
            {
                numLeaves++;
                if (OcclusionManager.LastLeaf != null && OcclusionManager.IsLeafVisible(OcclusionManager.LastLeaf, leaf))
                    numVisibleLeaves++;
            }

            int numVisibleClusters = OcclusionManager.LastLeaf != null
                ? OcclusionManager.GetPvsListForCluster(OcclusionManager.LastLeaf.Info.Cluster).Count
                : 0;

            int numRenderers = 0;
            int numVisibleRenderers = 0;            
            for (int i = 0; i < OcclusionManager.NumClusters; i++)
            {
                var renderers = OcclusionManager.GetRenderersInCluster(i);
                numRenderers += renderers.Count;
                if (OcclusionManager.LastLeaf != null && OcclusionManager.IsClusterVisible(OcclusionManager.LastLeaf, i))
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
        }
    }
}
