using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class KillCountUI : MonoBehaviour
{
    [SerializeField] private MatchStats matchStats;
    [SerializeField] private TMP_Text killCountText;

    private void OnEnable()
    {
        if (matchStats == null || killCountText == null) { enabled = false; return; }
        matchStats.KillsChanged += RefreshUI;
        RefreshUI();
    }

    private void OnDisable()
    {
        if (matchStats != null) matchStats.KillsChanged -= RefreshUI;
    }

    private void RefreshUI()
    {
        killCountText.text = matchStats.DeathMessageVisible ? "VOCÊ MORREU"
            : matchStats.Victory ? "QUARTO CONCLUÍDO"
            : matchStats.BossDefeated ? "GUNTER DERROTADO"
            : matchStats.BossSpawned ? "DERROTE GUNTER"
            : $"Derrotados: {matchStats.GhostKills}\n{matchStats.FragmentProgress}";
    }
}

