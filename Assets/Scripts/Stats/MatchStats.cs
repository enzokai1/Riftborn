using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class MatchStats : MonoBehaviour
{
    [SerializeField] private FirstStageSettings settings = new FirstStageSettings();
    public FirstStageSettings Rules => settings;
    public const int GhostTarget = 50;
    public int GhostsRemaining => Mathf.Max(0, GhostTarget - GhostKills);
    public bool BossReady => GhostsRemaining == 0 || (Objective != null && Objective.Assembled);
    private int ghostsSpawned;

    public bool TryReserveGhost()
    {
        if (!CombatActive || BossSpawned || ghostsSpawned >= GhostTarget) return false;
        ghostsSpawned++;
        return true;
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
        if (CombatActive && !IsPaused) ElapsedSeconds += Time.deltaTime;
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
        if (!CombatActive || !BossReady || BossSpawned) return false;
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

