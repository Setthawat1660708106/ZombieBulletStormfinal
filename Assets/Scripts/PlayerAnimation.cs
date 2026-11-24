using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Drives the player's Animator without touching Player.cs.
// Animator parameters expected: IsMoving (bool), Hurt (trigger), Dead (trigger).
// Missing parameters are skipped silently.
[RequireComponent(typeof(Player), typeof(Rigidbody2D))]
public class PlayerAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;       // optional (auto-found)
    [SerializeField] private float moveThreshold = 0.1f;

    private Player player;
    private Rigidbody2D rb;
    private int lastHp;
    private readonly HashSet<string> animParams = new HashSet<string>();

    private void Awake()
    {
        player = GetComponent<Player>();
        rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            foreach (var p in animator.parameters) animParams.Add(p.name);
        }
    }

    private void OnEnable() => player.Died += OnDied;
    private void OnDisable() => player.Died -= OnDied;

    private void Start() => lastHp = player.Status.Hp; // Start runs after Player.Awake set the HP

    private void Update()
    {
        SetBool("IsMoving", rb.linearVelocity.sqrMagnitude > moveThreshold * moveThreshold);

        // HP went down (and not dead yet) -> play the hurt animation
        int hp = player.Status.Hp;
        if (hp < lastHp && hp > 0) SetTrigger("Hurt");
        lastHp = hp;

        bool shooting = player.isActiveAndEnabled && Mouse.current != null
                && Mouse.current.leftButton.isPressed;
        SetBool("IsShooting", shooting);
    }

    private void OnDied() => SetTrigger("Dead");

    private void SetBool(string param, bool value)
    {
        if (animParams.Contains(param)) animator.SetBool(param, value);
    }

    private void SetTrigger(string param)
    {
        if (animParams.Contains(param)) animator.SetTrigger(param);
    }
}