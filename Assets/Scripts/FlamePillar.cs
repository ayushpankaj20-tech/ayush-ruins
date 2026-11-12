using UnityEngine;

/// <summary>A pillar that lights up when the player brings it a flame (optionally a specific one).</summary>
[RequireComponent(typeof(Collider))]
public class FlamePillar : MonoBehaviour
{
    public FlameObjective objective;
    [Tooltip("Optional: only this flame can light the pillar.")]
    public FlamePickup requiredFlame;
    [Tooltip("If false, lighting this pillar keeps the flame (it can still light another pillar).")]
    public bool consumesFlame = true;
    [Tooltip("Optional: this pillar can only be lit after that pillar is lit.")]
    public FlamePillar requiresLitFirst;
    [Tooltip("Fire shown on top of the pillar once lit.")]
    public GameObject fire;
    [Tooltip("Optional small marker shown while unlit (hints which flame it needs).")]
    public GameObject unlitMarker;
    [Tooltip("Optional banner shown when this pillar is lit.")]
    [TextArea] public string litMessage;
    public AudioClip lightSound;
    [Range(0f, 1f)] public float soundVolume = 0.8f;

    public bool IsLit { get; private set; }

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        if (fire != null) fire.SetActive(false);
        if (unlitMarker != null) unlitMarker.SetActive(true);
    }

    void OnTriggerEnter(Collider other) => TryLight(other, true);
    void OnTriggerStay(Collider other) => TryLight(other, false);

    void TryLight(Collider other, bool entered)
    {
        if (IsLit || objective == null || other.GetComponentInParent<PlayerController>() == null) return;
        if (!objective.CanLight(this))
        {
            if (entered) objective.PillarNeedsFlame(this);
            return;
        }
        IsLit = true; // mark lit first so the objective counts this pillar
        if (fire != null) fire.SetActive(true);
        if (unlitMarker != null) unlitMarker.SetActive(false);
        if (lightSound != null) AudioSource.PlayClipAtPoint(lightSound, transform.position, soundVolume);
        objective.TryUseFlame(this);
    }
}
