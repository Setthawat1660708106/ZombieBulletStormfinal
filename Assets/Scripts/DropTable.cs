using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DropEntry
{
    public ItemType itemType;
    public float weight = 1f;     // higher = drops more often
    public PickupItem prefab;
}

// Decides what a dead zombie drops. Weights live in a Dictionary<ItemType, float>.
public class DropTable : MonoBehaviour
{
    [SerializeField] private List<DropEntry> entries = new List<DropEntry>();

    private readonly Dictionary<ItemType, float> weights = new Dictionary<ItemType, float>();
    private readonly Dictionary<ItemType, PickupItem> prefabs = new Dictionary<ItemType, PickupItem>();

    private void Awake()
    {
        foreach (var e in entries)
        {
            weights[e.itemType] = e.weight;
            prefabs[e.itemType] = e.prefab;
        }
    }

    // First roll: does this zombie drop anything? (ZombieData.dropChance)
    public bool TryDrop(Zombie zombie)
    {
        if (zombie.Data == null || Random.value > zombie.Data.dropChance) return false;
        Drop(zombie.transform.position);
        return true;
    }

    // Weighted random pick among the item types.
    public ItemType Roll()
    {
        float total = 0f;
        foreach (var w in weights.Values) total += w;

        float r = Random.value * total;
        ItemType last = ItemType.Heal;
        foreach (var kv in weights)
        {
            last = kv.Key;
            r -= kv.Value;
            if (r <= 0f) return kv.Key;
        }
        return last;
    }

    public void Drop(Vector2 position)
    {
        if (weights.Count == 0) return;

        ItemType type = Roll();
        Debug.Log($"Dropped: {type}");
        
        if (prefabs.TryGetValue(type, out PickupItem prefab) && prefab != null)
        {
            Instantiate(prefab, position, Quaternion.identity);
        }
    }
}
