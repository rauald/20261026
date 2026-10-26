using UnityEngine;

public sealed class SmoothPlayerCamera : MonoBehaviour
{
    public Transform target;
    public float distance = 5.5f;
    public float height = 4f;
    public float smoothTime = 0.15f;
    [Min(0.01f)] public float rotationSmoothTime = 0.2f;
    [Min(1f)] public float maxRotationSpeed = 180f;
    private Vector3 anchor, velocity;
    private float yaw, yawVelocity;
    private bool initialized;

    void LateUpdate()
    {
        if (target == null) return;
        if (!initialized)
        {
            anchor = target.position;
            yaw = target.eulerAngles.y;
            initialized = true;
        }
        anchor = Vector3.SmoothDamp(anchor, target.position, ref velocity,
            Mathf.Max(0.01f, smoothTime), Mathf.Infinity, Time.deltaTime);
        // Camera heading follows body rotation only, never movement direction.
        yaw = Mathf.SmoothDampAngle(yaw, target.eulerAngles.y, ref yawVelocity,
            Mathf.Max(0.01f, rotationSmoothTime), Mathf.Max(1f, maxRotationSpeed), Time.deltaTime);
        Vector3 back = Quaternion.Euler(0, yaw, 0) * Vector3.back;
        transform.position = anchor + back * distance + Vector3.up * height;
        // Use the same smoothed anchor so reversing doesn't swing the camera.
        transform.LookAt(anchor + Vector3.up * 1.6f);
    }
}
