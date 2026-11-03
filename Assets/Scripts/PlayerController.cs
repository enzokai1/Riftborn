using UnityEngine;
using UnityEngine.InputSystem;

// Top-down movement: configure Rigidbody2D with zero gravity and frozen Z rotation.
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Tooltip("Movement speed in Unity units per second.")]
    [SerializeField, Min(0f)] private float movementSpeed = 2f;

    private Rigidbody2D body;
    private Vector2 movementDirection;
    private Collider2D bodyCollider;
    private PhysicsMaterial2D movementMaterial;

    public Vector2 MovementDirection => movementDirection;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        movementMaterial = new PhysicsMaterial2D("Player movement") { friction = 0f, bounciness = 0f };
        body.sharedMaterial = movementMaterial;
        // A top-down character collides at the feet, not across the entire sprite.
        if (bodyCollider is BoxCollider2D feet)
        {
            feet.size = new Vector2(0.5f, 0.32f);
            feet.offset = new Vector2(0f, -0.52f);
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        movementDirection = Vector2.zero;

        if (keyboard == null || Time.timeScale == 0f || !Application.isFocused)
        {
            return;
        }

        float horizontal = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f)
            - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
        float vertical = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f)
            - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);

        // Keep diagonal movement at the same speed as horizontal/vertical movement.
        movementDirection = new Vector2(horizontal, vertical).normalized;
    }

    private void FixedUpdate()
    {
        // Velocity is measured in units per second; do not multiply by delta time.
        Vector2 velocity = movementDirection * movementSpeed;
        HouseFloor map = HouseFloor.Active;
        if (map != null)
        {
            Vector2 margin = bodyCollider != null ? (Vector2)bodyCollider.bounds.extents : Vector2.one * 0.5f;
            Vector2 offset = bodyCollider != null ? (Vector2)bodyCollider.bounds.center - body.position : Vector2.zero;
            margin += Vector2.one * 0.5f;
            Vector2 center = body.position + offset;
            Vector2 clamped = map.Clamp(center, margin);
            if ((clamped - center).sqrMagnitude > 0.000001f)
                body.position = clamped - offset;
            Vector2 next = map.Clamp(clamped + velocity * Time.fixedDeltaTime, margin);
            velocity = (next - clamped) / Time.fixedDeltaTime;
        }
        body.linearVelocity = velocity;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            movementDirection = Vector2.zero;
            if (body != null) body.linearVelocity = Vector2.zero;
        }
    }

    private void OnDestroy()
    {
        if (movementMaterial != null) Destroy(movementMaterial);
    }

    private void OnDisable()
    {
        movementDirection = Vector2.zero;

        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
        }
    }
}
