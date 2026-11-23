using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(EffectManager))]
public class Player : MonoBehaviour, IDamageable
{
    [SerializeField] private PlayerStatus status = new PlayerStatus();
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private float muzzleDistance = 0.6f;
    [SerializeField] private SpriteRenderer sprite; // optional: flips to face the mouse

    private Rigidbody2D rb;
    private EffectManager effects;
    private Camera cam;

    // Input actions created in code, so no Input Actions asset is needed.
    private InputAction moveAction;
    private InputAction fireAction;

    private Vector2 moveInput;
    private Vector2 aimDir = Vector2.right;
    private float nextFireTime;
    public event System.Action Died;
    public PlayerStatus Status => status;
    public EffectManager Effects => effects;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        effects = GetComponent<EffectManager>();
        cam = Camera.main;
        status.Init();

        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");

        fireAction = new InputAction("Fire", InputActionType.Button, "<Mouse>/leftButton");
    }

    private void OnEnable()
    {
        moveAction.Enable();
        fireAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        fireAction.Disable();
    }

    private void Update()
    {
        moveInput = Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f);
        UpdateAim();
        if (fireAction.IsPressed() && Time.time >= nextFireTime) Shoot();
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = moveInput * GetCurrentSpeed();
    }

    private void UpdateAim()
    {
        if (Mouse.current == null || cam == null) return;

        Vector2 screen = Mouse.current.position.ReadValue();
        Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
        Vector2 dir = (Vector2)world - (Vector2)transform.position;
        if (dir.sqrMagnitude > 0.0001f) aimDir = dir.normalized;

        if (sprite != null) sprite.flipX = aimDir.x < 0f; // assumes the sprite faces right
    }

    public void Shoot()
    {
        if (bulletPrefab == null) return;

        float rate = status.FireRate * effects.GetFireRateMultiplier();
        nextFireTime = Time.time + 1f / rate;

        Vector2 spawnPos = (Vector2)transform.position + aimDir * muzzleDistance;
        Bullet bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
        bullet.Init(GetCurrentDamage(), aimDir);
    }

    // Values after buffs/debuffs are applied
    public int GetCurrentDamage() =>
        Mathf.RoundToInt(status.GetBaseDamage() * effects.GetDamageMultiplier());

    public float GetCurrentSpeed() =>
        status.MoveSpeed * effects.GetSpeedMultiplier();

    public void TakeDamage(int amount)
    {
        if (status.TakeDamage(amount)) Die();
    }

    public void Die()
    {
        Debug.Log("Player died");
        rb.linearVelocity = Vector2.zero;
        enabled = false;
        Died?.Invoke();
    }
}
