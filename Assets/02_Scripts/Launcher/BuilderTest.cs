using UnityEngine;

namespace Launcher
{
    public class BuilderTest : LauncherBase
    {
        [SerializeField] Dev.Local.TerritorySystem territorySystem;
        [SerializeField] HexaTileSnapSystem hexaTileSnapSystem;
        // [SerializeField] LocalTrackGenerationSystem trackGenerationSystem;
        // [SerializeField] LocalTrackMonsterSpawnSystem trackMonsterSpawnSystem;

        protected override void OnLauncherStarted()
        {
            // 영역 생성
            territorySystem.SetUp();

            // 육각 타일 맵 생성
            hexaTileSnapSystem.GenerateInitialHexaTileMap();

            // 트랙 생성
            // trackGenerationSystem.GenerateTrack();

            // 몬스터 생성
            // trackMonsterSpawnSystem.SpawnMonsters(trackGenerationSystem.Track);
        }
    }
}