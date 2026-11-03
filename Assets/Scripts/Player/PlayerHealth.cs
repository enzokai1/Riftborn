using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerHealth : MonoBehaviour
{
    [SerializeField, Min(1)] private int maxHealth = 100;
    [Tooltip("Cura base; o GDD define os multiplicadores, mas não a taxa inicial.")]
    [SerializeField, Min(0.1f)] private float secondsPerHeal = 5f;
    [SerializeField, Min(1)] private int healAmount = 1;
    private PlayerExperience experience;
    private MatchStats match;
    private float healingElapsed;
    public int MaxHealth => maxHealth;
    public int CurrentHealth { get; private set; }
    public bool Invulnerable { get; private set; }
    public event Action HealthChanged;
    public event Action Died;

    private void Awake()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        CurrentHealth = maxHealth;
        experience = GetComponent<PlayerExperience>();
        match = FindFirstObjectByType<MatchStats>();
        HealthChanged?.Invoke();
    }

    private void Update()
    {
        if (CurrentHealth <= 0 || CurrentHealth >= MaxHealth
            || experience == null || !experience.HasHealingAbility)
        {
            healingElapsed = 0f;
            return;
        }
        if (Time.deltaTime <= 0f || experience.HasUpgradeChoice
            || (match != null && (match.Finished || match.IsPaused))) return;
        healingElapsed += Time.deltaTime * experience.HealingSpeedMultiplier;
        if (healingElapsed < secondsPerHeal) return;
        healingElapsed -= secondsPerHeal;
        Heal(healAmount);
    }

    public void SetInvulnerable(bool value) => Invulnerable = value;

    public void Heal(int amount)
    {
        if (amount <= 0 || CurrentHealth <= 0) return;
        CurrentHealth = (int)Math.Min((long)maxHealth, (long)CurrentHealth + amount);
        HealthChanged?.Invoke();
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || CurrentHealth <= 0 || Invulnerable) return;
        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        HealthChanged?.Invoke();
        if (CurrentHealth > 0) return;
        PlayerController controller = GetComponent<PlayerController>();
        if (controller != null) controller.enabled = false;
        foreach (Weapon weapon in GetComponentsInChildren<Weapon>()) weapon.enabled = false;
        foreach (Collider2D collider in GetComponents<Collider2D>()) collider.enabled = false;
        PlayerSpriteAnimation animation = GetComponent<PlayerSpriteAnimation>();
        if (animation != null) animation.PlayDeath();
        Died?.Invoke();
    }
}

