using System.Collections;
using UnityEngine;

public class OverlappingAudioLooper : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioClip clip;

    [Header("Timing")]
    [Tooltip("Time between each new play call (seconds)")]
    [SerializeField] private float playInterval = 26.18f;

    [Tooltip("Volume passed to AudioManager")]
    [SerializeField] private float volume = 1f;

    [Tooltip("Pitch passed to AudioManager")]
    [SerializeField] private float pitch = 1f;

    [Tooltip("Spatial or 2D")]
    [SerializeField] private bool spatial = false;

    [Header("World Position (used if spatial)")]
    [SerializeField] private Vector3 worldPosition = Vector3.zero;

    private AudioManager audio;
    private Coroutine loopRoutine;

    private void Start()
    {
        audio = AudioManager.Instance;

        if (audio == null)
        {
            Debug.LogError("OverlappingAudioLooper: AudioManager.Instance not found.");
            enabled = false;
            return;
        }

        if (clip == null)
        {
            Debug.LogError("OverlappingAudioLooper: AudioClip is null.");
            enabled = false;
            return;
        }

        loopRoutine = StartCoroutine(LoopRoutine());
    }

    private IEnumerator LoopRoutine()
    {
        // Play immediately
        Play();

        // Then play every 26.18 seconds forever
        while (true)
        {
            yield return new WaitForSecondsRealtime(playInterval);
            Play();
        }
    }

    private void Play()
    {
        // Use overlapping music API so layers stack without stopping previous
        audio.PlayMusic(clip, volume, false);
    }

    private void OnDisable()
    {
        if (loopRoutine != null)
        {
            StopCoroutine(loopRoutine);
            loopRoutine = null;
        }
    }
}
