using System.Collections.Generic;
using UnityEngine;

public readonly struct ShotgunShotPresentation
{
    public ShotgunShotPresentation(
        int sequence,
        int seed,
        Vector3 centerDirection,
        Vector3[] pelletDirections)
    {
        Sequence = sequence;
        Seed = seed;
        CenterDirection = centerDirection;
        PelletDirections = pelletDirections;
    }

    public int Sequence { get; }
    public int Seed { get; }
    public Vector3 CenterDirection { get; }
    public IReadOnlyList<Vector3> PelletDirections { get; }
}
