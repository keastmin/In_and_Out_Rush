using Fusion;
using UnityEngine;

namespace KIM.Dev
{
    public sealed class TowerTargeting
    {
        /// <summary>
        /// 트랙 몬스터를 감지하여 우선 스폰된 몬스터의 콜라이더를 반환하는 함수
        /// </summary>
        /// <param name="position">감지할 위치</param>
        /// <param name="layer">감지할 레이어</param>
        /// <param name="range">감지할 범위</param>
        /// <param name="netObj">타겟의 네트워크 오브젝트</param>
        /// <returns>몬스터의 콜라이더</returns>
        public Collider SetTarget(Vector3 position, LayerMask layer, float range, out NetworkObject netObj)
        {
            Collider target = null;
            netObj = null;

            // 몬스터 감지
            Collider[] monsterCollider = Physics.OverlapSphere(
                position,
                range,
                layer);

            // 트랙몬스터이고 우선순위가 더 낮으면 우선 타겟팅
            int minPriority = int.MaxValue;
            foreach (var mc in monsterCollider)
            {
                if (mc.TryGetComponent(out TrackMonster tm))
                {
                    if (tm.Priority < minPriority)
                    {
                        minPriority = tm.Priority;
                        target = mc;
                    }
                }
            }

            // 타겟의 네트워크 오브젝트를 반환
            if (target != null) target.TryGetComponent(out netObj);

            return target;
        }

        /// <summary>
        /// 타워를 타겟을 향해 회전시키는 함수
        /// </summary>
        /// <param name="towerTransform">타워의 Transform</param>
        /// <param name="targetCollider">타겟의 Collider</param>
        /// <param name="rotationSpeed">타겟을 바라보는 속도</param>
        /// <param name="runner">네트워크 러너</param>
        public void TowerRotation(Transform towerTransform, Collider targetCollider, float rotationSpeed, NetworkRunner runner)
        {
            // Y축만 회전
            Vector3 dir = targetCollider.transform.position - towerTransform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
            {
                var look = Quaternion.LookRotation(dir);
                towerTransform.rotation = Quaternion.Lerp(
                    towerTransform.rotation, look, rotationSpeed * runner.DeltaTime);
            }
        }
    } 
}