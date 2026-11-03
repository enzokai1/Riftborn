using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CircleCollider2D))]
public class ExperiencePickup : MonoBehaviour
{
    [SerializeField, Min(1)] private int experienceAmount = 5;
    [SerializeField, Min(0.1f)] private float collectionRadius = 1.5f;
    private bool collected;
    private bool attracted;
    private PlayerExperience player;
    private PlayerHealth health;
    private MatchStats match;

    private void Start()
    {
        player = FindFirstObjectByType<PlayerExperience>();
        if (player != null) health = player.GetComponent<PlayerHealth>();
        match = FindFirstObjectByType<MatchStats>();
    }

    private void Update()
    {
        if (player == null || !CanCollect) return;

        Vector3 target = player.transform.position;
        target.z = transform.position.z;
        float distance = Vector2.Distance(transform.position, target);
        if (distance <= collectionRadius)
        {
            TryCollect(player);
            return;
        }

        // Once inside the field, the orb follows the player until collected.
        if (distance <= player.ExperienceAttractionRadius) attracted = true;
        if (!attracted) return;
        transform.position = Vector3.MoveTowards(transform.position, target,
            player.ExperienceAttractionSpeed * Time.deltaTime);
        if (Vector2.Distance(transform.position, target) <= collectionRadius)
            TryCollect(player);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out PlayerExperience experience)) TryCollect(experience);
    }

    private bool CanCollect => !collected && Time.timeScale > 0f
        && (health == null || health.CurrentHealth > 0)
        && (match == null || (match.CombatActive && !match.IsPaused));

    private void TryCollect(PlayerExperience experience)
    {
        if (!CanCollect) return;
        collected = true;
        experience.AddExperience(Mathf.Max(1, experienceAmount));
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}

