using UnityEngine;
using System.Collections.Generic;

namespace Grid
{
    /// <summary>
    /// Grid Manager API
    /// </summary>
    public partial class GridManager
    {
        /// <summary>
        /// 그리드 가이드 오버레이가 표시되는지 여부를 결정
        /// </summary>
        /// <param name="enabled">표시 여부</param>
        public void SetCellStateOverlayEnabled(bool enabled)
        {
            _showCellStateOverlay = enabled;
            BindTerritoryEventsIfNeeded();
            if (enabled)
            {
                RebuildCellStateTextures();
            }
            else
            {
                ClearBuildRangePreview();
            }
            PushShaderData();
        }

        /// <summary>
        /// 지정한 중심 셀과 range를 기준으로 설치 범위 프리뷰를 표시.
        /// </summary>
        public void SetBuildRangePreview(Vector2Int centerIndex, int range)
        {
            _previewCellIndices.Clear();
            AddCellIndicesInRangeToSet(centerIndex, range, _previewCellIndices, includeCenter: true);
            _previewEnabled = _previewCellIndices.Count > 0;
            RebuildPreviewTexture();
            PushShaderData();
        }

        /// <summary>
        /// 외부에서 계산한 셀 집합을 설치 범위 프리뷰로 표시.
        /// </summary>
        public void SetBuildRangePreview(IEnumerable<Vector2Int> indices)
        {
            _previewCellIndices.Clear();

            if (indices != null)
            {
                foreach (var idx in indices)
                {
                    if (IsValidCell(idx.x, idx.y))
                    {
                        _previewCellIndices.Add(idx);
                    }
                }
            }

            _previewEnabled = _previewCellIndices.Count > 0;
            RebuildPreviewTexture();
            PushShaderData();
        }

        /// <summary>
        /// 설치 범위 프리뷰를 제거.
        /// </summary>
        public void ClearBuildRangePreview()
        {
            _previewCellIndices.Clear();
            _previewEnabled = false;
            RebuildPreviewTexture();
            PushShaderData();
        }

        /// <summary>
        /// 버프를 발생시키는 소스(예: 버프 타워)의 셀 범위를 등록/갱신.
        /// 같은 sourceId로 재등록하면 이전 범위를 제거하고 새 범위로 교체한다.
        /// </summary>
        public void RegisterOrUpdateBuffSource(int sourceId, Vector2Int centerIndex, int range, Color color)
        {
            if (sourceId == 0) return;
            if (!IsValidCell(centerIndex.x, centerIndex.y)) return;

            int normalizedRange = Mathf.Max(0, range);
            if (_buffSources.TryGetValue(sourceId, out var oldState))
            {
                if (oldState.CenterIndex == centerIndex &&
                    oldState.Range == normalizedRange &&
                    oldState.Color == color)
                {
                    return;
                }

                RemoveBuffCells(oldState.Cells, oldState.Color);
            }

            var newCells = new HashSet<Vector2Int>();
            AddCellIndicesInRangeToSet(centerIndex, normalizedRange, newCells, includeCenter: true);
            AddBuffCells(newCells, color);

            _buffSources[sourceId] = new BuffSourceState
            {
                CenterIndex = centerIndex,
                Range = normalizedRange,
                Color = color,
                Cells = newCells
            };

            RebuildBuffTexture();
            PushShaderData();
        }

        /// <summary>
        /// 버프 소스를 해제하고 해당 소스가 점유하던 버프 셀을 제거.
        /// </summary>
        public void RemoveBuffSource(int sourceId)
        {
            if (sourceId == 0) return;
            if (!_buffSources.TryGetValue(sourceId, out var state)) return;

            RemoveBuffCells(state.Cells, state.Color);

            _buffSources.Remove(sourceId);
            RebuildBuffTexture();
            PushShaderData();
        }

