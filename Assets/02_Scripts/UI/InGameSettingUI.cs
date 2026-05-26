using UnityEngine;

namespace KIM.Dev
{
    public class InGameSettingUI : MonoBehaviour
    {
        // 게임 안에서 누르는 게임 종료 버튼 로직
        public void OnClickQuitButton()
        {
            Debug.Log("게임 종료");
            gameObject.SetActive(false);
            MatchMaker.Instance.QuitGame();
        }
    }
}