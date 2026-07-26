using UnityEngine;

namespace KIM.Dev
{
    public sealed class GridChunkVisibilityResolver
    {
        private const float PlaneEpsilon = 0.0001f;

        private readonly Vector3[] _nearCorners = new Vector3[4];
        private readonly Vector3[] _farCorners = new Vector3[4];
        private readonly Vector2Int[] _cellCorners = new Vector2Int[4];
        private Camera _cachedCamera;
        private GridCalculator _cachedCalculator;
        private Matrix4x4 _cachedCameraToWorld;
        private Matrix4x4 _cachedProjection;
        private Bounds _cachedDrawableBounds;
        private Vector3 _cachedGridOrigin;
        private float _cachedGridHeight;
        private float _cachedCellSize;
        private float _cachedNearClip;
        private float _cachedFarClip;
        private int _cachedChunkPadding;
        private bool _cachedClampToDrawableBounds;
        private bool _cachedResolveSucceeded;
        private bool _hasCachedResolve;
        private GridVisibleChunkRange _cachedRange;

        public bool TryResolve(
            Camera camera,
            float gridHeight,
            Vector3 gridOrigin,
            float cellSize,
            int chunkPadding,
            GridCalculator calculator,
            out GridVisibleChunkRange range)
        {
            return TryResolve(
                camera,
                gridHeight,
                gridOrigin,
                cellSize,
                chunkPadding,
                calculator,
                default,
                false,
                out range);
        }

        public bool TryResolve(
            Camera camera,
            float gridHeight,
            Vector3 gridOrigin,
            float cellSize,
            int chunkPadding,
            GridCalculator calculator,
            Bounds drawableBounds,
            out GridVisibleChunkRange range)
        {
            return TryResolve(
                camera,
                gridHeight,
                gridOrigin,
                cellSize,
                chunkPadding,
                calculator,
                drawableBounds,
                true,
                out range);
        }

