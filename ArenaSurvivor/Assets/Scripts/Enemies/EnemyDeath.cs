using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class EnemyDeath : MonoBehaviour
{
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
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
