using UnityEngine;

/// <summary>Trigger on the goal teleporter: finishes the level when the player steps in.</summary>
[RequireComponent(typeof(Collider))]
public class GoalZone : MonoBehaviour
{
    public GameManager gameManager;
    [Tooltip("Optional: the goal stays locked until this objective is complete.")]
    public FlameObjective requiredObjective;
    public AudioClip reachedSound;
    [Range(0f, 1f)] public float soundVolume = 0.7f;

    void Awake() => GetComponent<Collider>().isTrigger = true;

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerController>() == null || gameManager == null) return;
        if (requiredObjective != null && !requiredObjective.IsComplete) { requiredObjective.ShowLockedHint(); return; }
        if (gameManager.LevelComplete() && reachedSound != null)
            AudioSource.PlayClipAtPoint(reachedSound, transform.position, soundVolume);
    }
}
