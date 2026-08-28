using System;
using UnityEngine;

/// <summary>
/// 플레이어의 길찾기에 영향을 주는 상태를 관리합니다.
/// OnRoutingStateChanged에 경로 재계산 함수를 구독하면 HP/레벨 변경 시 재탐색할 수 있습니다.
/// </summary>
public class PlayerState : MonoBehaviour
{
    [Min(1)] public int level = 1;
    [Min(0.01f)] public float maxHp = 100f;
    [Min(0f)] public float currentHp = 100f;

    /// <summary>레벨 또는 HP 비율이 바뀌어 현재 경로의 비용이 달라졌을 때 발생합니다.</summary>
    public event Action OnRoutingStateChanged;

    /// <summary>현재 HP를 최대 HP로 나눈 값이며 항상 0~1입니다.</summary>
    public float HpRatio => maxHp <= 0f ? 0f : Mathf.Clamp01(currentHp / maxHp);

    private int lastLevel;
    private float lastMaxHp;
    private float lastCurrentHp;

    private void Awake()
    {
        ClampState();
        CacheState();
    }

    private void Update()
    {
        // 다른 게임플레이 코드가 public 필드를 직접 변경해도 재탐색 이벤트가 발생하도록 감시합니다.
        ClampState();
        if (lastLevel != level || !Mathf.Approximately(lastMaxHp, maxHp) ||
            !Mathf.Approximately(lastCurrentHp, currentHp))
        {
            CacheState();
            OnRoutingStateChanged?.Invoke();
        }
    }

    public void SetLevel(int newLevel)
    {
        level = Mathf.Max(1, newLevel);
        NotifyIfChanged();
    }

    public void SetHp(float newCurrentHp, float newMaxHp)
    {
        maxHp = Mathf.Max(0.01f, newMaxHp);
        currentHp = Mathf.Clamp(newCurrentHp, 0f, maxHp);
        NotifyIfChanged();
    }

    private void OnValidate()
    {
        ClampState();
    }

    private void ClampState()
    {
        level = Mathf.Max(1, level);
        maxHp = Mathf.Max(0.01f, maxHp);
        currentHp = Mathf.Clamp(currentHp, 0f, maxHp);
    }

    private void NotifyIfChanged()
    {
        ClampState();
        if (lastLevel == level && Mathf.Approximately(lastMaxHp, maxHp) &&
            Mathf.Approximately(lastCurrentHp, currentHp))
        {
            return;
        }

        CacheState();
        OnRoutingStateChanged?.Invoke();
    }

    private void CacheState()
    {
        lastLevel = level;
        lastMaxHp = maxHp;
        lastCurrentHp = currentHp;
    }
}
