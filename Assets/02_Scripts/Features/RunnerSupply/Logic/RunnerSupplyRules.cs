using System.Collections.Generic;
using KIM.Dev;

namespace ProjectIO.RunnerSupply
{
    public static class RunnerSupplyRules
    {
        public const int Capacity = 10;
        public const int SkillId = 1;
        public const int WeaponId = 2;

        public static RunnerItemType GetItemType(int id) =>
            id >= 7000 && id <= 7004 ? (RunnerItemType)(id - 6999) : RunnerItemType.None;

        public static bool IsValidProduct(RunnerSupplyDefinition product) =>
            product != null && product.Cost.Mineral >= 0 && product.Cost.Gas >= 0 &&
            ((product.Id == SkillId || product.Id == WeaponId)
                ? !product.IsItem
                : GetItemType(product.Id) != RunnerItemType.None && GetItemType(product.Id) == product.ItemType);

        public static int CenterBit(TowerPropertiesType type) =>
            type == TowerPropertiesType.None ? 0 : 1 << (int)type;

        public static RunnerSupplyResult Evaluate(RunnerSupplyDefinition product, int count, int mineral, int gas, int centers)
        {
            if (!IsValidProduct(product)) return RunnerSupplyResult.InvalidProduct;
            int required = CenterBit(product.RequiredCenter);
            if ((centers & required) != required) return RunnerSupplyResult.Locked;
            if (count >= Capacity) return RunnerSupplyResult.QueueFull;
            if (mineral < product.Cost.Mineral || gas < product.Cost.Gas) return RunnerSupplyResult.InsufficientResources;
            return RunnerSupplyResult.Success;
        }

        public static bool MatchesPrefix(IReadOnlyList<int> actual, IReadOnlyList<int> expected)
        {
            if (actual == null || expected == null || expected.Count == 0 || expected.Count > actual.Count)
                return false;
            for (int i = 0; i < expected.Count; i++)
                if (actual[i] != expected[i]) return false;
            return true;
        }

        public static string CenterName(TowerPropertiesType center) => center switch
        {
            TowerPropertiesType.Flame => "화염 센터",
            TowerPropertiesType.Blitz => "전격 센터",
            TowerPropertiesType.Biochemical => "생화학 센터",
            _ => string.Empty
        };

        public static string Message(RunnerSupplyResult result, RunnerSupplyDefinition product = null) => result switch
        {
            RunnerSupplyResult.Success => "보급 대기열에 추가했습니다.",
            RunnerSupplyResult.Pending => "구매 요청 중…",
            RunnerSupplyResult.NotReady => "보급 시스템을 준비하고 있습니다.",
            RunnerSupplyResult.NotBuilder => "빌더만 구매할 수 있습니다.",
            RunnerSupplyResult.Locked => $"{CenterName(product != null ? product.RequiredCenter : TowerPropertiesType.None)} 건설이 필요합니다.",
            RunnerSupplyResult.QueueFull => "보급 대기열이 가득 찼습니다. 보급 타워에 먼저 적재하세요.",
            RunnerSupplyResult.InsufficientResources => "보유 자원이 부족합니다.",
            RunnerSupplyResult.StateChanged => "보급 상태가 변경됐습니다. 다시 구매해 주세요.",
            _ => "구매할 수 없는 상품입니다."
        };
    }
}
