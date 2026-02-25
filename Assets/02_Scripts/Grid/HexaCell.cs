using UnityEngine;

namespace Grid
{
    public class HexaCell
    {
        public bool IsBuild; // 해당 셀에 건축물 설치 여부
        public Vector3 CenterPosition; // 셀의 중앙점
        public Vector3[] Vertices; // 육각형을 이루는 버텍스들

        public HexaCell(Vector3 centerPos, Vector3[] vertices)
        {
            IsBuild = false;
            CenterPosition = centerPos;
            Vertices = vertices;
        }
    }
}