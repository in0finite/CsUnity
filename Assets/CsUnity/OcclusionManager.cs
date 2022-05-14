using SourceUtils;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using uSource;
using uSource.MathLib;
using SourceUtils.ValveBsp;

namespace CsUnity
{
    public class OcclusionManager : MonoBehaviour
    {
        // array of PVS sets - index is cluster number, HashSet represents clusters which are visible by that cluster
        private static HashSet<int>[] m_vpsList = System.Array.Empty<HashSet<int>>();
        
        // key is cluster number, value is list of leafs that are inside of it
        private static Dictionary<int, List<BspTree.Leaf>> m_leafsPerCluster = 
            new Dictionary<int, List<BspTree.Leaf>>();

        private static BspTree m_worldSpawnBspTree;
        
        private static Renderer[] m_renderers = System.Array.Empty<Renderer>();

        // last leaf where camera was located
        public static BspTree.Leaf LastLeaf { get; private set; } = null;

        public static int NumClusters => m_vpsList.Length;


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

            m_worldSpawnBspTree = new BspTree(bspFile, 0);

            // cache leafs per clusters
            m_leafsPerCluster = new Dictionary<int, List<BspTree.Leaf>>();
            foreach (var leaf in GetAllLeaves())
            {
                if (m_leafsPerCluster.TryGetValue(leaf.Info.Cluster, out var list))
                    list.Add(leaf);
                else
                    m_leafsPerCluster.Add(leaf.Info.Cluster, new List<BspTree.Leaf> { leaf });
            }

            m_renderers = Object.FindObjectsOfType<Renderer>();
        }

        public static BspTree.Leaf GetLeafAt(UnityEngine.Vector3 pos)
        {
            if (null == m_worldSpawnBspTree)
                return null;

            var converted = Convert(pos);
            return m_worldSpawnBspTree.GetIntersectingLeaves(converted, converted).SingleOrDefault();
        }

        public static BspTree.Leaf CalculateCurrentLeaf()
        {
            if (null == Camera.current)
                return null;

            return GetLeafAt(Camera.current.transform.position);
        }

        public static bool IsLeafVisible(BspTree.Leaf leafFrom, BspTree.Leaf leafTarget)
        {
            if (leafFrom.Info.Cluster < 0)
                return false;

            var visibleClusters = m_vpsList[leafFrom.Info.Cluster];
            return visibleClusters.Contains(leafTarget.Info.Cluster);
        }

        public static HashSet<int> GetPvsListForCluster(int clusterNumber)
        {
            return m_vpsList[clusterNumber];
        }

        public static IEnumerable<BspTree.Leaf> GetAllLeaves()
        {
            if (null == m_worldSpawnBspTree)
                yield break;

            var stack = new Stack<BspTree.IElem>();
            stack.Push(m_worldSpawnBspTree.HeadNode);

            while (stack.Count > 0)
            {
                var elem = stack.Pop();
                if (null == elem)
                    continue;

                if (elem is BspTree.Node node)
                {
                    stack.Push(node.ChildA);
                    stack.Push(node.ChildB);
                }
                else if (elem is BspTree.Leaf leaf)
                {
                    yield return leaf;
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (null == m_worldSpawnBspTree)
                return;

            LastLeaf = CalculateCurrentLeaf();

            var currentLeaf = LastLeaf;

            foreach (var leaf in GetAllLeaves())
            {
                if (leaf == currentLeaf)
                    Gizmos.color = Color.blue;
                else if (IsLeafVisible(currentLeaf, leaf))
                    Gizmos.color = Color.green;
                else
                    continue;

                GizmosDrawCube(leaf.Info.Min, leaf.Info.Max);
            }

            // draw all renderers in current leaf

            /*for (int i = 0; i < m_renderers.Length; i++)
            {
                var renderer = m_renderers[i];
                m_worldSpawnBspTree.GetIntersectingLeaves(renderer.bounds.min, renderer.bounds.max);
            }*/
        }

        static void GizmosDrawCube(Vector3S min, Vector3S max)
        {
            UnityEngine.Vector3 convertedMin = Convert(min);
            UnityEngine.Vector3 convertedMax = Convert(max);

            convertedMin += UnityEngine.Vector3.one * 0.15f;
            convertedMax -= UnityEngine.Vector3.one * 0.15f;

            Gizmos.DrawWireCube((convertedMin + convertedMax) * 0.5f, convertedMax - convertedMin);
        }

        static UnityEngine.Vector3 Convert(Vector3S v)
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
