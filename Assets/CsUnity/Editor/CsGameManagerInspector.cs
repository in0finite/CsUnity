using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using uSource;
using System.Linq;
using System.IO;

namespace CsUnity.Editor
{
    [CustomEditor(typeof(CsGameManager))]
    public class CsGameManagerInspector : UnityEditor.Editor
    {
        private Vector2 m_scrollViewPos = Vector2.zero;
        private string[] m_maps = null;


        public override void OnInspectorGUI()
        {
            base.DrawDefaultInspector();

            if (null == m_maps)
                EnumerateMaps();

            GUILayout.Space(20);

            EditorGUILayout.LabelField("Load map:");

            m_scrollViewPos = GUILayout.BeginScrollView(m_scrollViewPos, GUILayout.MaxHeight(400));

            foreach (string map in m_maps)
            {
                if (GUILayout.Button(map))
                {
                    uSettings.Instance.MapName = map;
                    CsGameManager.ReloadMap();
                }
            }

            GUILayout.EndScrollView();

            GUILayout.Space(20);

            if (GUILayout.Button("Refresh map list"))
                EnumerateMaps();
        }

        private void EnumerateMaps()
        {
            m_maps = CsGameManager.EnumerateMaps()
                .Select(Path.GetFileNameWithoutExtension)
                .ToArray();
        }
    }
}
