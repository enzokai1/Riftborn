using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public class Weapon : MonoBehaviour
{
    public event System.Action<Vector2> ShotStarted;
    public event System.Action<Vector2> ShotFired;
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField, Min(1)] private int damage = 10;
    [SerializeField, Min(0.01f)] private float projectileSpeed = 10f;
    [SerializeField, Min(0.01f)] private float shotsPerSecond = 4f;
    [SerializeField, Min(0.1f)] private float detectionRange = 6f;
    [SerializeField] private LayerMask enemyLayer = 1 << 7;

    private readonly List<Collider2D> nearbyColliders = new List<Collider2D>();
    private PlayerExperience experience;
    private PlayerHealth ownerHealth;
    private PlayerSpriteAnimation playerAnimation;
    private Camera aimCamera;
    private float nextShotTime;
    private float releaseTime;
    private bool pendingShot;
    private Vector2 pendingDirection;

    public int CurrentDamage => (int)System.Math.Min((long)damage + (experience != null ? experience.DamageBonus : 0), int.MaxValue);
    public float ShotInterval => 1f / Mathf.Max(0.01f, shotsPerSecond * (experience != null ? experience.AttackSpeedMultiplier : 1f));
    public Vector3 AimOrigin => firePoint != null ? firePoint.position : transform.position;
    public Vector3 AimPosition { get; private set; }
    public Vector2 AimDirection { get; private set; }
    public bool HasAim { get; private set; }
    public bool HasEnemyInRange { get; private set; }

    private void Awake()
    {
        if (GetComponent<WeaponAimIndicator>() == null)
            gameObject.AddComponent<WeaponAimIndicator>();
        experience = GetComponentInParent<PlayerExperience>();
        ownerHealth = GetComponentInParent<PlayerHealth>();
        playerAnimation = GetComponentInParent<PlayerSpriteAnimation>();
        damage = Mathf.Max(1, damage);
        projectileSpeed = Mathf.Max(0.01f, projectileSpeed);
        shotsPerSecond = Mathf.Max(0.01f, shotsPerSecond);
        detectionRange = Mathf.Max(0.1f, detectionRange);
        if (projectilePrefab == null || firePoint == null)
        {
            Debug.LogError("Weapon requires a Projectile Prefab and Fire Point.", this);
            enabled = false;
        }
    }

    private bool EnemyInRange()
    {
        var filter = new ContactFilter2D();
        filter.SetLayerMask(enemyLayer);
        filter.useTriggers = true;
        Physics2D.OverlapCircle(transform.position, detectionRange, filter, nearbyColliders);
        foreach (Collider2D candidate in nearbyColliders)
        {
            EnemyHealth enemy = candidate.GetComponentInParent<EnemyHealth>();
            if (enemy != null && enemy.isActiveAndEnabled && !enemy.IsDead)
                return true;
        }
        return false;
    }

    // CameraFollow runs first; the attack playerAnimation runs after this weapon.
    private void LateUpdate()
    {
        HasAim = false;
        if (Time.timeScale == 0f) return;
        if (ownerHealth != null && ownerHealth.CurrentHealth <= 0)
        {
            CancelShot();
            return;
        }
        HasEnemyInRange = EnemyInRange();
        if (!HasEnemyInRange) pendingShot = false;

        Mouse mouse = Mouse.current;
        if (aimCamera == null || !aimCamera.isActiveAndEnabled) aimCamera = Camera.main;
        if (mouse == null || aimCamera == null) { pendingShot = false; return; }
        Ray ray = aimCamera.ScreenPointToRay(mouse.position.ReadValue());
        Plane aimPlane = new Plane(Vector3.forward, transform.position);
        if (!aimPlane.Raycast(ray, out float distance)) { pendingShot = false; return; }
        AimPosition = ray.GetPoint(distance);
        Vector2 direction = AimPosition - transform.position;
        if (direction.sqrMagnitude < 0.0001f) { pendingShot = false; return; }

        firePoint.position = CastOrigin(pendingShot ? pendingDirection : direction);
        Vector2 shotDirection = AimPosition - AimOrigin;
        if (shotDirection.sqrMagnitude < 0.0001f) { pendingShot = false; return; }
        AimDirection = shotDirection.normalized;
        HasAim = true;
        if (!HasEnemyInRange) return;

        if (pendingShot)
        {
            if (Time.time >= releaseTime)
            {
                Shoot(pendingDirection);
                pendingShot = false;
            }
            return;
        }
        if (Time.time < nextShotTime) return;

        pendingDirection = AimDirection;
        pendingShot = true;
        float duration = playerAnimation != null ? playerAnimation.AttackDuration : Mathf.Min(0.18f, ShotInterval * 0.85f);
        releaseTime = Time.time + duration / 3f;
        nextShotTime = Time.time + ShotInterval;
        ShotStarted?.Invoke(pendingDirection);
    }

    private Vector3 CastOrigin(Vector2 direction)
    {
        return playerAnimation != null ? playerAnimation.CastOrigin(direction)
            : transform.position + (Vector3)(direction.normalized * 0.5f);
    }

    private void Shoot(Vector2 direction)
    {
        Vector3 origin = CastOrigin(direction);
        firePoint.position = origin;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        int count = experience != null ? experience.ProjectileCount : 1;
        for (int i = 0; i < count; i++)
        {
            float spread = (i - (count - 1) * 0.5f) * 10f;
            Quaternion rotation = Quaternion.Euler(0f, 0f, angle + spread);
            Projectile projectile = Instantiate(projectilePrefab, origin, rotation);
            projectile.Initialize(rotation * Vector2.right, projectileSpeed, CurrentDamage, detectionRange);
        }
        ShotFired?.Invoke(direction);
    }

    private void CancelShot()
    {
        pendingShot = false;
        HasAim = false;
        HasEnemyInRange = false;
    }

    private void OnDisable() => CancelShot();
}
