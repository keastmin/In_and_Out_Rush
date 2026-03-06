#if UNITY_EDITOR
using UnityEditor;
#endif
using Dev;
using UnityEngine;

public class TrackMonster : Monster
{
    protected Track track;
    protected int currentPointIndex;
    private int _priority; // 스폰 우선순위

    public int Priority => _priority; // 스폰 우선순위

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

    // 타워가 타겟으로 설정할 우선순위: 낮을수록 우선 타겟팅
    public void SetTrackMonsterPriority(int priority)
    {
        _priority = priority;
    }

    public override void UpdateMonster() => FollowTrack();

    protected virtual void FollowTrack()
    {
        if (track.Vertices == null || track.Vertices.Length == 0) { return; }

        Vector3 target = track.Vertices[currentPointIndex];
        Vector3 moveDir = target - transform.position;
        moveDir.y = 0; // y축 고정(필요시)
        float distance = moveDir.magnitude;

        if (distance < arrivalThreshold)
        {
            currentPointIndex = (currentPointIndex + 1) % track.Vertices.Length;
            return;
        }

        Vector3 move = movementSpeed * Time.deltaTime * moveDir.normalized;
        if (move.magnitude > distance) { move = moveDir; } // 목표점 초과 방지
        transform.position += move;
        transform.LookAt(target);
    }
}