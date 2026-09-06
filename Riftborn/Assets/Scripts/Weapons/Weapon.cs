using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class Weapon : MonoBehaviour
{
    [SerializeField] private Camera aimCamera;
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField, Min(1)] private int damage = 1;
    [Tooltip("Projectile speed in Unity units per second.")]
    [SerializeField, Min(0.01f)] private float projectileSpeed = 10f;
    [Tooltip("Number of shots per second while the left mouse button is held.")]
    [SerializeField, Min(0.01f)] private float shotsPerSecond = 4f;

    private float nextShotTime;

    private void Awake()
    {
        damage = Mathf.Max(1, damage);
        projectileSpeed = Mathf.Max(0.01f, projectileSpeed);
        shotsPerSecond = Mathf.Max(0.01f, shotsPerSecond);

        if (aimCamera == null || projectilePrefab == null || firePoint == null)
        {
            Debug.LogError("Weapon requires an Aim Camera, Projectile Prefab and Fire Point.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || Time.timeScale == 0f)
        {
            return;
        }

        // Intersect the cursor ray with the weapon's XY plane to obtain a world position.
        Ray cursorRay = aimCamera.ScreenPointToRay(mouse.position.ReadValue());
        Plane aimPlane = new Plane(Vector3.forward, transform.position);
        if (!aimPlane.Raycast(cursorRay, out float distance))
        {
            return;
        }

        Vector2 aimDirection = cursorRay.GetPoint(distance) - transform.position;
        if (aimDirection.sqrMagnitude == 0f)
        {
            return;
        }

        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        if (mouse.leftButton.isPressed && Time.time >= nextShotTime)
        {
            Shoot();
            nextShotTime = Time.time + 1f / shotsPerSecond;
        }
    }

    private void Shoot()
    {
        Projectile projectile = Instantiate(projectilePrefab, firePoint.position, transform.rotation);
        projectile.Initialize(transform.right, projectileSpeed, damage);
    }
}
