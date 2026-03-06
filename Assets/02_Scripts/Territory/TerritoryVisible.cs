using System.Collections.Generic;
using UnityEngine;

namespace Dev.Local
{
    [RequireComponent(typeof(MeshFilter))]
    public class TerritoryVisible : Visible
    {
        private MeshFilter meshFilter;

        protected override void OnInitialize()
        {
            TryGetComponent(out meshFilter);
        }

        public void SetVertices(List<Vector2> vertices)
        {
            var mesh = Territory.GenerateMesh(vertices);

            if (mesh == null)
            {
                Debug.LogError("새로운 폴리곤 생성 실패");
                return;
            }

            meshFilter.mesh = mesh;
        }
    }
}
