using System;
using UnityEngine;

/// <summary>위험도와 플레이어 상태를 이동 비용으로 바꾸는 순수 계산 유틸리티입니다.</summary>
public static class CostCalculator
{
    public const int MaxPassableLevelGap = 15;
    public enum RouteMode
    {
        RiskTakingShortest,
        Recommended,
        MaximumSafety
    }

    public static float CalculateEdgeCost(MapEdge edge, int playerLevel, float hpRatio)
    {
        return CalculateEdgeCost(edge, playerLevel, hpRatio, RouteMode.Recommended);
    }

    public static float CalculateEdgeCost(MapEdge edge, int playerLevel, float hpRatio, RouteMode routeMode)
    {
        if (edge == null)
        {
            throw new ArgumentNullException(nameof(edge));
        }

        if (float.IsNaN(edge.baseDistance) || float.IsInfinity(edge.baseDistance) || edge.baseDistance < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(edge), "baseDistance must be a finite value greater than or equal to zero.");
        }

        if (routeMode == RouteMode.RiskTakingShortest)
        {
            return edge.baseDistance;
        }

        int levelDifference = edge.reqLevel - playerLevel;
        if (levelDifference > MaxPassableLevelGap)
        {
            return float.MaxValue;
        }

        if (routeMode == RouteMode.MaximumSafety)
        {
            return edge.reqLevel * 10000f + edge.baseRisk * 1000f + edge.baseDistance;
        }

        float levelPenalty = 0f;
        if (levelDifference > 0)
        {
            float normalizedDifference = levelDifference / 5f;
            levelPenalty = normalizedDifference * normalizedDifference * 2f;
        }

        float hpMultiplier = 1f + 3f * (1f - Mathf.Clamp01(hpRatio));
        float totalPenalty = (levelPenalty + edge.baseRisk) * hpMultiplier;

        return edge.baseDistance * (1f + totalPenalty);
    }
}
