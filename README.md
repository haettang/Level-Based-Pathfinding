# 플레이어 컨디션 기반 동적 가중치 위험 회피 경로 탐색 시스템


플레이어의 **레벨·HP와 지역 위험도를 이동 비용에 반영하여**, 같은 맵에서도 플레이어 상태에 따라 적합한 경로를 선택하는 Unity 기반 A* 경로 탐색 시스템입니다.

> 이 저장소는 원본 Unity 프로젝트에서 포트폴리오에 필요한 핵심 코드만 선별하여 공개한 코드 아카이브입니다. Unity 프로젝트 전체나 독립 실행 빌드를 제공하는 것을 목적으로 하지 않습니다.

---

## Project Overview

일반적인 최단 경로 탐색은 모든 플레이어에게 동일한 거리 중심 경로를 제공합니다.

하지만 게임에서는 플레이어의 현재 상태에 따라 같은 경로의 가치가 달라질 수 있습니다.

* 플레이어 레벨보다 높은 난이도의 지역
* 위험도가 높은 지역
* HP가 낮아 위험 지역을 피해야 하는 상황

이러한 요소를 경로 탐색에 반영하기 위해 **도로 또는 이동 가능한 지역을 그래프의 간선으로 모델링하고, 각 간선에 거리·요구 레벨·위험도 정보를 부여**했습니다.

`CostCalculator`는 플레이어 상태와 경로 탐색 모드에 따라 각 간선의 이동 비용을 계산하고, `InGamePathfinder`는 계산된 비용을 A* 탐색에 적용합니다.

---

## Core Concept

```text
게임 맵의 이동 경로
        ↓
거리만 사용하는 최단 경로의 한계
        ↓
플레이어 레벨·HP를 경로 비용에 반영
        ↓
간선별 요구 레벨·위험도 모델링
        ↓
A* 기반 탐색
        ↓
플레이어 상태에 적합한 경로 선택
```

### 상태 기반 비용 계산

`PlayerState`는 플레이어의 `level`과 `currentHp`, `maxHp`를 관리하며 HP 비율을 0~1 범위로 정규화합니다.

`Recommended` 모드에서는 플레이어의 상태에 따라 다음과 같이 비용이 달라집니다.

```text
levelDifference = reqLevel - playerLevel
levelPenalty    = ((levelDifference / 5)²) × 2   // 양수인 경우
hpMultiplier    = 1 + 3 × (1 - hpRatio)

edgeCost = baseDistance ×
           (1 + (levelPenalty + baseRisk) × hpMultiplier)
```

플레이어보다 요구 레벨이 높은 지역일수록 추가 비용이 증가하며, HP가 낮을수록 위험도와 레벨 차이에 따른 비용의 영향이 커집니다.

요구 레벨이 플레이어 레벨 이하인 경우 레벨 페널티는 발생하지 않지만, 해당 지역의 기본 위험도는 비용에 반영됩니다.

---

## Pathfinding Modes

플레이어의 목적에 따라 서로 다른 비용 정책을 적용할 수 있도록 세 가지 경로 탐색 모드를 구현했습니다.

| 모드                   | 비용 기준                    | 통과 제한                   |
| -------------------- | ------------------------ | ----------------------- |
| `RiskTakingShortest` | 이동 거리만 사용                | 없음                      |
| `Recommended`        | 거리 + 위험도 + 레벨 차이 + HP 상태 | 요구 레벨 차이가 15 초과 시 통과 불가 |
| `MaximumSafety`      | 요구 레벨 → 위험도 → 거리 순으로 우선  | 요구 레벨 차이가 15 초과 시 통과 불가 |

### RiskTakingShortest

```text
edgeCost = baseDistance
```

안전도와 플레이어 상태를 고려하지 않고 **이동 거리만 최소화**합니다.

따라서 다른 모드에서 적용되는 레벨 차이 제한도 적용하지 않습니다.

### Recommended

플레이어의 현재 레벨과 HP를 고려하여 거리와 위험도를 함께 평가합니다.

