using System.Collections.Generic;
using UnityEngine;

// Attach to anything that can receive buffs/debuffs (Player, Zombie).
// Active effects are kept in a LinkedList so expired ones can be removed mid-list in O(1).
public class EffectManager : MonoBehaviour
{
    private readonly LinkedList<StatusEffect> activeEffects = new LinkedList<StatusEffect>();

    public IEnumerable<StatusEffect> ActiveEffects => activeEffects;

    // Rule: same effect type picked up again -> refresh its timer (no stacking).
    public void AddEffect(StatusEffect effect)
    {
        for (var node = activeEffects.First; node != null; node = node.Next)
        {
            if (node.Value.GetType() == effect.GetType())
            {
                node.Value.Refresh();
                return;
            }
        }
        activeEffects.AddLast(effect);
    }

    private void Update() => Tick(Time.deltaTime);

    public void Tick(float dt)
    {
        foreach (var e in activeEffects) e.Tick(dt);
        RemoveExpired();
    }

    public void RemoveExpired()
    {
        var node = activeEffects.First;
        while (node != null)
        {
            var next = node.Next;
            if (node.Value.IsExpired()) activeEffects.Remove(node);
            node = next;
        }
    }

    public float GetSpeedMultiplier()
    {
        float m = 1f;
        foreach (var e in activeEffects) m *= e.SpeedMultiplier();
        return m;
    }

    public float GetDamageMultiplier()
    {
        float m = 1f;
        foreach (var e in activeEffects) m *= e.DamageMultiplier();
        return m;
    }

    public float GetFireRateMultiplier()
    {
        float m = 1f;
        foreach (var e in activeEffects) m *= e.FireRateMultiplier();
        return m;
    }
}
