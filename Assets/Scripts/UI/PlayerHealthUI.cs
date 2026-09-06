using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PlayerHealthUI : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Slider healthBar;
    [SerializeField] private TMP_Text healthLabel;

    private void OnEnable()
    {
        if (playerHealth == null || healthBar == null || healthLabel == null)
        {
            Debug.LogError("PlayerHealthUI requires Player Health, Health Bar and Health Label.", this);
            enabled = false;
            return;
        }

        playerHealth.HealthChanged += RefreshUI;
        RefreshUI();
    }

    private void Start()
    {
        // All scene Awake calls have completed, regardless of object initialization order.
        RefreshUI();
    }

    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.HealthChanged -= RefreshUI;
    }

    private void RefreshUI()
    {
        float progress = playerHealth.MaxHealth > 0
            ? (float)playerHealth.CurrentHealth / playerHealth.MaxHealth
            : 0f;
        healthBar.SetValueWithoutNotify(Mathf.Clamp01(progress));
        healthLabel.text = $"VIDA   {playerHealth.CurrentHealth} / {playerHealth.MaxHealth}";
    }
}
