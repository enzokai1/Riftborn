using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class KillCountUI : MonoBehaviour
{
    [SerializeField] private MatchStats matchStats;
    [SerializeField] private TMP_Text killCountText;

    private void OnEnable()
    {
        if (matchStats == null || killCountText == null)
        {
            Debug.LogError("KillCountUI requires Match Stats and Kill Count Text.", this);
            enabled = false;
            return;
        }

        matchStats.KillsChanged += RefreshUI;
        RefreshUI();
    }

    private void OnDisable()
    {
        if (matchStats != null)
            matchStats.KillsChanged -= RefreshUI;
    }

    private void RefreshUI()
    {
        string label = matchStats.KillCount == 1 ? "DERROTADO" : "DERROTADOS";
        killCountText.text = $"{matchStats.KillCount} {label}";
    }
}
