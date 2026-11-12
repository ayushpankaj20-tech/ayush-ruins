using UnityEngine;

/// <summary>A flame the player picks up and carries to a pillar. Can be locked behind a set of coins.</summary>
[RequireComponent(typeof(Collider))]
public class FlamePickup : MonoBehaviour
{
    public FlameObjective objective;
    [Tooltip("Name used in messages, e.g. \"blue flame\".")]
    public string displayName = "flame";
    [Tooltip("Corner objective shown while carrying this flame.")]
    public string carryObjective;
    [Tooltip("Visual that hides when picked up.")]
    public GameObject visual;
    [Tooltip("Optional: the flame only appears after all of these coins are collected.")]
    public CoinPickup[] requiredCoins;
    public AudioClip pickupSound;
    [Range(0f, 1f)] public float soundVolume = 0.8f;

    public bool Collected { get; private set; }
    /// <summary>True once this flame has been used to light a pillar.</summary>
    public bool Delivered { get; set; }
    public bool Unlocked { get; private set; }
    public int RequiredCollected { get; private set; }

    Collider trigger;

    void Awake()
    {
        trigger = GetComponent<Collider>();
        trigger.isTrigger = true;
        if (visual == null && transform.childCount > 0) visual = transform.GetChild(0).gameObject;
        Unlocked = requiredCoins == null || requiredCoins.Length == 0;
        SetShown(Unlocked);
    }

    void Update()
    {
        if (Unlocked) return;
        int n = 0;
        foreach (var c in requiredCoins) if (c != null && c.IsCollected) n++;
        RequiredCollected = n;
        if (n < requiredCoins.Length) return;
        Unlocked = true;
        SetShown(true);
        if (objective != null) objective.FlameUnlocked(this);
    }

    void SetShown(bool shown)
    {
        if (visual != null) visual.SetActive(shown && !Collected);
        trigger.enabled = shown && !Collected;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!Unlocked || Collected || objective == null || other.GetComponentInParent<PlayerController>() == null) return;
        Collected = true;
        SetShown(false);
        if (pickupSound != null) AudioSource.PlayClipAtPoint(pickupSound, transform.position, soundVolume);
        objective.FlamePickedUp(this);
    }

    /// <summary>Puts the flame back (used if the player dies while carrying it).</summary>
    public void ResetFlame()
    {
        Collected = false;
        Delivered = false;
        SetShown(Unlocked);
    }
}
