namespace ProjectIO.ResourceEconomy
{
    public readonly struct ResourceAmount
    {
        public int Mineral { get; }
        public int Gas { get; }

        public ResourceAmount(int mineral, int gas)
        {
            Mineral = mineral;
            Gas = gas;
        }
    }
}
