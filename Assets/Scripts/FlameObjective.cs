using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// End-of-level flame quest, split into stages that open one after another.
/// Routes are visible from the start but stay frozen (no coins, no flames, cubes still).
/// When a stage opens the screen fades out and in, then a banner explains the next step.
/// A small objective line in the top-left corner always says what to do next.
/// </summary>
public class FlameObjective : MonoBehaviour
{
    public enum StageTrigger { AllCoins, PillarsLit, FlamePicked }

    [System.Serializable]
    public class Stage
    {
        public string name;
        [Tooltip("Route that comes alive when this stage opens.")]
        public GameObject root;
        public StageTrigger trigger;
        [Tooltip("PillarsLit: open once this many pillars are lit.")]
        public int pillarsLit;
        [Tooltip("FlamePicked: open when this flame is picked up.")]
        public FlamePickup flame;
        [Tooltip("Bonus coins revealed with this stage (added to the coin total).")]
        public CoinPickup[] bonusCoins;
        [TextArea] public string revealMessage;
        [Tooltip("Short objective shown in the corner while this stage is current.")]
        public string objective;
        [Tooltip("Optional: a flame locked behind coins; while locked the corner shows lockedObjective with {0}/{1}.")]
        public FlamePickup lockedFlame;
        public string lockedObjective;
        [HideInInspector] public bool opened;
    }

    public ScoreManager scoreManager;
    public GameManager gameManager;
    public List<Stage> stages = new List<Stage>();
    public FlamePickup[] flames;
    public FlamePillar[] pillars;
    [Tooltip("Respawn point once the quest is unlocked (coins are kept).")]
    public Transform checkpoint;

    [Header("Reveal")]
    public Image fadeImage;
    public float fadeTime = 0.7f;
    public float blackHoldTime = 0.4f;

    [Header("Banner (big message)")]
    public Text bannerText;
    public float bannerTime = 9f;
    [TextArea] public string introMessage;
    [TextArea] public string completeMessage = "All pillars are lit!\nThe teleporter is open - step in to finish";

    [Header("Corner objective (small)")]
    public GameObject trackerRoot;
    public Text trackerText;
    public string coinsObjective = "Collect all the coins ({0}/{1})";
    public string finishObjective = "Step into the teleporter to finish";
    [Tooltip("How long the objective stays on screen after it changes (seconds).")]
    public float trackerDuration = 45f;
    public float trackerFadeTime = 1f;

    [Header("HUD")]
    public GameObject flamePanel;
    public Text flameCountText;
    public GameObject goalReadyEffect;

    public bool Unlocked { get; private set; }
    public bool Revealing { get; private set; }
    public bool BannerShowing { get; private set; }
    public int FlamesCarried => carried.Count;
    public int PillarsLit => pillars.Count(p => p.IsLit);
    public bool IsComplete => pillars.Length > 0 && PillarsLit == pillars.Length;
    public int StagesOpened => stages.Count(s => s.opened);

    readonly List<FlamePickup> carried = new List<FlamePickup>();
    Coroutine bannerRoutine;
    Stage current;

    void OnEnable() { if (scoreManager != null) scoreManager.ScoreChanged += OnScoreChanged; }
    void OnDisable() { if (scoreManager != null) scoreManager.ScoreChanged -= OnScoreChanged; }

    void Start()
    {
        foreach (var s in stages) { s.opened = false; SetAlive(s.root, false); }
        if (flamePanel != null) flamePanel.SetActive(false);
        if (bannerText != null) bannerText.gameObject.SetActive(false);
        if (goalReadyEffect != null) goalReadyEffect.SetActive(false);
        foreach (var f in flames) f.objective = this;
        foreach (var p in pillars) p.objective = this;
        if (gameManager != null) gameManager.Restarted += OnRestarted;
        if (!string.IsNullOrEmpty(introMessage)) ShowBanner(introMessage);
    }

    void OnDestroy() { if (gameManager != null) gameManager.Restarted -= OnRestarted; }

