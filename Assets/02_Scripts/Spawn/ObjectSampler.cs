using UnityEngine;

namespace Dev
{
    public class ObjectSampler
    {
        public virtual GameObject Sample(ObjectSampleParam param)
            => param.Objects[Random.Range(0, param.Objects.Length)];
    }
}