using System.Collections.Generic;
using UnityEngine;

/// <summary>FindPath 결과를 LineRenderer로 그리고, 각 지점을 아래 방향 Raycast로 지형에 맞춥니다.</summary>
[RequireComponent(typeof(LineRenderer))]
public class PathVisualizer : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField, Min(0f)] private float raycastStartHeight = 500f;
    [SerializeField, Min(0f)] private float raycastDistance = 1000f;
    [SerializeField] private LayerMask terrainLayers = ~0;
    [SerializeField] private bool use2DTopDown;
    [SerializeField] private float topDownLineDepth = -2f;

    private void Awake()
    {
        EnsureLineRenderer();
    }

    private void OnValidate()
    {
        EnsureLineRenderer();
    }

    public void DrawPath(IReadOnlyList<Vector3> path)
    {
        EnsureLineRenderer();
        if (path == null || path.Count == 0)
        {
            ClearPath();
            return;
        }

        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = path.Count;
        for (int index = 0; index < path.Count; index++)
        {
            lineRenderer.SetPosition(index, use2DTopDown ? ToTopDownPosition(path[index]) : ProjectToGround(path[index]));
        }
    }

    /// <summary>
    /// XY 평면을 사용하는 2D 탑뷰용 설정입니다. 2D에서는 Y가 월드 높이가 아니라 지도 좌표이므로 3D 지면 Raycast를 사용하지 않습니다.
    /// </summary>
    public void ConfigureForTopDown2D(float lineDepth = -2f)
    {
        use2DTopDown = true;
        topDownLineDepth = lineDepth;
    }

    public void ClearPath()
    {
        EnsureLineRenderer();
        lineRenderer.positionCount = 0;
    }

    private Vector3 ProjectToGround(Vector3 position)
    {
        Vector3 rayOrigin = position + Vector3.up * raycastStartHeight;
        float totalDistance = raycastStartHeight + raycastDistance;
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, totalDistance, terrainLayers, QueryTriggerInteraction.Ignore))
        {
            return hit.point;
        }

        Debug.LogWarning($"[PathVisualizer] No ground hit for path point {position}; using its original height.", this);
        return position;
    }

    private Vector3 ToTopDownPosition(Vector3 position)
    {
        return new Vector3(position.x, position.y, topDownLineDepth);
    }

    private void EnsureLineRenderer()
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
        }
    }
}
