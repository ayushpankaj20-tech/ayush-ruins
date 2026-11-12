using UnityEngine;

/// <summary>Main menu buttons: Start loads the first level, Quit exits the game.</summary>
public class MainMenu : MonoBehaviour
{
    [Tooltip("Scene loaded by the Start button.")]
    public string firstLevel = "Level01";
    [Tooltip("Panel listing the controls.")]
    public GameObject controlsPanel;
    [Tooltip("Main buttons, hidden while the controls are shown.")]
    public GameObject mainButtons;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        ShowControls(false);
    }

    public void StartGame() => SceneLoader.Load(firstLevel);

    public void ShowControls(bool show)
    {
        if (controlsPanel != null) controlsPanel.SetActive(show);
        if (mainButtons != null) mainButtons.SetActive(!show);
    }

    public void OpenControls() => ShowControls(true);
    public void CloseControls() => ShowControls(false);

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
