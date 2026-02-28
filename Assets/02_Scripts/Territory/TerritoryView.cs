using System.Collections.Generic;
using UnityEngine;

public class TerritoryView : MonoBehaviour
{
    MeshFilter meshFilter;

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
    }

    public void SetTerritory(List<Vector2> vertices)
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