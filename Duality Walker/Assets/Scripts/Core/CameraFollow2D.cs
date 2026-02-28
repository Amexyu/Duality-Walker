using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float followSmooth = 6f;
    [SerializeField] private float deadZone = 0.15f;
    [SerializeField] private bool lockY = true;
    [SerializeField] private float fixedY = 0f;

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 currentPosition = transform.position;
        float targetX = currentPosition.x;

        if (Mathf.Abs(target.position.x - currentPosition.x) > deadZone)
        {
            targetX = target.position.x;
        }

        float targetY = lockY ? fixedY : target.position.y;
        Vector3 desiredPosition = new Vector3(targetX, targetY, currentPosition.z);

        float t = 1f - Mathf.Exp(-followSmooth * Time.deltaTime);
        transform.position = Vector3.Lerp(currentPosition, desiredPosition, t);
    }
}
