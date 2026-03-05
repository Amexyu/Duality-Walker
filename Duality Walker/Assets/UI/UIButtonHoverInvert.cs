using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UIButtonHoverInvert : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("引用")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Graphic labelGraphic;
    [SerializeField] private Outline borderOutline;

    [Header("按钮设置")]
    [SerializeField] private bool forceDisableButtonTransition = true;
    [SerializeField] private bool tintAllBackgroundGraphics = true;

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

    private readonly List<Graphic> backgroundGraphics = new List<Graphic>();
    private Coroutine transitionRoutine;
    private Button cachedButton;

    private void Awake()
    {
        cachedButton = GetComponent<Button>();
        CacheLabelGraphic();
        ResolveBackgroundImage();

        if (cachedButton != null && cachedButton.targetGraphic == null && backgroundImage != null)
        {
            cachedButton.targetGraphic = backgroundImage;
        }

        CollectBackgroundGraphics();
        EnsureBorderOutline();
        ConfigureButtonTransition();

        ApplyAutoPresetIfNeeded();
        ApplyBorderStyle();
        ApplyInstant(false);
    }

    public void OnPointerEnter(PointerEventData eventData) => PlayTransition(true);
    public void OnPointerExit(PointerEventData eventData) => PlayTransition(false);
    public void OnPointerDown(PointerEventData eventData) => PlayTransition(true);
    public void OnPointerUp(PointerEventData eventData) => PlayTransition(false);

    private void CacheLabelGraphic()
    {
        if (labelGraphic != null)
        {
            return;
        }

        TMP_Text tmp = GetComponentInChildren<TMP_Text>(true);
        labelGraphic = tmp != null ? tmp : GetComponentInChildren<Text>(true);
    }

    private void ResolveBackgroundImage()
    {
        if (backgroundImage != null)
        {
            return;
        }

        if (cachedButton != null && cachedButton.targetGraphic is Image targetImage)
        {
            backgroundImage = targetImage;
            return;
        }

        backgroundImage = GetComponent<Image>();
    }

    private void ConfigureButtonTransition()
    {
        if (forceDisableButtonTransition && cachedButton != null)
        {
            cachedButton.transition = Selectable.Transition.None;
        }
    }

    private void CollectBackgroundGraphics()
    {
        backgroundGraphics.Clear();

        if (!tintAllBackgroundGraphics)
        {
            return;
        }

        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic g = graphics[i];
            if (g == null || g == labelGraphic || g is TMP_Text || g is Text)
            {
                continue;
            }

            backgroundGraphics.Add(g);

            if (backgroundImage == null && g is Image img)
            {
                backgroundImage = img;
            }
        }
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
            return;
        }

        normalBackgroundColor = Color.white;
        hoverBackgroundColor = Color.black;
        normalTextColor = Color.black;
        hoverTextColor = Color.white;
        normalBorderColor = Color.black;
        hoverBorderColor = Color.white;
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

            ApplyVisualState(
                Color.Lerp(fromBg, toBg, p),
                Color.Lerp(fromText, toText, p),
                Color.Lerp(fromBorder, toBorder, p));

            yield return null;
        }

        ApplyVisualState(toBg, toText, toBorder);
        transitionRoutine = null;
    }

    private void ApplyInstant(bool hover)
    {
        ApplyVisualState(
            hover ? hoverBackgroundColor : normalBackgroundColor,
            hover ? hoverTextColor : normalTextColor,
            hover ? hoverBorderColor : normalBorderColor);
    }

    private void ApplyVisualState(Color background, Color text, Color border)
    {
        SetBackgroundColor(background);

        if (labelGraphic != null)
        {
            labelGraphic.color = text;
        }

        if (borderOutline != null && borderOutline.enabled)
        {
            borderOutline.effectColor = border;
        }
    }

    private void SetBackgroundColor(Color color)
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = color;
        }

        for (int i = 0; i < backgroundGraphics.Count; i++)
        {
            if (backgroundGraphics[i] != null)
            {
                backgroundGraphics[i].color = color;
            }
        }

        if (cachedButton != null && cachedButton.targetGraphic != null && cachedButton.targetGraphic != backgroundImage)
        {
            cachedButton.targetGraphic.color = color;
        }
    }
}