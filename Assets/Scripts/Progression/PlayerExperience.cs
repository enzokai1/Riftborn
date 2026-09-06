using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerExperience : MonoBehaviour
{
    [SerializeField, Min(1)] private int baseExperience = 10;
    [SerializeField, Min(0)] private int experienceIncreasePerLevel = 5;

    public int CurrentLevel { get; private set; } = 1;
    public long CurrentExperience { get; private set; } = 0;
    public int NextLevelExperience { get; private set; }

    public event Action ExperienceChanged;

    private void Awake()
    {
        baseExperience = Mathf.Max(1, baseExperience);
        experienceIncreasePerLevel = Mathf.Max(0, experienceIncreasePerLevel);
        NextLevelExperience = CalculateNextLevelExperience();
    }

    public void AddExperience(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        // Keep a 64-bit balance so adding int amounts does not discard XP at int.MaxValue.
        CurrentExperience = System.Math.Min(CurrentExperience, long.MaxValue - amount) + amount;

        while (CurrentExperience >= NextLevelExperience)
        {
            if (CurrentLevel == int.MaxValue)
            {
                break;
            }

            CurrentExperience -= NextLevelExperience;
            CurrentLevel++;
            NextLevelExperience = CalculateNextLevelExperience();
            Debug.Log($"Player subiu para o nível {CurrentLevel}!", this);
        }

        Debug.Log($"Player coletou {amount} XP | XP atual: {CurrentExperience}/{NextLevelExperience} | Nível: {CurrentLevel}", this);
        ExperienceChanged?.Invoke();
    }

    private int CalculateNextLevelExperience()
    {
        // Cast before multiplication so large configurations cannot wrap to negative values.
        long requirement = baseExperience + ((long)CurrentLevel - 1) * experienceIncreasePerLevel;
        return (int)System.Math.Min(requirement, int.MaxValue);
    }
}
