using Fusion;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

public class PlayerBuilderTowerSystem : NetworkBehaviour
{
    public TeleportTower[] _teleportTowerArray;

    public int TeleportTowerCount
    {
        get
        {
            int teleportTowerCount = 0;
            for(int i = 0; i < _teleportTowerArray.Length; i++)
            {
                if (_teleportTowerArray[i] != null)
                {
                    teleportTowerCount++;
                }
            }
            return teleportTowerCount;
        }
    }

    private void Awake()
    {
        _teleportTowerArray = new TeleportTower[2];
    }

    public void AddTeleportTowerArray(TeleportTower teleportTower)
    {
        for(int i = 0; i < _teleportTowerArray.Length; i++)
        {
            if (_teleportTowerArray[i] == null)
            {
                _teleportTowerArray[i] = teleportTower;
                break;
            }
        }

        TeleportTowerRefereceSetting();
    }

    public void RemoveTeleportTowerArray(TeleportTower teleportTower)
    {
        if (_teleportTowerArray[0] == teleportTower)
        {
            _teleportTowerArray[0] = null;
            if (_teleportTowerArray[1] != null)
            {
                _teleportTowerArray[1].RemoveOherTowerRefence();
            }
        }

        if (_teleportTowerArray[1] == teleportTower)
        {
            _teleportTowerArray[1] = null;
            if (_teleportTowerArray[0] != null)
            {
                _teleportTowerArray[0].RemoveOherTowerRefence();
            }
        }
    }

    private void TeleportTowerRefereceSetting()
    {
        int notNullCount = 0;
        for (int i = 0; i < _teleportTowerArray.Length; i++)
        {
            if (_teleportTowerArray[i] != null)
                notNullCount++;
        }

        if (notNullCount == 2)
        {
            _teleportTowerArray[0].InjectionOtherTeleportReference(_teleportTowerArray[1]);
            _teleportTowerArray[1].InjectionOtherTeleportReference(_teleportTowerArray[0]);
        }
    }
}
