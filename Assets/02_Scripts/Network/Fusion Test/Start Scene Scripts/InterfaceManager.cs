using UnityEngine;

public class InterfaceManager : MonoBehaviour
{
    public static InterfaceManager Instance { get; private set; }

    [SerializeField] private GameObject _mainUI;
    [SerializeField] private InGameSettingUI _ingameSettingUI;
    [SerializeField] private VictoryUI _victoryUI;
    [SerializeField] private Canvas _canvas;
    [SerializeField] private NotificationWindow _notificationWindowPrefab;
    [SerializeField] private GameObject _prevFocusUI;
    [SerializeField] private GameObject _currFocusUI;

    public JoinSessionPanel JoinSession;
    public LobbyUI Lobby;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        FocusNextUI(_currFocusUI);
    }

    private void Update()
    {
        if(GameManager.Instance.CurrentGameState == GameState.Game && Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleInGameSettingUI();
        }
    }

    public void FocusNextUI(GameObject nextUI)
    {
        if(_currFocusUI != null)
        {
            _prevFocusUI = _currFocusUI;
            _currFocusUI.SetActive(false);
        }

        _currFocusUI = nextUI;
        _currFocusUI.SetActive(true);
    }

    public void ClearUI()
    {
        CloseInGameSettingUI();
        SetVictoryUIActivation(false);

        if (_currFocusUI != null)
        {
            _prevFocusUI = _currFocusUI;
            _currFocusUI.SetActive(false);
        }
    }

    /// <summary>
    /// 모든 UI 상태를 초기화하고 메인 화면을 포커스함
    /// </summary>
    public void FocusMainMenu()
    {
        CloseInGameSettingUI();
        SetVictoryUIActivation(false);

        if (_currFocusUI != null)
            _currFocusUI.SetActive(false);

        _prevFocusUI = null;
        _currFocusUI = _mainUI;
        _currFocusUI.SetActive(true);
    }

    /// <summary>
    /// 안내창을 띄움
    /// </summary>
    /// <param name="descript">안내 내용</param>
    public void PopupNotificationWindow(string descript)
    {
        var notificationWindow = Instantiate(_notificationWindowPrefab, _canvas.transform);
        notificationWindow.SetDescript(descript);
    }

    public void CloseInGameSettingUI()
    {
        if(_ingameSettingUI != null)
            _ingameSettingUI.gameObject.SetActive(false);
    }

    public void SetVictoryUIActivation(bool activated)
    {
        if (_victoryUI != null)
        {
            _victoryUI.gameObject.SetActive(activated);
            Debug.Log($"매개변수로 받은 값: {activated}, 실제로 활성화 되었는가? {_victoryUI.gameObject.activeSelf}");
        }
        else
            Debug.LogError("승리 UI가 없음");

        Debug.Log(_victoryUI.gameObject.activeSelf);
    }

    private void ToggleInGameSettingUI()
    {
        if(_ingameSettingUI == null)
        {
            Debug.LogError("인게임 세팅 UI가 없습니다.");
            return;
        }

        bool activeState = _ingameSettingUI.gameObject.activeSelf;
        _ingameSettingUI.gameObject.SetActive(!activeState);
    }
}