        private bool TryResolve(
            Camera camera,
            float gridHeight,
            Vector3 gridOrigin,
            float cellSize,
            int chunkPadding,
            GridCalculator calculator,
            Bounds drawableBounds,
            bool clampToDrawableBounds,
            out GridVisibleChunkRange range)
        {
            range = default;
            if (camera == null || calculator == null || cellSize <= 0f || camera.farClipPlane <= camera.nearClipPlane)
            {
                return false;
            }

            Matrix4x4 cameraToWorld = camera.cameraToWorldMatrix;
            Matrix4x4 projection = camera.projectionMatrix;
            if (CanReuseCachedResolve(
                    camera,
                    calculator,
                    cameraToWorld,
                    projection,
                    gridHeight,
                    gridOrigin,
                    cellSize,
                    chunkPadding,
                    drawableBounds,
                    clampToDrawableBounds))
            {
                range = _cachedRange;
                return _cachedResolveSucceeded;
            }

            camera.CalculateFrustumCorners(
                new Rect(0f, 0f, 1f, 1f),
                camera.nearClipPlane,
                Camera.MonoOrStereoscopicEye.Mono,
                _nearCorners);
            camera.CalculateFrustumCorners(
                new Rect(0f, 0f, 1f, 1f),
                camera.farClipPlane,
                Camera.MonoOrStereoscopicEye.Mono,
                _farCorners);

            for (int i = 0; i < 4; i++)
            {
                _nearCorners[i] = camera.transform.TransformPoint(_nearCorners[i]);
                _farCorners[i] = camera.transform.TransformPoint(_farCorners[i]);
            }

            bool hasPoint = false;
            Vector3 minPoint = default;
            Vector3 maxPoint = default;

            AddFrustumFaceIntersections(_nearCorners, gridHeight, ref hasPoint, ref minPoint, ref maxPoint);
            AddFrustumFaceIntersections(_farCorners, gridHeight, ref hasPoint, ref minPoint, ref maxPoint);
            for (int i = 0; i < 4; i++)
            {
                AddSegmentIntersection(
                    _nearCorners[i],
                    _farCorners[i],
                    gridHeight,
                    ref hasPoint,
                    ref minPoint,
                    ref maxPoint);
            }

            if (!hasPoint)
            {
                CacheResolve(
                    camera,
                    calculator,
                    cameraToWorld,
                    projection,
                    gridHeight,
                    gridOrigin,
                    cellSize,
                    chunkPadding,
                    drawableBounds,
                    clampToDrawableBounds,
                    false,
                    range);
                return false;
            }

            if (clampToDrawableBounds)
            {
                minPoint.x = Mathf.Max(minPoint.x, drawableBounds.min.x);
                minPoint.z = Mathf.Max(minPoint.z, drawableBounds.min.z);
                maxPoint.x = Mathf.Min(maxPoint.x, drawableBounds.max.x);
                maxPoint.z = Mathf.Min(maxPoint.z, drawableBounds.max.z);

                if (minPoint.x > maxPoint.x || minPoint.z > maxPoint.z)
                {
                    CacheResolve(
                        camera,
                        calculator,
                        cameraToWorld,
                        projection,
                        gridHeight,
                        gridOrigin,
                        cellSize,
                        chunkPadding,
                        drawableBounds,
                        clampToDrawableBounds,
                        false,
                        range);
                    return false;
                }
            }

            _cellCorners[0] = calculator.GetNearestCellIndexFromWorldPosition(
                gridOrigin,
                new Vector3(minPoint.x, gridHeight, minPoint.z),
                cellSize);
            _cellCorners[1] = calculator.GetNearestCellIndexFromWorldPosition(
                gridOrigin,
                new Vector3(minPoint.x, gridHeight, maxPoint.z),
                cellSize);
            _cellCorners[2] = calculator.GetNearestCellIndexFromWorldPosition(
                gridOrigin,
                new Vector3(maxPoint.x, gridHeight, minPoint.z),
                cellSize);
            _cellCorners[3] = calculator.GetNearestCellIndexFromWorldPosition(
                gridOrigin,
                new Vector3(maxPoint.x, gridHeight, maxPoint.z),
                cellSize);

            GridChunkKey first = calculator.GetChunkKeyFromCellIndex(_cellCorners[0]);
            int minChunkX = first.X;
            int maxChunkX = first.X;
            int minChunkY = first.Y;
            int maxChunkY = first.Y;

            for (int i = 1; i < _cellCorners.Length; i++)
            {
                GridChunkKey key = calculator.GetChunkKeyFromCellIndex(_cellCorners[i]);
                minChunkX = Mathf.Min(minChunkX, key.X);
                maxChunkX = Mathf.Max(maxChunkX, key.X);
                minChunkY = Mathf.Min(minChunkY, key.Y);
                maxChunkY = Mathf.Max(maxChunkY, key.Y);
            }

            int safePadding = Mathf.Max(0, chunkPadding);
            range = new GridVisibleChunkRange(
                new GridChunkKey(minChunkX - safePadding, minChunkY - safePadding),
                new GridChunkKey(maxChunkX + safePadding, maxChunkY + safePadding));
            CacheResolve(
                camera,
                calculator,
                cameraToWorld,
                projection,
                gridHeight,
                gridOrigin,
                cellSize,
                chunkPadding,
                drawableBounds,
                clampToDrawableBounds,
                true,
                range);
            return true;
        }

