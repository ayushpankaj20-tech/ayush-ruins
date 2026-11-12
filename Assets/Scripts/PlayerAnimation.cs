using UnityEngine;

/// <summary>Feeds the PlayerController's movement state into the character's Animator.</summary>
public class PlayerAnimation : MonoBehaviour
{
    public PlayerController player;
    public Animator animator;
    [Tooltip("Smoothing for the Speed parameter (seconds).")]
    public float speedDampTime = 0.08f;

    static readonly int SpeedId = Animator.StringToHash("Speed");
    static readonly int GroundedId = Animator.StringToHash("Grounded");
    static readonly int VerticalSpeedId = Animator.StringToHash("VerticalSpeed");
    static readonly int JumpId = Animator.StringToHash("Jump");

    void Awake()
    {
        if (player == null) player = GetComponent<PlayerController>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    void OnEnable() { if (player != null) player.Jumped += OnJumped; }
    void OnDisable() { if (player != null) player.Jumped -= OnJumped; }

    void OnJumped() => animator.SetTrigger(JumpId);

    void Update()
    {
        if (player == null || animator == null) return;
        float speed01 = player.moveSpeed > 0f ? player.HorizontalSpeed / player.moveSpeed : 0f;
        animator.SetFloat(SpeedId, speed01, speedDampTime, Time.deltaTime);
        animator.SetBool(GroundedId, player.IsGrounded);
        animator.SetFloat(VerticalSpeedId, player.Velocity.y);
    }

    /// <summary>Snaps the animator back to idle (used on respawn).</summary>
    public void ResetState()
    {
        animator.Rebind();
        animator.Update(0f);
    }
}
