using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Third-person platformer movement: camera-relative running, buffered/coyote jumping,
/// snappy gravity and facing the move direction. Uses a CharacterController.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Top running speed (units/second).")]
    public float moveSpeed = 8f;
    [Tooltip("How fast you reach top speed on the ground.")]
    public float acceleration = 60f;
    [Tooltip("How fast you stop on the ground when no input is held.")]
    public float deceleration = 70f;
    [Range(0f, 1f), Tooltip("Fraction of ground acceleration available in the air.")]
    public float airControl = 0.65f;
    [Tooltip("Degrees per second the character turns to face its movement.")]
    public float turnSpeed = 900f;

    [Header("Jumping")]
    [Tooltip("Peak jump height (units).")]
    public float jumpHeight = 2.6f;
    [Tooltip("Gravity while rising (negative).")]
    public float gravity = -35f;
    [Tooltip("Extra gravity multiplier while falling, for a less floaty arc.")]
    public float fallGravityMultiplier = 1.5f;
    [Tooltip("Gravity multiplier when Jump is released early (short hop).")]
    public float lowJumpMultiplier = 2f;
    [Tooltip("Maximum falling speed.")]
    public float maxFallSpeed = 30f;
    [Tooltip("Grace time after walking off a ledge during which you can still jump.")]
    public float coyoteTime = 0.1f;
    [Tooltip("A jump pressed this long before landing still counts.")]
    public float jumpBufferTime = 0.12f;

    [Header("Ground Check")]
    [Tooltip("Layers that count as ground.")]
    public LayerMask groundLayers = ~0;
    [Tooltip("Extra distance below the capsule to probe for ground.")]
    public float groundCheckDistance = 0.12f;

    [Header("References")]
    [Tooltip("Camera used for camera-relative movement. Defaults to Camera.main.")]
    public Transform cameraTransform;

    public bool IsGrounded { get; private set; }
    public Vector3 Velocity => velocity;
    public float HorizontalSpeed => new Vector3(velocity.x, 0f, velocity.z).magnitude;
    public bool InputEnabled { get; set; } = true;
    public event System.Action Jumped;

    CharacterController controller;
    Vector3 velocity;
    float lastGroundedTime = -10f;
    float lastJumpPressedTime = -10f;
    bool jumpHeld;
    InputAction moveAction, jumpAction;
    MovingPlatform groundPlatform;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;

        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
        moveAction.AddBinding("<Gamepad>/leftStick");

        jumpAction = new InputAction("Jump", InputActionType.Button);
        jumpAction.AddBinding("<Keyboard>/space");
        jumpAction.AddBinding("<Gamepad>/buttonSouth");
    }

    void OnEnable() { moveAction.Enable(); jumpAction.Enable(); }
    void OnDisable() { moveAction.Disable(); jumpAction.Disable(); }
    void OnDestroy() { moveAction.Dispose(); jumpAction.Dispose(); }

    void Update()
    {
        float dt = Time.deltaTime;
        Vector2 input = InputEnabled ? Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f) : Vector2.zero;
        bool jumpDown = InputEnabled && jumpAction.IsPressed();
        if (jumpDown && !jumpHeld) lastJumpPressedTime = Time.time; // press edge
        jumpHeld = jumpDown;

        UpdateGrounded();

        // Camera-relative desired velocity on the XZ plane.
        Vector3 fwd = cameraTransform ? Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized : Vector3.forward;
        Vector3 right = Vector3.Cross(Vector3.up, fwd);
        Vector3 wishDir = fwd * input.y + right * input.x;
        Vector3 target = wishDir * moveSpeed;

        Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
        float rate = wishDir.sqrMagnitude > 0.0001f ? acceleration : deceleration;
        if (!IsGrounded) rate *= airControl;
        horizontal = Vector3.MoveTowards(horizontal, target, rate * dt);

        // Jump (buffered + coyote time).
        bool canJump = Time.time - lastGroundedTime <= coyoteTime;
        if (Time.time - lastJumpPressedTime <= jumpBufferTime && canJump)
        {
            velocity.y = Mathf.Sqrt(2f * jumpHeight * -gravity);
            lastJumpPressedTime = -10f;
            lastGroundedTime = -10f;
            IsGrounded = false;
            Jumped?.Invoke();
        }

        // Gravity: heavier when falling or when jump is released early.
        float g = gravity;
        if (velocity.y < 0f) g *= fallGravityMultiplier;
        else if (velocity.y > 0f && !jumpHeld) g *= lowJumpMultiplier;
        if (IsGrounded && velocity.y < 0f) velocity.y = -2f; // keep snapped to the ground
        else velocity.y = Mathf.Max(velocity.y + g * dt, -maxFallSpeed);

        velocity.x = horizontal.x; velocity.z = horizontal.z;
        // Ride moving platforms: apply the platform's movement first.
        if (IsGrounded && groundPlatform != null && groundPlatform.Delta != Vector3.zero)
            controller.Move(groundPlatform.Delta);
        CollisionFlags flags = controller.Move(velocity * dt);
        if ((flags & CollisionFlags.Above) != 0 && velocity.y > 0f) velocity.y = 0f;

        // Face the direction of travel.
        if (wishDir.sqrMagnitude > 0.0001f)
        {
            Quaternion look = Quaternion.LookRotation(wishDir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnSpeed * dt);
        }
    }

    void UpdateGrounded()
    {
        float radius = controller.radius * 0.9f;
        Vector3 origin = transform.position + controller.center + Vector3.down * (controller.height * 0.5f - controller.radius);
        bool probe = Physics.SphereCast(origin + Vector3.up * 0.05f, radius, Vector3.down, out RaycastHit hit,
            groundCheckDistance + controller.skinWidth + 0.05f, groundLayers, QueryTriggerInteraction.Ignore);
        groundPlatform = probe ? hit.collider.GetComponentInParent<MovingPlatform>() : null;
        IsGrounded = velocity.y <= 0.01f && (controller.isGrounded || probe);
        if (IsGrounded) lastGroundedTime = Time.time;
    }

    /// <summary>Instantly moves the player (used on respawn) and clears momentum.</summary>
    public void Teleport(Vector3 position, Quaternion rotation)
    {
        controller.enabled = false;
        transform.SetPositionAndRotation(position, rotation);
        controller.enabled = true;
        velocity = Vector3.zero;
        lastJumpPressedTime = -10f;
    }
}
