using UnityEngine;
using Fusion;
using System.Collections.Generic;

public class GridPlaceable : NetworkBehaviour
{
    [Header("그리드 건축")]
    [SerializeField] protected List<AdditionalCellInfo> _additionalInfo; // 현재 위치 외에 추가적으로 이 구조물이 차지할 범위
}
