using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(EffectManager))]
public class Zombie : MonoBehaviour, IDamageable
{
    [SerializeField] protected SpriteRenderer sprite;   // optional (auto-found in children)
    [SerializeField] protected Animator animator;       // optional (auto-found in children)
    [SerializeField] private float deathDelay = 0.8f;   // time before the corpse is removed

    protected ZombieData data;
    protected int hp;
    protected EffectManager effects;

    private Rigidbody2D rb;
    private Player target;
    private bool isAttacking;
    private bool isDead;
    private float nextAttackTime;
    private readonly HashSet<string> animParams = new HashSet<string>();

    // Raised once when this zombie dies (WaveManager listens; item drop will too).
    public event Action<Zombie> Died;

    public ZombieData Data => data;
    public EffectManager Effects => effects;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        effects = GetComponent<EffectManager>();
        if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        // Remember which Animator parameters exist so missing ones are skipped silently.
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            foreach (var p in animator.parameters) animParams.Add(p.name);
        }
    }

    public void Init(ZombieData zombieData, Player player)
    {
        data = zombieData;
        target = player;
        hp = data.maxHp;
    }

    private void FixedUpdate()
    {
        if (isDead || data == null) return;

        if (target == null || !target.isActiveAndEnabled)
        {
            rb.linearVelocity = Vector2.zero;
            SetBool("IsMoving", false);
            return;
        }

        if (isAttacking)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 toPlayer = (Vector2)target.transform.position - rb.position;
        float dist = toPlayer.magnitude;

        if (dist <= data.attackRange)
        {
            rb.linearVelocity = Vector2.zero;
            SetBool("IsMoving", false);
            if (Time.time >= nextAttackTime) StartCoroutine(AttackRoutine());
            return;
        }

        Vector2 dir = toPlayer / dist;
        rb.linearVelocity = dir * GetMoveSpeed();
        SetBool("IsMoving", true);
        Flip(dir.x);
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;
        SetTrigger("Attack");

        yield return new WaitForSeconds(data.attackWindup);

        // The hit only lands if the player is still close when the swing connects.
        if (!isDead && target != null && target.isActiveAndEnabled &&
            Vector2.Distance(target.transform.position, transform.position) <= data.attackRange * 1.3f)
        {
            target.TakeDamage(data.damage);
        }

        nextAttackTime = Time.time + data.attackCooldown;
        isAttacking = false;
    }

    public virtual float GetMoveSpeed() => data.moveSpeed * effects.GetSpeedMultiplier();

    public virtual void TakeDamage(int amount)
    {
        if (isDead) return;

        hp -= amount;
        if (hp <= 0)
        {
            Die();
            return;
        }
        SetTrigger("Hurt");
    }

    public virtual void Die()
    {
        if (isDead) return;
        isDead = true;

        StopAllCoroutines();
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false; // no more collisions: bullets pass through the corpse
        SetTrigger("Dead");

        Died?.Invoke(this);
        Destroy(gameObject, deathDelay);
    }

    private void Flip(float dirX)
    {
        if (sprite != null && Mathf.Abs(dirX) > 0.01f) sprite.flipX = dirX < 0f; // assumes sprite faces right
    }

    protected void SetBool(string param, bool value)
    {
        if (animParams.Contains(param)) animator.SetBool(param, value);
    }

    protected void SetTrigger(string param)
    {
        if (animParams.Contains(param)) animator.SetTrigger(param);
    }
}
