using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PlayerProgressionUI : MonoBehaviour
{
    [SerializeField] private PlayerExperience playerExperience;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Slider experienceBar;

    private void OnEnable()
    {
        if (playerExperience == null || levelText == null || experienceBar == null)
        {
            Debug.LogError("PlayerProgressionUI requires Player Experience, Level Text and Experience Bar references.", this);
            enabled = false;
            return;
        }

        playerExperience.ExperienceChanged += RefreshUI;
        RefreshUI();
    }

    private void OnDisable()
    {
        if (playerExperience != null)
        {
            playerExperience.ExperienceChanged -= RefreshUI;
        }
    }

    private void RefreshUI()
    {
        levelText.text = $"Nível {playerExperience.CurrentLevel}";

        float progress = playerExperience.NextLevelExperience > 0
            ? (float)playerExperience.CurrentExperience / playerExperience.NextLevelExperience
            : 0f;

        experienceBar.SetValueWithoutNotify(Mathf.Clamp01(progress));
    }
}
