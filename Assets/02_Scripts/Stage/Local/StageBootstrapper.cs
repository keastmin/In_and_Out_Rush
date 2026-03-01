using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dev.Local
{
    public class StageBootstrapper : Entity
    {
        // [Header("Regacy")]
        // [SerializeField] private StageManager _stageManager;

        [Header("HexaGrid")]
        [SerializeField] private HexaTileSnapSystem _hexaTileSnapSystem;

        [Header("Territory")]
        [SerializeField] private TerritorySystem _territorySystem;
        [SerializeField] private LineRenderer _territoryExpansionLineRenderer;

        [Header("Player")]
        [SerializeField] private PlayerRunner _playerRunner;
        [SerializeField] private float _playerMovementSpeed = 5f;

        [Header("Resource")]
        [SerializeField] private ResourceSystem _resourceSystem;
        [SerializeField] private ResourceView _resourceView;

        [SerializeField] private LocalWorldMonsterSpawnSystem _worldMonsterSpawnSystem;

        protected override void OnInitialize()
        {
            StartCoroutine(InitializeRoutine());
        }

        private IEnumerator InitializeRoutine()
        {
            CreateStageInstance();
            InitializeSystems();

            CreateObjects();
            InitializeObjects();
            BindObjects();
            SetUpObjects();
            yield break;
        }

        private void CreateStageInstance()
        {
            StageInstance.Create();
        }

        private void InitializeSystems()
        {
            _territorySystem.Initialize();
            _territorySystem.CreateInitialCircleTerritory(out var territory, out var territoryVisible);
            StageInstance.Instance.Territory = territory;
            StageInstance.Instance.TerritoryVisible = territoryVisible;
            StageInstance.Instance.InitializeTerritoryExpansion();

            StageInstance.Instance.MovementSpeed = _playerMovementSpeed;

            _resourceSystem.Initialize();
            _resourceSystem.SetSpawnValidator(args =>
            {
                var (_, position, _) = (ValueTuple<GameObject, Vector3, Quaternion>)args;
                return !StageInstance.Instance.Territory.IsPointInPolygon(new Vector2(position.x, position.z));
            });

            // _hexaTileSnapSystem.Initialize();
            _hexaTileSnapSystem.GenerateInitialHexaTileMap();
        }

        private void CreateObjects() { }

        private void InitializeObjects() { }

        private void BindObjects()
        {
            StageInstance.Instance.TerritoryExpansion.OnPathCleared += HandleTerritoryExpansionPathCleared;
            StageInstance.Instance.TerritoryExpansion.OnPathUpdated += HandleTerritoryExpansionPathUpdated;
            StageInstance.Instance.TerritoryExpansion.OnTerritoryExpanded += HandleTerritoryExpansionPathUpdated;
            _playerRunner.OnMoved += StageInstance.Instance.TerritoryExpansion.HandlePlayerRunnerPositionChanged;
            _resourceSystem.OnResourceSpawned += HandleResourceSpawned;
        }

        private void HandleTerritoryExpansionPathCleared(object sender)
        {
            _territoryExpansionLineRenderer.positionCount = 0;
        }

        private void HandleTerritoryExpansionPathUpdated(List<Vector2> path, object sender)
        {
            _territoryExpansionLineRenderer.positionCount = path.Count;
            _territoryExpansionLineRenderer.SetPositions(path.ConvertAll(p => new Vector3(p.x, 0, p.y)).ToArray());
        }

        private void HandleTerritoryExpansionPathUpdated(List<Vector2> path, Territory territory, object sender)
        {
            StageInstance.Instance.TerritoryVisible.SetVertices(territory.Vertices);
        }

        private void HandleResourceSpawned(ResourceVisible resource)
        {
            void HandleTerritoryExpanded(List<Vector2> vertices, Territory territory, object sender)
            {
                var xzPosition = new Vector2(resource.transform.position.x, resource.transform.position.z);
                if (territory.IsPointInPolygon(xzPosition))
                {
                    StageInstance.Instance.TerritoryExpansion.OnTerritoryExpanded -= HandleTerritoryExpanded;
                    resource.Collect();
                }
            }
            StageInstance.Instance.TerritoryExpansion.OnTerritoryExpanded += HandleTerritoryExpanded;
            resource.OnCollected += HandleResourceCollected;
        }

        private void HandleResourceCollected(ResourceType type, int amount, ResourceVisible resource, object context)
        {
            switch (type)
            {
                case ResourceType.Mineral:
                    StageInstance.Instance.Mineral += amount;
                    _resourceView.SetMineral(StageInstance.Instance.Mineral);
                    break;
                case ResourceType.Gas:
                    StageInstance.Instance.Gas += amount;
                    _resourceView.SetGas(StageInstance.Instance.Gas);
                    break;
            }

            Destroy(resource.gameObject);
            // Debug.Log($"Obtained {amount} {type} from {resource.gameObject.name}");
        }

        private void SetUpObjects()
        {
            _resourceSystem.SetUp();
            // _worldMonsterSpawnSystem.SpawnMonsters();
        }
    }
}