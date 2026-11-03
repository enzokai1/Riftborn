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

    public void SnapToPlayer()
    {
        if (player == null) return;
        Vector2 position = player.position;
        Camera view = GetComponent<Camera>();
        if (HouseFloor.Active != null && view != null && view.orthographic)
            position = HouseFloor.Active.Clamp(position,
                new Vector2(view.orthographicSize * view.aspect, view.orthographicSize));
        transform.position = new Vector3(position.x, position.y, originalZ);
        followVelocity = Vector2.zero;
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

        Camera view = GetComponent<Camera>();
        if (HouseFloor.Active != null && view != null && view.orthographic)
            smoothedPosition = HouseFloor.Active.Clamp(smoothedPosition,
                new Vector2(view.orthographicSize * view.aspect, view.orthographicSize));
        transform.position = new Vector3(smoothedPosition.x, smoothedPosition.y, originalZ);
    }
}
