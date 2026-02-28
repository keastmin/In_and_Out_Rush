namespace Dev
{
    public class Resource : Model
    {
        private ResourceType _type;
        private int _amount;

        public Resource(ResourceType type, int amount) : base()
        {
            _type = type;
            _amount = amount;
        }
    }
}