using UnityEngine;

public class ScrollingLayer2D : MonoBehaviour
{
    public enum ScrollDirection
    {
        RightToLeft, // 推荐：角色看起来向前
        LeftToRight
    }

    [SerializeField] private RunnerSpeed2D speedSource;
    [SerializeField] private float speedMultiplier = 1f;
    [SerializeField] private ScrollDirection direction = ScrollDirection.RightToLeft;

    private void LateUpdate()
    {
        if (speedSource == null) return;

        float speed = speedSource.CurrentSpeed * speedMultiplier;
        float sign = direction == ScrollDirection.RightToLeft ? -1f : 1f;

        transform.position += new Vector3(sign * speed * Time.deltaTime, 0f, 0f);
    }
}