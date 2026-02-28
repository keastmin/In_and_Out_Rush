using UnityEngine;

public class TrackView : MonoBehaviour
{
    [SerializeField] Transform vertexParentTransform;
    [SerializeField] LineRenderer lineRenderer;

    GameObject[] vertexObjects;

    public void GenerateTrackVertices(Vector3[] vertices)
    {
        if (vertexObjects != null)
        {
            foreach (var vertexObject in vertexObjects)
            {
                Destroy(vertexObject);
            }
        }

        var vertexCount = vertices.Length;

        vertexObjects = new GameObject[vertexCount];

        for (int i = 0; i < vertexCount; i++)
        {
            var vertex = vertices[i];

            var vertexObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            vertexObject.name = $"Vertex {i}";
            vertexObject.transform.position = vertex;
            vertexObject.transform.localScale = Vector3.one * 0.1f;
            vertexObject.transform.SetParent(vertexParentTransform, false);

            vertexObjects[i] = vertexObject;
        }
    }

    public void GenerateTrackLine(Vector3[] vertices)
    {
        lineRenderer.positionCount = vertices.Length;
        lineRenderer.SetPositions(vertices);
        lineRenderer.loop = true; // 선을 닫아 원형 트랙을 만듭니다.
    }
}