        private void AddBuffCells(IEnumerable<Vector2Int> indices, Color color)
        {
            if (indices == null) return;

            foreach (var idx in indices)
            {
                if (_buffCellRefCount.TryGetValue(idx, out int count))
                {
                    _buffCellRefCount[idx] = count + 1;
                }
                else
                {
                    _buffCellRefCount[idx] = 1;
                }

                if (_buffCellColorSum.TryGetValue(idx, out Color sumColor))
                {
                    _buffCellColorSum[idx] = sumColor + color;
                }
                else
                {
                    _buffCellColorSum[idx] = color;
                }
            }
        }

        private void RemoveBuffCells(IEnumerable<Vector2Int> indices, Color color)
        {
            if (indices == null) return;

            foreach (var idx in indices)
            {
                if (_buffCellRefCount.TryGetValue(idx, out int count))
                {
                    if (count <= 1) _buffCellRefCount.Remove(idx);
                    else _buffCellRefCount[idx] = count - 1;
                }

                if (_buffCellColorSum.TryGetValue(idx, out Color sumColor))
                {
                    Color nextColor = sumColor - color;
                    if (_buffCellRefCount.ContainsKey(idx))
                    {
                        _buffCellColorSum[idx] = nextColor;
                    }
                    else
                    {
                        _buffCellColorSum.Remove(idx);
                    }
                }
            }
        }

        /// <summary>
        /// 해당 셀이 현재 버프 활성 셀인지 반환.
        /// </summary>
        public bool IsBuffCell(Vector2Int index)
        {
            if (!IsValidCell(index.x, index.y)) return false;
            return _buffCellRefCount.TryGetValue(index, out int count) && count > 0;
        }

        /// <summary>
        /// 월드 위치가 버프 활성 셀에 포함되는지 반환.
        /// </summary>
        public bool IsWorldPositionInBuffCell(Vector3 worldPosition)
        {
            Vector2Int index = GetNearestCellIndex(worldPosition);
            return IsBuffCell(index);
        }

        /// <summary>
        /// 특정 버프 소스가 활성화한 셀인지 반환.
        /// </summary>
        public bool IsCellInBuffSource(int sourceId, Vector2Int index)
        {
            if (sourceId == 0) return false;
            if (!IsValidCell(index.x, index.y)) return false;
            if (!_buffSources.TryGetValue(sourceId, out var sourceState)) return false;

            return sourceState.Cells != null && sourceState.Cells.Contains(index);
        }

        /// <summary>
        /// 월드 위치가 특정 버프 소스 셀 범위에 포함되는지 반환.
        /// </summary>
        public bool IsWorldPositionInBuffSource(int sourceId, Vector3 worldPosition)
        {
            Vector2Int index = GetNearestCellIndex(worldPosition);
            return IsCellInBuffSource(sourceId, index);
        }

        /// <summary>
        /// 셀의 설치 여부를 결정
        /// </summary>
        /// <param name="col">셀의 열</param>
        /// <param name="row">셀의 행</param>
        /// <param name="isBuild">상태</param>
        /// <returns>상태 변경 성공 여부</returns>
        public bool SetCellState(int col, int row, bool isBuild)
        {
            if (!IsValidCell(col, row)) return false;

            _grid[col, row].IsBuild = isBuild;
            RebuildCellStateTextures();
            PushShaderData();
            return true;
        }

