namespace Dev.Network
{
    public abstract class System : Entity, ISystem
    {
        public void SetUp() => OnSetUp();
        public void TearDown() => OnTearDown();

        protected virtual void OnSetUp() { }
        protected virtual void OnTearDown() { }
    }
}