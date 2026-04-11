using Fusion;
using System.Collections.Generic;
using UnityEngine;

public struct CellData : INetworkStruct
{
    public int ActiveRange;
    public BuffData BuffData;
    public CellData(int range, BuffData buffData)
    {
        ActiveRange = range;
        BuffData = buffData;
    }
}
