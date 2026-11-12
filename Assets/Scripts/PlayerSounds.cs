using UnityEngine;

/// <summary>Plays the player's jump and landing sounds.</summary>
[RequireComponent(typeof(AudioSource))]
public class PlayerSounds : MonoBehaviour
{
    public PlayerController player;
    public AudioClip jumpSound;
    [Range(0f, 1f)] public float jumpVolume = 0.6f;
    public AudioClip landSound;
    [Range(0f, 1f)] public float landVolume = 0.45f;
    [Tooltip("Only play the landing sound after falling at least this fast.")]
    public float minLandSpeed = 6f;

    AudioSource source;
    bool wasGrounded = true;
    float lastFallSpeed;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        if (player == null) player = GetComponent<PlayerController>();
    }

    void OnEnable() { if (player != null) player.Jumped += OnJumped; }
    void OnDisable() { if (player != null) player.Jumped -= OnJumped; }

    void OnJumped()
    {
        if (jumpSound != null) source.PlayOneShot(jumpSound, jumpVolume);
    }

    void Update()
    {
        if (player == null) return;
        if (!player.IsGrounded) lastFallSpeed = Mathf.Max(lastFallSpeed, -player.Velocity.y);
        if (player.IsGrounded && !wasGrounded && lastFallSpeed >= minLandSpeed && landSound != null)
            source.PlayOneShot(landSound, landVolume);
        if (player.IsGrounded) lastFallSpeed = 0f;
        wasGrounded = player.IsGrounded;
    }
}
