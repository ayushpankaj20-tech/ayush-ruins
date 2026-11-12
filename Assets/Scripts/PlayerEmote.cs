using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Press V (or gamepad Y) to play the cheer animation, like on the main menu.</summary>
public class PlayerEmote : MonoBehaviour
{
    public PlayerController player;
    public Animator animator;

    static readonly int CheerId = Animator.StringToHash("Cheer");
    InputAction emoteAction;
    bool held;

    void Awake()
    {
        if (player == null) player = GetComponent<PlayerController>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        emoteAction = new InputAction("Emote", InputActionType.Button);
        emoteAction.AddBinding("<Keyboard>/v");
        emoteAction.AddBinding("<Gamepad>/buttonNorth");
    }

    void OnEnable() => emoteAction.Enable();
    void OnDisable() => emoteAction.Disable();
    void OnDestroy() => emoteAction.Dispose();

    /// <summary>Cheer once the player has come to a stop (used when the level is completed).</summary>
    public void PlayVictoryCheer() => StartCoroutine(VictoryCheer());

    System.Collections.IEnumerator VictoryCheer()
    {
        for (float t = 0f; t < 1.5f; t += Time.deltaTime)
        {
            if (player == null || (player.IsGrounded && player.HorizontalSpeed < 0.3f)) break;
            yield return null;
        }
        animator.SetTrigger(CheerId);
    }

    void Update()
    {
        bool down = emoteAction.IsPressed();
        if (down && !held && player != null && player.InputEnabled && player.IsGrounded)
            animator.SetTrigger(CheerId);
        held = down;
    }
}
