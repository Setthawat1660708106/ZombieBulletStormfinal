using System.Collections.Generic;
using UnityEngine;

// One asset per wave. Create via: Create -> Zombie Game -> Wave Data
[CreateAssetMenu(fileName = "Wave_01", menuName = "Zombie Game/Wave Data")]
public class WaveData : ScriptableObject
{
    public int waveNumber = 1;
    public bool isBossWave;
    public List<SpawnEntry> entries = new List<SpawnEntry>();

    public int GetTotalCount()
    {
        int total = 0;
        foreach (var e in entries) total += e.count;
        return total;
    }
}
