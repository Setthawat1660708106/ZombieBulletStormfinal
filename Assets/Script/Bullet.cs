using UnityEngine;

/// <summary>
/// กระสุน: บินตรงตามทิศ, ชนแล้วทำดาเมจและหายไป, หมดเวลาก็หายไป
/// ต้องมี Rigidbody2D + Collider2D (ติ๊ก Is Trigger) อยู่บน Prefab
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    [SerializeField] private float lifeTime = 2f;
    [SerializeField] private LayerMask hitLayers = ~0;   // ชนได้กับ Layer ไหนบ้าง (ค่าเริ่มต้น = ทุก Layer)
    [SerializeField] private bool ignoreTriggers = true;  // ไม่ชนกับ trigger อื่น (เช่น โซนเก็บของ)
    [SerializeField] private GameObject hitEffectPrefab;  // ไม่ใส่ก็ได้

    private Rigidbody2D rb;
    private Transform owner;
    private int damage;
    private Vector2 direction;
    private bool hasHit;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // กระสุนเร็วไม่ทะลุ
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    public void Launch(Vector2 dir, float speed, int dmg, Transform shooter)
    {
        direction = dir.normalized;
        damage = dmg;
        owner = shooter;

        // หมุนรูปกระสุนให้ชี้ไปทางที่บิน (รูปต้องหันไปทางขวาเป็นค่าเริ่มต้น)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = direction * speed;
#else
        rb.velocity = direction * speed;
#endif
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;
        if (ignoreTriggers && other.isTrigger) return;
        if (owner != null && other.transform.IsChildOf(owner)) return;     // ไม่ยิงโดนตัวเอง
        if ((hitLayers.value & (1 << other.gameObject.layer)) == 0) return; // ไม่อยู่ใน Layer ที่กำหนด

        hasHit = true;

        IDamageable target = other.GetComponentInParent<IDamageable>();
        target?.TakeDamage(damage, direction);

        if (hitEffectPrefab != null)
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}

/// <summary>อะไรก็ตามที่โดนยิงแล้วเสียเลือดได้ ให้ implement interface นี้</summary>
public interface IDamageable
{
    void TakeDamage(int amount, Vector2 hitDirection);
}