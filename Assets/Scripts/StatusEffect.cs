// Base class for every buff/debuff. Plain C# class (not a MonoBehaviour).
public abstract class StatusEffect
{
    public string EffectName { get; }
    public float Duration { get; }
    public bool IsDebuff { get; }
    public float Remaining { get; private set; }

    protected StatusEffect(string effectName, float duration, bool isDebuff)
    {
        EffectName = effectName;
        Duration = duration;
        IsDebuff = isDebuff;
        Remaining = duration;
    }

    public void Tick(float dt) => Remaining -= dt;
    public bool IsExpired() => Remaining <= 0f;
    public void Refresh() => Remaining = Duration;

    public virtual float SpeedMultiplier() => 1f;
    public virtual float DamageMultiplier() => 1f;
    public virtual float FireRateMultiplier() => 1f;
}

public class SpeedBoostBuff : StatusEffect
{
    public SpeedBoostBuff(float duration = 10f) : base("Speed Boost", duration, false) { }
    public override float SpeedMultiplier() => 1.3f;
}

public class DoubleDamageBuff : StatusEffect
{
    public DoubleDamageBuff(float duration = 8f) : base("Double Damage", duration, false) { }
    public override float DamageMultiplier() => 2f;
}

public class FireRateUpBuff : StatusEffect
{
    public FireRateUpBuff(float duration = 10f) : base("Fire Rate Up", duration, false) { }
    public override float FireRateMultiplier() => 1.5f;
}

public class SlowDebuff : StatusEffect
{
    public SlowDebuff(float duration = 4f) : base("Slow", duration, true) { }
    public override float SpeedMultiplier() => 0.5f;
}
