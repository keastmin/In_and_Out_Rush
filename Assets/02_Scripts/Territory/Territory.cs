using System.Collections.Generic;
using UnityEngine;

public class Territory
{
    public List<Vector2> Vertices = new();

    struct Intersection
    {
        public Vector2 point;      // 교차 좌표
        public int polyEdgeIndex;  // 폴리곤 엣지 인덱스
        public int pathIndex;      // 경로 세그먼트 인덱스
        public bool isExit;        // true = 나간 지점, false = 들어온 지점
    }
    readonly List<Intersection> intersections = new();

    public void Expand(List<Vector2> path)
    {
        ExpandInternal(path);
    }

    void ExpandInternal(List<Vector2> path)
    {
        // 1. 교차점 찾기
        FindIntersections(path);

        if (intersections.Count < 2)
            return; // 교차점 충분치 않으면 종료

        // 2. 교차점 정렬 (폴리곤 둘레 순서, 경로 순서)
        SortIntersections();

        // 3. 폴리곤을 두 세그먼트로 분할
        var (seg1, seg2) = SplitPolygonSegments();

        // 4. 후보 영역 생성 & 테스트
        List<Vector2> candidate1 = BuildCandidate(seg1, intersections.GetRange(0, 2), path);
        if (IsValidCandidate(candidate1, seg2))
        {
            ApplyNewPolygon(candidate1);
            return;
        }

        List<Vector2> candidate2 = BuildCandidate(seg2, new List<Intersection> { intersections[1], intersections[0] }, path);
        ApplyNewPolygon(candidate2);
    }

    void FindIntersections(List<Vector2> playerPath)
    {
        intersections.Clear();
        int polyCount = Vertices.Count;
        int pathCount = playerPath.Count;
        for (int i = 0; i < polyCount; i++)
        {
            Vector2 a1 = Vertices[i];
            Vector2 a2 = Vertices[(i + 1) % polyCount];
            for (int j = 0; j < pathCount - 1; j++)
            {
                Vector2 b1 = playerPath[j];
                Vector2 b2 = playerPath[j + 1];
                if (LineSegmentsIntersect(a1, a2, b1, b2, out Vector2 intersection))
                {
                    // b1: 이전 경로점, b2: 다음 경로점
                    // b1이 폴리곤 내부면 나가는 교차(isExit=true), 외부면 들어오는 교차(isExit=false)
                    bool b1Inside = IsPointInPolygon(b1);
                    bool b2Inside = IsPointInPolygon(b2);
                    bool isExit = b1Inside && !b2Inside;
                    bool isEnter = !b1Inside && b2Inside;
                    // 들어오거나 나가는 경우만 추가
                    if (isExit || isEnter)
                    {
                        intersections.Add(new Intersection
                        {
                            point = intersection,
                            polyEdgeIndex = i,
                            pathIndex = j,
                            isExit = isExit
                        });

                        // Debug.Log($"교차점 추가: {intersection}, 폴리곤 엣지: {i}, 경로 인덱스: {j}, 나가는 곳?: {isExit}");
                    }
                }
            }
        }
    }

    // 두 선분 (p1,p2), (q1,q2)가 교차하면 true, 교차점은 intersection에 반환
    bool LineSegmentsIntersect(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2, out Vector2 intersection)
    {
        intersection = Vector2.zero;
        float s1_x = p2.x - p1.x;
        float s1_y = p2.y - p1.y;
        float s2_x = q2.x - q1.x;
        float s2_y = q2.y - q1.y;

        float denom = (-s2_x * s1_y + s1_x * s2_y);
        if (Mathf.Abs(denom) < Mathf.Epsilon)
            return false; // 평행

        float s = (-s1_y * (p1.x - q1.x) + s1_x * (p1.y - q1.y)) / denom;
        float t = (s2_x * (p1.y - q1.y) - s2_y * (p1.x - q1.x)) / denom;

        if (s > 0 && s <= 1 && t > 0 && t <= 1)
        {
            // 교차점 계산
            intersection = new Vector2(
                p1.x + (t * s1_x),
                p1.y + (t * s1_y)
            );
            return true;
        }
        return false;
    }

    void SortIntersections()
    {
        // polyEdgeIndex(폴리곤 둘레 순서) -> pathIndex(경로 순서) 기준 정렬
        intersections.Sort((a, b) =>
        {
            int cmp = a.polyEdgeIndex.CompareTo(b.polyEdgeIndex);
            if (cmp != 0) return cmp;
            var ad = Vector2.SqrMagnitude(Vertices[a.polyEdgeIndex] - a.point);
            var bd = Vector2.SqrMagnitude(Vertices[b.polyEdgeIndex] - b.point);
            return ad.CompareTo(bd);
        });
    }

