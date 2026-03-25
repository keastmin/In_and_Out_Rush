using Fusion;
using UnityEngine;

namespace Dev.Network
{
    public partial class StageBootstrapper
    {
        [Header("Scene Load Entities")]
        [SerializeField] private InfiniteGrid _grid;

        [Header("Prefabs")]
        [SerializeField] private Laboratory _laboratoryPrefab;

        public InfiniteGrid Grid => _grid;
        [Networked] public Laboratory NetworkLaboratory { get; private set; }

        private void KIMInitializeHost()
        {
            
        }

        private void KIMCreateObjects()
        {
            SpawnLaboratory();
        }

        private void KIMInitializeObjects()
        {

        }

        private void KIMBindObjects()
        {

        }

        private void KIMSetUpObjects()
        {

        }

        // ?곌뎄???앹꽦
        private void SpawnLaboratory()
        {
            if (HasStateAuthority)
            {
                Vector3 labSpawnPos = Grid.GetCellCenterPosition(Vector3.zero);
                NetworkLaboratory = Runner.Spawn(_laboratoryPrefab, labSpawnPos, Quaternion.identity);
            }
        }
    }
}
