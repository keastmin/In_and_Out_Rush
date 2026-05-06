using UnityEngine;

public enum GameState
{
    Lobby,
    Game
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    private GameState _currentGameState;

    public GameState CurrentGameState => _currentGameState;

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
        _currentGameState = GameState.Lobby;
    }

    public void SetGameMode(GameState state)
    {
        _currentGameState = state;
    }
}
