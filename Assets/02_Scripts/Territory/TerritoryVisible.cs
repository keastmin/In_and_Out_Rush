using System.Collections.Generic;
using ProjectIO.Territory;
using UnityEngine;

namespace Dev.Local
{
    [RequireComponent(typeof(MeshFilter))]
    public class TerritoryVisible : Visible
    {
        private MeshFilter _meshFilter;

        protected override void OnInitialize()
        {
            TryGetComponent(out _meshFilter);
        }

        public void SetVertices(List<Vector2> vertices)
        {
            SetMesh(Territory.GenerateMesh(vertices));
        }

        public void SetMeshData(TerritoryMeshData meshData)
        {
            SetMesh(Territory.GenerateMesh(meshData));
        }

        private void SetMesh(Mesh mesh)
        {
            if (_meshFilter == null && !TryGetComponent(out _meshFilter))
            {
                Debug.LogError($"{nameof(TerritoryVisible)} requires a {nameof(MeshFilter)}.");
                return;
            }

            if (mesh == null)
            {
                Debug.LogError("Territory mesh update skipped because the polygon is invalid.");
                return;
            }

            Mesh previousMesh = _meshFilter.mesh;
            _meshFilter.mesh = mesh;
            if (previousMesh != null)
                Destroy(previousMesh);
        }
    }
}
