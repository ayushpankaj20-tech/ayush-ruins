using UnityEngine;

/// <summary>Slowly orbits the camera around a point for an animated menu background.</summary>
public class MenuCameraOrbit : MonoBehaviour
{
    public Vector3 center = Vector3.zero;
    public float radius = 22f;
    public float height = 9f;
    public float lookHeight = 2f;
    public float degreesPerSecond = 4f;
    public float startAngle = 200f;

    float angle;

    void Start() => angle = startAngle;

    void LateUpdate()
    {
        angle += degreesPerSecond * Time.deltaTime;
        float r = angle * Mathf.Deg2Rad;
        transform.position = center + new Vector3(Mathf.Sin(r) * radius, height, Mathf.Cos(r) * radius);
        transform.LookAt(center + Vector3.up * lookHeight);
    }
}
