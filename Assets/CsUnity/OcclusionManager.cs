using SourceUtils;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using uSource;
using uSource.MathLib;

namespace CsUnity
{
    public class OcclusionManager : MonoBehaviour
    {
        private static HashSet<int>[] m_vpsList = System.Array.Empty<HashSet<int>>();
        private static SourceUtils.ValveBsp.BspTree m_worldSpawnBspTree;
        private static Renderer[] m_renderers = System.Array.Empty<Renderer>();


        static OcclusionManager()
        {
            CsGameManager.OnMapLoaded -= OnMapLoaded;
            CsGameManager.OnMapLoaded += OnMapLoaded;
        }

        private static void OnMapLoaded(ValveBspFile bspFile)
        {
            // we need to iterate through all PVS sets, because VisibilityLump does lazy loading
            int numClusters = bspFile.Visibility.NumClusters;
            m_vpsList = new HashSet<int>[numClusters];
            for (int i = 0; i < numClusters; i++)
            {
                m_vpsList[i] = bspFile.Visibility[i];
            }

            m_worldSpawnBspTree = new SourceUtils.ValveBsp.BspTree(bspFile, 0);

            m_renderers = Object.FindObjectsOfType<Renderer>();
        }

        static SourceUtils.ValveBsp.BspTree.Leaf GetLeafAt(
            SourceUtils.ValveBsp.BspTree bspTree,
            UnityEngine.Vector3 pos)
        {
            var converted = Convert(pos);
            return bspTree.GetIntersectingLeaves(converted, converted).SingleOrDefault();
        }

        static SourceUtils.ValveBsp.BspTree.Leaf GetCurrentLeaf()
        {
            if (null == Camera.current)
                return null;

            return GetLeafAt(m_worldSpawnBspTree, Camera.current.transform.position);
        }

        private void OnDrawGizmosSelected()
        {
            if (null == m_worldSpawnBspTree)
                return;

            var currentLeaf = GetCurrentLeaf();

            var stack = new Stack<SourceUtils.ValveBsp.BspTree.IElem>();
            stack.Push(m_worldSpawnBspTree.HeadNode);

            while (stack.Count > 0)
            {
                var elem = stack.Pop();
                if (null == elem)
                    continue;

                SourceUtils.ValveBsp.Vector3S min = default, max = default;
                if (elem is SourceUtils.ValveBsp.BspTree.Node node)
                {
                    Gizmos.color = Color.yellow;

                    min = node.Info.Min;
                    max = node.Info.Max;

                    stack.Push(node.ChildA);
                    stack.Push(node.ChildB);
                }
                else if (elem is SourceUtils.ValveBsp.BspTree.Leaf leaf)
                {
                    Gizmos.color = leaf == currentLeaf ? Color.blue : Color.Lerp(Color.red, Color.yellow, 0.5f);

                    min = leaf.Info.Min;
                    max = leaf.Info.Max;

                    GizmosDrawCube(min, max);
                }
            }

            // draw all renderers in current leaf

            /*for (int i = 0; i < m_renderers.Length; i++)
            {
                var renderer = m_renderers[i];
                m_worldSpawnBspTree.GetIntersectingLeaves(renderer.bounds.min, renderer.bounds.max);
            }*/
        }

        static void GizmosDrawCube(SourceUtils.ValveBsp.Vector3S min, SourceUtils.ValveBsp.Vector3S max)
        {
            UnityEngine.Vector3 convertedMin = Convert(min);
            UnityEngine.Vector3 convertedMax = Convert(max);

            convertedMin += UnityEngine.Vector3.one * 0.15f;
            convertedMax -= UnityEngine.Vector3.one * 0.15f;

            Gizmos.DrawWireCube((convertedMin + convertedMax) * 0.5f, convertedMax - convertedMin);
        }

        static UnityEngine.Vector3 Convert(SourceUtils.ValveBsp.Vector3S v)
        {
            var Vector3D = v.ToUnityVec3();

            float tempX = Vector3D.x;

            Vector3D.x = -Vector3D.y;
            Vector3D.y = Vector3D.z;
            Vector3D.z = tempX;

            return Vector3D * uLoader.UnitScale;
        }

        static SourceUtils.Vector3 Convert(UnityEngine.Vector3 v)
        {
            v /= uLoader.UnitScale;

            var result = new SourceUtils.Vector3();

            result.X = v.z;
            result.Y = -v.x;
            result.Z = v.y;

            return result;
        }
    }
}
