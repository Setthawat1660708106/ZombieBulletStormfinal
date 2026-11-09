using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ระบบยิงปืนทีละนัด (Semi-auto) สำหรับเกม 2D Top-down
/// - กด 1 ครั้ง = 1 นัด (กดค้างไม่ยิงรัว)
/// - เล็งตามเมาส์ หรือจอยขวา (ถ้าไม่มี ใช้ทิศที่ตัวละครหัน)
/// - หมุนปืนตามทิศเล็ง และกลับด้านปืนเมื่อเล็งไปทางซ้าย
/// - จำกัดความเร็วการยิง, กระสุนในแม็ก, รีโหลด
/// วางสคริปต์นี้ที่ Player
/// </summary>
public class PlayerShooting : MonoBehaviour
{
    [Header("อ้างอิง")]
    [SerializeField] private GameObject bulletPrefab; // Prefab กระสุน (ต้องมีสคริปต์ Bullet ที่ตัวนอกสุด)
    [SerializeField] private Transform weaponPivot;   // จุดหมุนปืน (ลูกของ Player)
    [SerializeField] private Transform firePoint;     // ปากกระบอกปืน (ลูกของ WeaponPivot)
    [SerializeField] private SpriteRenderer weaponSprite; // ไม่ใส่ก็ได้

    [Header("ปืน")]
    [SerializeField] private float bulletSpeed = 18f;
    [SerializeField] private int damage = 1;
    [SerializeField] private float minTimeBetweenShots = 0.15f; // กันกดรัวเร็วเกินไป
    [SerializeField, Range(0f, 15f)] private float spreadAngle = 2f; // ความคลาดเคลื่อน (องศา)

    [Header("กระสุน")]
    [SerializeField] private bool infiniteAmmo = false;
    [SerializeField] private int magazineSize = 8;
    [SerializeField] private float reloadTime = 1.2f;
    [SerializeField] private bool autoReloadWhenEmpty = true;

