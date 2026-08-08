using System.Collections.Generic;
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
            if (_meshFilter == null && !TryGetComponent(out _meshFilter))
            {
                Debug.LogError($"{nameof(TerritoryVisible)} requires a {nameof(MeshFilter)}.");
                return;
            }

            var mesh = Territory.GenerateMesh(vertices);
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
