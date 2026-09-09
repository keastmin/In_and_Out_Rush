using KIM.Dev;

namespace ProjectIO.ResourceEconomy.Adapters.Fusion
{
    public sealed class ResourcePaymentFusionAdapter
    {
        private readonly ResourceSystem _resourceSystem;

        public ResourcePaymentFusionAdapter(ResourceSystem resourceSystem)
        {
            _resourceSystem = resourceSystem;
        }

        public bool CanAfford(Cost cost)
        {
            return _resourceSystem != null &&
                   ResourcePaymentPolicy.CanAfford(
                       ReadBalance(),
                       ToResourceAmount(cost));
        }

        public bool TryPay(Cost cost)
        {
            if (!CanWriteAuthoritativeState())
            {
                return false;
            }

            if (!ResourcePaymentPolicy.TryCalculateRemaining(
                    ReadBalance(),
                    ToResourceAmount(cost),
                    out ResourceAmount remaining))
            {
                return false;
            }

            _resourceSystem.Mineral = remaining.Mineral;
            _resourceSystem.Gas = remaining.Gas;
            return true;
        }

        private ResourceAmount ReadBalance()
        {
            return new ResourceAmount(
                _resourceSystem.Mineral,
                _resourceSystem.Gas);
        }

        private static ResourceAmount ToResourceAmount(Cost cost)
        {
            return new ResourceAmount(cost.Mineral, cost.Gas);
        }

        private bool CanWriteAuthoritativeState()
        {
            return _resourceSystem != null &&
                   _resourceSystem.Object != null &&
                   _resourceSystem.Object.IsValid &&
                   _resourceSystem.Object.IsInSimulation &&
                   _resourceSystem.Object.HasStateAuthority;
        }
    }
}
