using System.Collections;
using UnityEngine;

/// <summary>Collectible coin: adds score on touch, plays a burst + scale pop, then hides until the level resets.</summary>
[RequireComponent(typeof(Collider))]
public class CoinPickup : MonoBehaviour
{
    public int value = 1;
    [Tooltip("Bonus coins belong to an objective lane and are not part of the level's starting total.")]
    public bool bonusCoin;
    [Tooltip("Visual child that pops and hides (the spinning model).")]
    public Transform visual;
    [Tooltip("Particle burst spawned on pickup.")]
    public ParticleSystem pickupEffectPrefab;
    public AudioClip pickupSound;
    [Range(0f, 1f)] public float soundVolume = 0.6f;
    public float popScale = 1.5f;
    public float popDuration = 0.18f;

    public static event System.Action<CoinPickup> Collected;
    public bool IsCollected => collected;

    Collider trigger;
    Vector3 visualScale;
    bool collected;

    void Awake()
    {
        trigger = GetComponent<Collider>();
        trigger.isTrigger = true;
        if (visual == null && transform.childCount > 0) visual = transform.GetChild(0);
        if (visual != null) visualScale = visual.localScale;
    }

    void OnTriggerEnter(Collider other)
    {
        if (collected || other.GetComponentInParent<PlayerController>() == null) return;
        collected = true;
        trigger.enabled = false;
        Collected?.Invoke(this);
        if (pickupEffectPrefab != null)
        {
            var fx = Instantiate(pickupEffectPrefab, visual != null ? visual.position : transform.position, Quaternion.identity);
            Destroy(fx.gameObject, 2f);
        }
        if (pickupSound != null) AudioSource.PlayClipAtPoint(pickupSound, transform.position, soundVolume);
        StartCoroutine(Pop());
    }

    IEnumerator Pop()
    {
        if (visual == null) yield break;
        float half = popDuration * 0.35f, t = 0f;
        while (t < half) { t += Time.deltaTime; visual.localScale = visualScale * Mathf.Lerp(1f, popScale, t / half); yield return null; }
        t = 0f;
        float rest = popDuration - half;
        while (t < rest) { t += Time.deltaTime; visual.localScale = visualScale * Mathf.Lerp(popScale, 0f, t / rest); yield return null; }
        visual.gameObject.SetActive(false);
    }

    /// <summary>Brings the coin back (used when the level restarts).</summary>
    public void ResetCoin()
    {
        StopAllCoroutines();
        collected = false;
        trigger.enabled = true;
        if (visual != null) { visual.localScale = visualScale; visual.gameObject.SetActive(true); }
    }
}
