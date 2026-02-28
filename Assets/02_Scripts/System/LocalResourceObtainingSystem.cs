using TMPro;
using UnityEngine;

public class LocalResourceObtainingSystem : MonoBehaviour
{
    [SerializeField] LocalRunner runner;
    [SerializeField] Transform fieldTransform;
    [SerializeField] TextMeshProUGUI mineralText;
    [SerializeField] TextMeshProUGUI gasText;

    public void TryObtainResources(Territory territory, LocalTerritorySystem territorySystem)
    {
        foreach (var resource in fieldTransform.GetComponentsInChildren<Dev.Local.ResourceVisible>())
        {
            var xzPosition = new Vector2(resource.transform.position.x, resource.transform.position.z);
            if (territory.IsPointInPolygon(xzPosition))
            {
                if (resource.Type == Dev.ResourceType.Mineral)
                {
                    runner.MineralAmount += resource.Amount;
                    Destroy(resource.gameObject);
                    mineralText.text = $"Minerals: {runner.MineralAmount}";
                }
                else if (resource.Type == Dev.ResourceType.Gas)
                {
                    runner.GasAmount += resource.Amount;
                    Destroy(resource.gameObject);
                    gasText.text = $"Gas: {runner.GasAmount}";
                }
            }
        }
    }
}