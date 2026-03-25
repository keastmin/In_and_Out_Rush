using Fusion;
using UnityEngine;

public class HostStartObjectSpawner<T>
{
    private NetworkRunner _hostRunner;

    public HostStartObjectSpawner(NetworkRunner hostRunner)
    {
        _hostRunner = hostRunner;
    }
}
