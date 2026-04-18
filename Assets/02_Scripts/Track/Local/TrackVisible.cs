using UnityEngine;

namespace Dev.Local
{
    public class TrackVisible : Visible
    {
        [SerializeField] private Transform _vertexContainer;
        [SerializeField] private LineRenderer _lineRenderer;

        private GameObject[] _vertexObjects;

        public float LineWidth => _lineRenderer != null ? _lineRenderer.widthMultiplier : 0f;

        public void GenerateTrackVertices(Vector3[] vertices)
        {
            if (_vertexObjects != null)
                foreach (var vertexObject in _vertexObjects)
                    Destroy(vertexObject);

            var vertexCount = vertices.Length;

            _vertexObjects = new GameObject[vertexCount];

            for (int i = 0; i < vertexCount; i++)
            {
                var vertex = vertices[i];

                var vertexObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                vertexObject.name = $"Vertex {i}";
                vertexObject.transform.position = vertex;
                vertexObject.transform.localScale = Vector3.one * 0.1f;
                vertexObject.transform.SetParent(_vertexContainer, false);

                _vertexObjects[i] = vertexObject;
            }
        }

        public void GenerateTrackLine(Vector3[] vertices)
        {
            _lineRenderer.positionCount = vertices.Length;
            _lineRenderer.SetPositions(vertices);
            _lineRenderer.loop = true; // 선을 닫아 원형 트랙을 만듭니다.
        }
    }
}