using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class Weapon : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float detectionRange = 6f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField, Min(1)] private int damage = 1;
    [Tooltip("Projectile speed in Unity units per second.")]
    [SerializeField, Min(0.01f)] private float projectileSpeed = 10f;
    [Tooltip("Maximum number of automatic shots per second.")]
    [SerializeField, Min(0.01f)] private float shotsPerSecond = 4f;

    private const float TargetSearchInterval = 0.1f;

    private readonly List<Collider2D> nearbyEnemies = new List<Collider2D>(32);
    private ContactFilter2D enemyFilter;
    private EnemyHealth currentTarget;
    private float nextShotTime;
    private float nextTargetSearchTime;

    private void Awake()
    {
        detectionRange = Mathf.Max(0.01f, detectionRange);
        damage = Mathf.Max(1, damage);
        projectileSpeed = Mathf.Max(0.01f, projectileSpeed);
        shotsPerSecond = Mathf.Max(0.01f, shotsPerSecond);

        if (projectilePrefab == null || firePoint == null)
        {
            Debug.LogError("Weapon requires a Projectile Prefab and Fire Point.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (Time.timeScale == 0f)
        {
            return;
        }

        if (!IsValidTarget(currentTarget))
        {
            currentTarget = null;
        }

        bool canAttemptShot = Time.time >= nextShotTime
            && Time.time >= nextTargetSearchTime;

        if (canAttemptShot)
        {
            currentTarget = FindNearestEnemy();
            if (currentTarget == null)
            {
                nextTargetSearchTime = Time.time + TargetSearchInterval;
            }
        }

        if (currentTarget == null)
        {
            return;
        }

        Vector2 aimDirection = currentTarget.transform.position - transform.position;
        if (aimDirection.sqrMagnitude > 0f)
        {
            float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        if (canAttemptShot)
        {
            if (Shoot())
            {
                nextShotTime = Time.time + 1f / shotsPerSecond;
            }
            else
            {
                // A target exactly at the muzzle has no valid shot direction.
                nextTargetSearchTime = Time.time + TargetSearchInterval;
            }
        }
    }

    private bool IsValidTarget(EnemyHealth enemy)
    {
        return enemy != null && enemy.gameObject.activeInHierarchy && !enemy.IsDead
            && (enemyLayer.value & (1 << enemy.gameObject.layer)) != 0
            && ((Vector2)(enemy.transform.position - transform.position)).sqrMagnitude
                <= detectionRange * detectionRange;
    }

    private EnemyHealth FindNearestEnemy()
    {
        enemyFilter.SetLayerMask(enemyLayer);
        enemyFilter.useTriggers = true;
        nearbyEnemies.Clear();
        Physics2D.OverlapCircle(transform.position, detectionRange, enemyFilter, nearbyEnemies);

        EnemyHealth nearestEnemy = null;
        float nearestDistanceSquared = float.PositiveInfinity;

        for (int i = 0; i < nearbyEnemies.Count; i++)
        {
            Collider2D candidate = nearbyEnemies[i];
            if (candidate == null || !candidate.TryGetComponent(out EnemyHealth enemy)
                || !IsValidTarget(enemy))
            {
                continue;
            }

            float distanceSquared = ((Vector2)(enemy.transform.position - transform.position)).sqrMagnitude;
            if (distanceSquared < nearestDistanceSquared)
            {
                nearestDistanceSquared = distanceSquared;
                nearestEnemy = enemy;
            }
        }

        return nearestEnemy;
    }

    private bool Shoot()
    {
        Vector2 direction = currentTarget.transform.position - firePoint.position;
        if (direction.sqrMagnitude == 0f)
        {
            return false;
        }

        direction.Normalize();
        Projectile projectile = Instantiate(projectilePrefab, firePoint.position, transform.rotation);
        projectile.Initialize(direction, projectileSpeed, damage);
        return true;
    }
}