    (List<Vector2> seg1, List<Vector2> seg2) SplitPolygonSegments()
    {
        // intersections[0], intersections[1] 기준 분할
        if (intersections.Count < 2)
            return (new List<Vector2>(), new List<Vector2>());

        var i0 = intersections[0];
        var i1 = intersections[1];

        // polygonPoints 복사본 생성
        List<Vector2> poly = new List<Vector2>(Vertices);

        // 교차점 삽입: 인덱스가 큰 것부터 삽입해야 인덱스가 밀리지 않음
        int insertIdx0 = i0.polyEdgeIndex + 1;
        int insertIdx1 = i1.polyEdgeIndex + 1;
        if (insertIdx0 > insertIdx1)
        {
            // 먼저 insertIdx0에 삽입, 그 다음 insertIdx1+1에 삽입
            if (poly[insertIdx0 % poly.Count] != i0.point) poly.Insert(insertIdx0, i0.point);
            if (poly[insertIdx1 % poly.Count] != i1.point) poly.Insert(insertIdx1, i1.point);
        }
        else
        {
            // 먼저 insertIdx1에 삽입, 그 다음 insertIdx0에 삽입
            if (poly[insertIdx1 % poly.Count] != i1.point) poly.Insert(insertIdx1, i1.point);
            if (poly[insertIdx0 % poly.Count] != i0.point) poly.Insert(insertIdx0, i0.point);
        }

        // foreach (var p in poly)
        // {
        //     Debug.Log($"Polygon Point: {p}");
        // }

        // 두 교차점 인덱스 재계산
        int idx0 = poly.IndexOf(i0.point);
        int idx1 = poly.IndexOf(i1.point);

        // Debug.Log($"교차점 인덱스: idx0={idx0}, idx1={idx1}");

        // 시계방향 분할 (idx0 -> idx1), 반대방향 분할 (idx1 -> idx0)
        List<Vector2> seg1 = new List<Vector2>();
        int n = poly.Count;
        for (int i = idx0; i != idx1; i = (i + 1) % n)
            seg1.Add(poly[i]);
        seg1.Add(poly[idx1]);

        List<Vector2> seg2 = new List<Vector2>();
        for (int i = idx1; i != idx0; i = (i + 1) % n)
            seg2.Add(poly[i]);
        seg2.Add(poly[idx0]);

        return (seg1, seg2);
    }

    List<Vector2> BuildCandidate(List<Vector2> segment, List<Intersection> segmentInters, List<Vector2> playerPath)
    {
        // segmentInters: 이 segment 위에 속한 두 개의 Intersection (정렬된 상태)
        var cand = new List<Vector2>(segment);

        if (segmentInters[1].isExit)
        {
            var ai = segmentInters[0].pathIndex;
            var bi = segmentInters[1].pathIndex;
            for (int i = bi + 1; i <= ai; i++)
            {
                // Exit인 경우, 경로 정방향으로 이어붙임
                cand.Add(playerPath[i]);
            }
        }
        else
        {
            var ai = segmentInters[0].pathIndex;
            var bi = segmentInters[1].pathIndex;
            for (int i = bi; i > ai; i--)
            {
                // Exit인 경우, 경로 역방향으로 이어붙임
                cand.Add(playerPath[i]);
            }
        }

        return cand;
    }

    bool IsValidCandidate(List<Vector2> candidate, List<Vector2> otherSegment)
    {
        // TODO:
        // - otherSegment의 대표 점 하나를 골라서
        //   점-다각형 포함 테스트(PointInPolygon)
        if (otherSegment.Count <= 2) { return true; }
        return PointInPolygon(otherSegment[1], candidate);
    }

