using System;

[Flags]
public enum BuffTargetType
{
    None = 0,
    Runner = 1 << 0,
    Tower = 1 << 1,
}

