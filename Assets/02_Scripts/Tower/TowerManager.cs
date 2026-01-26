using Fusion;
using System.Collections.Generic;
using UnityEngine;

public class TowerManager : MonoBehaviour
{
    public static TowerManager Instance { get; private set; }
    
    public Dictionary<string, int> TowerCount; // 타워 ID로 타워의 개수를 반환

    private void Awake()
    {
        Instance = this;
        TowerCount = new Dictionary<string, int>();
    }

    public void AddTowerID(string id)
    {
        if (!TowerCount.ContainsKey(id))
        {
            TowerCount.Add(id, 1);
        }
        else
        {
            TowerCount[id]++;
        }
    }

    public void RemoveTowerID(string id)
    {
        if (TowerCount.ContainsKey(id))
        {
            TowerCount[id]--;
        }
    }

    public int GetTowerCount(string id)
    {
        if (TowerCount.ContainsKey(id))
            return TowerCount[id];
        return 0;
    }
}
