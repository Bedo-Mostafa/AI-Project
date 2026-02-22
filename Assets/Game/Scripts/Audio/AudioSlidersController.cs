using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class AudioSlidersController : MonoBehaviour
{
    [Header("Settings Reference")]
    [Tooltip("Reference to your AudioSetting ScriptableObject")]
    public AudioSetting settings;

    [Header("UI Sliders (0..1)")]
    public Slider masterSlider;
    public Slider sfxSlider;
    public Slider musicSlider; // background music slider

    [Header("Optional Value Displays")]
    public TextMeshProUGUI masterValueText;
    public TextMeshProUGUI sfxValueText;
    public TextMeshProUGUI musicValueText;

    [Header("Auto save")]
    [Tooltip("If true, settings are written to PlayerPrefs on each change. If false, call SaveSettings() manually.")]
    public bool saveImmediately = true;

    private void Awake()
    {
        if (settings == null)
        {
            Debug.LogError("AudioSlidersController: 'settings' is not assigned.");
            enabled = false;
            return;
        }

        // Load persisted values into the ScriptableObject and mixer
        settings.LoadFromPrefs();
        settings.ApplySettings();

        // Ensure sliders exist (not required to have text displays)
        if (masterSlider == null || sfxSlider == null || musicSlider == null)
        {
            Debug.LogError("AudioSlidersController: One or more sliders are not assigned.");
            enabled = false;
            return;
        }

        // Setup slider ranges (expected 0..1)
        masterSlider.minValue = 0f; masterSlider.maxValue = 1f;
        sfxSlider.minValue = 0f; sfxSlider.maxValue = 1f;
        musicSlider.minValue = 0f; musicSlider.maxValue = 1f;

        // Initialize UI to current settings
        InitializeSlidersFromSettings();
    }

    private void OnEnable()
    {
        masterSlider.onValueChanged.AddListener(OnMasterChanged);
        sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        musicSlider.onValueChanged.AddListener(OnMusicChanged);
    }

    private void OnDisable()
    {
        masterSlider.onValueChanged.RemoveListener(OnMasterChanged);
        sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
        musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
    }

    private void InitializeSlidersFromSettings()
    {
        // settings.LoadFromPrefs() already called in Awake, but double-check values are consistent
        masterSlider.SetValueWithoutNotify(settings.masterVolume);
        sfxSlider.SetValueWithoutNotify(settings.sfxVolume);
        musicSlider.SetValueWithoutNotify(settings.musicVolume);

        UpdateValueText(masterValueText, settings.masterVolume);
        UpdateValueText(sfxValueText, settings.sfxVolume);
        UpdateValueText(musicValueText, settings.musicVolume);
    }

    private void OnMasterChanged(float value)
    {
        settings.masterVolume = Mathf.Clamp01(value);
        settings.ApplySettings();
        UpdateValueText(masterValueText, value);
        if (saveImmediately) settings.SaveToPrefs();
    }

    private void OnSfxChanged(float value)
    {
        settings.sfxVolume = Mathf.Clamp01(value);
        settings.ApplySettings();
        UpdateValueText(sfxValueText, value);
        if (saveImmediately) settings.SaveToPrefs();
    }

    private void OnMusicChanged(float value)
    {
        settings.musicVolume = Mathf.Clamp01(value);
        settings.ApplySettings();
        UpdateValueText(musicValueText, value);
        if (saveImmediately) settings.SaveToPrefs();
    }

    /// <summary>
    /// Call this to force-save current settings to PlayerPrefs (useful if saveImmediately=false).
    /// </summary>
    public void SaveSettings()
    {
        settings.SaveToPrefs();
    }

    /// <summary>
    /// Reset sliders and settings to provided defaults (0..1). Applies and optionally saves.
    /// </summary>
    public void ResetToDefaults(float master = 1f, float sfx = 1f, float music = 1f, bool save = true)
    {
        settings.masterVolume = Mathf.Clamp01(master);
        settings.sfxVolume = Mathf.Clamp01(sfx);
        settings.musicVolume = Mathf.Clamp01(music);

        settings.ApplySettings();

        masterSlider.SetValueWithoutNotify(settings.masterVolume);
        sfxSlider.SetValueWithoutNotify(settings.sfxVolume);
        musicSlider.SetValueWithoutNotify(settings.musicVolume);

        UpdateValueText(masterValueText, settings.masterVolume);
        UpdateValueText(sfxValueText, settings.sfxVolume);
        UpdateValueText(musicValueText, settings.musicVolume);

        if (save && saveImmediately) settings.SaveToPrefs();
        else if (save) settings.SaveToPrefs();
    }

    private void UpdateValueText(TextMeshProUGUI text, float normalized)
    {
        if (text == null) return;
        int percent = Mathf.RoundToInt(normalized * 100f);
        text.text = percent + "%";
    }
}
