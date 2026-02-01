using Fusion;
using UnityEngine;

public class GridManager : NetworkBehaviour
{
    public GridManager Instance { get; private set; }

    private HexaCell[] _grid;

    private LineRenderer _gridGuideLineRenderer;



    private void Awake()
    {
        Instance = this;
        TryGetComponent(out _gridGuideLineRenderer);
    }
}
