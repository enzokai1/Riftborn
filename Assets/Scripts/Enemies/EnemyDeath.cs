using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class EnemyDeath : MonoBehaviour
{
    [SerializeField] private GameObject experiencePickupPrefab;

    private EnemyHealth enemyHealth;

    private void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth>();
    }

    private void OnEnable()
    {
        enemyHealth.Died += HandleDeath;
    }

    private void OnDisable()
    {
        enemyHealth.Died -= HandleDeath;
    }

    private void HandleDeath()
    {
        // The boss keeps its completion reward; normal ghosts drop collectible XP.
        EnemyKillReporter reporter = GetComponent<EnemyKillReporter>();
        if (reporter != null && reporter.IsBoss)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }
        if (experiencePickupPrefab != null)
        {
            Instantiate(experiencePickupPrefab, transform.position, Quaternion.identity);
        }
        else
        {
            Debug.LogError("EnemyDeath requires an Experience Pickup Prefab to drop XP.", this);
        }

        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
