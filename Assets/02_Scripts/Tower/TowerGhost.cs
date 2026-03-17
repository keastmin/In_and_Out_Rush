using UnityEngine;

public class TowerGhost : MonoBehaviour
{
    [SerializeField] private GameObject _enableTowerObject;
    [SerializeField] private GameObject _disableTowerObject;
    [SerializeField] private Transform _buffTransform;
    [SerializeField] private bool _showLegacyBuffRangeCircle = false;

    public void Start()
    {
        _enableTowerObject.SetActive(false);
        _disableTowerObject.SetActive(false);

        if (_buffTransform != null)
        {
            _buffTransform.gameObject.SetActive(_showLegacyBuffRangeCircle);
        }
    }

    public void EnableTower()
    {
        _disableTowerObject.SetActive(false);
        _enableTowerObject.SetActive(true);
    }

    public void DisableTower()
    {
        _enableTowerObject.SetActive(false);
        _disableTowerObject.SetActive(true);
    }

    public void SetGhostBuffRange(float range)
    {
        if (_buffTransform != null && _showLegacyBuffRangeCircle)
        {
            Vector3 localScale = new Vector3(range, range, range);
            _buffTransform.localScale = localScale;
        }
    }
}

