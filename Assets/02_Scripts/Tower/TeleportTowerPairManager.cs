using UnityEngine;

public class TeleportTowerPairManager : MonoBehaviour
{
    public static TeleportTowerPairManager Instance { get; private set; }

    [SerializeField] private TeleportTower _t1;
    [SerializeField] private TeleportTower _t2;

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// 텔레포트 타워를 연결하기 위해 저장
    /// </summary>
    /// <param name="t">저장할 텔레포트 타워</param>
    public void AddTeleportTower(TeleportTower t)
    {
        if (_t1 == null) _t1 = t;
        else if (_t2 == null) _t2 = t;

        if (_t1 != null && _t2 != null)
        {
            _t1.OtherTeleportTower = _t2;
            _t2.OtherTeleportTower = _t1;
        }
    }

    /// <summary>
    /// 텔레포트 타워를 삭제하는 함수
    /// </summary>
    /// <param name="t">삭제할 텔레포트 타워</param>
    public void RemoveTeleportTower(TeleportTower t)
    {
        if      (_t1 == t) _t1 = null;
        else if (_t2 == t) _t2 = null;

        if      (_t1 != null) _t1.OtherTeleportTower = null;
        else if (_t2 != null) _t2.OtherTeleportTower = null;
    }
}