#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

public class PlayerRunnerTestModeGUI : MonoBehaviour
{
    private const int WindowId = 92710;
    private static readonly float[] TimeScales = { 1f, 1.5f, 2f, 3f };

    private PlayerRunner _runner;
    private Rect _windowRect = new Rect(20f, 20f, 260f, 135f);
    private bool _isVisible;
    private bool _isInvincible;
    private float _defaultFixedDeltaTime;

    public void Initialize(PlayerRunner runner)
    {
        _runner = runner;
        _isInvincible = runner.IsTestModeInvincible;
    }

    private void Awake()
    {
        _defaultFixedDeltaTime = Time.fixedDeltaTime;

        if (_runner == null)
            _runner = GetComponent<PlayerRunner>();
    }

    private void OnDestroy()
    {
        SetTimeScale(1f);
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

        GUILayout.Space(8f);
        GUILayout.Label($"Game Speed: {Time.timeScale:0.#}x");

        GUILayout.BeginHorizontal();
        foreach (float timeScale in TimeScales)
        {
            bool isCurrentScale = Mathf.Approximately(Time.timeScale, timeScale);
            GUI.enabled = !isCurrentScale;

            if (GUILayout.Button($"{timeScale:0.#}x"))
                SetTimeScale(timeScale);

            GUI.enabled = true;
        }
        GUILayout.EndHorizontal();

        GUI.DragWindow();
    }

    private void SetTimeScale(float timeScale)
    {
        Time.timeScale = timeScale;
        Time.fixedDeltaTime = _defaultFixedDeltaTime * timeScale;
    }
}
#endif
