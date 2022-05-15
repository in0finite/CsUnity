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
        public bool performCulling = true;
        public bool performCullingInEditMode = false;

        // array of PVS sets - index is cluster number, HashSet represents clusters which are visible by that cluster
        private static HashSet<int>[] m_vpsList = System.Array.Empty<HashSet<int>>();
        
        // key is cluster number, value is list of leafs that are inside of it
        private static Dictionary<int, List<BspTree.Leaf>> m_leafsPerCluster = 
            new Dictionary<int, List<BspTree.Leaf>>();

        private static BspTree m_worldSpawnBspTree;
        
        private static List<Renderer>[] m_renderersPerCluster = System.Array.Empty<List<Renderer>>();

        // last leaf where camera was located
        public static BspTree.Leaf LastLeaf { get; private set; } = null;

        public static int NumClusters => m_vpsList.Length;

        private readonly System.Diagnostics.Stopwatch m_stopwatch = new System.Diagnostics.Stopwatch();


        static OcclusionManager()
        {
            CsGameManager.OnMapLoaded -= OnMapLoaded;
            CsGameManager.OnMapLoaded += OnMapLoaded;
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            var obj = FindObjectOfType<OcclusionManager>();
            if (null == obj)
                return;

            UnityEditor.EditorApplication.update -= obj.EditorUpdate;
            UnityEditor.EditorApplication.update += obj.EditorUpdate;
        }
