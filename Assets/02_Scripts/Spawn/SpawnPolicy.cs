using System;
using UnityEngine;

namespace Dev
{
    public abstract class SpawnPolicy<T> where T : MonoBehaviour
    {
        public abstract T Spawn(GameObject prefab, SpawnParam param, Func<GameObject, Vector3, Quaternion, T> spawnFunc);
    }
}