using Fusion;
using System.Collections.Generic;
using UnityEngine;

public sealed class AmplificationTower : SupportTower, ICanDragObject
{
    private HashSet<IBuffReceiver> _recievers;

    protected override void TowerFixedUpdateNetwork()
    {
        if (HasStateAuthority)
        {
            // IBuffRecieve 객체들 감지

        }
    }

    #region ICanDragObject 구현부
    public void OnDragSelectedThisObject()
    {
        OnLeftMouseDownThisObject();
    }

    public void OnDragOverThisObject()
    {
        OnCancelClickThisObject();
    }

    public void OnDragCompleteThisObject()
    {
        var manager = StageManager.Instance;
        if (manager != null)
        {
            // 빌더의 타워 선택을 함수를 호출하여 자신을 선택된 타워로 넘겨줌
            manager.PlayerBuilder.TowerSelected(this);
        }
    }
    #endregion
}
