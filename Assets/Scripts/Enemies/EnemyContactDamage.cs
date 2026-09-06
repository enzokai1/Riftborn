using UnityEngine;

[DisallowMultipleComponent]
public class EnemyContactDamage : MonoBehaviour
{
    [SerializeField, Min(1)] private int damage = 10;
    [SerializeField, Min(0.01f)] private float damageInterval = 1f;

    private float nextDamageTime;

    private void Awake()
    {
        damage = Mathf.Max(1, damage);
        damageInterval = Mathf.Max(0.01f, damageInterval);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryDealDamage(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryDealDamage(collision);
    }

    private void TryDealDamage(Collision2D collision)
    {
        if (!isActiveAndEnabled || Time.time < nextDamageTime)
            return;

        if (!collision.gameObject.TryGetComponent(out PlayerHealth playerHealth)
            || playerHealth.CurrentHealth <= 0)
            return;

        nextDamageTime = Time.time + damageInterval;
        playerHealth.TakeDamage(damage);
    }
}
