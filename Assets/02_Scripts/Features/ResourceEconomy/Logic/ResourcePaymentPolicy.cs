namespace ProjectIO.ResourceEconomy
{
    public static class ResourcePaymentPolicy
    {
        public static bool CanAfford(ResourceAmount balance, ResourceAmount cost)
        {
            return TryCalculateRemaining(balance, cost, out _);
        }

        public static bool TryCalculateRemaining(
            ResourceAmount balance,
            ResourceAmount cost,
            out ResourceAmount remaining)
        {
            remaining = balance;
            if (cost.Mineral < 0 || cost.Gas < 0)
            {
                return false;
            }

            if (balance.Mineral < cost.Mineral || balance.Gas < cost.Gas)
            {
                return false;
            }

            remaining = new ResourceAmount(
                balance.Mineral - cost.Mineral,
                balance.Gas - cost.Gas);
            return true;
        }
    }
}
