using UnityEngine;
using UnityEngine.UI;

public class STARTUINPC : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Image targetImage;
    [SerializeField] private SpriteRenderer targetSpriteRenderer;

    [Header("RUN 帧动画")]
    [SerializeField] private Sprite[] runFrames;
    [SerializeField] private float runFps = 12f;
    [SerializeField] private bool useUnscaledTime = true;

    private int currentRunFrameIndex;
    private float runFrameTimer;
    private bool isPlayingRun;

    private void Awake()
    {
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }

        if (targetSpriteRenderer == null)
        {
            targetSpriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private void OnEnable()
    {
        PlayRun();
    }

    private void Update()
    {
        UpdateRunAnimation();
    }

    private void UpdateRunAnimation()
    {
        if (!isPlayingRun || runFrames == null || runFrames.Length == 0)
        {
            return;
        }

        if (runFps <= 0f)
        {
            runFps = 1f;
        }

        float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        float frameDuration = 1f / runFps;

        runFrameTimer += deltaTime;

        while (runFrameTimer >= frameDuration)
        {
            runFrameTimer -= frameDuration;
            currentRunFrameIndex = (currentRunFrameIndex + 1) % runFrames.Length;
            SetFrame(runFrames[currentRunFrameIndex]);
        }
    }

    public void PlayRun()
    {
        if (runFrames == null || runFrames.Length == 0)
        {
            isPlayingRun = false;
            Debug.LogWarning("[STARTUINPC] runFrames 为空，无法播放。", this);
            return;
        }

        if (targetImage == null && targetSpriteRenderer == null)
        {
            isPlayingRun = false;
            Debug.LogWarning("[STARTUINPC] 未找到 Image 或 SpriteRenderer。", this);
            return;
        }

        isPlayingRun = true;
        currentRunFrameIndex = 0;
        runFrameTimer = 0f;
        SetFrame(runFrames[currentRunFrameIndex]);
    }

    public void StopRun()
    {
        isPlayingRun = false;
    }

    private void SetFrame(Sprite frame)
    {
        if (targetImage != null)
        {
            targetImage.sprite = frame;
        }

        if (targetSpriteRenderer != null)
        {
            targetSpriteRenderer.sprite = frame;
        }
    }
}
