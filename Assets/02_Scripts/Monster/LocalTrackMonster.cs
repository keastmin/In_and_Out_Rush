#if UNITY_EDITOR
using UnityEditor;
#endif
using Dev;
using UnityEngine;

public class LocalTrackMonster : LocalMonster
{
    protected Track track;
    protected int currentPointIndex;

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

    public override void UpdateMonster() => FollowTrack();

    protected virtual void FollowTrack()
    {
        if (track == null || track.Vertices2d == null || track.Vertices2d.Count == 0) { return; }
        var targetVertex = track.Vertices2d[currentPointIndex];
        var target = new Vector3(targetVertex.x, 0, targetVertex.y);
        Vector3 moveDir = target - transform.position;
        moveDir.y = 0; // y축 고정(필요시)
        float distance = moveDir.magnitude;

        if (distance < arrivalThreshold)
        {
            currentPointIndex = (currentPointIndex + 1) % track.Vertices2d.Count;
            return;
        }

        Vector3 move = movementSpeed * Time.deltaTime * moveDir.normalized;
        if (move.magnitude > distance) { move = moveDir; } // 목표점 초과 방지
        transform.position += move;
        transform.LookAt(target);
    }
}