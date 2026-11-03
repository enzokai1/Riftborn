using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class MatchStats : MonoBehaviour
{
    [SerializeField] private FirstStageSettings settings = new FirstStageSettings();
    public FirstStageSettings Rules => settings;
    public bool BossReady => Objective != null && Objective.Assembled;
    public string FragmentProgress => Objective != null
        ? $"Fragmentos: {Objective.CollectedFragments}/{Objective.RequiredFragments}"
        : "Colete os fragmentos";

    public bool TryReserveGhost() => CombatActive && !IsPaused && !BossReady && !BossSpawned;

    public void PrepareBossEncounter()
    {
        if (!CombatActive || !BossReady || BossSpawned) return;
        // Remove the wave immediately, without invoking kill rewards or pausing for XP.
        foreach (EnemyHealth enemy in FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
        {
            enemy.gameObject.SetActive(false);
            Destroy(enemy.gameObject);
        }
    }
    public int KillCount { get; private set; }
    public int GhostKills { get; private set; }
    public bool BossSpawned { get; private set; }
    public bool BossDefeated { get; private set; }
    public bool Victory { get; private set; }
    public bool GameOver { get; private set; }
    public bool DeathMessageVisible { get; private set; }
    public bool Finished => Victory || GameOver;
    public bool CombatActive => !Finished && !BossDefeated;
    public bool IsPaused => !Finished && (userPaused || (Experience != null && Experience.HasUpgradeChoice));
    public float ElapsedSeconds { get; private set; }
    // Driven by match time so pauses and upgrade menus freeze the whole effect.
    private float BlackoutPhase => (ElapsedSeconds - Mathf.Max(10f, Rules.blackoutFirstDelay))
        % Mathf.Max(Rules.blackoutDuration + 12f, Rules.blackoutInterval);
    private bool CanBlackout => CombatActive && Objective != null && !BossReady
        && !BossSpawned && ElapsedSeconds >= Mathf.Max(10f, Rules.blackoutFirstDelay);
    public bool IsBlackout => CanBlackout && BlackoutPhase >= 2f
        && BlackoutPhase < 2f + Mathf.Max(2f, Rules.blackoutDuration);
    public float BlackoutDarkness
    {
        get
        {
            if (!CanBlackout) return 0f;
            float phase = BlackoutPhase;
            // Two slow dimming pulses signal the approaching darkness.
            if (phase < 2f) return 0.3f * Mathf.Pow(Mathf.Sin(phase * Mathf.PI), 2f);
            float age = phase - 2f;
            float duration = Mathf.Max(2f, Rules.blackoutDuration);
            if (age >= duration) return 0f;
            return Mathf.SmoothStep(0f, 1f, Mathf.Min(age / 0.7f, (duration - age) / 1f));
        }
    }
    public EnemyHealth Boss { get; private set; }
    public PlayerHealth Player { get; private set; }
    public PlayerExperience Experience { get; private set; }
    public FirstStageObjective Objective { get; private set; }
    public event Action KillsChanged;
    private bool userPaused;

    private void Start()
    {
        Time.timeScale = 1f;
        Player = FindFirstObjectByType<PlayerHealth>();
        if (Player == null) { enabled = false; return; }
        Experience = Player.GetComponent<PlayerExperience>();
        Player.Died += OnPlayerDied;
        if (Experience != null) Experience.ExperienceChanged += RefreshPause;
        PlayerRing ring = Player.GetComponent<PlayerRing>();
        if (ring == null) ring = Player.gameObject.AddComponent<PlayerRing>();
        ring.Configure(this);
        Objective = GetComponent<FirstStageObjective>();
        if (Objective == null) Objective = gameObject.AddComponent<FirstStageObjective>();
        Objective.Initialize(this);
        if (GetComponent<FirstStageHUD>() == null) gameObject.AddComponent<FirstStageHUD>();
        NotifyChanged();
    }

    private void Update()
    {
        if (!Finished && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame
            && (Experience == null || !Experience.HasUpgradeChoice))
        {
            userPaused = !userPaused;
            RefreshPause();
        }
        if (CombatActive && !IsPaused)
        {
            float firstEnd = Mathf.Max(10f, Rules.blackoutFirstDelay) + 2f + Mathf.Max(2f, Rules.blackoutDuration);
            float cycle = Mathf.Max(Rules.blackoutDuration + 12f, Rules.blackoutInterval);
            int previousEnd = Mathf.FloorToInt((ElapsedSeconds - firstEnd) / cycle);
            ElapsedSeconds += Time.deltaTime;
            int currentEnd = Mathf.FloorToInt((ElapsedSeconds - firstEnd) / cycle);
            if (currentEnd >= 0 && currentEnd > previousEnd && Objective != null && !BossReady && !BossSpawned)
                Objective.RelocateRemainingFragments();
        }
    }

    public void Resume() { userPaused = false; RefreshPause(); }
    private void RefreshPause() => Time.timeScale = IsPaused ? 0f : 1f;
    public void NotifyChanged() => KillsChanged?.Invoke();

    private void OnDestroy()
    {
        if (Player != null) Player.Died -= OnPlayerDied;
        if (Experience != null) Experience.ExperienceChanged -= RefreshPause;
        Time.timeScale = 1f;
    }

    public bool TryReserveBoss()
    {
        if (!CombatActive || IsPaused || !BossReady || BossSpawned) return false;
        PrepareBossEncounter();
        BossSpawned = true;
        NotifyChanged();
        return true;
    }

    public void SetBoss(EnemyHealth boss) => Boss = boss;

    public void RegisterKill(bool isBoss = false)
    {
        if (!CombatActive) return;
        KillCount++;
        if (isBoss)
        {
            BossDefeated = true;
            Vector3 dropPosition = Boss != null ? Boss.transform.position : Player.transform.position;
            StopCombat();
            Objective.DropRewards(dropPosition);
        }
        else GhostKills++;
        NotifyChanged();
    }

    public bool CompleteStage()
    {
        if (Finished || !BossDefeated || !Objective.RingCollected || !Objective.LetterCollected) return false;
        Victory = true;
        userPaused = false;
        RefreshPause();
        Player.GetComponent<PlayerController>().enabled = false;
        NotifyChanged();
        return true;
    }

    private void OnPlayerDied()
    {
        if (Finished) return;
        GameOver = true;
        userPaused = false;
        RefreshPause();
        StopCombat();
        StartCoroutine(ShowDeathMessage());
        NotifyChanged();
    }

    private System.Collections.IEnumerator ShowDeathMessage()
    {
        yield return new WaitForSecondsRealtime(3f);
        DeathMessageVisible = true;
        NotifyChanged();
    }

    private void StopCombat()
    {
        foreach (EnemyController enemy in FindObjectsByType<EnemyController>(FindObjectsSortMode.None)) enemy.enabled = false;
        foreach (EnemyContactDamage contact in FindObjectsByType<EnemyContactDamage>(FindObjectsSortMode.None)) contact.enabled = false;
        foreach (GunterAreaAttack attack in FindObjectsByType<GunterAreaAttack>(FindObjectsSortMode.None)) attack.enabled = false;
        foreach (Weapon weapon in FindObjectsByType<Weapon>(FindObjectsSortMode.None)) weapon.enabled = false;
        foreach (Projectile projectile in FindObjectsByType<Projectile>(FindObjectsSortMode.None))
        {
            projectile.gameObject.SetActive(false);
            Destroy(projectile.gameObject);
        }
        // Defeated ghosts must not physically block the path to the rewards.
        foreach (EnemyHealth enemy in FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
        {
            enemy.gameObject.SetActive(false);
            Destroy(enemy.gameObject);
        }
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().path);
    }
}

