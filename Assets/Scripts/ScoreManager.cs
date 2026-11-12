using UnityEngine;

/// <summary>Keeps the coin score and tells listeners when it changes.</summary>
public class ScoreManager : MonoBehaviour
{
    public int Score { get; private set; }
    public int TotalCoins { get; private set; }
    public event System.Action<int> ScoreChanged;

    void OnEnable() => CoinPickup.Collected += OnCoinCollected;
    void OnDisable() => CoinPickup.Collected -= OnCoinCollected;

    void Start()
    {
        TotalCoins = 0;
        foreach (var c in FindObjectsByType<CoinPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (!c.bonusCoin) TotalCoins++;
        ScoreChanged?.Invoke(Score);
    }

    void OnCoinCollected(CoinPickup coin) => AddScore(coin.value);

    public void AddScore(int amount)
    {
        Score += amount;
        ScoreChanged?.Invoke(Score);
    }

    /// <summary>Adds newly revealed coins (e.g. an objective lane) to the total.</summary>
    public void AddToTotal(int amount)
    {
        TotalCoins += amount;
        ScoreChanged?.Invoke(Score);
    }

    public void ResetScore()
    {
        Score = 0;
        ScoreChanged?.Invoke(Score);
    }
}
