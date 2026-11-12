using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Smooth orbiting third-person follow camera. Mouse / right stick orbits,
/// and a sphere cast pulls the camera in so it never clips through platforms.
/// </summary>
public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;
    [Tooltip("Point on the target the camera looks at.")]
    public Vector3 pivotOffset = new Vector3(0f, 1.6f, 0f);
    public float distance = 9f;
    public float minDistance = 1.5f;

    [Header("Orbit")]
    public float mouseSensitivity = 0.12f;
    public float stickSensitivity = 160f;
    public float minPitch = -15f;
    public float maxPitch = 65f;
    public float startPitch = 18f;
    [Tooltip("Lock and hide the cursor while playing (Esc to release, click to re-lock).")]
    public bool lockCursor = true;

    [Header("Smoothing")]
    public float followSmoothTime = 0.08f;
    public float distanceSmoothTime = 0.12f;

    [Header("Collision")]
    public LayerMask collisionLayers = ~0;
    public float collisionRadius = 0.35f;

    float yaw, pitch, currentDistance, distanceVelocity;
    Vector3 pivot, pivotVelocity;

    void Start()
    {
        if (target != null) yaw = target.eulerAngles.y;
        pitch = startPitch;
        SnapToTarget();
        if (lockCursor) LockCursor(true);
    }

    void LateUpdate()
    {
        if (target == null) return;
        ReadOrbitInput();

        pivot = Vector3.SmoothDamp(pivot, target.position + pivotOffset, ref pivotVelocity, followSmoothTime);
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 dir = rot * Vector3.back;

        float wanted = distance;
        if (Physics.SphereCast(pivot, collisionRadius, dir, out RaycastHit hit, distance, collisionLayers, QueryTriggerInteraction.Ignore))
            wanted = Mathf.Max(minDistance, hit.distance - 0.1f);
        // Pull in instantly when blocked, ease back out when clear.
        currentDistance = wanted < currentDistance ? wanted
            : Mathf.SmoothDamp(currentDistance, wanted, ref distanceVelocity, distanceSmoothTime);

        transform.SetPositionAndRotation(pivot + dir * currentDistance, rot);
    }

    void ReadOrbitInput()
    {
        var mouse = Mouse.current;
        if (mouse != null)
        {
            if (lockCursor && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) LockCursor(false);
            if (lockCursor && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked) LockCursor(true);
            if (!lockCursor || Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 d = mouse.delta.ReadValue() * mouseSensitivity;
                yaw += d.x; pitch -= d.y;
            }
        }
        var pad = Gamepad.current;
        if (pad != null)
        {
            Vector2 s = pad.rightStick.ReadValue() * stickSensitivity * Time.deltaTime;
            yaw += s.x; pitch -= s.y;
        }
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    static void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    /// <summary>Jumps the camera straight behind the target (used on respawn).</summary>
    public void SnapToTarget()
    {
        if (target == null) return;
        yaw = target.eulerAngles.y; pitch = startPitch;
        pivot = target.position + pivotOffset; pivotVelocity = Vector3.zero;
        currentDistance = distance; distanceVelocity = 0f;
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        transform.SetPositionAndRotation(pivot + rot * Vector3.back * distance, rot);
    }
}
