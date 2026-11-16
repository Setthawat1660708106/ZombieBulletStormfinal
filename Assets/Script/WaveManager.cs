using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// ระบบซอมบี้มาเป็น Wave
/// - ตั้งจำนวนซอมบี้ / ความถี่ / ความเร็ว ของแต่ละ Wave ได้ใน Inspector
/// - ฆ่าซอมบี้ใน Wave หมด -> นับถอยหลัง -> Wave ถัดไป
/// - ครบทุก Wave -> ชนะ
/// วางที่ GameObject เปล่าใน Scene (ใช้แทน ZombieSpawner เดิม)
/// </summary>
public class WaveManager : MonoBehaviour
{
    [System.Serializable]
    public class Wave
    {
        public string name = "Wave";
        public int zombieCount = 20;          // ซอมบี้ทั้งหมดใน Wave นี้
        public float spawnInterval = 1f;      // เกิดทุกกี่วินาที
        public int maxAliveAtOnce = 10;       // อยู่บนจอพร้อมกันได้สูงสุด
        [Range(0.5f, 3f)] public float speedMultiplier = 1f; // ซอมบี้วิ่งเร็วขึ้นใน Wave หลังๆ
    }

    [Header("ซอมบี้")]
    [SerializeField] private GameObject zombiePrefab;
    [SerializeField] private bool zombiesAlwaysChase = true; // เกิดมาแล้ววิ่งเข้าหาผู้เล่นทันที

    [Header("จุดเกิด")]
    [SerializeField] private Transform[] spawnPoints;   // ถ้าว่าง จะสุ่มรอบตัว WaveManager
    [SerializeField] private float randomRadius = 1.5f; // กระจายรอบจุดเกิด
    [SerializeField] private float minDistanceFromPlayer = 5f; // ไม่เกิดชิดผู้เล่นเกินไป

    [Header("Waves")]
    [SerializeField]
    private List<Wave> waves = new List<Wave>
    {
        new Wave { name = "Wave 1", zombieCount = 20, spawnInterval = 1.2f, maxAliveAtOnce = 8,  speedMultiplier = 1.0f },
        new Wave { name = "Wave 2", zombieCount = 25, spawnInterval = 1.0f, maxAliveAtOnce = 10, speedMultiplier = 1.1f },
        new Wave { name = "Wave 3", zombieCount = 30, spawnInterval = 0.9f, maxAliveAtOnce = 12, speedMultiplier = 1.2f },
        new Wave { name = "Wave 4", zombieCount = 40, spawnInterval = 0.7f, maxAliveAtOnce = 15, speedMultiplier = 1.3f },
        new Wave { name = "Wave 5", zombieCount = 50, spawnInterval = 0.5f, maxAliveAtOnce = 20, speedMultiplier = 1.5f },
    };

    [Header("จังหวะเวลา")]
    [SerializeField] private float firstWaveDelay = 3f;
    [SerializeField] private float timeBetweenWaves = 5f;

    [Header("UI (ไม่ใส่ก็ได้)")]
    [SerializeField] private TMP_Text waveText;       // เช่น "Wave 2 / 5"
    [SerializeField] private TMP_Text zombiesLeftText; // เช่น "Zombies: 13"
    [SerializeField] private TMP_Text announceText;   // ข้อความกลางจอ

    [Header("Events")]
    public UnityEvent<int> onWaveStart;      // ส่งเลข Wave (เริ่มที่ 1)
    public UnityEvent<int> onWaveComplete;
    public UnityEvent onAllWavesComplete;

    private readonly List<GameObject> alive = new List<GameObject>();
    private Transform player;
    private int spawnedThisWave;
    private int killedThisWave;

    public int CurrentWave { get; private set; } // เริ่มที่ 1
    public int TotalWaves => waves.Count;

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        if (zombiePrefab == null)
        {
            Debug.LogError("WaveManager: ยังไม่ได้ใส่ Zombie Prefab (ลากจากโฟลเดอร์ Prefabs)", this);
            enabled = false;
            return;
        }

