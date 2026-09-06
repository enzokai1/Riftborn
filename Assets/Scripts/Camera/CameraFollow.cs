using UnityEngine;

[DisallowMultipleComponent]
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform player;
    [Tooltip("Approximate follow response time in seconds. Smaller values follow more closely.")]
    [SerializeField, Min(0.01f)] private float smoothTime = 0.15f;

    private Vector2 followVelocity;
    private float originalZ;

    private void Awake()
    {
        originalZ = transform.position.z;
        smoothTime = Mathf.Max(0.01f, smoothTime);

        if (player == null)
        {
            Debug.LogError("CameraFollow requires a Player reference.", this);
            enabled = false;
        }
    }

    private void LateUpdate()
    {
        if (player == null)
        {
            return;
        }

        Vector2 smoothedPosition = Vector2.SmoothDamp(
            transform.position,
            player.position,
            ref followVelocity,
            smoothTime);

        transform.position = new Vector3(smoothedPosition.x, smoothedPosition.y, originalZ);
    }
}
