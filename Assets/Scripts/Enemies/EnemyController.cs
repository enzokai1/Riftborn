using UnityEngine;

// Top-down movement: configure Rigidbody2D with zero gravity and frozen Z rotation.
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyController : MonoBehaviour
{
    [SerializeField] private Transform target;
    [Tooltip("Movement speed in Unity units per second.")]
    [SerializeField, Min(0f)] private float movementSpeed = 3f;

    private Rigidbody2D body;

    private void Awake()
    {
        movementSpeed = Mathf.Max(0f, movementSpeed);
        body = GetComponent<Rigidbody2D>();
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    private void FixedUpdate()
    {
        if (target == null)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 offset = (Vector2)target.position - body.position;

        // Limit the final step so the enemy does not overshoot a stationary target.
        float speed = Mathf.Min(movementSpeed, offset.magnitude / Time.fixedDeltaTime);
        body.linearVelocity = offset.normalized * speed;
    }

    private void OnDisable()
    {
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
        }
    }
}
