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
    private Vector2 launchPosition;
    private float maximumDistance = float.PositiveInfinity;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        maximumLifetime = Mathf.Max(0.01f, maximumLifetime);
    }

    private void Start() => Destroy(gameObject, maximumLifetime);

    public void Initialize(Vector2 direction, float speed, int damageAmount, float range = float.PositiveInfinity)
    {
        damage = Mathf.Max(1, damageAmount);
        launchPosition = body.position;
        maximumDistance = Mathf.Max(0.01f, range);
        body.linearVelocity = direction.normalized * Mathf.Max(0f, speed);
    }

    private void FixedUpdate()
    {
        if ((body.position - launchPosition).sqrMagnitude >= maximumDistance * maximumDistance)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;
        EnemyHealth enemy = other.GetComponentInParent<EnemyHealth>();
        if (enemy != null)
        {
            if (enemy.IsDead) return;
            hasHit = true;
            enemy.TakeDamage(damage);
            Destroy(gameObject);
        }
        else if (!other.isTrigger && other.GetComponentInParent<PlayerController>() == null)
        {
            hasHit = true;
            Destroy(gameObject);
        }
    }
}