#endif

        private static void OnMapLoaded(ValveBspFile bspFile)
        {
            Debug.Log("OcclusionManager: initializing map data");

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

            // cache renderers, so they can be quickly enabled/disabled later
            m_renderersPerCluster = new List<Renderer>[numClusters];
            var allRenderers = Object.FindObjectsOfType<Renderer>();
            var intersectingLeavesList = new List<BspTree.Leaf>();
            var intersectingClusters = new HashSet<int>();
            for (int i = 0; i < allRenderers.Length; i++)
            {
                var r = allRenderers[i];

                if (!r.gameObject.isStatic)
                    continue;

                var bounds = r.bounds;

                intersectingLeavesList.Clear();
                intersectingClusters.Clear();
                m_worldSpawnBspTree.GetIntersectingLeaves(Convert(bounds.min), Convert(bounds.max), intersectingLeavesList);
                for (int j = 0; j < intersectingLeavesList.Count; j++)
                    intersectingClusters.Add(intersectingLeavesList[j].Info.Cluster);

                foreach (int clusterIndex in intersectingClusters)
                {
                    m_renderersPerCluster[clusterIndex] ??= new List<Renderer>();
                    m_renderersPerCluster[clusterIndex].Add(r);
                }
            }
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
            return IsClusterVisible(leafFrom, leafTarget.Info.Cluster);
        }

        public static bool IsClusterVisible(BspTree.Leaf leafFrom, int clusterNumber)
        {
            if (leafFrom.Info.Cluster < 0)
                return false;

            var visibleClusters = m_vpsList[leafFrom.Info.Cluster];
            return visibleClusters.Contains(clusterNumber);
        }

        public static HashSet<int> GetPvsListForCluster(int clusterNumber)
        {
            return m_vpsList[clusterNumber];
        }

        public static IReadOnlyList<Renderer> GetRenderersInCluster(int clusterNumber)
        {
            return (IReadOnlyList<Renderer>)m_renderersPerCluster[clusterNumber] ?? System.Array.Empty<Renderer>();
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

        public static void EnableAllRenderers(bool enable)
        {
            foreach (var renderers in m_renderersPerCluster)
                EnableRenderers(renderers, enable);
        }

        private void OnDrawGizmosSelected()
        {
            if (null == m_worldSpawnBspTree)
                return;

            var currentLeaf = CalculateCurrentLeaf();

            foreach (var leaf in GetAllLeaves())
            {
                if (leaf == currentLeaf)
                    Gizmos.color = Color.blue;
                else if (currentLeaf != null && IsLeafVisible(currentLeaf, leaf))
                    Gizmos.color = Color.green;
                else
                    continue;

                GizmosDrawCube(leaf.Info.Min, leaf.Info.Max);
            }

            // draw all renderers in current cluster
            if (currentLeaf != null)
            {
                Gizmos.color = Color.red;
                var renderers = m_renderersPerCluster[currentLeaf.Info.Cluster];
                foreach (var renderer in renderers)
                {
                    var bounds = renderer.bounds;
                    GizmosDrawCube(bounds.min, bounds.max);
                }
            }

        }

        static void GizmosDrawCube(Vector3S min, Vector3S max)
        {
            UnityEngine.Vector3 convertedMin = Convert(min);
            UnityEngine.Vector3 convertedMax = Convert(max);
            GizmosDrawCube(convertedMin, convertedMax);
        }

        static void GizmosDrawCube(UnityEngine.Vector3 min, UnityEngine.Vector3 max)
        {
            // add some offset so we can easily distinguish between neighbouring boxes
            min += UnityEngine.Vector3.one * 0.15f;
            max -= UnityEngine.Vector3.one * 0.15f;

            Gizmos.DrawWireCube((min + max) * 0.5f, max - min);
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

        private void LateUpdate()
        {
            if (this.performCulling)
                this.UpdateCulling();
        }

        private void EditorUpdate()
        {
            if (Application.isPlaying)
                return; // it will be done in regular updates

            if (this.performCulling && this.performCullingInEditMode)
                this.UpdateCulling();
        }

        private void UpdateCulling()
        {
            if (null == Camera.current)
                return;

            var newLeaf = GetLeafAt(Camera.current.transform.position);
            if (newLeaf == LastLeaf)
                return;

            if (null == newLeaf)
                return; // don't update anything

            m_stopwatch.Restart();

            var oldLeaf = LastLeaf;

            // current leaf changed to a valid one - update renderers

            int newVisibleClusterIndex = newLeaf.Info.Cluster;
            var newVisibleClusters = m_vpsList[newVisibleClusterIndex];

            int numOldVisibleClusters = 0;
            int numClustersActivated = 0;
            int numClustersDeactivated = 0;

            if (null == oldLeaf)
            {
                // all renderers were enabled
                // disable all renderers except newly visible ones
                for (int i = 0; i < NumClusters; i++)
                    EnableRenderers(m_renderersPerCluster[i], i == newVisibleClusterIndex);
            }
            else
            {
                var oldVisibleClusters = m_vpsList[oldLeaf.Info.Cluster];
                numOldVisibleClusters = oldVisibleClusters.Count;

                foreach (int cluster in newVisibleClusters)
                {
                    if (!oldVisibleClusters.Contains(cluster)) // this cluster just became visible
                    {
                        EnableRenderers(m_renderersPerCluster[cluster], true);
                        numClustersActivated++;
                    }
                }

                foreach (int cluster in oldVisibleClusters)
                {
                    if (!newVisibleClusters.Contains(cluster)) // this cluster just became invisible
                    {
                        EnableRenderers(m_renderersPerCluster[cluster], false);
                        numClustersDeactivated++;
                    }
                }
            }

            LastLeaf = newLeaf;

            double elapsedMs = m_stopwatch.Elapsed.TotalMilliseconds;
            Debug.Log($"Culling updated: " +
                $"elapsed {elapsedMs} ms, " +
                $"old num clusters visible {numOldVisibleClusters}, " +
                $"new num clusters visible {newVisibleClusters.Count}, " +
                $"num clusters became visible {numClustersActivated}, " +
                $"num clusters became invisible {numClustersDeactivated}");
        }

        static void EnableRenderers(List<Renderer> renderers, bool enable)
        {
            for (int i = 0; i < renderers.Count; i++)
                renderers[i].enabled = enable;
        }
    }
}
