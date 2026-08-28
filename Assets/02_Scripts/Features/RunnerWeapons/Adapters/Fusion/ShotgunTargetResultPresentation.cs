using Fusion;
using UnityEngine;

public readonly struct ShotgunTargetResultPresentation
{
    public ShotgunTargetResultPresentation(
        NetworkId targetId,
        Vector3 targetPosition,
        bool hit,
        float appliedDamage,
        bool knockbackApplied,
        int shotSequence)
    {
        TargetId = targetId;
        TargetPosition = targetPosition;
        Hit = hit;
        AppliedDamage = appliedDamage;
        KnockbackApplied = knockbackApplied;
        ShotSequence = shotSequence;
    }

    public NetworkId TargetId { get; }
    public Vector3 TargetPosition { get; }
    public bool Hit { get; }
    public float AppliedDamage { get; }
    public bool KnockbackApplied { get; }
    public int ShotSequence { get; }
}