        /// <summary>
        /// 월드 좌표를 받아 가장 가까운 셀의 인덱스(col, row)를 반환.
        /// - col: Z방향 인덱스 (0 .. _gridCol-1)
        /// - row: X방향 인덱스 (0 .. _gridRow-1)
        /// </summary>
        public Vector2Int GetNearestCellIndex(Vector3 worldPos)
        {
            // 1) 월드 -> 로컬(그리드 원점 기준) XZ
            Vector3 p = worldPos - _gridOriginPosition;

            // 2) Flat-top axial(q,r) (연속좌표)
            float qf = (2f / 3f * p.x) / _hexSize;
            float rf = (-1f / 3f * p.x + (SQRT3 / 3f) * p.z) / _hexSize;

            // 3) axial -> cube -> cube rounding
            // cube: (x=q, z=r, y=-x-z)
            float xf = qf;
            float zf = rf;
            float yf = -xf - zf;

            int rx = Mathf.RoundToInt(xf);
            int ry = Mathf.RoundToInt(yf);
            int rz = Mathf.RoundToInt(zf);

            float dx = Mathf.Abs(rx - xf);
            float dy = Mathf.Abs(ry - yf);
            float dz = Mathf.Abs(rz - zf);

            if (dx > dy && dx > dz) rx = -ry - rz;
            else if (dy > dz) ry = -rx - rz;
            else rz = -rx - ry;

            // rounded axial
            int q0 = rx;
            int r0 = rz;

            // 4) 가장 가까운 셀(무한 그리드 기준)을 offset으로 변환해보고, 유효하면 바로 리턴
            Vector2Int idx0 = AxialToOddQOffset(q0, r0);
            if (IsValidCell(idx0.x, idx0.y)) return idx0;

            // 5) 그리드 밖 처리(브루트포스 X):
            //    경계에서 q를 clamp한 값만 쓰면 사선 경계 때문에 한 칸 어긋날 수 있어서
            //    q 후보를 (qClamped-1, qClamped, qClamped+1)로 두고,
            //    각 후보에 대해 r를 해당 q에서 가능한 범위로 clamp 한 뒤,
            //    그 셀 + 6이웃(총 7개)을 검사해서 최단거리 선택. (최대 3*7=21개)
            int qMin = 0;
            int qMax = _gridRow - 1;

            int qC = Mathf.Clamp(q0, qMin, qMax);

            float bestD2 = float.PositiveInfinity;
            Vector2Int best = new Vector2Int(-1, -1);

            for (int dq = -1; dq <= 1; dq++)
            {
                int q = qC + dq;
                if (q < qMin || q > qMax) continue;

                // 이 q에서 r가 가질 수 있는 범위(odd-q offset 사각형을 axial로 본 범위)
                int shift = q >> 1; // floor(q/2)
                int rMin = -shift;
                int rMax = (_gridCol - 1) - shift;

                int r = Mathf.Clamp(r0, rMin, rMax);

                // 후보 axial (q,r) 및 6방향 이웃 검사
                EvaluateAxialCandidateAndNeighbors(q, r, worldPos, ref best, ref bestD2);
            }

            return best;
        }

        /// <summary>
        /// 인덱스를 통해 셀의 중심점을 반환
        /// </summary>
        /// <param name="index">인덱스: x = col, y = row</param>
        /// <returns>해당 인덱스의 중심 위치</returns>
        public Vector3 GetCellCenterPositionFromIndex(Vector2Int index)
        {
            if (!IsValidCell(index.x, index.y)) return Vector3.zero;
            return _grid[index.x, index.y].CenterPosition;
        }

        /// <summary>
        /// 그리드의 중앙 셀 인덱스를 반환.
        /// </summary>
        public Vector2Int GetCenterCellIndex()
        {
            int centerCol = _gridCol / 2;
            int centerRow = _gridRow / 2;
            return new Vector2Int(centerCol, centerRow);
        }

        /// <summary>
        /// 그리드 중앙 셀의 월드 중심 좌표를 반환.
        /// </summary>
        public Vector3 GetCenterCellWorldPosition()
        {
            return GetCellCenterPositionFromIndex(GetCenterCellIndex());
        }

        /// <summary>
        /// 해당 셀에 아무것도 설치되어 있지 않은지 확인하는 함수
        /// </summary>
        /// <param name="col">열</param>
        /// <param name="row">행</param>
        /// <returns>해당 셀이 비어있는지 여부, 비어있다면 true 아니라면 false 반환</returns>
        public bool IsCellCanBuild(int col, int row)
        {
            if (!IsValidCell(col, row)) return false;
            return !_grid[col, row].IsBuild;
        }

        /// <summary>
        /// 해당 셀이 영역 위에 있는지 확인하는 함수
        /// </summary>
        /// <param name="col">열</param>
        /// <param name="row">행</param>
        /// <returns>해당 셀이 영역 위에 있는지 여부, 위에 있다면 true 아니라면 false 반환</returns>
        public bool IsCellInTerritory(int col, int row)
        {
            if (!IsValidCell(col, row)) return false;
            if (_territorySystem == null) return false;
            if (_territorySystem.Territory == null) return false;

            Vector3 center = _grid[col, row].CenterPosition;
            Vector2 centerXZ = new Vector2(center.x, center.z);
            return _territorySystem.Territory.IsPointInPolygon(centerXZ);
        }

