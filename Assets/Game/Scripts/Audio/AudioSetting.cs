using UnityEngine;
using UnityEngine.Audio;
[CreateAssetMenu(fileName = "AudioSetting", menuName = "Scriptable Objects/AudioSetting")]
public class AudioSetting : ScriptableObject
{
    
    [Header("Mixer + Groups")]
    public AudioMixer audioMixer;               // assign in inspector
    public AudioMixerGroup masterGroup;         // optional: for routing created AudioSources
    public AudioMixerGroup sfxGroup;
    public AudioMixerGroup musicGroup;

    [Header("Linear volumes (0..1)")]
    [Range(0f,1f)] public float masterVolume = 1f;
    [Range(0f,1f)] public float sfxVolume = 1f;
    [Range(0f,1f)] public float musicVolume = 1f;

    [Header("Mixer exposed param names")]
    public string masterParam = "Master";
    public string sfxParam = "SFX";
    public string musicParam = "Background";

    // convert 0..1 linear to decibel
    public static float LinearToDB(float linear)
    {
        linear = Mathf.Clamp(linear, 0.0001f, 1f);
        return Mathf.Log10(linear) * 20f;
    }

    public void ApplySettings()
    {
        if (audioMixer == null) return;
        audioMixer.SetFloat(masterParam, LinearToDB(masterVolume));
        audioMixer.SetFloat(sfxParam, LinearToDB(sfxVolume));
        audioMixer.SetFloat(musicParam, LinearToDB(musicVolume));
    }

    public void SaveToPrefs()
    {
        if (audioMixer == null) return;
        PlayerPrefs.SetFloat(masterParam, masterVolume);
        PlayerPrefs.SetFloat(sfxParam, sfxVolume);
        PlayerPrefs.SetFloat(musicParam, musicVolume);
        PlayerPrefs.Save();
    }

    public void LoadFromPrefs()
    {
        if (audioMixer == null) return;
        masterVolume = PlayerPrefs.GetFloat(masterParam, masterVolume);
        sfxVolume = PlayerPrefs.GetFloat(sfxParam, sfxVolume);
        musicVolume = PlayerPrefs.GetFloat(musicParam, musicVolume);
        ApplySettings();
    }
}
