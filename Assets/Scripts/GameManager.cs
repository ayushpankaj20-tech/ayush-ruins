using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Handles death and restarting: shows "You Died", then resets player, score and coins.</summary>
public class GameManager : MonoBehaviour
{
    public PlayerController player;
    public PlayerAnimation playerAnimation;
    public ThirdPersonCamera followCamera;
    public ScoreManager scoreManager;
    [Tooltip("Where the player respawns. Defaults to the player's start position.")]
    public Transform spawnPoint;
    [Tooltip("Shown while dead.")]
    public GameObject deathScreen;
    [Tooltip("Shown when the player reaches the goal.")]
    public GameObject levelCompleteScreen;
    [Tooltip("Optional text that shows how many coins were collected.")]
    public Text levelCompleteCoinsText;
    [Tooltip("Scene loaded after the level is completed. Leave empty to replay this level.")]
    public string nextScene = "";
    [Tooltip("Seconds the Level Complete screen stays before moving on.")]
    public float levelCompleteDelay = 4f;
    [Tooltip("Seconds before restarting automatically.")]
    public float restartDelay = 2f;
    [Tooltip("Wait this long before a key press can skip the delay.")]
    public float minDeathTime = 0.4f;

    [Tooltip("When true, dying keeps collected coins (used after a checkpoint).")]
    public bool keepCoinsOnRespawn;
    /// <summary>Raised after the player respawns.</summary>
    public event System.Action Restarted;

    public bool IsDead { get; private set; }
    public bool IsComplete { get; private set; }

    Vector3 spawnPos;
    Quaternion spawnRot;
    CoinPickup[] coins;

    void Start()
    {
        spawnPos = spawnPoint ? spawnPoint.position : player.transform.position;
        spawnRot = spawnPoint ? spawnPoint.rotation : player.transform.rotation;
        coins = FindObjectsByType<CoinPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (deathScreen != null) deathScreen.SetActive(false);
        if (levelCompleteScreen != null) levelCompleteScreen.SetActive(false);
    }

    /// <summary>Moves the respawn point (e.g. to a checkpoint).</summary>
    public void SetCheckpoint(Vector3 position, Quaternion rotation)
    {
        spawnPos = position;
        spawnRot = rotation;
    }

    public void PlayerDied()
    {
        if (IsDead || IsComplete) return;
        IsDead = true;
        player.InputEnabled = false;
        if (deathScreen != null) deathScreen.SetActive(true);
        StartCoroutine(RestartRoutine(restartDelay));
    }

    /// <summary>Called by the goal. Returns true the first time the level is completed.</summary>
    public bool LevelComplete()
    {
        if (IsDead || IsComplete) return false;
        IsComplete = true;
        player.InputEnabled = false;
        if (levelCompleteCoinsText != null)
            levelCompleteCoinsText.text = $"Coins: {scoreManager.Score} / {scoreManager.TotalCoins}";
        if (levelCompleteScreen != null) levelCompleteScreen.SetActive(true);
        var emote = player.GetComponent<PlayerEmote>();
        if (emote != null) emote.PlayVictoryCheer();
        StartCoroutine(RestartRoutine(levelCompleteDelay));
        return true;
    }

    IEnumerator RestartRoutine(float delay)
    {
        float t = 0f;
        while (t < delay)
        {
            t += Time.deltaTime;
            var kb = Keyboard.current;
            bool skip = t > minDeathTime && ((kb != null && (kb.rKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                        || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame));
            if (skip) break;
            yield return null;
        }
        if (IsComplete && !string.IsNullOrEmpty(nextScene)) { SceneLoader.Load(nextScene); yield break; }
        RestartLevel();
    }

    public void RestartLevel()
    {
        player.Teleport(spawnPos, spawnRot);
        if (playerAnimation != null) playerAnimation.ResetState();
        if (!keepCoinsOnRespawn)
        {
            foreach (var c in coins) c.ResetCoin();
            scoreManager.ResetScore();
        }
        if (followCamera != null) followCamera.SnapToTarget();
        if (deathScreen != null) deathScreen.SetActive(false);
        if (levelCompleteScreen != null) levelCompleteScreen.SetActive(false);
        player.InputEnabled = true;
        IsDead = false;
        IsComplete = false;
        Restarted?.Invoke();
    }
}
