using System.Collections.Generic;
using UnityEngine;

namespace KIM.Dev
{
    public interface IFogOfWarAlwaysVisible
    {
    }

    internal static class FogOfWarHiddenObjectController
    {
        private static readonly List<Transform> RegisteredRoots = new();

        public static int Revision { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry()
        {
            RegisteredRoots.Clear();
            Revision = 0;
        }

        public static void Register(Transform root)
        {
            if (root == null || RegisteredRoots.Contains(root))
                return;

            RegisteredRoots.Add(root);
            Revision++;
        }

        public static void Unregister(Transform root)
        {
            if (root == null || !RegisteredRoots.Remove(root))
                return;

            Revision++;
        }

        public static void CopyRegisteredRoots(List<Transform> destination)
        {
            destination.Clear();
            bool removedDestroyedRoot = false;

            for (int i = RegisteredRoots.Count - 1; i >= 0; i--)
            {
                Transform root = RegisteredRoots[i];
                if (root == null)
                {
                    RegisteredRoots.RemoveAt(i);
                    removedDestroyedRoot = true;
                    continue;
                }

                destination.Add(root);
            }

            if (removedDestroyedRoot)
                Revision++;
        }
    }
}
