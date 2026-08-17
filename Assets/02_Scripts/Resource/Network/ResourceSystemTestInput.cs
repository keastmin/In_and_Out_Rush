using UnityEngine;

namespace KIM.Dev
{
    internal sealed class ResourceSystemTestInput
    {
        private const int TestResourceGrantAmount = 500;

        public void Tick(PlayerBuilder builder)
        {
            if (!CanHandleInput(builder))
                return;

            if (Input.GetKeyDown(KeyCode.N))
                builder.RequestTestResourceGrant(TestResourceGrantAmount, 0);

            if (Input.GetKeyDown(KeyCode.M))
                builder.RequestTestResourceGrant(0, TestResourceGrantAmount);
        }

        private static bool CanHandleInput(PlayerBuilder builder)
        {
            return builder != null &&
                   builder.Object != null &&
                   builder.Object.IsValid &&
                   builder.Object.IsInSimulation &&
                   builder.HasInputAuthority;
        }
    }
}
