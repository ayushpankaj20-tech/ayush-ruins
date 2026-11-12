using UnityEngine;

/// <summary>Spins an object around Y and bobs it up and down.</summary>
public class SpinAndBob : MonoBehaviour
{
    public float spinSpeed = 120f;
    public float bobHeight = 0.18f;
    public float bobSpeed = 2.2f;

    Vector3 startLocalPos;
    float phase;

    void Awake()
    {
        startLocalPos = transform.localPosition;
        phase = Random.value * Mathf.PI * 2f; // desync neighbouring coins
    }

    void Update()
    {
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
        transform.localPosition = startLocalPos + Vector3.up * (Mathf.Sin(Time.time * bobSpeed + phase) * bobHeight);
    }
}
