using System;
using UnityEngine;

// Values left as variables in the GDD are exposed on MatchStats in the Inspector.
[Serializable]
public class FirstStageSettings
{
    [Header("Fragmentos - quantidade não definida no GDD")]
    [Min(2)] public int fragmentCount = 4;
    [Min(1)] public float minimumFragmentPixels = 125f;
    [Min(1)] public float referencePixelsPerUnit = 100f;
    [Min(0.5f)] public float pickupRadius = 1.5f;
    [Header("Fantasma branco (demais cores: 1,5 / 2 / 2,5 vezes)")]
    [Min(1)] public int ghostHealth = 20;
    [Min(1)] public int ghostDamage = 5;
    [Min(0.1f)] public float ghostSpeed = 1.8f;
    [Header("Gunter")]
    [Min(1)] public int bossHealth = 1200;
    [Min(1)] public int bossContactDamage = 15;
    [Min(1)] public int bossAreaDamage = 20;
    [Min(0.1f)] public float bossSpeed = 2.2f;
    [Min(1f)] public float bossAreaRadius = 3.5f;
    [Min(1f)] public float bossAttackInterval = 6f;
    [Min(0.1f)] public float bossWarningDuration = 1.25f;
    [Header("Anel dos mortos")]
    [Min(5f)] public float ringCooldown = 30f;

    public static int WaveAt(float seconds) => Mathf.Clamp(Mathf.FloorToInt(seconds / 300f), 0, 3);
    public static float SpawnInterval(int wave) => wave == 0 ? 1f : wave == 1 ? 0.75f : 0.5f;
    public static int SpawnCount(int wave) => wave >= 3 ? 2 : 1;
    public static float GhostMultiplier(int wave) => 1f + Mathf.Clamp(wave, 0, 3) * 0.5f;
}