    [Header("เอฟเฟกต์ (ไม่ใส่ก็ได้)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private AudioClip emptySound;
    [SerializeField] private AudioClip reloadSound;
    [SerializeField] private GameObject muzzleFlash;      // วัตถุลูกของ FirePoint ปิดไว้ก่อน
    [SerializeField] private float muzzleFlashTime = 0.05f;

    private InputAction fireAction;
    private InputAction reloadAction;
    private InputAction aimStickAction;

    private PlayerMovement movement;
    private Camera cam;
    private Vector2 aimDirection = Vector2.right;
    private float nextShotTime;
    private bool isReloading;

    public int CurrentAmmo { get; private set; }
    public int MagazineSize => magazineSize;
    public bool IsReloading => isReloading;
    public Vector2 AimDirection => aimDirection;

    // ให้ UI ฟังได้ เช่น แสดงจำนวนกระสุน
    public event System.Action<int, int> OnAmmoChanged;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        cam = Camera.main;
        CurrentAmmo = magazineSize;

        fireAction = new InputAction("Fire", InputActionType.Button);
        fireAction.AddBinding("<Mouse>/leftButton");
        fireAction.AddBinding("<Gamepad>/rightTrigger");

        reloadAction = new InputAction("Reload", InputActionType.Button);
        reloadAction.AddBinding("<Keyboard>/r");
        reloadAction.AddBinding("<Gamepad>/buttonWest");

        aimStickAction = new InputAction("AimStick", InputActionType.Value);
        aimStickAction.AddBinding("<Gamepad>/rightStick");

        if (muzzleFlash != null) muzzleFlash.SetActive(false);
    }

    private void Start() => OnAmmoChanged?.Invoke(CurrentAmmo, magazineSize);

    private void OnEnable()
    {
        fireAction.Enable();
        reloadAction.Enable();
        aimStickAction.Enable();
    }

    private void OnDisable()
    {
        fireAction.Disable();
        reloadAction.Disable();
        aimStickAction.Disable();
        isReloading = false; // ถ้าโดนปิดกลางคันรีโหลด ไม่ให้ค้าง
    }

    private void OnDestroy()
    {
        fireAction.Dispose();
        reloadAction.Dispose();
        aimStickAction.Dispose();
    }

    private void Update()
    {
        UpdateAim();
        RotateWeapon();

        // WasPressedThisFrame = จริงแค่เฟรมแรกที่กด -> ได้ระบบทีละนัด
        if (fireAction.WasPressedThisFrame())
            TryShoot();

        if (reloadAction.WasPressedThisFrame())
            TryReload();
    }

    private void UpdateAim()
    {
        Vector2 stick = aimStickAction.ReadValue<Vector2>();
        Vector2 origin = weaponPivot != null ? (Vector2)weaponPivot.position : (Vector2)transform.position;

        if (stick.sqrMagnitude > 0.04f)
        {
            aimDirection = stick.normalized;               // ใช้จอยขวา
        }
        else if (Mouse.current != null && cam != null)
        {
            Vector2 mouseWorld = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Vector2 dir = mouseWorld - origin;
            if (dir.sqrMagnitude > 0.01f) aimDirection = dir.normalized; // ใช้เมาส์
        }
        else if (movement != null)
        {
            aimDirection = movement.FacingDirection;        // ไม่มีทั้งคู่ ใช้ทิศที่หัน
        }
    }

    private void RotateWeapon()
    {
        if (weaponPivot == null) return;

        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        weaponPivot.rotation = Quaternion.Euler(0f, 0f, angle);

        // เล็งซ้ายแล้วปืนจะกลับหัว -> พลิกแกน Y ให้ปืนตั้งตรง
        Vector3 s = weaponPivot.localScale;
        s.y = Mathf.Abs(s.y) * (aimDirection.x < 0f ? -1f : 1f);
        weaponPivot.localScale = s;

        // ให้ปืนอยู่หลังตัวละครเมื่อเล็งขึ้น อยู่หน้าตัวละครเมื่อเล็งลง
        if (weaponSprite != null)
            weaponSprite.sortingOrder = aimDirection.y > 0.3f ? -1 : 1;
    }

    private void TryShoot()
    {
        if (isReloading || Time.time < nextShotTime) return;

        if (!infiniteAmmo && CurrentAmmo <= 0)
        {
            PlaySound(emptySound);
            if (autoReloadWhenEmpty) TryReload();
            return;
        }

        Shoot();
    }

    private void Shoot()
    {
        nextShotTime = Time.time + minTimeBetweenShots;

        if (!infiniteAmmo)
        {
            CurrentAmmo--;
            OnAmmoChanged?.Invoke(CurrentAmmo, magazineSize);
        }

        // สุ่มองศาเล็กน้อยให้ไม่แม่นเป๊ะ
        float spread = Random.Range(-spreadAngle, spreadAngle);
        Vector2 dir = Quaternion.Euler(0f, 0f, spread) * aimDirection;

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        if (bulletPrefab == null)
        {
            Debug.LogError("PlayerShooting: ยังไม่ได้ใส่ Bullet Prefab", this);
            return;
        }

        GameObject bulletObj = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet == null)
        {
            Debug.LogError("PlayerShooting: Prefab กระสุนไม่มีสคริปต์ Bullet", bulletPrefab);
            Destroy(bulletObj);
            return;
        }
        bullet.Launch(dir, bulletSpeed, damage, transform);

        PlaySound(shootSound);
        if (muzzleFlash != null) StartCoroutine(FlashRoutine());

        if (!infiniteAmmo && CurrentAmmo <= 0 && autoReloadWhenEmpty)
            TryReload();
    }

    private void TryReload()
    {
        if (infiniteAmmo || isReloading || CurrentAmmo >= magazineSize) return;
        StartCoroutine(ReloadRoutine());
    }

    private IEnumerator ReloadRoutine()
    {
        isReloading = true;
        PlaySound(reloadSound);
        yield return new WaitForSeconds(reloadTime);
        CurrentAmmo = magazineSize;
        isReloading = false;
        OnAmmoChanged?.Invoke(CurrentAmmo, magazineSize);
    }

    private IEnumerator FlashRoutine()
    {
        muzzleFlash.SetActive(true);
        yield return new WaitForSeconds(muzzleFlashTime);
        muzzleFlash.SetActive(false);
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }
}
