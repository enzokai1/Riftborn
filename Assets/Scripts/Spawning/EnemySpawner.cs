using UnityEngine;

[DisallowMultipleComponent]
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform player;
    [SerializeField] private MatchStats matchStats;
    [SerializeField, Min(0.01f)] private float minimumSpawnDistance = 5f;
    [SerializeField, Min(0.01f)] private float maximumSpawnDistance = 8f;
    private float elapsedTime;

    private void Awake()
    {
        minimumSpawnDistance = Mathf.Max(1f, minimumSpawnDistance);
        maximumSpawnDistance = Mathf.Max(minimumSpawnDistance, maximumSpawnDistance);
        if (enemyPrefab == null || player == null || matchStats == null
            || !enemyPrefab.TryGetComponent<EnemyController>(out _)
            || !enemyPrefab.TryGetComponent<EnemyKillReporter>(out _))
        {
            Debug.LogError("EnemySpawner: configure prefab, jogador e MatchStats.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (player == null || !matchStats.CombatActive || matchStats.IsPaused) return;
        if (matchStats.BossReady && !matchStats.BossSpawned)
        {
            SpawnEnemy(true, 0);
            return;
        }
        // Gunter is the final encounter; no additional waves during his fight.
        if (matchStats.BossSpawned) return;
        int wave = FirstStageSettings.WaveAt(matchStats.ElapsedSeconds);
        elapsedTime += Time.deltaTime;
        float interval = FirstStageSettings.SpawnInterval(wave);
        if (elapsedTime < interval) return;
        elapsedTime -= interval;
        for (int i = 0; i < FirstStageSettings.SpawnCount(wave); i++) SpawnEnemy(false, wave);
    }

    private void SpawnEnemy(bool boss, int wave)
    {
        Vector3 position = player.position;
        bool found = false;
        for (int attempt = 0; attempt < 64; attempt++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float distance = Random.Range(minimumSpawnDistance, maximumSpawnDistance);
            position = player.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
            if (matchStats.Objective != null && matchStats.Objective.IsFurnitureBlocked(position, boss ? 2.5f : 1f))
                continue;
            if (HouseFloor.Active == null || HouseFloor.Active.Contains(position, boss ? 2.5f : 1f))
            { found = true; break; }
        }
        if (!found || (boss ? !matchStats.TryReserveBoss() : !matchStats.TryReserveGhost())) return;
        GameObject enemy = Instantiate(enemyPrefab, position, Quaternion.identity);
        EnemyController controller = enemy.GetComponent<EnemyController>();
        controller.SetTarget(player);
        EnemyKillReporter reporter = enemy.GetComponent<EnemyKillReporter>();
        reporter.SetMatchStats(matchStats);
        FirstStageSettings rules = matchStats.Rules;
        EnemyHealth health = enemy.GetComponent<EnemyHealth>();
        EnemyContactDamage contact = enemy.GetComponent<EnemyContactDamage>();
        SpriteRenderer visual = enemy.GetComponent<SpriteRenderer>();
        if (boss)
        {
            enemy.name = "Gunter";
            enemy.transform.localScale *= 2.5f;
            visual.color = new Color(0.65f, 0.3f, 0.95f);
            health.ConfigureHealth(rules.bossHealth);
            controller.SetMovementSpeed(rules.bossSpeed);
            contact.SetDamage(rules.bossContactDamage);
            reporter.MarkAsBoss();
            matchStats.SetBoss(health);
            enemy.AddComponent<GunterAreaAttack>().Initialize(matchStats, visual);
        }
        else
        {
            string[] names = { "Fantasma branco", "Fantasma verde", "Fantasma azul", "Fantasma vermelho" };
            Color[] colors = { Color.white, new Color(0.4f, 1f, 0.45f), new Color(0.35f, 0.65f, 1f), new Color(1f, 0.35f, 0.4f) };
            float multiplier = FirstStageSettings.GhostMultiplier(wave);
            enemy.name = names[wave];
            visual.color = colors[wave];
            health.ConfigureHealth(Mathf.CeilToInt(rules.ghostHealth * multiplier));
            contact.SetDamage(Mathf.CeilToInt(rules.ghostDamage * multiplier));
            controller.SetMovementSpeed(rules.ghostSpeed);
        }
    }
}

