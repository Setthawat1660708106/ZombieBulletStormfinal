using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// เลือดของศัตรู/กล่อง/อะไรก็ได้ที่ยิงได้
/// โดนยิงจะกระพริบสีขาว + โดนกระเด็นเล็กน้อย, เลือดหมดจะถูกทำลาย
/// </summary>
public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private float knockbackForce = 3f;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color flashColor = Color.red;
    [SerializeField] private float flashTime = 0.08f;
    [SerializeField] private bool destroyOnDeath = true;

    public UnityEvent onDamaged;  // ผูก event ใน Inspector ได้ เช่น เล่นเสียง
    public UnityEvent onDeath;

    public int CurrentHealth { get; private set; }
    private Rigidbody2D rb;
    private Color originalColor;
    private bool isDead;

    private void Awake()
    {
        CurrentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;
    }

    public void TakeDamage(int amount, Vector2 hitDirection)
    {
        if (isDead) return;

        CurrentHealth -= amount;
        onDamaged?.Invoke();

        if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
            rb.AddForce(hitDirection.normalized * knockbackForce, ForceMode2D.Impulse);

        if (spriteRenderer != null)
        {
            StopAllCoroutines();
            StartCoroutine(Flash());
        }

        if (CurrentHealth <= 0) Die();
    }

    private IEnumerator Flash()
    {
        spriteRenderer.color = flashColor;
        yield return new WaitForSeconds(flashTime);
        spriteRenderer.color = originalColor;
    }

    private void Die()
    {
        isDead = true;
        onDeath?.Invoke();
        if (destroyOnDeath) Destroy(gameObject);
    }
}
