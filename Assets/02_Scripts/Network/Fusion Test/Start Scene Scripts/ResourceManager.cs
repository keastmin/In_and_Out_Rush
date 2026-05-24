using UnityEngine;
using KIM.Dev;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    public PlayerBuilder PlayerBuilderPrefab;
    public PlayerRunner PlayerRunnerPrefab;
    public Laboratory LaboratoryPrefab;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        DontDestroyOnLoad(this);
    }
}
