using Fusion;
using System;
using UnityEngine;

public class Laboratory : NetworkBehaviour, ICanClickObject
{
    public event Action<bool> OnClickLaboratoryObjectAction;

    #region IInteractableObject

    public void OnLeftMouseDownThisObject()
    {

    }

    // 빌더의 연구소를 통한 강화 UI 띄우기
    public void OnLeftMouseUpThisObject()
    {
        OnClickLaboratoryObjectAction?.Invoke(true);
    }

    public void OnCancelClickThisObject()
    {

    }

    #endregion
}
