using System.Collections.Generic;

public class RunnerItemConsumer
{
    private readonly Dictionary<RunnerItemType, IItemConsumptionStrategy> _consumptionStrategies = new();

    public RunnerItemConsumer()
    {
        // _consumptionStrategies[RunnerItemType.Barrier] = new SpawnShieldStrategy();
        // _consumptionStrategies[RunnerItemType.Incinerator] = new SpawnDroneStrategy();
        // _consumptionStrategies[RunnerItemType.ElectricGrenade] = new ElectricGrenadeStrategy();
    }

    public void Use(RunnerItemType itemType, object parameters = null)
    {
        if (!_consumptionStrategies.ContainsKey(itemType))
            throw new KeyNotFoundException($"No consumption strategy found for item type: {itemType}");

        _consumptionStrategies[itemType].Use(parameters);
    }

    public bool TryUse(RunnerItemType itemType, object parameters = null)
    {
        if (!_consumptionStrategies.TryGetValue(itemType, out var consumptionStrategy))
            return false;

        consumptionStrategy.Use(parameters);
        return true;
    }

    private void SpawnShield()
    {
        // Shield 오브젝트 생성
        // Shield 내부 로직: 점차 커지면서 최대 크기로 커지면 사라짐
    }

    private void SpawnDrone()
    {
        // Drone 오브젝트 생성 및 초기화
        // Drone 내부 로직: 일정 시간 동안 플레이어 주변을 맴돌며 적 공격, 일정 시간 후 터지면서 사라짐
    }

    private void ThrowElectricGrenade()
    {
        // 전기 수류탄 생성 및 투척
        // 수류탄 내부 로직: 던지면 물리 적용해서 움직임, 오브젝트에 닿으면 폭발하면서 주변 적에게 데미지 입힘
    }
    private void InstallGasMine()
    {
        // 가스 지뢰 생성
        // 지뢰 내부 로직: 적과 닿으면 터지고 적이 밀림
    }
}
