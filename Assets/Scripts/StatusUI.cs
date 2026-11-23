using TMPro;
using UnityEngine;

// Shows the player's HP / Damage / Speed (including active buffs).
public class StatusUI : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private TMP_Text text;

    private void Update()
    {
        if (player == null || text == null) return;
        var s = player.Status;
        text.text = $"HP: {s.Hp}/{s.MaxHp}\nDamage: {player.GetCurrentDamage()}\nSpeed: {player.GetCurrentSpeed():0.0}";
    }
}
