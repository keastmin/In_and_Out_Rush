using System;
using UnityEngine;

public class VictoryUI : MonoBehaviour
{
    [SerializeField] private GameObject _playerCheck1;
    [SerializeField] private GameObject _playerCheck2;

    private void Awake()
    {
        _playerCheck1.SetActive(false);
        _playerCheck2.SetActive(false);
    }

    private void OnEnable()
    {
        _playerCheck1.SetActive(false);
        _playerCheck2.SetActive(false);

        if (NetworkManager.Instance)
        {
            NetworkManager.Instance.OnLocalPlayerReady += ToggleLocalPlayerCheckBox;
            NetworkManager.Instance.OnNetworkPlayerReady += ToggleNetworkPlayerCheckBox;
        }
    }

    private void OnDisable()
    {
        if (NetworkManager.Instance)
        {
            NetworkManager.Instance.OnLocalPlayerReady -= ToggleLocalPlayerCheckBox;
            NetworkManager.Instance.OnNetworkPlayerReady -= ToggleNetworkPlayerCheckBox;
        }
    }

    public void OnClickNextStageButton()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.RequestNetworkReady();
        }
    }

    public void ToggleLocalPlayerCheckBox()
    {
        ToggleActivation(_playerCheck1);
    }

    public void ToggleNetworkPlayerCheckBox()
    {
        ToggleActivation(_playerCheck2);
    }

    private void ToggleActivation(GameObject obj)
    {
        var activation = obj.activeSelf;
        obj.SetActive(!activation);
    }
}
