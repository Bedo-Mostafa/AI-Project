using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class ResolutionController : MonoBehaviour
{
    public TMP_Dropdown resolutionDropdown;

    // Prefer FullScreenWindow by default to avoid ExclusiveFullScreen black-screen issues on some systems.
    public bool useExclusiveFullScreenWhenMatchingMonitor = false;

    private List<Resolution> uniqueResolutions = new List<Resolution>();

    void Start()
    {
        if (resolutionDropdown == null)
        {
            Debug.LogError("ResolutionController: resolutionDropdown is not assigned.");
            return;
        }

        var all = Screen.resolutions;
        uniqueResolutions.Clear();

        for (int i = 0; i < all.Length; i++)
        {
            Resolution r = all[i];
            bool exists = uniqueResolutions.Exists(x => x.width == r.width && x.height == r.height);
            if (!exists)
                uniqueResolutions.Add(r);
        }

        var options = new List<string>();
        int currentIndex = 0;
        for (int i = 0; i < uniqueResolutions.Count; i++)
        {
            var r = uniqueResolutions[i];
            options.Add($"{r.width} x {r.height}");

            if (r.width == Screen.currentResolution.width && r.height == Screen.currentResolution.height)
            {
                currentIndex = i;
            }
        }

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(options);

        int savedIndex = PlayerPrefs.GetInt("resolution_index", currentIndex);
        if (savedIndex < 0 || savedIndex >= uniqueResolutions.Count) savedIndex = currentIndex;

        resolutionDropdown.value = savedIndex;
        resolutionDropdown.RefreshShownValue();

        resolutionDropdown.onValueChanged.RemoveAllListeners();
        resolutionDropdown.onValueChanged.AddListener(ApplyResolution);

        //ApplyResolution(savedIndex);
    }

    public void ApplyResolution(int resolutionIndex)
    {
        if (resolutionIndex < 0 || resolutionIndex >= uniqueResolutions.Count)
        {
            Debug.LogWarning($"ApplyResolution: index {resolutionIndex} out of range. Clamping.");
            resolutionIndex = Mathf.Clamp(resolutionIndex, 0, uniqueResolutions.Count - 1);
        }

        Resolution res = uniqueResolutions[resolutionIndex];

        bool matchMonitor = (res.width == Screen.currentResolution.width && res.height == Screen.currentResolution.height);

        FullScreenMode mode;
        if (matchMonitor)
        {
            
            mode = useExclusiveFullScreenWhenMatchingMonitor ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.FullScreenWindow;
        }
        else
        {
            mode = FullScreenMode.Windowed;
        }

    Screen.SetResolution(res.width, res.height, mode);

    Debug.Log($"Applying resolution {res.width}x{res.height}, mode={mode}, matchMonitor={matchMonitor}");

    // Rebuild UI after resolution change to avoid black/blank UI. This runs layout rebuilds on all canvases.
    StartCoroutine(ApplyResolutionCoroutine());

    PlayerPrefs.SetInt("resolution_index", resolutionIndex);
    PlayerPrefs.Save();

    }

    IEnumerator ApplyResolutionCoroutine()
    {
        yield return null;

        Canvas.ForceUpdateCanvases();

#if UNITY_2023_2_OR_NEWER
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
#else
        Canvas[] canvases = FindObjectsOfType<Canvas>();
#endif
        foreach (var c in canvases)
        {
            var rt = c.GetComponent<RectTransform>();
            if (rt != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }

        Debug.Log("UI layout rebuilt after resolution change.");
    }
}
