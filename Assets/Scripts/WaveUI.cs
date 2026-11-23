using TMPro;
using UnityEngine;

public class WaveUI : MonoBehaviour
{
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private TMP_Text text;

    private void Update()
    {
        if (waveManager == null || text == null) return;
        text.text = $"Wave {waveManager.CurrentWave}\nZombies left: {waveManager.ZombiesRemaining}";
    }
}