        private bool CanReuseCachedResolve(
            Camera camera,
            GridCalculator calculator,
            Matrix4x4 cameraToWorld,
            Matrix4x4 projection,
            float gridHeight,
            Vector3 gridOrigin,
            float cellSize,
            int chunkPadding,
            Bounds drawableBounds,
            bool clampToDrawableBounds)
        {
            return _hasCachedResolve &&
                   _cachedCamera == camera &&
                   _cachedCalculator == calculator &&
                   _cachedCameraToWorld == cameraToWorld &&
                   _cachedProjection == projection &&
                   _cachedGridHeight.Equals(gridHeight) &&
                   _cachedGridOrigin == gridOrigin &&
                   _cachedCellSize.Equals(cellSize) &&
                   _cachedNearClip.Equals(camera.nearClipPlane) &&
                   _cachedFarClip.Equals(camera.farClipPlane) &&
                   _cachedChunkPadding == chunkPadding &&
                   _cachedClampToDrawableBounds == clampToDrawableBounds &&
                   (!clampToDrawableBounds || _cachedDrawableBounds.Equals(drawableBounds));
        }

        private void CacheResolve(
            Camera camera,
            GridCalculator calculator,
            Matrix4x4 cameraToWorld,
            Matrix4x4 projection,
            float gridHeight,
            Vector3 gridOrigin,
            float cellSize,
            int chunkPadding,
            Bounds drawableBounds,
            bool clampToDrawableBounds,
            bool succeeded,
            GridVisibleChunkRange range)
        {
            _cachedCamera = camera;
            _cachedCalculator = calculator;
            _cachedCameraToWorld = cameraToWorld;
            _cachedProjection = projection;
            _cachedGridHeight = gridHeight;
            _cachedGridOrigin = gridOrigin;
            _cachedCellSize = cellSize;
            _cachedNearClip = camera.nearClipPlane;
            _cachedFarClip = camera.farClipPlane;
            _cachedChunkPadding = chunkPadding;
            _cachedDrawableBounds = drawableBounds;
            _cachedClampToDrawableBounds = clampToDrawableBounds;
            _cachedResolveSucceeded = succeeded;
            _cachedRange = range;
            _hasCachedResolve = true;
        }

        private static void AddFrustumFaceIntersections(
            Vector3[] corners,
            float gridHeight,
            ref bool hasPoint,
            ref Vector3 minPoint,
            ref Vector3 maxPoint)
        {
            for (int i = 0; i < corners.Length; i++)
            {
                AddSegmentIntersection(
                    corners[i],
                    corners[(i + 1) % corners.Length],
                    gridHeight,
                    ref hasPoint,
                    ref minPoint,
                    ref maxPoint);
            }
        }

        private static void AddSegmentIntersection(
            Vector3 start,
            Vector3 end,
            float gridHeight,
            ref bool hasPoint,
            ref Vector3 minPoint,
            ref Vector3 maxPoint)
        {
            float startDistance = start.y - gridHeight;
            float endDistance = end.y - gridHeight;

            if (Mathf.Abs(startDistance) <= PlaneEpsilon)
            {
                AddPoint(start, ref hasPoint, ref minPoint, ref maxPoint);
            }

            if (Mathf.Abs(endDistance) <= PlaneEpsilon)
            {
                AddPoint(end, ref hasPoint, ref minPoint, ref maxPoint);
            }

            if ((startDistance < -PlaneEpsilon && endDistance < -PlaneEpsilon) ||
                (startDistance > PlaneEpsilon && endDistance > PlaneEpsilon))
            {
                return;
            }

            float denominator = startDistance - endDistance;
            if (Mathf.Abs(denominator) <= PlaneEpsilon)
            {
                return;
            }

            float t = startDistance / denominator;
            if (t < 0f || t > 1f)
            {
                return;
            }

            AddPoint(Vector3.LerpUnclamped(start, end, t), ref hasPoint, ref minPoint, ref maxPoint);
        }

        private static void AddPoint(
            Vector3 point,
            ref bool hasPoint,
            ref Vector3 minPoint,
            ref Vector3 maxPoint)
        {
            if (!hasPoint)
            {
                minPoint = point;
                maxPoint = point;
                hasPoint = true;
                return;
            }

            minPoint = Vector3.Min(minPoint, point);
            maxPoint = Vector3.Max(maxPoint, point);
        }
    }
}
