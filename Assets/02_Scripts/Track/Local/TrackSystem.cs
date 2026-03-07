using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dev.Local
{
    // * TrackSystem 역할
    // 1. 트랙 생성 및 확장 관리
    // 2. 트랙 관련 이벤트 관리 (예: 트랙 변경, 몬스터 스폰 등)
    public class TrackSystem : System
    {
        [Header("Track")]
        [SerializeField] private TrackVisible _trackVisiblePrefab;
        [SerializeField] private int _trackVertexCount;
        [SerializeField] private float _horizontalRadius;
        [SerializeField] private float _verticalRadius;
        [SerializeField] private float _noise;

        [Header("Expansion")]
        [SerializeField] private float[] _levelUpTimes;
        [SerializeField] private int _smoothingIteration;
        [SerializeField] private int _noiseVertexCount;
        [SerializeField] private float _noiseIntensity = 1f;

        [Header("Monster")]
        [SerializeField] private LocalTrackMonster _trackMonsterPrefab;
        [SerializeField] private float _spawnInterval;
        [SerializeField] private int _spawnCount;

        private int _baseTrackVertexCount;
        private float _baseHorizontalRadius;
        private float _baseVerticalRadius;

        public event Action<int, List<Vector2>, TrackSystem, object> OnTrackChanged;

        protected override void OnInitialize()
        {
            _baseTrackVertexCount = _trackVertexCount;
            _baseHorizontalRadius = _horizontalRadius;
            _baseVerticalRadius = _verticalRadius;
        }

        public void SpawnMonsters()
        {
            StartCoroutine(MonsterSpawnRoutine());
        }

        IEnumerator MonsterSpawnRoutine()
        {
            var track = StageInstance.Instance.Track;
            var startVertex = track.Vertices2d[0];
            var startPosition = new Vector3(startVertex.x, 0, startVertex.y);

            for (int i = 0; i < _spawnCount; i++)
            {
                var monster = Instantiate(_trackMonsterPrefab, startPosition, Quaternion.identity);
                monster.name = $"Monster_{i}";
                monster.SetTrack(track);
                monster.Initialize();

                yield return new WaitForSeconds(_spawnInterval);
            }
        }

        public bool CreateInitialTrack(out Track track, out TrackVisible trackVisible)
        {
            var vertices = CreateEllipseTrackVertices();
            track = CreateTrack(vertices);
            trackVisible = CreateTrackVisible(vertices);
            return true;
        }

        private List<Vector2> CreateEllipseTrackVertices(float scaler = 1f)
        {
            var vertices = new List<Vector2>();

            var trackVertexCount = Mathf.RoundToInt(_baseTrackVertexCount * scaler);
            var horizontalRadius = _baseHorizontalRadius * scaler;
            var verticalRadius = _baseVerticalRadius * scaler;

            for (int i = 0; i < trackVertexCount; i++)
            {
                float angle = Mathf.PI + 2 * Mathf.PI * i / trackVertexCount;
                float x = Mathf.Cos(angle) * horizontalRadius;
                float y = Mathf.Sin(angle) * verticalRadius;

                x += UnityEngine.Random.Range(-_noise, _noise);
                y += UnityEngine.Random.Range(-_noise, _noise);

                var vertex = new Vector2(x, y);
                vertices.Add(vertex);
            }

            return vertices;
        }

        private Track CreateTrack(List<Vector2> vertices)
            => new() { Vertices2d = vertices };

        private TrackVisible CreateTrackVisible(List<Vector2> vertices)
        {
            var vertices3d = vertices.ConvertAll(v => new Vector3(v.x, 0, v.y)).ToArray();
            var trackVisible = Instantiate(_trackVisiblePrefab);
            trackVisible.name = $"Track";
            trackVisible.GenerateTrackVertices(vertices3d);
            trackVisible.GenerateTrackLine(vertices3d);
            return trackVisible;
        }
        
        public void HandleTimeChanged(float time, TimeSystem timerSystem, object sender)
        {
            var level = StageInstance.Instance.Track.Level;
            for (int i = level - 1; i < _levelUpTimes.Length; i++)
            {
                if (_levelUpTimes[i] <= time)
                    ExpandTrack(StageInstance.Instance.Track.Level);
            }
        }

        public void ExpandTrack(int level)
        {
            level++;

            var vertices = CreateEllipseTrackVertices(level);

            var noiseCount = UnityEngine.Random.Range(2, _noiseVertexCount);
            for (int i = 0; i < noiseCount; i++)
            {
                var randomIndex = UnityEngine.Random.Range(0, vertices.Count);
                var intensity = UnityEngine.Random.Range(1f / _noiseIntensity, 1f * _noiseIntensity);
                vertices[randomIndex] *= intensity;
            }

            for (int itr = 0; itr < _smoothingIteration; itr++)
            {
                var temporaryVertices = new List<Vector2>();
                for (int i = 0; i < vertices.Count; i++)
                {
                    var prevVertex = vertices[(i - 1 + vertices.Count) % vertices.Count];
                    var currentVertex = vertices[i];
                    var nextVertex = vertices[(i + 1) % vertices.Count];
                    var prevDistance = prevVertex.magnitude;
                    var currentDistance = currentVertex.magnitude;
                    var nextDistance = nextVertex.magnitude;
                    var averageDistance = (prevDistance + currentDistance + nextDistance) / 3f;
                    var direction = currentVertex.normalized;
                    var newVertex = direction * averageDistance;
                    temporaryVertices.Add(newVertex);
                }
                vertices = temporaryVertices;
            }

            OnTrackChanged?.Invoke(level, vertices, this, this);
        }
    }
}