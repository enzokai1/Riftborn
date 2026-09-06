using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CircleCollider2D))]
public class ExperiencePickup : MonoBehaviour
{
    [SerializeField, Min(1)] private int experienceAmount = 5;

    private bool collected;

    private void Awake()
    {
        experienceAmount = Mathf.Max(1, experienceAmount);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected || !other.TryGetComponent(out PlayerExperience playerExperience))
        {
            return;
        }

        // Mark before awarding XP to prevent duplicate collection callbacks.
        collected = true;
        playerExperience.AddExperience(experienceAmount);
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
