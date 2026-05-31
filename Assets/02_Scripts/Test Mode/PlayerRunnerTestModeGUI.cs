#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

public class PlayerRunnerTestModeGUI : MonoBehaviour
{
    private const int WindowId = 92710;

    private PlayerRunner _runner;
    private Rect _windowRect = new Rect(20f, 20f, 220f, 90f);
    private bool _isVisible;
    private bool _isInvincible;

    public void Initialize(PlayerRunner runner)
    {
        _runner = runner;
        _isInvincible = runner.IsTestModeInvincible;
    }

    private void Awake()
    {
        if (_runner == null)
            _runner = GetComponent<PlayerRunner>();
    }

    private void OnGUI()
    {
        HandleToggleKey();

        if (!_isVisible || _runner == null) return;

        _windowRect = GUI.Window(WindowId, _windowRect, DrawWindow, "Player Runner Test");
    }

    private void HandleToggleKey()
    {
        Event currentEvent = Event.current;
        if (currentEvent == null) return;
        if (currentEvent.type != EventType.KeyDown) return;
        if (currentEvent.keyCode != KeyCode.F10) return;

        _isVisible = !_isVisible;
        currentEvent.Use();
    }

    private void DrawWindow(int windowId)
    {
        bool nextInvincible = GUILayout.Toggle(_isInvincible, "Runner 무적");
        if (nextInvincible != _isInvincible)
        {
            _isInvincible = nextInvincible;
            _runner.SetTestModeInvincible(_isInvincible);
        }

        GUI.DragWindow();
    }
}
#endif
