using UnityEngine;

[DisallowMultipleComponent]
public class GunterAreaAttack : MonoBehaviour
{
    private MatchStats match;
    private PlayerHealth player;
    private EnemyHealth health;
    private EnemyController movement;
    private LineRenderer warning;
    private float elapsed;
    private float warningElapsed;
    private float closeAttackElapsed;
    private float lastCloseShot = float.NegativeInfinity;
    private Weapon playerWeapon;
    private Rigidbody2D body;
    private EnemyContactDamage contact;
    public bool IsWarning { get; private set; }

    public void Initialize(MatchStats stats, SpriteRenderer ghost)
    {
        match = stats;
        player = stats.Player;
        health = GetComponent<EnemyHealth>();
        movement = GetComponent<EnemyController>();
        body = GetComponent<Rigidbody2D>();
        contact = GetComponent<EnemyContactDamage>();
        playerWeapon = player.GetComponentInChildren<Weapon>();
        if (playerWeapon != null) playerWeapon.ShotFired += OnPlayerShot;
        var visuals = gameObject.AddComponent<StageVisuals>();
        visuals.Initialize(ghost);
        Transform marker = new GameObject("Área de Gunter").transform;
        marker.SetParent(transform, false);
        marker.localScale = new Vector3(1f / transform.lossyScale.x, 1f / transform.lossyScale.y, 1);
        warning = visuals.Circle(marker, "Aviso de ataque", match.Rules.bossAreaRadius, new Color(1, 0.15f, 0.25f));
        warning.enabled = false;
        // Local sprite bounds preserve eye placement regardless of sprite PPU or boss scale.
        Bounds b = ghost.sprite.bounds;
        for (int i = -1; i <= 1; i += 2)
            visuals.Block(transform, "Olho vermelho", new Vector2(b.center.x + i * b.size.x * 0.13f,
                b.center.y + b.size.y * 0.1f), new Vector2(b.size.x * 0.09f, b.size.y * 0.08f),
                new Color(1, 0.05f, 0.1f), ghost.sortingOrder + 1);
    }

    private void OnEnable()
    {
        if (playerWeapon != null) playerWeapon.ShotFired += OnPlayerShot;
    }

    private void OnPlayerShot(Vector2 direction)
    {
        if (match == null || !match.CombatActive || match.IsPaused || IsWarning || health.IsDead) return;
        Vector2 towardBoss = (Vector2)transform.position - (Vector2)player.transform.position;
        Vector2 aimToBoss = (Vector2)transform.position - (Vector2)playerWeapon.AimOrigin;
        if (towardBoss.sqrMagnitude <= match.Rules.bossAreaRadius * match.Rules.bossAreaRadius
            && Vector2.Dot(direction.normalized, aimToBoss.normalized) >= 0.7f)
            lastCloseShot = Time.time;
        else
        {
            lastCloseShot = float.NegativeInfinity;
            closeAttackElapsed = 0f;
        }
    }

    private void Update()
    {
        if (match == null || !match.CombatActive || match.IsPaused || health.IsDead) return;
        if (IsWarning)
        {
            warningElapsed += Time.deltaTime;
            if (warningElapsed < Mathf.Max(0.1f, match.Rules.bossWarningDuration)) return;
            elapsed = 0f;
            closeAttackElapsed = 0f;
            lastCloseShot = float.NegativeInfinity;
            IsWarning = false;
            warning.enabled = false;
            if (Vector2.Distance(transform.position, player.transform.position) <= match.Rules.bossAreaRadius)
                player.TakeDamage(Mathf.CeilToInt(match.Rules.bossAreaDamage * match.Rules.DamageScale(match.ElapsedSeconds)));
            if (match.CombatActive) movement.enabled = true;
            return;
        }

        elapsed += Time.deltaTime;
        float shotGrace = playerWeapon != null ? Mathf.Max(0.75f, playerWeapon.ShotInterval * 1.5f) : 0.75f;
        bool attackingNearby = playerWeapon != null && playerWeapon.isActiveAndEnabled
            && Time.time - lastCloseShot <= shotGrace
            && Vector2.Distance(transform.position, player.transform.position) <= match.Rules.bossAreaRadius;
        closeAttackElapsed = attackingNearby ? closeAttackElapsed + Time.deltaTime : 0f;
        float interval = Mathf.Max(1f, match.Rules.bossAttackInterval);
        float lead = Mathf.Max(0.1f, match.Rules.bossWarningDuration);
        bool reacting = closeAttackElapsed >= Mathf.Max(0.5f, match.Rules.bossCloseAttackDuration)
            && elapsed >= Mathf.Max(1f, match.Rules.bossReactiveCooldown);
        if (!reacting && elapsed < Mathf.Max(0f, interval - lead)) return;
        // Complete the dash and swing before starting an area telegraph.
        if (contact != null && contact.IsBusy) return;
        IsWarning = true;
        warningElapsed = 0f;
        warning.enabled = true;
        movement.enabled = false;
        if (body != null) body.linearVelocity = Vector2.zero;
    }

    private void OnDisable()
    {
        if (playerWeapon != null) playerWeapon.ShotFired -= OnPlayerShot;
        if (warning != null) warning.enabled = false;
        if (IsWarning && match != null && match.CombatActive && health != null && !health.IsDead)
            movement.enabled = true;
        IsWarning = false;
        warningElapsed = 0f;
        closeAttackElapsed = 0f;
        lastCloseShot = float.NegativeInfinity;
    }
}
