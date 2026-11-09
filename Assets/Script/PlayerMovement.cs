using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ระบบเคลื่อนที่ตัวละครแบบ 2D Top-down
/// - เดิน 8 ทิศ (WASD / ลูกศร / จอยซ้าย) เดินทแยงไม่เร็วกว่าปกติ
/// - มีความเร่ง/หน่วง ทำให้การเคลื่อนที่นุ่มนวล
/// - วิ่ง (Shift / กดจอยซ้าย)
/// - Dash (Space / ปุ่ม A ของจอย) พร้อม cooldown
/// - จำทิศที่หันล่าสุด ส่งค่าให้ Animator และกลับด้าน Sprite ได้
/// ใช้ Rigidbody2D จึงชนกำแพงได้ถูกต้อง
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("ความเร็ว")]
    [SerializeField] private float moveSpeed = 5f;          // ความเร็วเดิน (หน่วย/วินาที)
    [SerializeField] private float sprintMultiplier = 1.6f; // ตัวคูณตอนวิ่ง
    [SerializeField] private float acceleration = 50f;      // เร่งถึงความเร็วเป้าหมายเร็วแค่ไหน
    [SerializeField] private float deceleration = 60f;      // หยุดเร็วแค่ไหนเมื่อปล่อยปุ่ม

    [Header("Dash")]
    [SerializeField] private bool enableDash = true;
    [SerializeField] private float dashSpeed = 14f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 0.6f;

    [Header("ภาพและแอนิเมชัน (ไม่ใส่ก็ได้)")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private bool flipSpriteHorizontally = true;

    private Rigidbody2D rb;
    private InputAction moveAction;
    private InputAction sprintAction;
    private InputAction dashAction;

    private Vector2 moveInput;
    private Vector2 lastDirection = Vector2.down; // เริ่มเกมหันหน้าลง
    private Vector2 dashDirection;
    private bool isDashing;
    private float dashTimer;
    private float cooldownTimer;

    // ให้สคริปต์อื่นอ่านค่าได้ (เช่น ระบบโจมตีอยากรู้ว่าหันทางไหน)
    public Vector2 FacingDirection => lastDirection;
    public bool IsDashing => isDashing;
    public bool IsMoving => moveInput.sqrMagnitude > 0.01f;

    // ชื่อ Parameter ใน Animator (ต้องสร้างให้ตรง)
    private static readonly int HashMoveX = Animator.StringToHash("MoveX");
    private static readonly int HashMoveY = Animator.StringToHash("MoveY");
    private static readonly int HashSpeed = Animator.StringToHash("Speed");

    // รองรับทั้ง Unity 6 (linearVelocity) และเวอร์ชันเก่า (velocity)
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
        rb.gravityScale = 0f;                 // top-down ไม่มีแรงโน้มถ่วง
        rb.freezeRotation = true;             // ชนแล้วตัวไม่หมุน
        rb.interpolation = RigidbodyInterpolation2D.Interpolate; // ภาพไม่กระตุก
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // dash ไม่ทะลุกำแพง

        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        SetupInput();
    }

    private void SetupInput()
    {
        // สร้าง Input ในโค้ดเลย ไม่ต้องสร้างไฟล์ Input Actions เพิ่ม
        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");
        moveAction.AddBinding("<Gamepad>/leftStick");

        sprintAction = new InputAction("Sprint", InputActionType.Button);
        sprintAction.AddBinding("<Keyboard>/leftShift");
        sprintAction.AddBinding("<Gamepad>/leftStickPress");

        dashAction = new InputAction("Dash", InputActionType.Button);
        dashAction.AddBinding("<Keyboard>/space");
        dashAction.AddBinding("<Gamepad>/buttonSouth");
    }

    private void OnEnable()
    {
        moveAction.Enable();
        sprintAction.Enable();
        dashAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        sprintAction.Disable();
        dashAction.Disable();
    }

    private void OnDestroy()
    {
        moveAction.Dispose();
        sprintAction.Dispose();
        dashAction.Dispose();
    }

    // Update: อ่าน input และอัปเดตภาพ (ทำทุกเฟรม)
    private void Update()
    {
        moveInput = moveAction.ReadValue<Vector2>();

        // กันเดินทแยงเร็วกว่าปกติ (ยาวสุด = 1) แต่จอยที่ดันเบาๆ ยังเดินช้าได้
        if (moveInput.sqrMagnitude > 1f) moveInput.Normalize();

        if (IsMoving) lastDirection = moveInput.normalized;

        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;

        if (enableDash && dashAction.WasPressedThisFrame() && !isDashing && cooldownTimer <= 0f)
            StartDash();

        UpdateVisuals();
    }

    // FixedUpdate: งานฟิสิกส์ (ทำตามรอบฟิสิกส์ ค่าเริ่มต้น 50 ครั้ง/วินาที)
    private void FixedUpdate()
    {
        if (isDashing)
        {
            Velocity = dashDirection * dashSpeed;
            dashTimer -= Time.fixedDeltaTime;
            if (dashTimer <= 0f)
            {
                isDashing = false;
                cooldownTimer = dashCooldown;
            }
            return;
        }

        float speed = moveSpeed * (sprintAction.IsPressed() ? sprintMultiplier : 1f);
        Vector2 targetVelocity = moveInput * speed;
        float rate = IsMoving ? acceleration : deceleration;

        // ค่อยๆ ปรับความเร็วเข้าหาเป้าหมาย = มีความเร่ง/หน่วง
        Velocity = Vector2.MoveTowards(Velocity, targetVelocity, rate * Time.fixedDeltaTime);
    }

    private void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;
        dashDirection = IsMoving ? moveInput.normalized : lastDirection;
    }

    private void UpdateVisuals()
    {
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetFloat(HashMoveX, lastDirection.x);
            animator.SetFloat(HashMoveY, lastDirection.y);
            animator.SetFloat(HashSpeed, Velocity.magnitude);
        }

        if (flipSpriteHorizontally && spriteRenderer != null && Mathf.Abs(moveInput.x) > 0.01f)
            spriteRenderer.flipX = moveInput.x < 0f;
    }

    // ให้สคริปต์อื่นหยุดตัวละครได้ (เช่น ตอนเปิดบทสนทนา)
    public void StopImmediately()
    {
        isDashing = false;
        moveInput = Vector2.zero;
        Velocity = Vector2.zero;
    }
}
