using UnityEngine;
using UnityEngine.Rendering;

namespace Dev.Network
{
    [AddComponentMenu("ProjectIO/Sacred Zone View")]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class SacredZoneView : MonoBehaviour
    {
        private const float DefaultLineWidth = 2f;

        [SerializeField] private MeshFilter fillMeshFilter;
        [SerializeField] private MeshRenderer fillRenderer;
        [SerializeField] private LineRenderer innerLine;
        [SerializeField] private LineRenderer outerLine;

        private Mesh fillMesh;
        private Material fillMaterial;
        private Material lineMaterial;
        private float innerRadius;
        private float outerRadius;

        public void Initialize(float innerRadius, float outerRadius, int segmentCount)
        {
            this.innerRadius = Mathf.Max(0f, Mathf.Min(innerRadius, outerRadius));
            this.outerRadius = Mathf.Max(this.innerRadius + 0.01f, outerRadius);
            int normalizedSegmentCount = Mathf.Max(8, segmentCount);

            if (!EnsureFillRenderer())
                return;

            EnsureLineRenderers();

            fillMesh = BuildRingMesh(this.innerRadius, this.outerRadius, normalizedSegmentCount);
            fillMeshFilter.sharedMesh = fillMesh;

            if (innerLine != null)
                DrawCircle(innerLine, this.innerRadius, normalizedSegmentCount);

            if (outerLine != null)
                DrawCircle(outerLine, this.outerRadius, normalizedSegmentCount);

            UpdateProgress(0f);
        }

        public void UpdateProgress(float progressRatio)
        {
            float normalizedProgress = Mathf.Clamp01(progressRatio);
            Color fillColor = Color.Lerp(
                new Color(0.05f, 0.8f, 1f, 0.08f),
                new Color(0.2f, 1f, 0.85f, 0.22f),
                normalizedProgress);
            Color lineColor = Color.Lerp(
                new Color(0.1f, 0.9f, 1f, 0.55f),
                new Color(0.3f, 1f, 0.7f, 0.95f),
                normalizedProgress);

            SetMaterialColor(fillMaterial, fillColor);

            if (innerLine != null)
            {
                innerLine.startColor = lineColor;
                innerLine.endColor = lineColor;
            }

            if (outerLine != null)
            {
                outerLine.startColor = lineColor;
                outerLine.endColor = lineColor;
            }
        }

        public bool IsPointInSacredZone(Vector3 worldPosition)
        {
            Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
            float sqrDistance = new Vector2(localPosition.x, localPosition.z).sqrMagnitude;
            return sqrDistance >= innerRadius * innerRadius &&
                   sqrDistance <= outerRadius * outerRadius;
        }

        private bool EnsureFillRenderer()
        {
            if (fillMeshFilter == null)
                fillMeshFilter = gameObject.GetComponent<MeshFilter>();

            if (fillRenderer == null)
                fillRenderer = gameObject.GetComponent<MeshRenderer>();

            if (fillMeshFilter == null || fillRenderer == null)
            {
                Debug.LogWarning("SacredZoneView requires MeshFilter and MeshRenderer components on the same GameObject.", this);
                return false;
            }

            if (fillMaterial == null)
                fillMaterial = CreateTransparentMaterial("Sacred Zone Fill");

            fillRenderer.sharedMaterial = fillMaterial;
            return true;
        }

        private void EnsureLineRenderers()
        {
            if (lineMaterial == null)
                lineMaterial = CreateTransparentMaterial("Sacred Zone Line");

            innerLine = ResolveLineRenderer("Inner Border", innerLine);
            outerLine = ResolveLineRenderer("Outer Border", outerLine);
        }

        private LineRenderer ResolveLineRenderer(string objectName, LineRenderer lineRenderer)
        {
            if (lineRenderer == null)
            {
                Transform existing = transform.Find(objectName);
                if (existing != null)
                    lineRenderer = existing.GetComponent<LineRenderer>();
            }

            if (lineRenderer == null)
            {
                Debug.LogWarning($"SacredZoneView requires a child LineRenderer named '{objectName}' or an inspector reference.", this);
                return null;
            }

            lineRenderer.sharedMaterial = lineMaterial;
            lineRenderer.useWorldSpace = false;
            lineRenderer.loop = true;
            lineRenderer.widthMultiplier = DefaultLineWidth;
            lineRenderer.shadowCastingMode = ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
            lineRenderer.numCapVertices = 4;
            return lineRenderer;
        }

        private static Mesh BuildRingMesh(float innerRadius, float outerRadius, int segmentCount)
        {
            var vertices = new Vector3[(segmentCount + 1) * 2];
            var triangles = new int[segmentCount * 12];

            for (int i = 0; i <= segmentCount; i++)
            {
                float angle = Mathf.PI * 2f * i / segmentCount;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);
                int vertexIndex = i * 2;

                vertices[vertexIndex] = new Vector3(cos * outerRadius, 0f, sin * outerRadius);
                vertices[vertexIndex + 1] = new Vector3(cos * innerRadius, 0f, sin * innerRadius);
            }

            int triangleIndex = 0;
            for (int i = 0; i < segmentCount; i++)
            {
                int outer0 = i * 2;
                int inner0 = outer0 + 1;
                int outer1 = outer0 + 2;
                int inner1 = outer0 + 3;

                triangles[triangleIndex++] = outer0;
                triangles[triangleIndex++] = outer1;
                triangles[triangleIndex++] = inner0;
                triangles[triangleIndex++] = inner0;
                triangles[triangleIndex++] = outer1;
                triangles[triangleIndex++] = inner1;

                triangles[triangleIndex++] = inner0;
                triangles[triangleIndex++] = outer1;
                triangles[triangleIndex++] = outer0;
                triangles[triangleIndex++] = inner1;
                triangles[triangleIndex++] = outer1;
                triangles[triangleIndex++] = inner0;
            }

            var mesh = new Mesh
            {
                name = "SacredZoneRing",
                vertices = vertices,
                triangles = triangles
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void DrawCircle(LineRenderer lineRenderer, float radius, int segmentCount)
        {
            lineRenderer.positionCount = segmentCount;
            for (int i = 0; i < segmentCount; i++)
            {
                float angle = Mathf.PI * 2f * i / segmentCount;
                lineRenderer.SetPosition(i, new Vector3(
                    Mathf.Cos(angle) * radius,
                    0.05f,
                    Mathf.Sin(angle) * radius));
            }
        }

        private static Material CreateTransparentMaterial(string materialName)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");

            var material = new Material(shader)
            {
                name = materialName,
                renderQueue = (int)RenderQueue.Transparent
            };

            if (material.HasProperty("_Surface"))
                material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_SrcBlend"))
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend"))
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite"))
                material.SetFloat("_ZWrite", 0f);
            if (material.HasProperty("_Cull"))
                material.SetFloat("_Cull", (float)CullMode.Off);

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            return material;
        }

        private static void SetMaterialColor(Material material, Color color)
        {
            if (material == null)
                return;

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
        }

        private void OnDestroy()
        {
            if (Application.isPlaying)
            {
                Destroy(fillMesh);
                Destroy(fillMaterial);
                Destroy(lineMaterial);
            }
            else
            {
                DestroyImmediate(fillMesh);
                DestroyImmediate(fillMaterial);
                DestroyImmediate(lineMaterial);
            }
        }
    }
}
