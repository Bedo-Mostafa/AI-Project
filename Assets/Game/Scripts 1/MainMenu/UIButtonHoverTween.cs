using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Button))]
public class UIButtonHoverTween : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler
{
    [Header("Scale")]
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float duration = 0.2f;

    [Header("Colors")]
    [SerializeField] private Color hoverColor = Color.yellow;
    [Header("Audio")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;
    private RectTransform rect;
    private TextMeshProUGUI tmpText;
    private Button button;

    private Vector3 originalScale;
    private Color originalColor;

    private Tween scaleTween;
    private Tween colorTween;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        button = GetComponent<Button>();
        tmpText = GetComponentInChildren<TextMeshProUGUI>(true);

        originalScale = rect.localScale;

        if (tmpText != null)
            originalColor = tmpText.color;

        // Reset when clicked
        button.onClick.AddListener(ResetVisuals);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!button.interactable)
            return;

        AnimateHover();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ResetVisuals();
    }

    // 🔑 This fixes the issue
    public void OnPointerDown(PointerEventData eventData)
    {
        if (clickSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(clickSound, transform.position, 1f, 1f, false);

        ResetVisuals();
    }

    private void OnDisable()
    {
        ResetVisuals();
    }

    private void AnimateHover()
    {
        scaleTween?.Kill();
        colorTween?.Kill();

        scaleTween = rect
            .DOScale(originalScale * hoverScale, duration)
            .SetEase(Ease.OutBack);

        if (tmpText != null)
        {
            colorTween = tmpText
                .DOColor(hoverColor, duration);
        }

        if (hoverSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(hoverSound, transform.position, 1f, 1f, false);
    }

    private void ResetVisuals()
    {
        scaleTween?.Kill();
        colorTween?.Kill();

        rect.localScale = originalScale;

        if (tmpText != null)
            tmpText.color = originalColor;
    }
}
