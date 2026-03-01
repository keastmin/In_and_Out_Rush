using UnityEngine;

namespace Dev
{
    public class ObjectSampler
    {
        public virtual GameObject Sample(ObjectSampleParam param)
        {
            if (param.Prefabs == null || param.Prefabs.Length == 0)
                return null;

            if (param.Weights == null)
            {
                param.Weights = new float[param.Prefabs.Length];
                for (int i = 0; i < param.Weights.Length; i++)
                    param.Weights[i] = 1f;
            }

            if (param.Weights.Length != param.Prefabs.Length)
                return null;

            var totalWeight = 0f;
            foreach (var weight in param.Weights)
                totalWeight += weight;

            var randomValue = Random.Range(0f, totalWeight);
            for (int i = 0; i < param.Prefabs.Length; i++)
            {
                var weight = param.Weights[i];
                if (randomValue < weight)
                    return param.Prefabs[i];
                randomValue -= weight;
            }

            return null;
        }
    }
}