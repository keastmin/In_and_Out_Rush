using System;

namespace Dev
{
    [Serializable]
    public abstract class SpawnParam
    {
        public int Count = 1;
        public ObjectSampleParam ObjectSampleParam;
        public Func<object, bool> SpawnValidator = _ => true;
        public int MaxRetryCount = 10;
        public bool AllowSpawnFailure = false;
    }
}