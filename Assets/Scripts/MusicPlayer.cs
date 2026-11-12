using System.Collections;
using UnityEngine;

/// <summary>
/// Plays a looping background track with no gap: silence at the start/end of the file is
/// trimmed automatically and the end crossfades into the beginning.
/// </summary>
public class MusicPlayer : MonoBehaviour
{
    public AudioClip music;
    [Range(0f, 1f)] public float volume = 0.4f;
    public float fadeInTime = 2f;
    [Tooltip("Seconds of overlap when the track loops back to its start.")]
    public float loopCrossfade = 1.5f;
    [Tooltip("Samples quieter than this count as silence when trimming.")]
    public float silenceThreshold = 0.004f;

    AudioSource a, b;
    float loopStart, loopEnd;
    float master;          // fade-in / fade-out multiplier
    Coroutine masterFade;

    void Awake()
    {
        a = GetComponent<AudioSource>();
        if (a == null) a = gameObject.AddComponent<AudioSource>();
        b = gameObject.AddComponent<AudioSource>();
        foreach (var s in new[] { a, b })
        {
            s.clip = music; s.loop = false; s.playOnAwake = false; s.spatialBlend = 0f; s.volume = 0f;
        }
    }

    void Start()
    {
        if (music == null) return;
        FindLoopPoints();
        a.time = loopStart;
        a.Play();
        masterFade = StartCoroutine(FadeMaster(volume, fadeInTime));
        StartCoroutine(LoopRoutine());
    }

    void FindLoopPoints()
    {
        loopStart = 0f; loopEnd = music.length;
        if (music.loadState != AudioDataLoadState.Loaded) music.LoadAudioData();
        int ch = Mathf.Max(1, music.channels), total = music.samples;
        var data = new float[Mathf.Min(total, music.frequency * 4) * ch];
        // leading silence
        if (music.GetData(data, 0))
        {
            for (int i = 0; i < data.Length; i++) if (Mathf.Abs(data[i]) > silenceThreshold) { loopStart = (i / ch) / (float)music.frequency; break; }
        }
        // trailing silence
        int tailStart = Mathf.Max(0, total - data.Length / ch);
        if (music.GetData(data, tailStart))
        {
            for (int i = data.Length - 1; i >= 0; i--) if (Mathf.Abs(data[i]) > silenceThreshold) { loopEnd = (tailStart + i / ch) / (float)music.frequency; break; }
        }
        if (loopEnd - loopStart < loopCrossfade * 3f) { loopStart = 0f; loopEnd = music.length; }
    }

    IEnumerator LoopRoutine()
    {
        var current = a; var next = b;
        float partWeight = 1f;
        while (true)
        {
            // wait until we're one crossfade away from the end of the audible part
            while (current.isPlaying && current.time < loopEnd - loopCrossfade)
            {
                current.volume = master * partWeight;
                yield return null;
            }
            next.time = loopStart;
            next.Play();
            for (float t = 0f; t < loopCrossfade; t += Time.unscaledDeltaTime)
            {
                float k = t / loopCrossfade;
                current.volume = master * Mathf.Cos(k * Mathf.PI * 0.5f);
                next.volume = master * Mathf.Sin(k * Mathf.PI * 0.5f);
                yield return null;
            }
            current.Stop();
            var tmp = current; current = next; next = tmp;
        }
    }

    /// <summary>Fades the music out (e.g. before loading another scene).</summary>
    public void FadeOut(float time)
    {
        if (masterFade != null) StopCoroutine(masterFade);
        masterFade = StartCoroutine(FadeMaster(0f, time));
    }

    IEnumerator FadeMaster(float target, float time)
    {
        float start = master;
        for (float t = 0f; t < time; t += Time.unscaledDeltaTime)
        {
            master = Mathf.Lerp(start, target, t / time);
            yield return null;
        }
        master = target;
    }
}