    bool PointInPolygon(Vector2 point, List<Vector2> poly)
    {
        // TODO:
        // - Ray-casting 또는 Winding-number 알고리즘으로 구현
        int n = poly.Count;
        bool isInside = false;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            Vector2 pi = poly[i];
            Vector2 pj = poly[j];

            // Raycast: point.y가 에지의 y범위 내에 있고, point.x가 에지와의 교차점보다 작은지
            if ((pi.y >= point.y) != (pj.y >= point.y))
            {
                float atX = (pj.x - pi.x) * (point.y - pi.y) / (pj.y - pi.y + Mathf.Epsilon) + pi.x;
                if (point.x <= atX)
                    isInside = !isInside;
            }
        }
        // 홀수면 내부, 짝수면 외부
        return isInside;
    }

    void ApplyNewPolygon(List<Vector2> newPoly)
    {
        // TODO:
        // - basePolygon 갱신
        // - PolygonCollider2D 또는 MeshCollider 갱신 및 Triangulation

        var mesh = GenerateMesh(newPoly);
        if (mesh == null)
        {
            Debug.LogError("새로운 폴리곤 생성 실패");
            return;
        }

        // meshFilter.mesh = mesh;
        Vertices.Clear();
        Vertices.AddRange(newPoly);
    }

    public static Mesh GenerateMesh(List<Vector2> polygon)
    {
        if (polygon == null || polygon.Count < 3)
        {
            Debug.LogError("점이 3개 이상 필요합니다.");
            return null;
        }

        // 3D로 변환 (z=0)
        Vector3[] vertices = new Vector3[polygon.Count];
        for (int i = 0; i < polygon.Count; i++)
            vertices[i] = new Vector3(polygon[i].x, 0, polygon[i].y);

        // Ear Clipping 삼각분할
        List<int> trianglesList = EarClippingTriangulate(polygon);
        int[] triangles = trianglesList.ToArray();

        var mesh = new Mesh
        {
            name = "PolygonMesh",
            vertices = vertices,
            triangles = triangles
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    // Ear Clipping 삼각분할 알고리즘 (오목 다각형 지원)
    static List<int> EarClippingTriangulate(List<Vector2> poly)
    {
        List<int> indices = new List<int>();
        int n = poly.Count;
        if (n < 3) return indices;

        List<int> V = new List<int>();
        if (Area(poly) > 0)
        {
            for (int v = 0; v < n; v++) V.Add(v);
        }
        else
        {
            for (int v = 0; v < n; v++) V.Add((n - 1) - v);
        }

        int nv = n;
        int count = 2 * nv;
        for (int v = nv - 1; nv > 2;)
        {
            if ((count--) <= 0)
                break; // 비정상 다각형

            int u = v; if (nv <= u) u = 0;
            v = u + 1; if (nv <= v) v = 0;
            int w = v + 1; if (nv <= w) w = 0;

            if (Snip(poly, V, u, v, w, nv))
            {
                int a = V[u], b = V[v], c = V[w];
                indices.Add(c);
                indices.Add(b);
                indices.Add(a);
                V.RemoveAt(v);
                nv--;
                count = 2 * nv;
            }
        }
        return indices;
    }

    // 다각형의 부호(시계/반시계) 및 넓이
    static float Area(List<Vector2> poly)
    {
        int n = poly.Count;
        float A = 0f;
        for (int p = n - 1, q = 0; q < n; p = q++)
        {
            A += poly[p].x * poly[q].y - poly[q].x * poly[p].y;
        }
        return A * 0.5f;
    }

    // u,v,w가 이등분하는 삼각형이 poly 내부에 귀(ear)인지 판정
    static bool Snip(List<Vector2> poly, List<int> V, int u, int v, int w, int nv)
    {
        Vector2 A = poly[V[u]];
        Vector2 B = poly[V[v]];
        Vector2 C = poly[V[w]];
        if (Mathf.Epsilon > (((B.x - A.x) * (C.y - A.y)) - ((B.y - A.y) * (C.x - A.x))))
            return false;
        for (int p = 0; p < nv; p++)
        {
            if (p == u || p == v || p == w) continue;
            Vector2 P = poly[V[p]];
            if (PointInTriangle(P, A, B, C))
                return false;
        }
        return true;
    }

    // 점 P가 삼각형 ABC 내부에 있는지
    static bool PointInTriangle(Vector2 P, Vector2 A, Vector2 B, Vector2 C)
    {
        float ax = C.x - B.x, ay = C.y - B.y;
        float bx = A.x - C.x, by = A.y - C.y;
        float cx = B.x - A.x, cy = B.y - A.y;
        float apx = P.x - A.x, apy = P.y - A.y;
        float bpx = P.x - B.x, bpy = P.y - B.y;
        float cpx = P.x - C.x, cpy = P.y - C.y;
        float aCROSSbp = ax * bpy - ay * bpx;
        float cCROSSap = cx * apy - cy * apx;
        float bCROSScp = bx * cpy - by * cpx;
        return (aCROSSbp >= 0f) && (bCROSScp >= 0f) && (cCROSSap >= 0f);
    }

    public bool IsPointInPolygon(Vector2 point)
    {
        return PointInPolygon(point, Vertices);
    }
}