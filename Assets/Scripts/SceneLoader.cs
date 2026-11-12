using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Loads scenes with a quick fade to black. Use from buttons or other scripts.</summary>
public class SceneLoader : MonoBehaviour
{
    [Tooltip("Full-screen black image used for fading (optional).")]
    public Image fadeImage;
    public float fadeTime = 0.6f;

    static SceneLoader instance;
    bool loading;

    void Awake()
    {
        instance = this;
        if (fadeImage != null) StartCoroutine(FadeFromBlack());
    }

    public static void Load(string sceneName)
    {
        if (instance != null) instance.LoadScene(sceneName);
        else SceneManager.LoadScene(sceneName);
    }

    public void LoadScene(string sceneName)
    {
        if (loading) return;
        loading = true;
        StartCoroutine(LoadRoutine(sceneName));
    }

    IEnumerator LoadRoutine(string sceneName)
    {
        var music = FindFirstObjectByType<MusicPlayer>();
        if (music != null) music.FadeOut(fadeTime);
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
            {
                fadeImage.color = new Color(0f, 0f, 0f, t / fadeTime);
                yield return null;
            }
            fadeImage.color = Color.black;
        }
        SceneManager.LoadScene(sceneName);
    }

    IEnumerator FadeFromBlack()
    {
        fadeImage.gameObject.SetActive(true);
        for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
        {
            fadeImage.color = new Color(0f, 0f, 0f, 1f - t / fadeTime);
            yield return null;
        }
        fadeImage.color = Color.clear;
        fadeImage.gameObject.SetActive(false);
    }
}
