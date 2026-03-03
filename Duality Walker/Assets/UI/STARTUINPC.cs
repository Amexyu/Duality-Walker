using System;
using System.Collections;
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

    [Header("向右移动")]
    [SerializeField] private float moveSpeed = 500f;
    [SerializeField] private RectTransform uiBoundary;
    [SerializeField] private float rightBoundaryOffset = 0f;
    [SerializeField] private float delayBeforeComplete = 2f;

    private int currentRunFrameIndex;
    private float runFrameTimer;
    private bool isPlayingRun;
    private bool isMovingRight;
    private Action onMoveCompleted;
    private Coroutine completeCoroutine;

    private RectTransform cachedRectTransform;
    private Canvas parentCanvas;

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

        cachedRectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();

        if (uiBoundary == null && parentCanvas != null)
        {
            uiBoundary = parentCanvas.GetComponent<RectTransform>();
        }
    }

    private void OnEnable()
    {
        PlayRun();
    }

    private void OnDisable()
    {
        if (completeCoroutine != null)
        {
            StopCoroutine(completeCoroutine);
            completeCoroutine = null;
        }
    }

    private void Update()
    {
        UpdateRunAnimation();
        UpdateMoveRight();
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

    private void UpdateMoveRight()
    {
        if (!isMovingRight)
        {
            return;
        }

        float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        Vector3 delta = Vector3.right * moveSpeed * deltaTime;

        if (cachedRectTransform != null)
        {
            cachedRectTransform.position += delta;
        }
        else
        {
            transform.position += delta;
        }

        if (HasReachedRightBoundary())
        {
            isMovingRight = false;

            if (completeCoroutine != null)
            {
                StopCoroutine(completeCoroutine);
            }

            completeCoroutine = StartCoroutine(InvokeCompletedAfterDelay());
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

    public void PlayRunAndMoveRight(Action onCompleted)
    {
        if (completeCoroutine != null)
        {
            StopCoroutine(completeCoroutine);
            completeCoroutine = null;
        }

        PlayRun();
        onMoveCompleted = onCompleted;
        isMovingRight = true;
    }

    private IEnumerator InvokeCompletedAfterDelay()
    {
        float delay = Mathf.Max(0f, delayBeforeComplete);

        if (useUnscaledTime)
        {
            yield return new WaitForSecondsRealtime(delay);
        }
        else
        {
            yield return new WaitForSeconds(delay);
        }

        Action callback = onMoveCompleted;
        onMoveCompleted = null;
        completeCoroutine = null;
        callback?.Invoke();
    }

    private bool HasReachedRightBoundary()
    {
        if (uiBoundary == null)
        {
            return false;
        }

        Vector3[] boundaryCorners = new Vector3[4];
        uiBoundary.GetWorldCorners(boundaryCorners);
        float boundaryRightX = boundaryCorners[3].x + rightBoundaryOffset;

        if (cachedRectTransform != null)
        {
            Vector3[] npcCorners = new Vector3[4];
            cachedRectTransform.GetWorldCorners(npcCorners);
            float npcRightX = npcCorners[3].x;
            return npcRightX >= boundaryRightX;
        }

        if (targetSpriteRenderer != null)
        {
            return targetSpriteRenderer.bounds.max.x >= boundaryRightX;
        }

        return transform.position.x >= boundaryRightX;
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
