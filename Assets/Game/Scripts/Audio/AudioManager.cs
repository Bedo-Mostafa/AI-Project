using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class AudioManager : MonoBehaviour
{
    [Header("Settings")]
    public AudioSetting settings;

    [Header("SFX Pool")]
    public int poolSize = 16;
    [Tooltip("Allow the pool to grow beyond poolSize when all sources are busy.")]
    public bool allowPoolExpansion = true;
    [Tooltip("Absolute maximum number of SFX sources when expanding.")]
    public int maxPoolSize = 64;

    private Queue<AudioSource> sfxPool = new Queue<AudioSource>();
    private List<AudioSource> allSfxSources = new List<AudioSource>();

    [Header("Music")]
    [SerializeField] private AudioSource musicSourcePrefab;
    //singlton
        [Header("Singleton")]
    [Tooltip("When true this AudioManager will persist across scene loads.")]
    public bool persistAcrossScenes = true;
    public static AudioManager Instance { get; private set; }
    private AudioSource musicSourceA;
    private AudioSource musicSourceB;
    private AudioSource activeMusicSource;
    private AudioSource inactiveMusicSource;
    private Coroutine musicCrossfadeCoroutine;

    // Pool for overlapping background music sources
    private List<AudioSource> overlappingMusicSources = new List<AudioSource>();

    private readonly List<Coroutine> runningCoroutines = new List<Coroutine>();

    private void Awake()
    {
        // Singleton enforcement
        if (Instance != null && Instance != this)
        {
            // There is already an instance — destroy this duplicate GameObject
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Persist across scene loads if requested
        if (persistAcrossScenes)
        {
            DontDestroyOnLoad(gameObject);
        }

        // Your existing init
        if (settings == null)
        {
            Debug.LogWarning("AudioManager: settings null (expected an AudioSetting reference).");
        }
        else
        {
            settings.LoadFromPrefs();
            settings.ApplySettings();
        }

        InitializePool();
        CreateMusicSources();

        // Ensure background music from previous scenes is stopped/removed when a new scene loads
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        StopAllRunningCoroutines();
    }

        private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

            SceneManager.sceneLoaded -= OnSceneLoaded;
            StopAllRunningCoroutines();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Stop all running coroutines (SFX / overlapping music playback)
        StopAllRunningCoroutines();

        // Stop crossfade music
        if (musicCrossfadeCoroutine != null)
        {
            StopCoroutine(musicCrossfadeCoroutine);
            musicCrossfadeCoroutine = null;
        }

        // Stop main music sources
        if (activeMusicSource != null)
        {
            activeMusicSource.Stop();
            activeMusicSource.clip = null;
        }

        if (inactiveMusicSource != null)
        {
            inactiveMusicSource.Stop();
            inactiveMusicSource.clip = null;
        }

        // Stop all overlapping music
        StopAllOverlappingMusic();

        // Stop all SFX and return them to the pool
        StopAllSFX();
    }

    private void StopAllRunningCoroutines()
    {
        foreach (var c in runningCoroutines)
        {
            if (c != null) StopCoroutine(c);
        }

        runningCoroutines.Clear();
        if (musicCrossfadeCoroutine != null)
        {
            StopCoroutine(musicCrossfadeCoroutine);
            musicCrossfadeCoroutine = null;
        }
    }

    #region Pool
    void InitializePool()
    {
        sfxPool.Clear();
        allSfxSources.Clear();

        for (int i = 0; i < Mathf.Max(0, poolSize); i++)
        {
            var a = CreateSfxSource($"SFX_{i}");
            sfxPool.Enqueue(a);
            allSfxSources.Add(a);
        }
    }

    private AudioSource CreateSfxSource(string name = "SFX")
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var a = go.AddComponent<AudioSource>();
        a.playOnAwake = false;
        a.spatialBlend = 1f;
        a.loop = false;
        if (settings != null && settings.sfxGroup != null) a.outputAudioMixerGroup = settings.sfxGroup;
        go.SetActive(false);
        return a;
    }

    public void PrewarmPool(int newDesiredSize)
    {
        if (newDesiredSize <= allSfxSources.Count) return;
        int toCreate = newDesiredSize - allSfxSources.Count;
        for (int i = 0; i < toCreate; i++)
        {
            var a = CreateSfxSource($"SFX_prewarm_{allSfxSources.Count + i}");
            sfxPool.Enqueue(a);
            allSfxSources.Add(a);
        }
    }

    private AudioSource CreateAndRegisterExtra()
    {
        var a = CreateSfxSource($"SFX_extra_{allSfxSources.Count}");
        allSfxSources.Add(a);
        return a;
    }

    private AudioSource GetPooledSource()
    {
        if (sfxPool.Count > 0)
        {
            var a = sfxPool.Dequeue();
            if (a == null)
            {
                allSfxSources.RemoveAll(x => x == null);
                return GetPooledSource();
            }
            a.gameObject.SetActive(true);
            return a;
        }

        if (allowPoolExpansion && allSfxSources.Count < maxPoolSize)
        {
            var extra = CreateAndRegisterExtra();
            extra.gameObject.SetActive(true);
            return extra;
        }

        AudioSource best = null;
        float bestRemaining = float.MaxValue;
        foreach (var s in allSfxSources)
        {
            if (s == null) continue;
            if (s.isPlaying)
            {
                float rem = Mathf.Max(0f, (s.clip != null ? s.clip.length / Mathf.Abs(s.pitch) - s.time : 0f));
                if (rem < bestRemaining)
                {
                    bestRemaining = rem;
                    best = s;
                }
            }
            else
            {
                best = s;
                break;
            }
        }

        if (best != null)
        {
            best.Stop();
            ResetSourceProperties(best);
            best.gameObject.SetActive(true);
            return best;
        }

        var fallback = CreateAndRegisterExtra();
        fallback.gameObject.SetActive(true);
        return fallback;
    }

    private void ReleasePooledSource(AudioSource a)
    {
        if (a == null) return;
        a.clip = null;
        ResetSourceProperties(a);
        a.gameObject.SetActive(false);
        if (!sfxPool.Contains(a))
            sfxPool.Enqueue(a);
    }

    private void ResetSourceProperties(AudioSource a)
    {
        if (a == null) return;
        a.volume = 1f;
        a.pitch = 1f;
        a.spatialBlend = 1f;
        a.mute = false;
        a.loop = false;
    }

    public void StopAllSFX()
    {
        foreach (var s in allSfxSources)
        {
            if (s == null) continue;
            s.Stop();
            ReleasePooledSource(s);
        }
    }

    public void SetAllowPoolExpansion(bool allow, int max = 64)
    {
        allowPoolExpansion = allow;
        maxPoolSize = max;
    }
    #endregion

    #region Music
    void CreateMusicSources()
    {
        if (musicSourcePrefab != null)
        {
            musicSourceA = Instantiate(musicSourcePrefab, transform);
            musicSourceB = Instantiate(musicSourcePrefab, transform);
        }
        else
        {
            musicSourceA = new GameObject("MusicSourceA").AddComponent<AudioSource>();
            musicSourceA.transform.SetParent(transform, false);
            musicSourceA.playOnAwake = false;
            musicSourceA.loop = true;
            musicSourceA.spatialBlend = 0f;

            musicSourceB = new GameObject("MusicSourceB").AddComponent<AudioSource>();
            musicSourceB.transform.SetParent(transform, false);
            musicSourceB.playOnAwake = false;
            musicSourceB.loop = true;
            musicSourceB.spatialBlend = 0f;
        }

        if (settings != null && settings.musicGroup != null)
        {
            musicSourceA.outputAudioMixerGroup = settings.musicGroup;
            musicSourceB.outputAudioMixerGroup = settings.musicGroup;
        }

        activeMusicSource = musicSourceA;
        inactiveMusicSource = musicSourceB;
    }

    public void PlayMusic(AudioClip clip, float crossfadeTime = 0.5f, bool loop = true)
    {
        if (clip == null) return;

        if (musicCrossfadeCoroutine != null) StopCoroutine(musicCrossfadeCoroutine);
        musicCrossfadeCoroutine = StartCoroutine(CrossfadeMusicDualSource(clip, crossfadeTime, loop));
    }

    public void StopMusic(float fadeOut = 0.5f)
    {
        if (musicCrossfadeCoroutine != null) StopCoroutine(musicCrossfadeCoroutine);
        musicCrossfadeCoroutine = StartCoroutine(FadeOutActiveMusic(fadeOut));
    }

    private IEnumerator CrossfadeMusicDualSource(AudioClip newClip, float time, bool loop)
    {
        float fadeTime = Mathf.Max(0.01f, time);

        inactiveMusicSource.clip = newClip;
        inactiveMusicSource.loop = loop;
        inactiveMusicSource.volume = 0f;
        inactiveMusicSource.Play();

        float t = 0f;
        float startActiveVol = activeMusicSource != null ? activeMusicSource.volume : 1f;
        while (t < fadeTime)
        {
            float k = t / fadeTime;
            if (activeMusicSource != null) activeMusicSource.volume = Mathf.Lerp(startActiveVol, 0f, k);
            inactiveMusicSource.volume = Mathf.Lerp(0f, 1f, k);
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (activeMusicSource != null)
        {
            activeMusicSource.volume = 0f;
            activeMusicSource.Stop();
            activeMusicSource.clip = null;
        }

        inactiveMusicSource.volume = 1f;

        var tmp = activeMusicSource;
        activeMusicSource = inactiveMusicSource;
        inactiveMusicSource = tmp;

        musicCrossfadeCoroutine = null;
    }

    private IEnumerator FadeOutActiveMusic(float time)
    {
        float fadeTime = Mathf.Max(0.01f, time);
        float t = 0f;
        float startVol = activeMusicSource != null ? activeMusicSource.volume : 1f;
        while (t < fadeTime)
        {
            float k = t / fadeTime;
            if (activeMusicSource != null) activeMusicSource.volume = Mathf.Lerp(startVol, 0f, k);
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (activeMusicSource != null)
        {
            activeMusicSource.Stop();
            activeMusicSource.clip = null;
        }

        musicCrossfadeCoroutine = null;
    }

    /// <summary>
    /// Plays a music clip that overlaps with any currently playing music.
    /// Each call creates a new source; sources auto-clean after playback.
    /// </summary>
    public void PlayMusicOverlapping(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null) return;
        var c = StartCoroutine(PlayMusicOverlappingRoutine(clip, volume, pitch));
        runningCoroutines.Add(c);
    }

    private IEnumerator PlayMusicOverlappingRoutine(AudioClip clip, float volume, float pitch)
    {
        var go = new GameObject("OverlappingMusic");
        go.transform.SetParent(transform, false);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = false;
        src.spatialBlend = 0f;
        if (settings != null && settings.musicGroup != null)
            src.outputAudioMixerGroup = settings.musicGroup;

        float effectiveVol = Mathf.Clamp01(volume * (settings != null ? settings.musicVolume * settings.masterVolume : 1f));
        src.volume = effectiveVol;
        src.pitch = pitch;
        src.clip = clip;
        src.Play();

        overlappingMusicSources.Add(src);

        float duration = clip.length / Mathf.Max(0.0001f, Mathf.Abs(pitch));
        yield return new WaitForSecondsRealtime(duration);

        overlappingMusicSources.Remove(src);
        if (src != null)
        {
            src.Stop();
            Destroy(go);
        }

        runningCoroutines.RemoveAll(r => r == null);
    }

    /// <summary>
    /// Stops and destroys all overlapping music sources immediately.
    /// </summary>
    public void StopAllOverlappingMusic()
    {
        foreach (var src in overlappingMusicSources)
        {
            if (src != null)
            {
                src.Stop();
                Destroy(src.gameObject);
            }
        }
        overlappingMusicSources.Clear();
    }
    #endregion

    #region Public SFX API
    public void PlaySFX(AudioClip clip, Vector3 worldPos, float volume = 1f, float pitch = 1f, bool spatial = true)
    {
        if (clip == null) return;
        var c = StartCoroutine(PlaySFXRoutine(clip, worldPos, volume, pitch, spatial));
        runningCoroutines.Add(c);
    }

    private IEnumerator PlaySFXRoutine(AudioClip clip, Vector3 pos, float volume, float pitch, bool spatial)
    {
        AudioSource src = GetPooledSource();
        src.transform.position = pos;
        src.spatialBlend = spatial ? 1f : 0f;
        src.clip = clip;
        float effectiveVol = Mathf.Clamp01(volume * (settings != null ? settings.sfxVolume * settings.masterVolume : 1f));
        src.volume = effectiveVol;
        src.pitch = pitch;
        src.loop = false;
        src.Play();

        float expected = clip.length / Mathf.Max(0.0001f, Mathf.Abs(pitch));
        yield return new WaitForSeconds(expected);

        if (src.isPlaying) src.Stop();
        ReleasePooledSource(src);

        runningCoroutines.RemoveAll(r => r == null);
    }

    public void PlaySFXOnSource(AudioSource source, AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (source == null || clip == null) return;
        if (settings != null && settings.sfxGroup != null)
            source.outputAudioMixerGroup = settings.sfxGroup;

        source.clip = clip;
        float effectiveVol = Mathf.Clamp01(volume * (settings != null ? settings.sfxVolume * settings.masterVolume : 1f));
        source.volume = effectiveVol;
        source.pitch = pitch;
        source.loop = false;
        source.Play();
    }
    #endregion

    #region Settings
    public void ApplySettings()
    {
        if (settings == null) return;
        settings.ApplySettings();

        if (settings.sfxGroup != null)
        {
            foreach (var s in allSfxSources)
            {
                if (s == null) continue;
                if (s == musicSourceA || s == musicSourceB) continue;
                s.outputAudioMixerGroup = settings.sfxGroup;
            }
        }

        if (settings.musicGroup != null)
        {
            if (musicSourceA != null) musicSourceA.outputAudioMixerGroup = settings.musicGroup;
            if (musicSourceB != null) musicSourceB.outputAudioMixerGroup = settings.musicGroup;
        }
    }
    #endregion
}
