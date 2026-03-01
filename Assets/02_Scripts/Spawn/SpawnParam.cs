using System;

namespace Dev
{
    [Serializable]
    public abstract class SpawnParam
    {
        public ObjectSampleParam ObjectSampleParam;
        public Func<object, bool> SpawnValidator = _ => true;
        public int MaxRetryCount = 10;
    }
}