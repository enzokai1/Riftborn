using System;
using UnityEngine;

// Values left as variables in the GDD are exposed on MatchStats in the Inspector.
[Serializable]
public class FirstStageSettings : ISerializationCallbackReceiver
{
    [Header("Fragmentos - quantidade não definida no GDD")]
    [Min(2)] public int fragmentCount = 4;
    [Min(1)] public float minimumFragmentPixels = 125f;
    [Min(5f)] public float minimumFragmentDistance = 20f;
    [Min(1f)] public float fragmentRelocationDistance = 10f;
    [Min(1)] public float referencePixelsPerUnit = 100f;
    [Min(0.5f)] public float pickupRadius = 1.5f;
    [Header("Fantasma branco (demais cores: 1,5 / 2 / 2,5 vezes)")]
    [Min(1)] public int ghostHealth = 20;
    [Min(1)] public int ghostDamage = 5;
    [Min(0.1f)] public float ghostSpeed = 1.8f;
    [Header("Gunter")]
    [Min(1)] public int bossHealth = 20000;
    [SerializeField, HideInInspector] private int bossHealthVersion;
    [SerializeField, HideInInspector] private int bossAreaVersion;

    public void OnBeforeSerialize() { }

    public void OnAfterDeserialize()
    {
        // Unity retains serialized values when a field initializer changes.
        // Upgrade the old default once, including scenes already open in the editor.
        if (bossHealthVersion < 1)
        {
            if (bossHealth == 1200) bossHealth = 20000;
            bossHealthVersion = 1;
        }
        if (bossAreaVersion < 1)
        {
            if (bossAreaRadius == 3.5f) bossAreaRadius = 4f;
            bossAreaVersion = 1;
        }
    }

    [Min(1)] public int bossContactDamage = 30;
    [Min(1)] public int bossAreaDamage = 30;
    [Min(0.1f)] public float bossSpeed = 2.2f;
    [Min(1f)] public float bossDashRange = 7f;
    [Min(1f)] public float bossDashSpeed = 12f;
    [Min(0.1f)] public float bossDashDuration = 0.6f;
    [Min(1f)] public float bossDashCooldown = 2.5f;
    [Min(1f)] public float bossAreaRadius = 4f;
    [Min(1f)] public float bossAttackInterval = 6f;
    [Min(0.1f)] public float bossWarningDuration = 1.25f;
    [Min(0.5f)] public float bossCloseAttackDuration = 3f;
    [Min(1f)] public float bossReactiveCooldown = 3f;
    [Header("Anel dos mortos")]
    [Min(5f)] public float ringCooldown = 30f;

    [Header("Apagoes durante a busca")]
    [Min(10f)] public float blackoutFirstDelay = 45f;
    [Min(10f)] public float blackoutInterval = 50f;
    [Min(2f)] public float blackoutDuration = 8f;
    [Min(2f)] public float blackoutVisionRadius = 3.5f;

    [Header("Dificuldade por tempo")]
    [Min(10f)] public float difficultyStepSeconds = 60f;
    [Min(0f)] public float healthGrowthPerStep = 0.20f;
    [Min(0f)] public float damageGrowthPerStep = 0.15f;
    [Min(0f)] public float speedGrowthPerStep = 0.03f;
    [Min(1)] public int maximumAliveGhosts = 120;

    public int DifficultyAt(float seconds) => Mathf.Max(0, Mathf.FloorToInt(seconds / Mathf.Max(10f, difficultyStepSeconds)));
    public float HealthScale(float seconds) => 1f + DifficultyAt(seconds) * Mathf.Max(0f, healthGrowthPerStep);
    public float DamageScale(float seconds) => 1f + DifficultyAt(seconds) * Mathf.Max(0f, damageGrowthPerStep);
    public float SpeedScale(float seconds) => Mathf.Min(1.6f, 1f + DifficultyAt(seconds) * Mathf.Max(0f, speedGrowthPerStep));
    public float TimedSpawnInterval(float seconds) => Mathf.Max(0.15f, 0.5f / (1f + DifficultyAt(seconds) * 0.12f));
    public int TimedSpawnCount(float seconds) => Mathf.Min(4, 1 + DifficultyAt(seconds) / 3);

    public static int WaveAt(float seconds) => Mathf.Clamp(Mathf.FloorToInt(seconds / 300f), 0, 3);
    public static float SpawnInterval(int wave) => wave == 0 ? 0.5f : wave == 1 ? 0.375f : 0.25f;
    public static int SpawnCount(int wave) => wave >= 3 ? 2 : 1;
    public static float GhostMultiplier(int wave) => 1f + Mathf.Clamp(wave, 0, 3) * 0.5f;
}

