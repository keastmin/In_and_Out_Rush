using Fusion;
using System.Collections.Generic;
using UnityEngine;

namespace KIM.Dev
{
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
}