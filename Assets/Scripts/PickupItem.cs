using UnityEngine;

// Item lying on the ground. Needs a Collider2D with "Is Trigger" ticked.
// Make one prefab per ItemType (Duplicate the prefab and change Item Type).
[RequireComponent(typeof(Collider2D))]
public class PickupItem : MonoBehaviour
{
    public ItemType itemType;

    [SerializeField] private int healAmount = 5;
    [SerializeField] private float lifetime = 15f;       // disappears if not picked up
    [SerializeField] private SpriteRenderer sprite;      // optional (auto-found)
    [SerializeField] private bool autoColor = true;      // tint by type so placeholder circles are distinguishable

    private void Awake()
    {
        if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
        if (autoColor && sprite != null) sprite.color = GetColor(itemType);
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Player player = other.GetComponentInParent<Player>();
        if (player == null) return;

        OnPickup(player);
        Destroy(gameObject);
    }

    public void OnPickup(Player player)
    {
        Debug.Log($"Picked up: {itemType}");

        switch (itemType)
        {
            case ItemType.Heal:
                player.Status.Heal(healAmount);
                break;
            case ItemType.SpeedBoost:
                player.Effects.AddEffect(new SpeedBoostBuff());
                break;
            case ItemType.DoubleDamage:
                player.Effects.AddEffect(new DoubleDamageBuff());
                break;
            case ItemType.FireRateUp:
                player.Effects.AddEffect(new FireRateUpBuff());
                break;
        }
    }

    private static Color GetColor(ItemType type)
    {
        switch (type)
        {
            case ItemType.Heal: return new Color(0.3f, 1f, 0.3f);
            case ItemType.SpeedBoost: return new Color(0.3f, 0.9f, 1f);
            case ItemType.DoubleDamage: return new Color(1f, 0.5f, 0.5f);
            case ItemType.FireRateUp: return new Color(1f, 0.55f, 0.1f);
            default: return Color.white;
        }
    }
}
