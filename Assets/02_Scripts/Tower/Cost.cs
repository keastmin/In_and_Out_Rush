using Fusion;
using System;

[Serializable]
public struct Cost : INetworkStruct
{
    public int Mineral;
    public int Gas;

    public Cost(int mineral, int gas)
    {
        Mineral = mineral;
        Gas = gas;
    }
}