        if (announceText != null) announceText.text = "";
        StartCoroutine(RunWaves());
    }

    private IEnumerator RunWaves()
    {
        yield return Countdown(firstWaveDelay, "Get Ready!");

        for (int i = 0; i < waves.Count; i++)
        {
            CurrentWave = i + 1;
            Wave wave = waves[i];
            spawnedThisWave = 0;
            killedThisWave = 0;

            onWaveStart?.Invoke(CurrentWave);
            yield return Announce($"{wave.name}", 1.5f);

            // เกิดซอมบี้จนครบจำนวน
            while (spawnedThisWave < wave.zombieCount)
            {
                CleanupDead();
                if (alive.Count < wave.maxAliveAtOnce)
                {
                    SpawnZombie(wave);
                    spawnedThisWave++;
                }
                UpdateUI(wave);
                yield return new WaitForSeconds(wave.spawnInterval);
            }

            // รอจนฆ่าหมด
            while (true)
            {
                CleanupDead();
                UpdateUI(wave);
                if (alive.Count == 0) break;
                yield return null;
            }

            onWaveComplete?.Invoke(CurrentWave);

            if (i < waves.Count - 1)
                yield return Countdown(timeBetweenWaves, $"{wave.name} Clear!");
        }

        onAllWavesComplete?.Invoke();
        if (announceText != null) announceText.text = "YOU SURVIVED!";
        Debug.Log("ชนะ! ผ่านครบทุก Wave");
    }

    private void SpawnZombie(Wave wave)
    {
        Vector2 pos = GetSpawnPosition();
        GameObject z = Instantiate(zombiePrefab, pos, Quaternion.identity);

        ZombieAI ai = z.GetComponent<ZombieAI>();
        if (ai != null)
        {
            ai.SetSpeedMultiplier(wave.speedMultiplier);
            if (zombiesAlwaysChase) ai.ForceChase();
        }

        alive.Add(z);
    }

    private Vector2 GetSpawnPosition()
    {
        // ลองสุ่มหลายครั้ง หาจุดที่ไม่ใกล้ผู้เล่นเกินไป
        for (int attempt = 0; attempt < 10; attempt++)
        {
            Vector2 basePos = (spawnPoints != null && spawnPoints.Length > 0)
                ? (Vector2)spawnPoints[Random.Range(0, spawnPoints.Length)].position
                : (Vector2)transform.position;

            Vector2 pos = basePos + Random.insideUnitCircle * randomRadius;

            if (player == null || Vector2.Distance(pos, player.position) >= minDistanceFromPlayer)
                return pos;
        }
        // หาไม่ได้จริงๆ ก็ใช้จุดสุดท้ายไป
        return spawnPoints != null && spawnPoints.Length > 0
            ? (Vector2)spawnPoints[Random.Range(0, spawnPoints.Length)].position
            : (Vector2)transform.position;
    }

    // ซอมบี้ที่ตาย (ถูก Destroy) จะกลายเป็น null -> นับเป็นฆ่าได้
    private void CleanupDead()
    {
        int before = alive.Count;
        alive.RemoveAll(z => z == null);
        killedThisWave += before - alive.Count;
    }

    private void UpdateUI(Wave wave)
    {
        if (waveText != null)
            waveText.text = $"Wave {CurrentWave} / {waves.Count}";
        if (zombiesLeftText != null)
            zombiesLeftText.text = $"Zombies: {wave.zombieCount - killedThisWave}";
    }

    private IEnumerator Countdown(float seconds, string title)
    {
        float t = seconds;
        while (t > 0f)
        {
            if (announceText != null)
                announceText.text = $"{title}\nNext wave in {Mathf.CeilToInt(t)}";
            t -= Time.deltaTime;
            yield return null;
        }
        if (announceText != null) announceText.text = "";
    }

    private IEnumerator Announce(string message, float duration)
    {
        if (announceText == null) yield break;
        announceText.text = message;
        yield return new WaitForSeconds(duration);
        announceText.text = "";
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            foreach (Transform sp in spawnPoints)
                if (sp != null) Gizmos.DrawWireSphere(sp.position, randomRadius);
        }
        else
        {
            Gizmos.DrawWireSphere(transform.position, randomRadius);
        }
    }
}