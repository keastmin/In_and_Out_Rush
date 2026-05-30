using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KIM.Dev
{
    public class JoinSessionPanel : MonoBehaviour
    {
        [SerializeField] private TMP_InputField _inputField;
        [SerializeField] private Button _joinButton;

        public string RoomCode => _inputField.text;

        public void OnButtonInteractable()
        {
            _joinButton.interactable = (_inputField.text.Length == MatchMaker.Instance.RoomCodeLength);
        }
    }

}