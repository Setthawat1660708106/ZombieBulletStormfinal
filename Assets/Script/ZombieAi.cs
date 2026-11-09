using UnityEngine;

/// <summary>
/// ซอมบี้เดินตามผู้เล่น (2D Top-down)
/// - หาผู้เล่นจาก Tag "Player" อัตโนมัติ
/// - เห็นผู้เล่นในระยะ detectRange จะไล่ตาม, ไกลเกิน loseRange จะเลิกไล่และเดินเตร่
/// - เข้าใกล้ถึง attackRange จะโจมตี (ใช้ IDamageable จาก Bullet.cs)
/// วางคู่กับ Rigidbody2D + Collider2D + Health
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class ZombieAI : MonoBehaviour
{
    [Header("การเคลื่อนที่")]
    [SerializeField] private float chaseSpeed = 2f;
    [SerializeField] private float wanderSpeed = 0.8f;
    [SerializeField] private float acceleration = 15f;

    [Header("การมองเห็น")]
    [SerializeField] private float detectRange = 6f;
    [SerializeField] private float loseRange = 9f;
    [SerializeField] private bool needLineOfSight = true;
    [SerializeField] private LayerMask obstacleLayers; // ใส่ Layer กำแพง

    [Header("การโจมตี")]
    [SerializeField] private float attackRange = 0.8f;
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private float attackCooldown = 1f;

    [Header("ภาพ (ไม่ใส่ก็ได้)")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    private enum State { Wander, Chase, Attack }
    private State state = State.Wander;

    private Rigidbody2D rb;
    private Transform player;
    private Vector2 moveDir;
    private Vector2 wanderDir;
    private float wanderTimer;
    private float nextAttackTime;

    private static readonly int HashSpeed = Animator.StringToHash("Speed");

    private Vector2 Velocity
    {
#if UNITY_6000_0_OR_NEWER
        get => rb.linearVelocity;
        set => rb.linearVelocity = value;
#else
        get => rb.velocity;
        set => rb.velocity = value;
#endif
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
        else Debug.LogWarning("ZombieAI: หา Player ไม่เจอ ตั้ง Tag ของผู้เล่นเป็น Player ด้วย", this);
    }

    private void Update()
    {
        if (player == null) { state = State.Wander; Wander(); return; }

        float dist = Vector2.Distance(transform.position, player.position);

        switch (state)
        {
            case State.Wander:
                if (dist <= detectRange && CanSeePlayer()) state = State.Chase;
                break;
            case State.Chase:
                if (dist > loseRange) state = State.Wander;
                else if (dist <= attackRange) state = State.Attack;
                break;
            case State.Attack:
                if (dist > attackRange * 1.2f) state = State.Chase;
                break;
        }

        switch (state)
        {
            case State.Wander: Wander(); break;
            case State.Chase:
                moveDir = ((Vector2)player.position - (Vector2)transform.position).normalized;
                break;
            case State.Attack:
                moveDir = Vector2.zero;
                TryAttack();
                break;
        }

        UpdateVisuals();
    }

    private void FixedUpdate()
    {
        float speed = state == State.Chase ? chaseSpeed : wanderSpeed;
        Velocity = Vector2.MoveTowards(Velocity, moveDir * speed, acceleration * Time.fixedDeltaTime);
    }

    private bool CanSeePlayer()
    {
        if (!needLineOfSight) return true;
        Vector2 dir = player.position - transform.position;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dir.normalized, dir.magnitude, obstacleLayers);
        return hit.collider == null; // ไม่มีกำแพงขวาง
    }

    private void Wander()
    {
        wanderTimer -= Time.deltaTime;
        if (wanderTimer <= 0f)
        {
            // สุ่มเดินหรือยืนนิ่ง สลับกันไป
            wanderDir = Random.value < 0.4f ? Vector2.zero : Random.insideUnitCircle.normalized;
            wanderTimer = Random.Range(1f, 3f);
        }
        moveDir = wanderDir;
    }

    private void TryAttack()
    {
        if (Time.time < nextAttackTime) return;
        nextAttackTime = Time.time + attackCooldown;

        IDamageable target = player.GetComponentInParent<IDamageable>();
        Vector2 dir = (player.position - transform.position).normalized;
        target?.TakeDamage(attackDamage, dir);
    }

    private void UpdateVisuals()
    {
        if (spriteRenderer != null && Mathf.Abs(moveDir.x) > 0.01f)
            spriteRenderer.flipX = moveDir.x < 0f;
        if (animator != null && animator.runtimeAnimatorController != null)
            animator.SetFloat(HashSpeed, Velocity.magnitude);
    }

    // วาดวงระยะใน Scene view ให้เห็นง่าย
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = Color.gray; Gizmos.DrawWireSphere(transform.position, loseRange);
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
