using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyHealth))]
public class EnemyDeath : MonoBehaviour
{
    [SerializeField] private GameObject experiencePickupPrefab;
    [SerializeField] private Sprite[] deathFrames;
    [SerializeField, Min(1f)] private float framesPerSecond = 8f;

    private EnemyHealth enemyHealth;
    private SpriteRenderer visual;
    private bool dying;
    private float elapsed;

    private void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        visual = GetComponent<SpriteRenderer>();
    }

    private void OnEnable() => enemyHealth.Died += HandleDeath;
    private void OnDisable() => enemyHealth.Died -= HandleDeath;

    private void HandleDeath()
    {
        if (dying) return;
        dying = true;
        elapsed = 0f;

        // The corpse stays visible but stops participating in combat immediately.
        EnemyController movement = GetComponent<EnemyController>();
        if (movement != null) movement.enabled = false;
        EnemyContactDamage contact = GetComponent<EnemyContactDamage>();
        if (contact != null) contact.enabled = false;
        GunterAreaAttack area = GetComponent<GunterAreaAttack>();
        if (area != null) area.enabled = false;
        foreach (Collider2D collider in GetComponentsInChildren<Collider2D>()) collider.enabled = false;
        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
        }
        // Hide the boss's fixed eye overlays and warning markers while its body falls.
        foreach (Renderer overlay in GetComponentsInChildren<Renderer>())
            if (overlay != visual) overlay.enabled = false;

        EnemyKillReporter reporter = GetComponent<EnemyKillReporter>();
        if (reporter == null || !reporter.IsBoss)
        {
            if (experiencePickupPrefab != null)
                Instantiate(experiencePickupPrefab, transform.position, Quaternion.identity);
            else
                Debug.LogError("EnemyDeath requires an Experience Pickup Prefab to drop XP.", this);
        }

        if (visual != null && deathFrames != null && deathFrames.Length > 0)
            visual.sprite = deathFrames[0];
        else
            RemoveBody();
    }

    private void Update()
    {
        if (!dying) return;
        elapsed += Time.deltaTime;
        int frame = Mathf.FloorToInt(elapsed * Mathf.Max(1f, framesPerSecond));
        if (deathFrames == null || frame >= deathFrames.Length) { RemoveBody(); return; }
        if (visual != null && deathFrames[frame] != null) visual.sprite = deathFrames[frame];
    }

    private void RemoveBody()
    {
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}