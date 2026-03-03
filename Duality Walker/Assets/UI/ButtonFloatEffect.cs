using System;
using System.Collections.Generic;
using UnityEngine;

public class ButtonFloatEffect : MonoBehaviour
{
    [Serializable]
    private class FloatingButton
    {
        public RectTransform button;
        public float speed = 40f;
        public Vector2 initialDirection = new Vector2(1f, 1f);
        public bool randomDirectionOnEnable = true;
    }

    [Header("区域（RectTransform）")]
    [SerializeField] private RectTransform whiteArea;
    [SerializeField] private RectTransform blackArea;

    [Header("白区按钮")]
    [SerializeField] private FloatingButton[] whiteAreaButtons;

    [Header("黑区按钮")]
    [SerializeField] private FloatingButton[] blackAreaButtons;

    [Header("随机漂移参数")]
    [SerializeField] private float randomTurnIntervalMin = 0.3f;
    [SerializeField] private float randomTurnIntervalMax = 1.0f;
    [SerializeField] private float randomTurnAngle = 45f;
    [SerializeField] private float jitterStrength = 20f;

    [Header("时间")]
    [SerializeField] private bool useUnscaledTime = true;

    private readonly Dictionary<RectTransform, Vector2> velocities = new Dictionary<RectTransform, Vector2>();
    private readonly Dictionary<RectTransform, float> nextTurnTimers = new Dictionary<RectTransform, float>();

    private void OnEnable()
    {
        velocities.Clear();
        nextTurnTimers.Clear();

        InitButtons(whiteAreaButtons);
        InitButtons(blackAreaButtons);
    }

    private void Update()
    {
        float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        UpdateArea(whiteArea, whiteAreaButtons, deltaTime);
        UpdateArea(blackArea, blackAreaButtons, deltaTime);
    }

    private void InitButtons(FloatingButton[] buttons)
    {
        if (buttons == null)
        {
            return;
        }

        for (int i = 0; i < buttons.Length; i++)
        {
            FloatingButton item = buttons[i];
            if (item == null || item.button == null)
            {
                continue;
            }

            float speed = Mathf.Max(1f, item.speed);
            Vector2 dir = item.initialDirection.sqrMagnitude > 0.0001f
                ? item.initialDirection.normalized
                : Vector2.right;

            if (item.randomDirectionOnEnable)
            {
                dir = UnityEngine.Random.insideUnitCircle.normalized;
                if (dir.sqrMagnitude < 0.0001f)
                {
                    dir = Vector2.right;
                }
            }

            velocities[item.button] = dir * speed;
            nextTurnTimers[item.button] = GetRandomTurnInterval();
        }
    }

    private void UpdateArea(RectTransform area, FloatingButton[] buttons, float deltaTime)
    {
        if (area == null || buttons == null || buttons.Length == 0)
        {
            return;
        }

        for (int i = 0; i < buttons.Length; i++)
        {
            FloatingButton item = buttons[i];
            if (item == null || item.button == null)
            {
                continue;
            }

            float speed = Mathf.Max(1f, item.speed);

            if (!velocities.TryGetValue(item.button, out Vector2 velocity))
            {
                velocity = Vector2.right * speed;
                velocities[item.button] = velocity;
                nextTurnTimers[item.button] = GetRandomTurnInterval();
            }

            ApplyRandomDrift(item.button, ref velocity, speed, deltaTime);
            MoveAndBounce(item.button, area, ref velocity, deltaTime);

            velocities[item.button] = velocity;
        }
    }

    private void ApplyRandomDrift(RectTransform button, ref Vector2 velocity, float speed, float deltaTime)
    {
        if (!nextTurnTimers.TryGetValue(button, out float timer))
        {
            timer = GetRandomTurnInterval();
        }

        timer -= deltaTime;

        if (timer <= 0f)
        {
            float angle = UnityEngine.Random.Range(-randomTurnAngle, randomTurnAngle);
            velocity = Rotate(velocity, angle);
            timer = GetRandomTurnInterval();
        }

        if (jitterStrength > 0f)
        {
            velocity += UnityEngine.Random.insideUnitCircle * jitterStrength * deltaTime;
        }

        if (velocity.sqrMagnitude < 0.0001f)
        {
            velocity = Vector2.right * speed;
        }
        else
        {
            velocity = velocity.normalized * speed;
        }

        nextTurnTimers[button] = timer;
    }

    private static void MoveAndBounce(RectTransform button, RectTransform area, ref Vector2 velocity, float deltaTime)
    {
        RectTransform parent = button.parent as RectTransform;
        if (parent == null)
        {
            return;
        }

        Vector3[] areaCorners = new Vector3[4];
        area.GetWorldCorners(areaCorners);

        Vector2 min = parent.InverseTransformPoint(areaCorners[0]);
        Vector2 max = parent.InverseTransformPoint(areaCorners[2]);

        Vector2 pos = button.anchoredPosition + velocity * deltaTime;

        float halfW = button.rect.width * 0.5f * button.localScale.x;
        float halfH = button.rect.height * 0.5f * button.localScale.y;

        float left = min.x + halfW;
        float right = max.x - halfW;
        float bottom = min.y + halfH;
        float top = max.y - halfH;

        if (pos.x < left)
        {
            pos.x = left;
            velocity.x = Mathf.Abs(velocity.x);
        }
        else if (pos.x > right)
        {
            pos.x = right;
            velocity.x = -Mathf.Abs(velocity.x);
        }

        if (pos.y < bottom)
        {
            pos.y = bottom;
            velocity.y = Mathf.Abs(velocity.y);
        }
        else if (pos.y > top)
        {
            pos.y = top;
            velocity.y = -Mathf.Abs(velocity.y);
        }

        button.anchoredPosition = pos;
    }

    private float GetRandomTurnInterval()
    {
        float min = Mathf.Max(0.01f, randomTurnIntervalMin);
        float max = Mathf.Max(min, randomTurnIntervalMax);
        return UnityEngine.Random.Range(min, max);
    }

    private static Vector2 Rotate(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(rad);
        float cos = Mathf.Cos(rad);
        return new Vector2(cos * v.x - sin * v.y, sin * v.x + cos * v.y);
    }
}