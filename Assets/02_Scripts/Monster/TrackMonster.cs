#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using Dev;
using Dev.Network;
using UnityEngine;

public class TrackMonster : Monster
{
    [SerializeField] private float _completionDamage = 10f;

    protected Track track;
    protected int currentPointIndex;
    private int _priority;
    private bool _isInternalized;

    public int Priority => _priority;
    public event Action<TrackMonster> OnDestroyed;

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Handles.color = Color.white;
        Handles.Label(transform.position + Vector3.up * 0.5f, name);
    }
#endif

    public void SetTrack(Track track) => this.track = track;

    public override void Initialize()
    {
        currentPointIndex = 0;
    }

    public override void ApplyStatMultiplier(float multiplier)
    {
        base.ApplyStatMultiplier(multiplier);
        if (!Object.HasStateAuthority)
            return;

        _completionDamage *= multiplier;
    }

    public void SetTrackMonsterPriority(int priority)
    {
        _priority = priority;
    }

    public void SetInternalized(bool isInternalized)
    {
        _isInternalized = isInternalized;
    }

    public override void UpdateMonster() => FollowTrack();

    protected virtual void FollowTrack()
    {
        if (track == null || track.Vertices == null || track.Vertices.Length == 0)
            return;

        Vector3 target = track.Vertices[currentPointIndex];
        Vector3 moveDir = target - transform.position;
        moveDir.y = 0f;
        float distance = moveDir.magnitude;

        if (distance < arrivalThreshold)
        {
            if (currentPointIndex == track.Vertices.Length - 1)
            {
                CompleteLap();
                return;
            }

            currentPointIndex = (currentPointIndex + 1) % track.Vertices.Length;
            return;
        }

        Vector3 move = movementSpeed * Time.deltaTime * moveDir.normalized;
        if (move.magnitude > distance)
            move = moveDir;

        transform.position += move;
        transform.LookAt(target);
    }

    private void CompleteLap()
    {
        if (!Object.HasStateAuthority)
            return;

        var runner = StageBootstrapper.Instance != null ? StageBootstrapper.Instance.PlayerRunner : null;
        if (!_isInternalized && runner != null)
        {
            runner.TakeDamage(_completionDamage);
            Debug.Log($"{name} completed a lap and dealt {_completionDamage} damage to PlayerRunner.");
        }

        DestroyMonster();
    }

    public override void DestroyMonster()
    {
        OnDestroyed?.Invoke(this);
        base.DestroyMonster();
    }
}
