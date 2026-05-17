using UnityEngine;

public class TeleportTowerPairManager : MonoBehaviour
{
    public const int MaxTeleportTowerCount = 2;

    public static TeleportTowerPairManager Instance { get; private set; }

    [SerializeField] private TeleportTower _t1;
    [SerializeField] private TeleportTower _t2;

    public int RegisteredTowerCount
    {
        get
        {
            int count = 0;
            if (_t1 != null) count++;
            if (_t2 != null) count++;
            return count;
        }
    }

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// 텔레포트 타워를 연결하기 위해 저장
    /// </summary>
    /// <param name="t">저장할 텔레포트 타워</param>
    public bool AddTeleportTower(TeleportTower t)
    {
        if (t == null)
            return false;

        if (_t1 == t || _t2 == t)
        {
            RefreshPair();
            return true;
        }

        if (RegisteredTowerCount >= MaxTeleportTowerCount)
            return false;

        if (_t1 == null) _t1 = t;
        else if (_t2 == null) _t2 = t;

        RefreshPair();
        return true;
    }

    /// <summary>
    /// 텔레포트 타워를 삭제하는 함수
    /// </summary>
    /// <param name="t">삭제할 텔레포트 타워</param>
    public void RemoveTeleportTower(TeleportTower t)
    {
        if      (_t1 == t) _t1 = null;
        else if (_t2 == t) _t2 = null;

        RefreshPair();
    }

    private void RefreshPair()
    {
        if (_t1 != null) _t1.OtherTeleportTower = null;
        if (_t2 != null) _t2.OtherTeleportTower = null;

        if (_t1 == null || _t2 == null)
            return;

        _t1.OtherTeleportTower = _t2;
        _t2.OtherTeleportTower = _t1;
    }
}