    /// <summary>Routes stay visible; only their coins, flames, portals and moving cubes are switched.</summary>
    static void SetAlive(GameObject root, bool alive)
    {
        if (root == null) return;
        if (!root.activeSelf) root.SetActive(true);
        foreach (var mp in root.GetComponentsInChildren<MovingPlatform>(true)) mp.enabled = alive;
        foreach (var c in root.GetComponentsInChildren<CoinPickup>(true)) c.gameObject.SetActive(alive);
        foreach (var f in root.GetComponentsInChildren<FlamePickup>(true)) f.gameObject.SetActive(alive);
        foreach (var p in root.GetComponentsInChildren<FlamePortal>(true)) p.gameObject.SetActive(alive);
    }

    void OnScoreChanged(int score)
    {
        if (Unlocked || scoreManager.TotalCoins == 0 || score < scoreManager.TotalCoins) return;
        Unlocked = true;
        if (gameManager != null)
        {
            gameManager.keepCoinsOnRespawn = true;
            if (checkpoint != null) gameManager.SetCheckpoint(checkpoint.position, checkpoint.rotation);
        }
        OpenStages(s => s.trigger == StageTrigger.AllCoins, true);
    }

    bool OpenStages(System.Func<Stage, bool> match, bool showPanel)
    {
        var toOpen = stages.Where(s => !s.opened && match(s)).ToList();
        if (toOpen.Count == 0) return false;
        foreach (var s in toOpen) s.opened = true;
        StartCoroutine(Reveal(toOpen, showPanel));
        return true;
    }

    IEnumerator Reveal(List<Stage> toOpen, bool showPanel)
    {
        Revealing = true;
        var player = gameManager != null ? gameManager.player : null;
        if (player != null) player.InputEnabled = false;
        yield return Fade(0f, 1f);
        foreach (var s in toOpen)
        {
            SetAlive(s.root, true);
            if (s.bonusCoins != null && s.bonusCoins.Length > 0) scoreManager.AddToTotal(s.bonusCoins.Length);
            current = s;
        }
        if (showPanel && flamePanel != null) flamePanel.SetActive(true);
        RefreshUI();
        yield return new WaitForSeconds(blackHoldTime);
        yield return Fade(1f, 0f);
        if (player != null && gameManager != null && !gameManager.IsDead && !gameManager.IsComplete) player.InputEnabled = true;
        Revealing = false;
        ShowBanner(string.Join("\n", toOpen.Select(s => s.revealMessage).Where(m => !string.IsNullOrEmpty(m))));
    }

