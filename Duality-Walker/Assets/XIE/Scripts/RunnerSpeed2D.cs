using UnityEngine;

public class RunnerSpeed2D : MonoBehaviour
{
    [Header("Speed")]
    [SerializeField] private float startSpeed = 2.5f;
    [SerializeField] private float acceleration = 0.25f;
    [SerializeField] private float maxSpeed = 12f;

    public float CurrentSpeed { get; private set; }

    private void OnEnable()
    {
        CurrentSpeed = Mathf.Max(0f, startSpeed);
    }

    private void Update()
    {
        CurrentSpeed += Mathf.Max(0f, acceleration) * Time.deltaTime;
        if (CurrentSpeed > maxSpeed) CurrentSpeed = maxSpeed;
    }

    public void ResetSpeed()
    {
        CurrentSpeed = Mathf.Max(0f, startSpeed);
    }
}