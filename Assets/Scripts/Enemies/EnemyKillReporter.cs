using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyHealth))]
public class EnemyKillReporter : MonoBehaviour
{
    [SerializeField] private MatchStats matchStats;

    private EnemyHealth enemyHealth;
    private bool reported;

    private void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth>();
    }

    private void OnEnable()
    {
        enemyHealth.Died += ReportDeath;
    }

    private void Start()
    {
        if (matchStats == null)
            Debug.LogError("EnemyKillReporter requires MatchStats in the Inspector or through SetMatchStats().", this);
    }

    private void OnDisable()
    {
        if (enemyHealth != null)
            enemyHealth.Died -= ReportDeath;
    }

    public void SetMatchStats(MatchStats matchStats)
    {
        this.matchStats = matchStats;
    }

    private void ReportDeath()
    {
        // Died may already be invoking this callback when EnemyDeath disables us.
        // Do not reject the notification based on isActiveAndEnabled.
        if (reported)
            return;

        reported = true;
        if (matchStats == null)
        {
            Debug.LogError("EnemyKillReporter could not register the death: MatchStats is missing.", this);
            return;
        }

        matchStats.RegisterKill();
    }
}
