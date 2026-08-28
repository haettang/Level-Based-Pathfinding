using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sparse Graph용 동기식 A* 경로 탐색기입니다. MapEdge는 nodeA와 nodeB 사이의 양방향 경로입니다.
/// </summary>
public class InGamePathfinder : MonoBehaviour
{
    [SerializeField] private MapGraphData mapGraphData;
    [SerializeField] private PlayerState playerState;
    [SerializeField] private CostCalculator.RouteMode routeMode = CostCalculator.RouteMode.Recommended;

    private readonly Dictionary<int, MapNode> nodesById = new Dictionary<int, MapNode>();
    private readonly Dictionary<int, MapEdge> edgesById = new Dictionary<int, MapEdge>();

    public void Configure(MapGraphData graphData, PlayerState state)
    {
        mapGraphData = graphData;
        playerState = state;
    }

    public void SetRouteMode(CostCalculator.RouteMode newRouteMode)
    {
        routeMode = newRouteMode;
    }

    private void OnValidate()
    {
        // AddComponent 직후 OnValidate가 Configure보다 먼저 호출될 수 있습니다.
        if (Application.isPlaying && mapGraphData != null)
        {
            RebuildLookupTables();
        }
    }

    /// <summary>시작 노드부터 목표 노드까지의 월드 좌표 목록입니다. 실패하면 빈 목록을 반환합니다.</summary>
    public List<Vector3> FindPath(int startNodeId, int targetNodeId)
    {
        try
        {
            RebuildLookupTables();
            if (playerState == null)
            {
                throw new InvalidOperationException("PlayerState reference is not assigned.");
            }

            if (!nodesById.TryGetValue(startNodeId, out MapNode startNode) ||
                !nodesById.TryGetValue(targetNodeId, out MapNode targetNode))
            {
                throw new ArgumentException("Start or target node ID does not exist in MapGraphData.");
            }

            if (startNodeId == targetNodeId)
            {
                return new List<Vector3> { startNode.worldPosition };
            }

            return RunAStar(startNode, targetNode);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[InGamePathfinder] Path search failed ({startNodeId} -> {targetNodeId}): {exception.Message}", this);
            return new List<Vector3>();
        }
    }

    private List<Vector3> RunAStar(MapNode startNode, MapNode targetNode)
    {
        var openSet = new HashSet<int> { startNode.id };
        var closedSet = new HashSet<int>();
        var cameFrom = new Dictionary<int, int>();
        var gCost = new Dictionary<int, float> { { startNode.id, 0f } };
        var hCost = new Dictionary<int, float>
        {
            { startNode.id, Vector3.Distance(startNode.worldPosition, targetNode.worldPosition) }
        };

        while (openSet.Count > 0)
        {
            int currentId = GetLowestFCostNode(openSet, gCost, hCost);
            if (currentId == targetNode.id)
            {
                return ReconstructPath(cameFrom, currentId);
            }

            openSet.Remove(currentId);
            closedSet.Add(currentId);
            MapNode currentNode = nodesById[currentId];

            if (currentNode.connectedEdgeIds == null)
            {
                Debug.LogWarning($"[InGamePathfinder] Node {currentId} has no connectedEdgeIds list.", this);
                continue;
            }

            foreach (int edgeId in currentNode.connectedEdgeIds)
            {
                if (!edgesById.TryGetValue(edgeId, out MapEdge edge))
                {
                    Debug.LogWarning($"[InGamePathfinder] Node {currentId} references missing edge {edgeId}.", this);
                    continue;
                }

                if (!TryGetOtherEndpoint(edge, currentId, out int neighborId))
                {
                    Debug.LogWarning($"[InGamePathfinder] Edge {edgeId} is not connected to node {currentId}.", this);
                    continue;
                }

                if (closedSet.Contains(neighborId))
                {
                    continue;
                }

                float edgeCost = CostCalculator.CalculateEdgeCost(
                    edge,
                    playerState.level,
                    playerState.HpRatio,
                    routeMode);

                Debug.Log(
                    $"[A*] Node {currentId} -> {neighborId} | " +
                    $"Edge {edge.id} | " +
                    $"ReqLv {edge.reqLevel} | " +
                    $"PlayerLv {playerState.level} | " +
                    $"Cost {edgeCost}");

                if (edgeCost == float.MaxValue)
                {
                    Debug.Log(
                        $"[A*] BLOCKED: Edge {edge.id} " +
                        $"({currentId} -> {neighborId}), " +
                        $"ReqLv {edge.reqLevel}, " +
                        $"PlayerLv {playerState.level}");

                    continue;
                }

                float tentativeGCost = gCost[currentId] + edgeCost;
                if (float.IsInfinity(tentativeGCost) || tentativeGCost >= float.MaxValue)
                {
                    continue;
                }

                if (!gCost.TryGetValue(neighborId, out float knownGCost) || tentativeGCost < knownGCost)
                {
                    cameFrom[neighborId] = currentId;
                    gCost[neighborId] = tentativeGCost;
                    hCost[neighborId] = Vector3.Distance(nodesById[neighborId].worldPosition, targetNode.worldPosition);
                    openSet.Add(neighborId);
                }
            }
        }

        Debug.LogError($"[InGamePathfinder] No passable path exists from node {startNode.id} to node {targetNode.id}.", this);
        return new List<Vector3>();
    }

