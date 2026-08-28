using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MapNode
{
    public int id;
    public string nodeName;
    public Vector3 worldPosition;
    public List<int> connectedEdgeIds = new List<int>();
}

[Serializable]
public class MapEdge
{
    public int id;
    public int nodeA;
    public int nodeB;
    [Min(0f)] public float baseDistance;
    [Min(1)] public int reqLevel = 1;
    [Range(0f, 2f)] public float baseRisk;
}

/// <summary>
/// Create > RPG Navigation > Map Graph Data에서 생성합니다.
/// 각 엣지 ID는 양 끝 노드의 connectedEdgeIds에 모두 등록해야 합니다.
/// </summary>
[CreateAssetMenu(fileName = "MapGraphData", menuName = "RPG Navigation/Map Graph Data")]
public class MapGraphData : ScriptableObject
{
    public List<MapNode> nodes = new List<MapNode>();
    public List<MapEdge> edges = new List<MapEdge>();
}
