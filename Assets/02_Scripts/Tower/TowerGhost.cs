using UnityEngine;

public class TowerGhost : MonoBehaviour
{
    [SerializeField] private GameObject _enableTowerObject;
    [SerializeField] private GameObject _disableTowerObject;
    [SerializeField] private Transform _buffTransform; // 버프 범위 표시 트랜스폼

    public void Start()
    {
        _enableTowerObject.SetActive(false);
        _disableTowerObject.SetActive(false);
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

    /// <summary>
    /// 버프가 있을 경우 타워 고스트에서 버프 범위를 미리 확인할 수 있도록 하는 함수
    /// </summary>
    /// <param name="range">버프 범위</param>
    public void SetGhostBuffRange(float range)
    {
        if (_buffTransform != null)
        {
            Vector3 localScale = new Vector3(range, range, range);
            _buffTransform.localScale = localScale;
        }
    }
}
