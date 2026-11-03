using System;
using System.Collections.Generic;
using UnityEngine;

public enum UpgradeKind { Power, Projectiles, AttackSpeed, HealingSpeed }

[DisallowMultipleComponent]
public class PlayerExperience : MonoBehaviour
{
    [SerializeField, Min(1)] private int baseExperience = 20;
    [Tooltip("O x do GDD é interpretado como bônus máximo de dano.")]
    [SerializeField, Min(3)] private int maximumPowerBonus = 30;
    [Header("Atração de XP")]
    [SerializeField, Min(0f)] private float experienceAttractionRadius = 4f;
    [SerializeField, Min(0.1f)] private float experienceAttractionSpeed = 8f;
    public float ExperienceAttractionRadius => Mathf.Max(0f, experienceAttractionRadius);
    public float ExperienceAttractionSpeed => Mathf.Max(0.1f, experienceAttractionSpeed);

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.55f, 0.36f, 0.96f, 0.65f);
        Gizmos.DrawWireSphere(transform.position, ExperienceAttractionRadius);
    }

    private readonly int[] ranks = new int[4];
    private readonly List<UpgradeKind> choices = new List<UpgradeKind>(3);

    public long TotalExperience { get; private set; }
    public int CurrentLevel { get; private set; } = 1;
    public long CurrentExperience { get; private set; }
    public int NextLevelExperience { get; private set; }
    public int PendingUpgrades { get; private set; }
    public IReadOnlyList<UpgradeKind> Choices => choices;
    public bool HasUpgradeChoice => PendingUpgrades > 0 && choices.Count > 0;
    public int DamageBonus => Mathf.RoundToInt(maximumPowerBonus * Rank(UpgradeKind.Power) / 3f);
    public int ProjectileCount => Rank(UpgradeKind.Projectiles) == 0 ? 1 : Rank(UpgradeKind.Projectiles) * 2;
    public float AttackSpeedMultiplier => 1f + Rank(UpgradeKind.AttackSpeed) * 0.5f;
    public bool HasHealingAbility => Rank(UpgradeKind.HealingSpeed) > 0;
    public float HealingSpeedMultiplier => HasHealingAbility
        ? 1f + (Rank(UpgradeKind.HealingSpeed) - 1) * 0.5f : 0f;
    public event Action ExperienceChanged;
    public int Rank(UpgradeKind kind) => ranks[(int)kind];

    private void Awake()
    {
        baseExperience = Mathf.Max(1, baseExperience);
        NextLevelExperience = Requirement(CurrentLevel, baseExperience);
    }

    public static int Requirement(int level, int initial = 20)
    {
        // Saturate before shifting, so large levels cannot wrap around.
        long value = (long)Mathf.Max(1, initial) << Mathf.Clamp(level - 1, 0, 31);
        return (int)Math.Min(value, int.MaxValue);
    }

    public void AddExperience(int amount)
    {
        if (amount <= 0) return;
        TotalExperience = Math.Min(TotalExperience, long.MaxValue - amount) + amount;
        CurrentExperience = Math.Min(CurrentExperience, long.MaxValue - amount) + amount;
        while (CurrentExperience >= NextLevelExperience && CurrentLevel < int.MaxValue)
        {
            CurrentExperience -= NextLevelExperience;
            CurrentLevel++;
            PendingUpgrades++;
            NextLevelExperience = Requirement(CurrentLevel, baseExperience);
        }
        if (choices.Count == 0) RollChoices();
        ExperienceChanged?.Invoke();
    }

    private void RollChoices()
    {
        choices.Clear();
        if (PendingUpgrades == 0) return;
        var available = new List<UpgradeKind>();
        for (int i = 0; i < ranks.Length; i++)
            if (ranks[i] < 3) available.Add((UpgradeKind)i);
        while (choices.Count < 3 && available.Count > 0)
        {
            int index = UnityEngine.Random.Range(0, available.Count);
            choices.Add(available[index]);
            available.RemoveAt(index);
        }
        // After all twelve upgrades, XP continues without opening an empty modal.
        if (choices.Count == 0) PendingUpgrades = 0;
    }

    public bool ChooseUpgrade(int index)
    {
        if (!HasUpgradeChoice || index < 0 || index >= choices.Count) return false;
        ranks[(int)choices[index]]++;
        PendingUpgrades--;
        RollChoices();
        ExperienceChanged?.Invoke();
        return true;
    }

    public string Describe(UpgradeKind kind)
    {
        int next = Rank(kind) + 1;
        switch (kind)
        {
            case UpgradeKind.Power: return $"PODER {next}/3\n+{Mathf.RoundToInt(maximumPowerBonus * next / 3f)} de dano";
            case UpgradeKind.Projectiles: return $"PROJÉTEIS {next}/3\n{next * 2} esferas por disparo";
            case UpgradeKind.AttackSpeed: return $"VELOCIDADE DE ATAQUE {next}/3\n{1f + next * 0.5f:0.0}x a cadência inicial";
            default: return next == 1
                ? "REGENERA\u00c7\u00c3O 1/3\nDesbloqueia cura passiva lenta"
                : $"REGENERA\u00c7\u00c3O {next}/3\n{1f + (next - 1) * 0.5f:0.0}x a velocidade de cura passiva";
        }
    }
}

