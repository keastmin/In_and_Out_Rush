using System.Collections.Generic;
using UnityEngine;

namespace KIM.Dev
{
    public static class DraggingCollector
    {
        /// <summary>
        /// 드래그 영역 내의 콜라이더 수집
        /// </summary>
        /// <param name="dragObjects">드래그를 통해 최종적으로 선별된 드래그 가능 오브젝트들의 해쉬</param>
        /// <param name="frameDetect">이번 프레임에 감지된 드래그 가능 오브젝트들의 해쉬</param>
        /// <param name="toRemove">드래그 영역에 들었었지만 이번 프레임에 드래그 영역 밖에 있어 지워질 드래그 가능 오브젝트들의 리스트</param>
        /// <param name="dragColliders">드래그 영역 내에 들어갈 오브젝트 후보들을 담아둘 Collider 배열</param>
        /// <param name="layer">드래그 영역 안에 있을 때 감지할 Layer</param>
        /// <param name="cam">플레이어가 보는 시점의 카메라</param>
        /// <param name="minX">드래그 영역의 min X</param>
        /// <param name="minY">드래그 영역의 min Y</param>
        /// <param name="maxX">드래그 영역의 max X</param>
        /// <param name="maxY">드래그 영역의 max Y</param>
        public static void FrustumDraggingCollectCollider(HashSet<ICanDragObject> dragObjects, HashSet<ICanDragObject> frameDetect, List<ICanDragObject> toRemove,
            Collider[] dragColliders, LayerMask layer,
            Camera cam, float minX, float minY, float maxX, float maxY)
        {
            // 픽셀 사각형 정리(스왑 + 카메라 뷰포트로 클램프)
            if (minX > maxX) (minX, maxX) = (maxX, minX);
            if (minY > maxY) (minY, maxY) = (maxY, minY);
            Rect vp = cam.pixelRect;
            minX = Mathf.Clamp(minX, vp.xMin, vp.xMax);
            maxX = Mathf.Clamp(maxX, vp.xMin, vp.xMax);
            minY = Mathf.Clamp(minY, vp.yMin, vp.yMax);
            maxY = Mathf.Clamp(maxY, vp.yMin, vp.yMax);
            if (maxX <= minX || maxY <= minY) return; // 화면 밖이면 종료

            // 드래그 사각형 4모서리를 near/far로 언프로젝션(월드에 8점)
            float nearZ = Mathf.Max(cam.nearClipPlane, 0.03f);
            float farZ = 30f; // 감지할 거리 조절 가능

            Vector3 Near(float x, float y) => cam.ScreenToWorldPoint(new Vector3(x, y, nearZ));
            Vector3 Far(float x, float y) => cam.ScreenToWorldPoint(new Vector3(x, y, farZ));

            // 인덱스 규칙: 0=BL, 1=TL, 2=TR, 3=BR (Camera.main.CalculateFrustumCorners와 동일 규칙로 맞춤)
            Vector3[] n = new Vector3[4];
            Vector3[] f = new Vector3[4];
            n[0] = Near(minX, minY); n[1] = Near(minX, maxY); n[2] = Near(maxX, maxY); n[3] = Near(maxX, minY);
            f[0] = Far(minX, minY); f[1] = Far(minX, maxY); f[2] = Far(maxX, maxY); f[3] = Far(maxX, minY);

            // 8점 AABB (브로드페이즈 후보 수집용)
            Bounds aabb = new Bounds(n[0], Vector3.zero);
            for (int i = 1; i < 4; i++) aabb.Encapsulate(n[i]);
            for (int i = 0; i < 4; i++) aabb.Encapsulate(f[i]);

            // 수동 평면 구성(프러스텀 안쪽이 항상 +측이 되도록 정렬)
            Vector3 centerNear = (n[0] + n[1] + n[2] + n[3]) * 0.25f;
            Vector3 centerFar = (f[0] + f[1] + f[2] + f[3]) * 0.25f;
            Vector3 insidePoint = (centerNear + centerFar) * 0.5f;

            Plane MakeOriented(Vector3 a, Vector3 b, Vector3 c)
            {
                var p = new Plane(a, b, c); // 노멀 = (b - a) x (c - a)
                if (!p.GetSide(insidePoint)) p.Flip(); // insidePoint가 -측이면 뒤집어 +측으로
                return p;
            }

            Plane[] planes = new Plane[]
            {
            // Near / Far (TL->TR->BR / BR->TR->TL)
            MakeOriented(n[1], n[2], n[3]),
            MakeOriented(f[3], f[2], f[1]),

            // Left / Right / Top / Bottom
            MakeOriented(n[0], n[1], f[1]), // BL(n)->TL(n)->TL(f)
            MakeOriented(n[3], n[2], f[2]), // BR(n)->TR(n)->TR(f)
            MakeOriented(n[1], n[2], f[2]), // TL(n)->TR(n)->TR(f)
            MakeOriented(n[3], n[0], f[0]), // BR(n)->BL(n)->BL(f)
            };

            // 후보 수집
            int candidateCount = Physics.OverlapBoxNonAlloc(aabb.center, aabb.extents, dragColliders, Quaternion.identity, layer, QueryTriggerInteraction.Collide);

            // 정밀 필터: 프러스텀 평면으로 통과만 in-place 압축
            int dragObjectCount = 0;
            for (int i = 0; i < candidateCount; i++)
            {
                var col = dragColliders[i];
                if (!col) continue;
                if (GeometryUtility.TestPlanesAABB(planes, col.bounds))
                    dragColliders[dragObjectCount++] = col;
            }
            for (int i = dragObjectCount; i < dragColliders.Length; i++) dragColliders[i] = null;

            // 현재 프레임에 감지된 드래그 가능한 오브젝트 집합 만들기
            frameDetect.Clear();
            for (int i = 0; i < dragObjectCount; i++)
            {
                if (dragColliders[i].TryGetComponent(out ICanDragObject d))
                    frameDetect.Add(d);
            }

            // 새로 들어온 것들 추가 + 콜백
            foreach (var d in frameDetect)
            {
                if (dragObjects.Add(d)) // 새로 추가된 경우에만
                    d.OnDragSelectedThisObject();
            }

            // 나간 것들 모아서 제거 + 콜백
            toRemove.Clear();
            foreach (var d in dragObjects)
            {
                if (!frameDetect.Contains(d))
                    toRemove.Add(d);
            }
            foreach (var d in toRemove)
            {
                d.OnDragOverThisObject();
                dragObjects.Remove(d);
            }

            // 시각 디버그: 프러스텀 와이어 + AABB
            float dur = 0f; bool depthTest = true;
            void L(Vector3 a, Vector3 b, Color c) => Debug.DrawLine(a, b, c, dur, depthTest);

            // near/far 사각형
            L(n[0], n[1], Color.cyan); L(n[1], n[2], Color.cyan);
            L(n[2], n[3], Color.cyan); L(n[3], n[0], Color.cyan);
            L(f[0], f[1], Color.blue); L(f[1], f[2], Color.blue);
            L(f[2], f[3], Color.blue); L(f[3], f[0], Color.blue);
            // 엣지
            L(n[0], f[0], Color.green); L(n[1], f[1], Color.green);
            L(n[2], f[2], Color.green); L(n[3], f[3], Color.green);
            // AABB
            Vector3 c0 = aabb.center, e = aabb.extents;
            Vector3 V(float sx, float sy, float sz) => c0 + new Vector3(e.x * sx, e.y * sy, e.z * sz);
            var v000 = V(-1, -1, -1); var v100 = V(1, -1, -1); var v110 = V(1, 1, -1); var v010 = V(-1, 1, -1);
            var v001 = V(-1, -1, 1); var v101 = V(1, -1, 1); var v111 = V(1, 1, 1); var v011 = V(-1, 1, 1);
            L(v000, v100, Color.yellow); L(v100, v110, Color.yellow); L(v110, v010, Color.yellow); L(v010, v000, Color.yellow);
            L(v001, v101, Color.yellow); L(v101, v111, Color.yellow); L(v111, v011, Color.yellow); L(v011, v001, Color.yellow);
            L(v000, v001, Color.yellow); L(v100, v101, Color.yellow); L(v110, v111, Color.yellow); L(v010, v011, Color.yellow);
        }
    }
}