    private void RebuildLookupTables()
    {
        if (mapGraphData == null)
        {
            throw new InvalidOperationException("MapGraphData reference is not assigned.");
        }

        nodesById.Clear();
        edgesById.Clear();

        foreach (MapNode node in mapGraphData.nodes)
        {
            if (node == null || !nodesById.TryAdd(node.id, node))
            {
                throw new InvalidOperationException("MapGraphData contains a null node or duplicate node ID.");
            }
        }

        foreach (MapEdge edge in mapGraphData.edges)
        {
            if (edge == null || !edgesById.TryAdd(edge.id, edge))
            {
                throw new InvalidOperationException("MapGraphData contains a null edge or duplicate edge ID.");
            }

            if (!nodesById.ContainsKey(edge.nodeA) || !nodesById.ContainsKey(edge.nodeB))
            {
                throw new InvalidOperationException($"Edge {edge.id} references a node that does not exist.");
            }
        }
    }

    private static bool TryGetOtherEndpoint(MapEdge edge, int nodeId, out int otherNodeId)
    {
        if (edge.nodeA == nodeId)
        {
            otherNodeId = edge.nodeB;
            return true;
        }

        if (edge.nodeB == nodeId)
        {
            otherNodeId = edge.nodeA;
            return true;
        }

        otherNodeId = default;
        return false;
    }

    private static int GetLowestFCostNode(HashSet<int> openSet, Dictionary<int, float> gCost, Dictionary<int, float> hCost)
    {
        int bestNodeId = -1;
        float bestFCost = float.MaxValue;
        float bestHCost = float.MaxValue;

        foreach (int nodeId in openSet)
        {
            float currentGCost = gCost[nodeId];
            float currentHCost = hCost[nodeId];
            float currentFCost = currentGCost + currentHCost;
            if (currentFCost < bestFCost ||
                (Mathf.Approximately(currentFCost, bestFCost) && currentHCost < bestHCost))
            {
                bestNodeId = nodeId;
                bestFCost = currentFCost;
                bestHCost = currentHCost;
            }
        }

        return bestNodeId;
    }

    private List<Vector3> ReconstructPath(Dictionary<int, int> cameFrom, int currentId)
    {
        var path = new List<Vector3>();
        path.Add(nodesById[currentId].worldPosition);

        while (cameFrom.TryGetValue(currentId, out int previousId))
        {
            currentId = previousId;
            path.Add(nodesById[currentId].worldPosition);
        }

        path.Reverse();
        return path;
    }
}
