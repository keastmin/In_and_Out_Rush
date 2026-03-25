using UnityEngine;

public struct BuffData
{
    public static BuffData Empty => new BuffData(0, BuffType.None);
    public int BuffRange;
    public BuffType Type;
    public BuffData(int range, BuffType type)
    {
        BuffRange = range;
        Type = type;
    }
}
