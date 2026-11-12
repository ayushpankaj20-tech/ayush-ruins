using UnityEngine;
using UnityEngine.UI;

/// <summary>Shows the score as "coins / total" with a little pop when it goes up.</summary>
public class ScoreUI : MonoBehaviour
{
    public ScoreManager scoreManager;
    public Text scoreText;
    [Tooltip("Element that pops when a coin is collected (e.g. the coin icon).")]
    public RectTransform popTarget;
    public float popScale = 1.35f;
    public float popReturnSpeed = 8f;
    public bool showTotal = true;

    int lastScore;

    void OnEnable() { if (scoreManager != null) scoreManager.ScoreChanged += Refresh; }
    void OnDisable() { if (scoreManager != null) scoreManager.ScoreChanged -= Refresh; }

    void Refresh(int score)
    {
        if (scoreText != null)
            scoreText.text = showTotal && scoreManager.TotalCoins > 0 ? $"{score} / {scoreManager.TotalCoins}" : score.ToString();
        if (score > lastScore && popTarget != null) popTarget.localScale = Vector3.one * popScale;
        lastScore = score;
    }

    void Update()
    {
        if (popTarget != null)
            popTarget.localScale = Vector3.Lerp(popTarget.localScale, Vector3.one, popReturnSpeed * Time.unscaledDeltaTime);
    }
}