    IEnumerator Fade(float from, float to)
    {
        if (fadeImage == null) yield break;
        fadeImage.gameObject.SetActive(true);
        for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
        {
            fadeImage.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, to, t / fadeTime));
            yield return null;
        }
        fadeImage.color = new Color(0f, 0f, 0f, to);
        if (to <= 0f) fadeImage.gameObject.SetActive(false);
    }

    public void FlameUnlocked(FlamePickup flame) => ShowBanner($"The {flame.displayName} has appeared!\nGo and grab it");

    public void FlamePickedUp(FlamePickup flame)
    {
        if (!carried.Contains(flame)) carried.Add(flame);
        RefreshUI();
        if (OpenStages(s => s.trigger == StageTrigger.FlamePicked && s.flame == flame, false)) return;
        ShowBanner($"You found the {flame.displayName}!\n{CarryObjective(flame)}");
    }

    string CarryObjective(FlamePickup f) => string.IsNullOrEmpty(f.carryObjective) ? $"Bring the {f.displayName} to its pillar" : f.carryObjective;

    public bool CanLight(FlamePillar pillar)
    {
        if (carried.Count == 0) return false;
        if (pillar.requiresLitFirst != null && !pillar.requiresLitFirst.IsLit) return false;
        return pillar.requiredFlame == null || carried.Contains(pillar.requiredFlame);
    }

    public void PillarNeedsFlame(FlamePillar pillar)
    {
        if (carried.Count == 0) return;
        if (pillar.requiresLitFirst != null && !pillar.requiresLitFirst.IsLit) ShowBanner("Light the other pillar first");
        else if (pillar.requiredFlame != null) ShowBanner($"This pillar needs the {pillar.requiredFlame.displayName}");
    }

    public bool TryUseFlame(FlamePillar pillar)
    {
        if (carried.Count == 0) return false;
        var flame = pillar.requiredFlame != null && carried.Contains(pillar.requiredFlame) ? pillar.requiredFlame : carried[0];
        if (pillar.consumesFlame) { carried.Remove(flame); flame.Delivered = true; }
        RefreshUI();
        if (IsComplete)
        {
            if (goalReadyEffect != null) goalReadyEffect.SetActive(true);
            ShowBanner(completeMessage);
            return true;
        }
        int lit = PillarsLit;
        if (OpenStages(s => s.trigger == StageTrigger.PillarsLit && lit >= s.pillarsLit, false)) return true;
        if (!string.IsNullOrEmpty(pillar.litMessage)) ShowBanner(pillar.litMessage);
        else ShowBanner(pillars.Length - lit == 1 ? "Pillar lit! One more to go" : $"Pillar lit! {pillars.Length - lit} more to go");
        return true;
    }

    /// <summary>Called by the goal when the player tries to finish early.</summary>
    public void ShowLockedHint()
    {
        if (!Unlocked) ShowBanner("Collect every coin to unlock the final quest");
        else ShowBanner("Light all the pillars to open the teleporter\n" + CurrentObjective());
    }

    void OnRestarted()
    {
        foreach (var f in carried) f.ResetFlame();
        carried.Clear();
        RefreshUI();
    }

    void RefreshUI()
    {
        if (flameCountText != null) flameCountText.text = $"{PillarsLit} / {pillars.Length}" + (carried.Count > 0 ? $"  (+{carried.Count})" : "");
    }

    /// <summary>The short "what to do now" line shown in the corner.</summary>
    public string CurrentObjective()
    {
        if (!Unlocked) return string.Format(coinsObjective, scoreManager.Score, scoreManager.TotalCoins);
        if (IsComplete) return finishObjective;
        bool stageHasFlameToFind = current != null && current.root != null &&
            current.root.GetComponentsInChildren<FlamePickup>().Any(f => !f.Collected);
        if (current != null && (stageHasFlameToFind || carried.Count == 0))
        {
            if (current.lockedFlame != null && !current.lockedFlame.Unlocked && !string.IsNullOrEmpty(current.lockedObjective))
                return string.Format(current.lockedObjective, current.lockedFlame.RequiredCollected, current.lockedFlame.requiredCoins.Length);
            return current.objective;
        }
        if (carried.Count > 0) return CarryObjective(carried[carried.Count - 1]);
        return "";
    }

    string lastObjective;
    float objectiveChangedAt = -999f;
    CanvasGroup trackerGroup;

    void Update()
    {
        if (trackerRoot == null) return;
        string text = CurrentObjective();
        if (text != lastObjective) { lastObjective = text; objectiveChangedAt = Time.time; }
        if (trackerText != null) trackerText.text = text;
        if (trackerGroup == null)
        {
            trackerGroup = trackerRoot.GetComponent<CanvasGroup>();
            if (trackerGroup == null) trackerGroup = trackerRoot.AddComponent<CanvasGroup>();
        }
        bool blocked = BannerShowing || Revealing || string.IsNullOrEmpty(text) ||
                       (gameManager != null && (gameManager.IsDead || gameManager.IsComplete));
        // the timer starts once the objective can actually be seen
        if (blocked) objectiveChangedAt = Mathf.Max(objectiveChangedAt, Time.time);
        float age = Time.time - objectiveChangedAt;
        float alpha = blocked ? 0f : Mathf.Clamp01((trackerDuration + trackerFadeTime - age) / trackerFadeTime);
        trackerGroup.alpha = alpha;
        if (trackerRoot.activeSelf != alpha > 0f) trackerRoot.SetActive(alpha > 0f);
    }

    void ShowBanner(string message)
    {
        if (bannerText == null || string.IsNullOrEmpty(message)) return;
        if (bannerRoutine != null) StopCoroutine(bannerRoutine);
        bannerRoutine = StartCoroutine(Banner(message));
    }

    IEnumerator Banner(string message)
    {
        BannerShowing = true;
        bannerText.text = message;
        bannerText.gameObject.SetActive(true);
        yield return new WaitForSeconds(bannerTime);
        bannerText.gameObject.SetActive(false);
        BannerShowing = false;
    }
}
