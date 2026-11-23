using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class WaveManager : MonoBehaviour
{
    [SerializeField] private List<WaveData> waves = new List<WaveData>();
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private Player player;
    [SerializeField] private DropTable dropTable;         // optional: item drops
    [SerializeField] private float spawnInterval = 0.5f;
    [SerializeField] private float timeBetweenWaves = 3f;
    [SerializeField] private int maxAlive = 40;           // cap on zombies on screen at once

    // Queue: zombies waiting to spawn, in the order they will appear.
    private readonly Queue<ZombieData> spawnQueue = new Queue<ZombieData>();
    private readonly List<Zombie> aliveZombies = new List<Zombie>();
    private int currentWave;

    public int CurrentWave => currentWave;
    public int ZombiesRemaining => spawnQueue.Count + aliveZombies.Count;
    public IReadOnlyList<Zombie> AliveZombies => aliveZombies;

    public event Action<int> WaveStarted;
    public event Action<int> WaveCleared;
    public event Action AllWavesCleared;

    private void Start()
    {
        if (player == null) player = FindAnyObjectByType<Player>();

        if (player == null || spawnPoints == null || spawnPoints.Length == 0 || waves.Count == 0)
        {
            Debug.LogError("WaveManager: assign Player, Spawn Points and at least one Wave Data.");
            return;
        }

        StartCoroutine(RunWaves());
    }

    private IEnumerator RunWaves()
    {
        yield return new WaitForSeconds(timeBetweenWaves);

        foreach (var wave in waves)
        {
            StartWave(wave);

            // Spawn everything in the queue, but never exceed maxAlive.
            while (spawnQueue.Count > 0)
            {
                if (!player.isActiveAndEnabled) yield break;
                if (aliveZombies.Count < maxAlive) SpawnNext();
                yield return new WaitForSeconds(spawnInterval);
            }

            // Wait until every zombie of this wave is dead.
            while (aliveZombies.Count > 0)
            {
                if (!player.isActiveAndEnabled) yield break;
                yield return null;
            }

            OnWaveCleared();
            yield return new WaitForSeconds(timeBetweenWaves);
        }

        Debug.Log("All waves cleared!");
        AllWavesCleared?.Invoke();
    }

    public void StartWave(WaveData wave)
    {
        currentWave = wave.waveNumber;

        // Build the list, shuffle it (Fisher-Yates) so zombie types are mixed, then enqueue.
        var list = new List<ZombieData>();
        foreach (var entry in wave.entries)
        {
            if (entry.zombie == null || entry.zombie.prefab == null)
            {
                Debug.LogWarning($"Wave {wave.waveNumber}: a spawn entry has no ZombieData/prefab, skipped.");
                continue;
            }
            for (int i = 0; i < entry.count; i++) list.Add(entry.zombie);
        }

        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }

        spawnQueue.Clear();
        foreach (var z in list) spawnQueue.Enqueue(z);

        Debug.Log($"Wave {currentWave} start: {spawnQueue.Count} zombies{(wave.isBossWave ? " (BOSS)" : "")}");
        WaveStarted?.Invoke(currentWave);
    }

    public void SpawnNext()
    {
        if (spawnQueue.Count == 0) return;

        ZombieData data = spawnQueue.Dequeue();
        Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];

        Zombie zombie = Instantiate(data.prefab, point.position, Quaternion.identity);
        zombie.Init(data, player);
        zombie.Died += OnZombieDied;
        aliveZombies.Add(zombie);
    }

    public void OnZombieDied(Zombie zombie)
    {
        zombie.Died -= OnZombieDied;
        aliveZombies.Remove(zombie);

        if (dropTable != null) dropTable.TryDrop(zombie);
    }

    public void OnWaveCleared()
    {
        Debug.Log($"Wave {currentWave} cleared");
        WaveCleared?.Invoke(currentWave);
        GrantWaveBonus(currentWave);
    }

    // Clearing wave 10, 20, ... gives +10 permanent damage (active when wave 11, 21, ... starts).
    public void GrantWaveBonus(int clearedWave)
    {
        if (clearedWave > 0 && clearedWave % 10 == 0)
        {
            player.Status.AddPermanentDamage(10);
            Debug.Log("Wave bonus: Base Damage +10");
        }
    }
}
