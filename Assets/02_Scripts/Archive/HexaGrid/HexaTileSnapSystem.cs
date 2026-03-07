using UnityEngine;

namespace Dev.Local
{
    public class HexaTileSnapSystem : System
    {
        [SerializeField] float hexSize = 1.0f; // Set your desired hex size
        [SerializeField] bool isFlatTop = true; // Set to true for flat-top

        public HexaTileMap hexaTileMap;

        [Header("Debug")]
        public Vector2 mousePositionInWorld;
        public Vector2 snappedPosition;

        void Update()
        {
            var mousePositionInScreen = Input.mousePosition;
            var ray = Camera.main.ScreenPointToRay(mousePositionInScreen);
            var xzPlane = new Plane(Vector3.up, Vector3.zero); // y=0 평면
            if (xzPlane.Raycast(ray, out var enter))
            {
                var hitPoint = ray.GetPoint(enter);
                mousePositionInWorld = new Vector2(hitPoint.x, hitPoint.z);
            }
            snappedPosition = hexaTileMap.SnapToHexTile(mousePositionInWorld);
        }

        public void CreateHexaGrid(out HexaTileMap hexaTileMap)
        {
            hexaTileMap = new HexaTileMap
            {
                HexSize = hexSize, // Set your desired hex size
                IsFlatTop = isFlatTop // Set to true for flat-top hexes, false for pointy-top
            };
        }
    }
}