using UnityEngine;

public class BLACKMOVE : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3f;

    private Camera mainCamera;
    private Vector3 cameraOffset;

    private void Start()
    {
        mainCamera = Camera.main;

        if (mainCamera != null)
        {
            // 记录相机与方块的初始偏移（通常只需要保持 Z 偏移）
            cameraOffset = mainCamera.transform.position - transform.position;
        }
    }

    private void Update()
    {
        // 方块持续从左向右移动
        transform.position += Vector3.right * moveSpeed * Time.deltaTime;
    }

    private void LateUpdate()
    {
        if (mainCamera == null)
        {
            return;
        }

        // 相机跟随，保证方块始终处于画面中心位置
        mainCamera.transform.position = transform.position + cameraOffset;
    }
}
