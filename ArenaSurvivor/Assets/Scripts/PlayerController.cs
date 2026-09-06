using UnityEngine;
using UnityEngine.InputSystem;

// Top-down movement: configure Rigidbody2D with zero gravity and frozen Z rotation.
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Tooltip("Movement speed in Unity units per second.")]
    [SerializeField, Min(0f)] private float movementSpeed = 5f;

    private Rigidbody2D body;
    private Vector2 movementDirection;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        movementDirection = Vector2.zero;

        if (keyboard == null)
        {
            return;
        }

        float horizontal = (keyboard.dKey.isPressed ? 1f : 0f)
            - (keyboard.aKey.isPressed ? 1f : 0f);
        float vertical = (keyboard.wKey.isPressed ? 1f : 0f)
            - (keyboard.sKey.isPressed ? 1f : 0f);

        // Keep diagonal movement at the same speed as horizontal/vertical movement.
        movementDirection = new Vector2(horizontal, vertical).normalized;
    }

    private void FixedUpdate()
    {
        // Velocity is measured in units per second; do not multiply by delta time.
        body.linearVelocity = movementDirection * movementSpeed;
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
