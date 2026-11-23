using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public enum GameState
{
    Playing,
    GameOver,
    Victory
}

// Watches for the player dying (Game Over) and for the last wave being cleared (Victory).
// Press R (or click the Restart button) to reload the scene.
public class GameManager : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private GameObject endPanel;   // panel shown when the game ends
    [SerializeField] private TMP_Text resultText;   // text inside the panel

    private InputAction restartAction;

    public GameState State { get; private set; } = GameState.Playing;

    private void Awake()
    {
        restartAction = new InputAction("Restart", InputActionType.Button, "<Keyboard>/r");
    }

    private void OnEnable()
    {
        restartAction.Enable();
        if (player != null) player.Died += GameOver;
        if (waveManager != null) waveManager.AllWavesCleared += Victory;
    }

    private void OnDisable()
    {
        restartAction.Disable();
        if (player != null) player.Died -= GameOver;
        if (waveManager != null) waveManager.AllWavesCleared -= Victory;
    }

    private void Start() => StartGame();

    private void Update()
    {
        if (State != GameState.Playing && restartAction.WasPressedThisFrame()) Restart();
    }

    public void StartGame()
    {
        Time.timeScale = 1f;
        State = GameState.Playing;
        if (endPanel != null) endPanel.SetActive(false);
    }

    public void GameOver()
    {
        if (State != GameState.Playing) return;
        State = GameState.GameOver;

        int wave = waveManager != null ? waveManager.CurrentWave : 0;
        Show($"GAME OVER\nReached Wave {wave}\nPress R to restart");
    }

    public void Victory()
    {
        if (State != GameState.Playing) return;
        State = GameState.Victory;

        if (player != null) player.enabled = false; // stop input
        Show("YOU WIN!\nPress R to restart");
    }

    private void Show(string message)
    {
        Debug.Log(message.Replace("\n", " - "));
        if (resultText != null) resultText.text = message;
        if (endPanel != null) endPanel.SetActive(true);
        Time.timeScale = 0f;   // หยุดทุกอย่าง
    }

    public void Restart()
    {
        Time.timeScale = 1f;   // ต้องคืนค่า ไม่งั้นเกมใหม่จะค้าง
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
