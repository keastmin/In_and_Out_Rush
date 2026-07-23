using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dev
{
    public class TerritoryExpansion
    {
        private Territory _territory;

        public bool IsExpanding;
        public Vector2 previousPosition;
        public List<Vector2> PlayerPath;

        public event Action<object> OnPathCleared;
        public event Action<List<Vector2>, object> OnPathUpdated;
        public event Action<List<Vector2>, Territory, object> OnTerritoryExpanded;

        public TerritoryExpansion(Territory territory)
        {
            _territory = territory;
            IsExpanding = false;
            previousPosition = Vector2.zero;
            PlayerPath = new List<Vector2>();
        }

        public void HandlePlayerRunnerPositionChanged(Vector3 position, Local.PlayerRunner playerRunner, object context)
        {
            var currentPosition = new Vector2(position.x, position.z);

            if (_territory.IsPointInPolygon(currentPosition))
                HandlePlayerMovedInTerritory(currentPosition);
            else
                HandlePlayerMovedOutOfTerritory(currentPosition);
        }

        private void HandlePlayerMovedInTerritory(Vector2 currentPosition)
        {
            if (IsExpanding)
            {
                if (PlayerPath.Count > 1)
                {
                    AddExpandingPathPoint(currentPosition);
                    ExpandTerritory();
                }
                StopExpanding();
            }
            previousPosition = currentPosition;
        }

        private void HandlePlayerMovedOutOfTerritory(Vector2 currentPosition)
        {
            if (!IsExpanding)
            {
                StartExpanding();
                AddExpandingPathPoint(previousPosition);
                AddExpandingPathPoint(currentPosition);
            }

            if (Vector2.SqrMagnitude(currentPosition - previousPosition) > 0.01f)
            {
                AddExpandingPathPoint(currentPosition);
                previousPosition = currentPosition;
            }
        }

        public void StartExpanding()
        {
            IsExpanding = true;
            PlayerPath.Clear();
            OnPathCleared?.Invoke(this);
        }

        public void StopExpanding()
        {
            PlayerPath.Clear();
            IsExpanding = false;
            OnPathCleared?.Invoke(this);
        }

        public void AddExpandingPathPoint(Vector2 point)
        {
            PlayerPath.Add(point);
            OnPathUpdated?.Invoke(PlayerPath, this);
        }

        public void ExpandTerritory()
        {
            if (!_territory.TryExpand(PlayerPath))
            {
                Debug.LogWarning($"Territory expansion rejected. Path point count: {PlayerPath.Count}");
                return;
            }

            OnTerritoryExpanded?.Invoke(_territory.Vertices, _territory, this);
        }
    }
}