        /// <summary>
        /// 중심 셀 기준으로 hex 반경(range) 내 모든 셀 인덱스를 반환.
        /// </summary>
        public List<Vector2Int> GetCellIndicesInRange(Vector2Int centerIndex, int range, bool includeCenter = true)
        {
            var result = new List<Vector2Int>();
            if (!IsValidCell(centerIndex.x, centerIndex.y)) return result;

            int radius = Mathf.Max(0, range);
            int q0 = centerIndex.y;
            int r0 = centerIndex.x - (q0 >> 1);

            int estimatedCount = (radius == 0) ? 1 : (1 + 3 * radius * (radius + 1));
            result.Capacity = estimatedCount;

            for (int dq = -radius; dq <= radius; dq++)
            {
                int drMin = Mathf.Max(-radius, -dq - radius);
                int drMax = Mathf.Min(radius, -dq + radius);

                for (int dr = drMin; dr <= drMax; dr++)
                {
                    int q = q0 + dq;
                    int r = r0 + dr;
                    Vector2Int idx = AxialToOddQOffset(q, r);

                    if (!IsValidCell(idx.x, idx.y)) continue;
                    if (!includeCenter && idx == centerIndex) continue;

                    result.Add(idx);
                }
            }

            return result;
        }

        /// <summary>
        /// 중심 셀 기준 range 범위 셀을 targetSet에 추가.
        /// </summary>
        public void AddCellIndicesInRangeToSet(Vector2Int centerIndex, int range, HashSet<Vector2Int> targetSet, bool includeCenter = true)
        {
            if (targetSet == null) return;
            if (!IsValidCell(centerIndex.x, centerIndex.y)) return;

            int radius = Mathf.Max(0, range);
            int q0 = centerIndex.y;
            int r0 = centerIndex.x - (q0 >> 1);

            for (int dq = -radius; dq <= radius; dq++)
            {
                int drMin = Mathf.Max(-radius, -dq - radius);
                int drMax = Mathf.Min(radius, -dq + radius);

                for (int dr = drMin; dr <= drMax; dr++)
                {
                    int q = q0 + dq;
                    int r = r0 + dr;
                    Vector2Int idx = AxialToOddQOffset(q, r);

                    if (!IsValidCell(idx.x, idx.y)) continue;
                    if (!includeCenter && idx == centerIndex) continue;

                    targetSet.Add(idx);
                }
            }
        }

        /// <summary>
        /// 중심 셀 + range 범위가 설치 가능한지 검사.
        /// </summary>
        public bool CanPlaceInRange(
            Vector2Int centerIndex,
            int range,
            bool requireEmpty = true,
            bool requireTerritory = true,
            HashSet<Vector2Int> ignoreOccupiedIndices = null)
        {
            var indices = GetCellIndicesInRange(centerIndex, range, includeCenter: true);
            if (indices.Count == 0) return false;

            foreach (var idx in indices)
            {
                if (requireTerritory && !IsCellInTerritory(idx.x, idx.y))
                    return false;

                if (requireEmpty)
                {
                    bool isIgnored = ignoreOccupiedIndices != null && ignoreOccupiedIndices.Contains(idx);
                    if (!isIgnored && !IsCellCanBuild(idx.x, idx.y))
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 중심 셀 + range 범위 전체의 상태를 한 번에 변경.
        /// </summary>
        public bool SetCellStateInRange(Vector2Int centerIndex, int range, bool isBuild)
        {
            var indices = GetCellIndicesInRange(centerIndex, range, includeCenter: true);
            if (indices.Count == 0) return false;

            foreach (var idx in indices)
            {
                _grid[idx.x, idx.y].IsBuild = isBuild;
            }

            RebuildCellStateTextures();
            PushShaderData();
            return true;
        }
    }
}
