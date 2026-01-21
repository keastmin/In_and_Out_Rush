using KDM;
using System.Collections.Generic;
using UnityEngine;

public class SupplyTower<T> : SupportTower, IRunnerInteractableTower
{
    private List<T> _obtainableList;

    /// <summary>
    /// 보급 물자 채워넣기
    /// </summary>
    /// <param name="obtainables"></param>
    public void FillObtainableList(List<T> obtainables)
    {
        _obtainableList = obtainables;
    }

    /// <summary>
    /// 러너에게 보급물자를 전달하는 인터페이스 함수
    /// </summary>
    /// <param name="runner">전달 받을 플레이어 러너</param>
    public void Interact(PlayerRunner runner)
    {
        foreach(T obtainable in _obtainableList)
        {
            // runner.Supply(obtainable);
        }
    }

    private void DestroySupplyTower()
    {

    }
}
