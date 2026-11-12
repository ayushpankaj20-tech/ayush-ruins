using UnityEngine;

/// <summary>Teleports the player to a destination, but only while they carry a specific flame.</summary>
[RequireComponent(typeof(Collider))]
public class FlamePortal : MonoBehaviour
{
    public FlamePickup requiredFlame;
    [Tooltip("Optional: the portal only works once this pillar is lit.")]
    public FlamePillar requiredPillar;
    public Transform destination;
    [Tooltip("Shown while the portal is active.")]
    public GameObject activeEffect;
    public AudioClip teleportSound;

    bool IsActive => (requiredFlame == null || (requiredFlame.Collected && !requiredFlame.Delivered))
                     && (requiredPillar == null || requiredPillar.IsLit);

    void Awake() => GetComponent<Collider>().isTrigger = true;

    void Update()
    {
        if (activeEffect != null && activeEffect.activeSelf != IsActive) activeEffect.SetActive(IsActive);
    }

    void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponentInParent<PlayerController>();
        if (player == null || !IsActive || destination == null) return;
        player.Teleport(destination.position, destination.rotation);
        var cam = FindFirstObjectByType<ThirdPersonCamera>();
        if (cam != null) cam.SnapToTarget();
        if (teleportSound != null) AudioSource.PlayClipAtPoint(teleportSound, destination.position, 0.8f);
    }
}
