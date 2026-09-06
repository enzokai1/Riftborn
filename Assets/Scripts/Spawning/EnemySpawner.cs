using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform player;
    [SerializeField] private MatchStats matchStats;
    [SerializeField, Min(0.01f)] private float minimumSpawnDistance = 5f;
    [SerializeField, Min(0.01f)] private float maximumSpawnDistance = 8f;
    [SerializeField, Min(0.01f)] private float spawnInterval = 2f;

    private float elapsedTime;

    private void Awake()
    {
        // A positive minimum keeps the spawn position away from the player's center.
        minimumSpawnDistance = Mathf.Max(0.01f, minimumSpawnDistance);
        maximumSpawnDistance = Mathf.Max(minimumSpawnDistance, maximumSpawnDistance);
        spawnInterval = Mathf.Max(0.01f, spawnInterval);

        if (enemyPrefab == null || player == null)
        {
            Debug.LogError("EnemySpawner requires an Enemy Prefab and a Player reference.", this);
            enabled = false;
            return;
        }

        if (!enemyPrefab.TryGetComponent<EnemyController>(out _))
        {
            Debug.LogError("EnemySpawner requires EnemyController on the Enemy Prefab's root GameObject.", this);
            enabled = false;
        }

        if (matchStats == null)
        {
            Debug.LogError("EnemySpawner requires a MatchStats scene reference.", this);
            enabled = false;
        }

        if (!enemyPrefab.TryGetComponent<EnemyKillReporter>(out _))
        {
            Debug.LogError("EnemySpawner requires EnemyKillReporter on the Enemy Prefab's root GameObject.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (player == null)
        {
            Debug.LogError("EnemySpawner lost its Player reference.", this);
            enabled = false;
            return;
        }

        elapsedTime += Time.deltaTime;
        if (elapsedTime < spawnInterval)
        {
            return;
        }

        elapsedTime = 0f;
        SpawnEnemy();
    }

    private void SpawnEnemy()
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float distance = Random.Range(minimumSpawnDistance, maximumSpawnDistance);
        Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * distance;
        Vector3 spawnPosition = player.position + offset;

        GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        EnemyController enemyController = enemy.GetComponent<EnemyController>();
        enemyController.SetTarget(player);
        EnemyKillReporter killReporter = enemy.GetComponent<EnemyKillReporter>();
        killReporter.SetMatchStats(matchStats);
    }
}