```text
거리
 + 지역 위험도
 + 플레이어 레벨 대비 난이도
 + 현재 HP 상태
```

게임에서 일반적으로 사용할 수 있는 **상태 기반 추천 경로**를 목표로 한 모드입니다.

### MaximumSafety

다음 우선순위를 기준으로 비용을 계산합니다.

```text
요구 레벨 → 위험도 → 이동 거리
```

높은 난이도의 지역을 최대한 회피하고, 동일한 요구 레벨에서는 위험도가 낮은 경로를 우선하도록 설계했습니다.

현재 구현에서는 HP 상태는 이 모드의 비용 계산에 직접 반영하지 않습니다.

---

## Pathfinding Process

```text
┌──────────────┐
│ PlayerState  │
│ Level / HP   │
└──────┬───────┘
       ↓
┌────────────────┐
│ CostCalculator │
│ Route Mode     │
└───────┬────────┘
        ↓
┌─────────────────┐
│ InGamePathfinder │
│ A* Search        │
└────────┬────────┘
         ↓
┌──────────────────────┐
│ NavigationController │
│ Path Result           │
└──────────────────────┘
```

1. `PlayerState`에서 현재 레벨과 HP 상태를 확인합니다.
2. `CostCalculator`가 선택된 경로 모드에 따라 각 간선의 비용을 계산합니다.
3. `InGamePathfinder`가 계산된 비용을 A*의 누적 비용으로 사용하여 탐색합니다.
4. 탐색이 완료되면 노드 연결 관계를 따라 최종 경로를 복원합니다.
5. `NavigationController`가 경로 요청 및 이동을 관리합니다.
6. 플레이어 상태가 변경되면 목적지가 설정된 경우 경로를 다시 계산합니다.

`InGamePathfinder`의 A* 휴리스틱에는 현재 노드와 목표 노드 사이의 직선거리(`Vector3.Distance`)를 사용합니다.

---

## Key Implementation

### `MapGraphData`

노드와 간선으로 구성된 희소 그래프 데이터를 관리합니다.

* `MapNode`
* `MapEdge`
* 간선 연결 관계
* 직렬화 가능한 그래프 데이터

각 간선에는 다음과 같은 경로 탐색 정보를 저장합니다.

```text
baseDistance
reqLevel
baseRisk
```

### `PlayerState`

플레이어의 현재 상태를 관리합니다.

* 플레이어 레벨
* 현재 HP / 최대 HP
* HP 비율 정규화
* 경로 재계산을 위한 상태 변경 이벤트

### `CostCalculator`

경로 탐색의 비용 정책을 담당합니다.

* 경로 탐색 모드별 비용 계산
* 레벨 차이에 따른 페널티
* HP 상태에 따른 비용 변화
* 위험도 반영
* 통과 가능한 레벨 차이 제한

### `InGamePathfinder`

희소 그래프에서 A* 경로 탐색을 수행합니다.

* 그래프 유효성 검증
* 노드 및 간선 ID 관리
* A* 탐색
* 누적 비용(`gCost`) 계산
* 휴리스틱(`hCost`) 계산
* 최종 경로 복원

### `NavigationController`

경로 탐색과 실제 게임 시스템을 연결합니다.

* 경로 요청
* 탐색 결과 처리
* 플레이어 이동
* 플레이어 상태 변경에 따른 재탐색

### `PathVisualizer`

탐색 결과를 Unity의 `LineRenderer`를 통해 시각화하는 어댑터입니다.

---

## Repository Structure

포트폴리오에서는 핵심 시스템을 이해하는 데 필요한 코드만 다음과 같이 선별하여 공개합니다.

```text
Level-Based-Pathfinding/
├── README.md
│
└── Scripts/
    ├── Pathfinding/
    │   ├── CostCalculator.cs
    │   ├── InGamePathfinder.cs
    │   └── MapGraphData.cs
    │
    ├── Player/
    │   └── PlayerState.cs
    │
    └── Runtime/
        ├── NavigationController.cs
        └── PathVisualizer.cs
```
