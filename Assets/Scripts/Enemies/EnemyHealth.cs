using System;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyHealth : MonoBehaviour
{
    [SerializeField, Min(1)] private int maximumHealth = 10;
    public int MaximumHealth => maximumHealth;
    public int CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0;
    public event Action Died;

    private void Awake() => ConfigureHealth(maximumHealth);

    public void ConfigureHealth(int health)
    {
        maximumHealth = Mathf.Max(1, health);
        CurrentHealth = maximumHealth;
    }

    private MatchStats match;
    private int baseHealth;
    private int baseDamage;
    private float baseSpeed;
    private int appliedDifficulty = -1;
    private EnemyContactDamage contact;
    private EnemyController movement;

    public void EnableTimeScaling(MatchStats stats, int damage, float speed)
    {
        match = stats;
        baseHealth = maximumHealth;
        baseDamage = damage;
        baseSpeed = speed;
        contact = GetComponent<EnemyContactDamage>();
        movement = GetComponent<EnemyController>();
        appliedDifficulty = -1;
        UpdateTimeScaling();
    }

    private void Update()
    {
        if (match == null || !match.CombatActive || match.IsPaused || IsDead) return;
        UpdateTimeScaling();
    }

    private void UpdateTimeScaling()
    {
        int difficulty = match.Rules.DifficultyAt(match.ElapsedSeconds);
        if (difficulty == appliedDifficulty || IsDead) return;
        appliedDifficulty = difficulty;
        // Preserve remaining health fraction rather than refilling damaged enemies.
        float remaining = (float)CurrentHealth / maximumHealth;
        maximumHealth = Mathf.Max(1, Mathf.CeilToInt(baseHealth * match.Rules.HealthScale(match.ElapsedSeconds)));
        CurrentHealth = Mathf.Clamp(Mathf.CeilToInt(maximumHealth * remaining), 1, maximumHealth);
        if (contact != null) contact.SetDamage(Mathf.CeilToInt(baseDamage * match.Rules.DamageScale(match.ElapsedSeconds)));
        if (movement != null) movement.SetMovementSpeed(baseSpeed * match.Rules.SpeedScale(match.ElapsedSeconds));
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || IsDead) return;
        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        if (IsDead) Died?.Invoke();
        else
        {
            if (movement == null) movement = GetComponent<EnemyController>();
            if (movement != null) movement.PlayHurt();
        }
    }
}

