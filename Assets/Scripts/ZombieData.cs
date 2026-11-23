using UnityEngine;

// One asset per zombie type (Normal, Fast, Tank, Boss...). Create via:
// Right click in Project -> Create -> Zombie Game -> Zombie Data
[CreateAssetMenu(fileName = "ZombieData", menuName = "Zombie Game/Zombie Data")]
public class ZombieData : ScriptableObject
{
    public string zombieName = "Zombie";
    public Zombie prefab;

    [Header("Stats")]
    public int maxHp = 30;
    public float moveSpeed = 2f;

    [Header("Attack")]
    public int damage = 5;
    public float attackRange = 0.9f;
    public float attackCooldown = 1f;
    public float attackWindup = 0.25f; // delay between starting the attack animation and the hit

    [Header("Drop")]
    [Range(0f, 1f)] public float dropChance = 0.3f;
}
