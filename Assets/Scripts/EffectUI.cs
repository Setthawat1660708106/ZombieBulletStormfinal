using System.Text;
using TMPro;
using UnityEngine;

// Lists the player's active buffs with their remaining time, plus the Overclock cooldown.
public class EffectUI : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private OverclockSkill overclock; // optional
    [SerializeField] private TMP_Text text;

    private readonly StringBuilder sb = new StringBuilder();

    private void Update()
    {
        if (player == null || text == null) return;

        sb.Clear();
        foreach (var e in player.Effects.ActiveEffects)
        {
            sb.AppendLine($"{e.EffectName}: {e.Remaining:0.0}s");
        }

        if (overclock != null)
        {
            sb.Append(overclock.CanUse()
                ? "Overclock [Q]: Ready"
                : $"Overclock [Q]: {overclock.CooldownRemaining:0}s");
        }

        text.text = sb.ToString();
    }
}
