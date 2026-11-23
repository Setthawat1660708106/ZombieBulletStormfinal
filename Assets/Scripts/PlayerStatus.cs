using UnityEngine;

// The player's stats. [Serializable] so the values can be tuned in the Inspector.
[System.Serializable]
public class PlayerStatus
{
    [SerializeField] private int maxHp = 50;
    [SerializeField] private int baseDamage = 10;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float fireRate = 4f; // shots per second

    private int hp;

    public int MaxHp => maxHp;
    public int Hp => hp;
    public float MoveSpeed => moveSpeed;
    public float FireRate => fireRate;

    public void Init() => hp = maxHp;
    public int GetBaseDamage() => baseDamage;
    public void AddPermanentDamage(int n) => baseDamage += n;
    public void Heal(int n) => hp = Mathf.Min(maxHp, hp + n);

    // returns true if the player is dead after this hit
    public bool TakeDamage(int n)
    {
        hp = Mathf.Max(0, hp - n);
        return hp <= 0;
    }
}
