using Fusion;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KIM.Dev
{
    public class LobbyUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _sessionName;
        [SerializeField] private TextMeshProUGUI[] _slots;

        [SerializeField] private Button _startButton;

        private string _slotDefaultText = "None";

        private void Update()
        {
            // PlayerRegistry가 아직 스폰되지 않으면 작동하지 않음
            if (NetworkManager.Instance == null || !NetworkManager.Instance.IsLocalPlayerRegistered)
            {
                if (_startButton.gameObject.activeSelf)
                    _startButton.gameObject.SetActive(false);
                return;
            }
            var playerRegistry = NetworkManager.Instance.Registry;
            if (playerRegistry)
            {
                int myIndex = GetMySlotIndex(playerRegistry); // 내 인덱스 저장
                int otherIndex = myIndex == 0 ? 1 : 0; // 나와 다른 인덱스 저장

                SetSlotPositionText(playerRegistry, myIndex, otherIndex); // 슬롯의 포지션 텍스트 설정
                SetSessionNameText(playerRegistry); // 세션 이름 텍스트 설정
                StartButtonActivation(playerRegistry); // 시작 버튼 활성화/비활성화
            }
        }

        // 내 슬롯 번호를 반환하는 함수
        private int GetMySlotIndex(PlayerRegistry registry)
        {
            return registry.Runner.IsServer ? 0 : 1;
        }

        // 슬롯의 역할군 표시
        private void SetSlotPositionText(PlayerRegistry registry, int myIndex, int otherIndex)
        {
            // 슬롯 초기화
            _slots[myIndex].text = _slotDefaultText;
            _slots[otherIndex].text = _slotDefaultText;

            // 플레이어 정보 순회
            foreach (var player in registry.RefToPosition)
            {
                var position = player.Value;

                if (player.Key == registry.Runner.LocalPlayer)
                {
                    _slots[myIndex].text = (position == PlayerPosition.Builder) ? "Builder" : "Runner";
                }
                else
                {
                    _slots[otherIndex].text = (position == PlayerPosition.Builder) ? "Builder" : "Runner";
                }
            }
        }

        // 세션 이름 설정
        private void SetSessionNameText(PlayerRegistry registry)
        {
            _sessionName.text = registry.Runner.SessionInfo.Name;
        }

        private void StartButtonActivation(PlayerRegistry registry)
        {
            if (registry.Runner.IsServer)
            {
                if (!_startButton.gameObject.activeSelf)
                    _startButton.gameObject.SetActive(true);

                // 버튼 활성화 여부 결정
                _startButton.interactable = IsPlayersReady(registry);
            }
            else
            {
                if (_startButton.gameObject.activeSelf)
                    _startButton.gameObject.SetActive(false);
            }
        }

        public bool IsPlayersReady(PlayerRegistry registry)
        {
            if (registry.RefToPosition.Count >= 2)
            {
                PlayerRef[] players = new PlayerRef[2];
                int count = 0;
                foreach (var player in registry.RefToPosition)
                {
                    players[count++] = player.Key;
                }

                if (registry.RefToPosition[players[0]] != registry.RefToPosition[players[1]])
                {
                    return true;
                }
            }

            return false;
        }

        public void OnClickPositionChange()
        {
            if (NetworkManager.Instance == null ||
                !NetworkManager.Instance.IsLocalPlayerRegistered ||
                MatchMaker.Instance == null ||
                MatchMaker.Instance.Runner == null)
                return;
            var key = MatchMaker.Instance.Runner.LocalPlayer;
            NetworkManager.Instance.Registry.RPC_ChangeRole(key);
        }

        public void OnClickSessionCodeCopyButton()
        {
            GUIUtility.systemCopyBuffer = _sessionName.text;
        }
    }
}