using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class Projectile : MonoBehaviour
{
    [Tooltip("Maximum lifetime in seconds, including shots that miss.")]
    [SerializeField, Min(0.01f)] private float maximumLifetime = 3f;

    private Rigidbody2D body;
    private int damage;
    private bool hasHit;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        maximumLifetime = Mathf.Max(0.01f, maximumLifetime);
    }

    private void Start()
    {
        Destroy(gameObject, maximumLifetime);
    }

    public void Initialize(Vector2 direction, float speed, int damageAmount)
    {
        damage = Mathf.Max(1, damageAmount);
        body.linearVelocity = direction.normalized * Mathf.Max(0f, speed);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit || !other.TryGetComponent(out EnemyHealth enemyHealth))
        {
            return;
        }

        // Destroy is deferred, so guard against additional impact callbacks first.
        hasHit = true;
        enemyHealth.TakeDamage(damage);
        Destroy(gameObject);
    }
}
