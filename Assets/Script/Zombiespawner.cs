using UnityEngine;

/// <summary>
/// เกิดซอมบี้รอบๆ จุดนี้เป็นระยะ จำกัดจำนวนสูงสุด
/// </summary>
public class ZombieSpawner : MonoBehaviour
{
    [SerializeField] private GameObject zombiePrefab;
    [SerializeField] private float spawnInterval = 3f;
    [SerializeField] private int maxAlive = 10;
    [SerializeField] private float spawnRadius = 5f;

    private float timer;

    private void Update()
    {
        if (zombiePrefab == null) return;
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = spawnInterval;

        if (transform.childCount >= maxAlive) return; // ซอมบี้ที่ตายแล้วจะหายจาก child เอง

        Vector2 pos = (Vector2)transform.position + Random.insideUnitCircle * spawnRadius;
        Instantiate(zombiePrefab, pos, Quaternion.identity, transform);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
