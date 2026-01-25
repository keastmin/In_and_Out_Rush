using UnityEngine;

public class RangeDetector<T>
{
    private readonly Collider[] _colliders;
    private readonly T[] _results;

    // (선택) 중복 제거용: 같은 대상이 콜라이더 여러 개면 중복으로 들어오는 걸 방지
    private readonly int[] _ids;

    public int Count { get; private set; }
    public T this[int index] => _results[index];

    /// <param name="colliderBufferSize">OverlapSphereNonAlloc이 채울 Collider 버퍼 크기</param>
    /// <param name="resultBufferSize">최대 반환할 인터페이스 개수(결과 버퍼)</param>
    public RangeDetector(int colliderBufferSize = 64, int resultBufferSize = 32)
    {
        _colliders = new Collider[colliderBufferSize];
        _results = new T[resultBufferSize];
        _ids = new int[resultBufferSize];
    }

    /// <summary>
    /// center/radius 범위 안에서 T(인터페이스)를 구현한 컴포넌트를 NonAlloc으로 찾아 결과 버퍼에 채움.
    /// 반환값 = 결과 개수(Count)
    /// </summary>
    public int Query(
        Vector3 center,
        float radius,
        int layerMask,
        QueryTriggerInteraction queryTriggers = QueryTriggerInteraction.Collide,
        bool searchInParent = true,
        bool deduplicateByInstanceId = true)
    {
        Count = 0;

        int hitCount = Physics.OverlapSphereNonAlloc(
            center, radius, _colliders, layerMask, queryTriggers);

        for (int i = 0; i < hitCount; i++)
        {
            var col = _colliders[i];
            if (!col) continue;

            // 콜라이더가 자식에 달려있고 실제 컴포넌트는 부모에 있을 수 있으니 InParent 옵션 제공
            T target = searchInParent ? col.GetComponentInParent<T>() : col.GetComponent<T>();
            if (target == null) continue;

            if (Count >= _results.Length) break; // 결과 버퍼 넘치면 중단

            if (deduplicateByInstanceId && target is Object uobj) // UnityEngine.Object만 dedupe 가능
            {
                int id = uobj.GetInstanceID();
                bool exists = false;
                for (int j = 0; j < Count; j++)
                {
                    if (_ids[j] == id) { exists = true; break; }
                }
                if (exists) continue;

                _ids[Count] = id;
            }

            _results[Count] = target;
            Count++;
        }

        return Count;
    }

    // 필요하면 버퍼 직접 접근도 가능(Count까지만 유효)
    public T[] ResultsBuffer => _results;
}
