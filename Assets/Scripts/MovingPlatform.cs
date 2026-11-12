using UnityEngine;

/// <summary>
/// Moves back and forth between its start position and an offset, easing at each end.
/// Players standing on it are carried along (see PlayerController).
/// </summary>
[DefaultExecutionOrder(-100)]
public class MovingPlatform : MonoBehaviour
{
    [Tooltip("Where the platform travels to, relative to its start position.")]
    public Vector3 travel = new Vector3(12f, 0f, 0f);
    [Tooltip("Seconds for one trip from start to end.")]
    public float tripTime = 3f;
    [Tooltip("Seconds to wait at each end.")]
    public float pauseTime = 0.6f;
    [Tooltip("Start part-way through the cycle (0-1).")]
    [Range(0f, 1f)] public float phaseOffset = 0f;

    /// <summary>How far the platform moved this frame.</summary>
    public Vector3 Delta { get; private set; }

    Vector3 startPos;
    float timer;

    void Awake()
    {
        startPos = transform.position;
        timer = phaseOffset * Cycle;
    }

    float Cycle => 2f * (tripTime + pauseTime);

    void Update()
    {
        timer = (timer + Time.deltaTime) % Cycle;
        Vector3 before = transform.position;
        transform.position = startPos + travel * Progress(timer);
        Delta = transform.position - before;
    }

    float Progress(float t)
    {
        if (t < pauseTime) return 0f;
        t -= pauseTime;
        if (t < tripTime) return Mathf.SmoothStep(0f, 1f, t / tripTime);
        t -= tripTime;
        if (t < pauseTime) return 1f;
        t -= pauseTime;
        return Mathf.SmoothStep(1f, 0f, t / tripTime);
    }

    void OnDrawGizmos()
    {
        Vector3 a = Application.isPlaying ? startPos : transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(a, a + travel);
        Gizmos.DrawWireCube(a + travel, transform.lossyScale);
    }
}
