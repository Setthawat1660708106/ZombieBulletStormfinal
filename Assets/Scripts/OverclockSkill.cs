using UnityEngine;
using UnityEngine.InputSystem;

// Press Q: every living zombie gets a SlowDebuff (move speed x0.5) for a few seconds.
// Put this on the Player (or any object) and assign the WaveManager.
public class OverclockSkill : MonoBehaviour
{
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private float cooldown = 30f;
    [SerializeField] private float duration = 4f;

    private InputAction action;
    private float readyTime; // Time.time when the skill can be used again

    public float CooldownRemaining => Mathf.Max(0f, readyTime - Time.time);

    private void Awake()
    {
        action = new InputAction("Overclock", InputActionType.Button, "<Keyboard>/q");
    }

    private void OnEnable() => action.Enable();
    private void OnDisable() => action.Disable();

    private void Update()
    {
        if (!action.WasPressedThisFrame()) return;   // ไม่ได้กด Q ก็ไม่ทำอะไร

        if (CanUse()) Activate();
        else Debug.Log($"Overclock on cooldown: {CooldownRemaining:0.0}s left");
    }

    public bool CanUse() => Time.time >= readyTime;

    public void Activate()
    {
        if (waveManager == null) return;

        readyTime = Time.time + cooldown;
        foreach (Zombie z in waveManager.AliveZombies)
        {
            z.Effects.AddEffect(new SlowDebuff(duration));
        }

        Debug.Log($"Overclock activated: slowed {waveManager.AliveZombies.Count} zombies for {duration}s");
    }
}
