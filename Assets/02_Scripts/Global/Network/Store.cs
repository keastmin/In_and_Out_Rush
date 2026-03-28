using Fusion;

namespace Dev.Network
{
    public class Store : NetworkBehaviour
    {
        [Networked] public float PlayerRunnerMovementSpeed { get; set; }
        [Networked] public float PlayerRunnerDashScaler { get; set; }
    }
}