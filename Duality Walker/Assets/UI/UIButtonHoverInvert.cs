using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class UIButtonHoverInvert : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("引用")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Graphic labelGraphic;
    [SerializeField] private Outline borderOutline;

    [Header("按钮设置")]
    [SerializeField] private bool forceDisableButtonTransition = true;

    [Header("自动配色")]
    [SerializeField] private bool useAutoPreset = true;
    [SerializeField] private bool normalIsDark = false;

    [Header("边框")]
    [SerializeField] private bool showBorder = true;
    [SerializeField] private Vector2 borderThickness = new Vector2(2f, 2f);

    [Header("颜色")]
    [SerializeField] private Color normalBackgroundColor = Color.white;
    [SerializeField] private Color hoverBackgroundColor = Color.black;
    [SerializeField] private Color normalTextColor = Color.black;
    [SerializeField] private Color hoverTextColor = Color.white;
    [SerializeField] private Color normalBorderColor = Color.black;
    [SerializeField] private Color hoverBorderColor = Color.white;

    [Header("动画")]
    [SerializeField] private float transitionDuration = 0.2f;
    [SerializeField] private bool useUnscaledTime = true;

    private Coroutine transitionRoutine;

    private void OnValidate()
    {
        ApplyAutoPresetIfNeeded();
        EnsureBorderOutline();
        ApplyBorderStyle();
    }

    private void Awake()
    {
        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
        }

        if (labelGraphic == null)
        {
            var tmp = GetComponentInChildren<TMP_Text>(true);
            if (tmp != null)
            {
                labelGraphic = tmp;
            }
            else
            {
                labelGraphic = GetComponentInChildren<Text>(true);
            }
        }

        EnsureBorderOutline();

        if (forceDisableButtonTransition)
        {
            var button = GetComponent<Button>();
            if (button != null)
            {
                button.transition = Selectable.Transition.None;
            }
        }

        ApplyAutoPresetIfNeeded();
        ApplyBorderStyle();
        ApplyInstant(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PlayTransition(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        PlayTransition(false);
    }

    private void EnsureBorderOutline()
    {
        if (borderOutline == null)
        {
            borderOutline = GetComponent<Outline>();
        }

        if (borderOutline == null)
        {
            borderOutline = gameObject.AddComponent<Outline>();
        }

        borderOutline.useGraphicAlpha = false;
    }

    private void ApplyAutoPresetIfNeeded()
    {
        if (!useAutoPreset)
        {
            return;
        }

        if (normalIsDark)
        {
            normalBackgroundColor = Color.black;
            hoverBackgroundColor = Color.white;
            normalTextColor = Color.white;
            hoverTextColor = Color.black;
            normalBorderColor = Color.white;
            hoverBorderColor = Color.black;
        }
        else
        {
            normalBackgroundColor = Color.white;
            hoverBackgroundColor = Color.black;
            normalTextColor = Color.black;
            hoverTextColor = Color.white;
            normalBorderColor = Color.black;
            hoverBorderColor = Color.white;
        }
    }

    private void ApplyBorderStyle()
    {
        if (borderOutline == null)
        {
            return;
        }

        borderOutline.enabled = showBorder;
        borderOutline.effectDistance = borderThickness;
        borderOutline.effectColor = normalBorderColor;
    }

    private void PlayTransition(bool hover)
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
        }

        transitionRoutine = StartCoroutine(TransitionCoroutine(hover));
    }

    private System.Collections.IEnumerator TransitionCoroutine(bool hover)
    {
        float duration = Mathf.Max(0.01f, transitionDuration);
        float t = 0f;

        Color fromBg = backgroundImage != null ? backgroundImage.color : Color.white;
        Color toBg = hover ? hoverBackgroundColor : normalBackgroundColor;

        Color fromText = labelGraphic != null ? labelGraphic.color : Color.white;
        Color toText = hover ? hoverTextColor : normalTextColor;

        Color fromBorder = borderOutline != null ? borderOutline.effectColor : Color.clear;
        Color toBorder = hover ? hoverBorderColor : normalBorderColor;

        while (t < duration)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            t += dt;
            float p = Mathf.Clamp01(t / duration);

            if (backgroundImage != null) backgroundImage.color = Color.Lerp(fromBg, toBg, p);
            if (labelGraphic != null) labelGraphic.color = Color.Lerp(fromText, toText, p);
            if (borderOutline != null && borderOutline.enabled) borderOutline.effectColor = Color.Lerp(fromBorder, toBorder, p);

            yield return null;
        }

        if (backgroundImage != null) backgroundImage.color = toBg;
        if (labelGraphic != null) labelGraphic.color = toText;
        if (borderOutline != null && borderOutline.enabled) borderOutline.effectColor = toBorder;

        transitionRoutine = null;
    }

    private void ApplyInstant(bool hover)
    {
        if (backgroundImage != null) backgroundImage.color = hover ? hoverBackgroundColor : normalBackgroundColor;
        if (labelGraphic != null) labelGraphic.color = hover ? hoverTextColor : normalTextColor;
        if (borderOutline != null && borderOutline.enabled) borderOutline.effectColor = hover ? hoverBorderColor : normalBorderColor;
    }
}