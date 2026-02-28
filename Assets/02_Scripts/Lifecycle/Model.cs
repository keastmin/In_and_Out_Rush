namespace Dev
{
    public abstract class Model
    {
        protected string id;

        public string Id => id;

        public Model(string id)
        {
            this.id = id;
        }

        public Model()
        {
            id = System.Guid.NewGuid().ToString();
        }
    }
}