using TMPro;
using UnityEngine;

public class DistanceRecord : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private TMP_Text distanceText;
    [SerializeField] private string prefix = "Distance: ";

    private Vector3 startPosition;

    private void Start()
    {
        if (target == null)
        {
            Debug.LogWarning("DistanceRecord: target 未设置。");
            enabled = false;
            return;
        }

        if (distanceText == null)
        {
            distanceText = GetComponent<TMP_Text>();
        }

        if (distanceText == null)
        {
            Debug.LogWarning("DistanceRecord: distanceText 未设置，且当前对象没有 TMP_Text 组件。");
            enabled = false;
            return;
        }

        startPosition = target.position;
        UpdateDistanceText(0f);
    }

    private void Update()
    {
        float distance = Vector3.Distance(startPosition, target.position);
        UpdateDistanceText(distance);
    }

    private void UpdateDistanceText(float distance)
    {
        distanceText.text = prefix + distance.ToString("F2") + " m";
    }
}