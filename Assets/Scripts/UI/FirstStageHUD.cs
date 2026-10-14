using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(MatchStats))]
public class FirstStageHUD : MonoBehaviour
{
    private MatchStats match;

    private GUIStyle title;
    private GUIStyle body;
    private GUIStyle button;
    private GUIStyle prompt;
    private readonly Color panel = new Color(0.055f, 0.045f, 0.09f, 0.96f);

    private void Start()
    {
        match = GetComponent<MatchStats>();
        // Keep all gameplay information in the same HUD as objectives and menus.
        // This also works in scenes with missing or stale Canvas bindings.
        GameObject legacyHUD = GameObject.Find("RiftbornHUD");
        if (legacyHUD != null) legacyHUD.SetActive(false);
    }

    private void Update()
    {
        if (match == null) return;

        if (match.Finished || match.Experience == null || !match.Experience.HasUpgradeChoice || Keyboard.current == null) return;
        Keyboard keys = Keyboard.current;
        if (keys.digit1Key.wasPressedThisFrame) match.Experience.ChooseUpgrade(0);
        else if (keys.digit2Key.wasPressedThisFrame) match.Experience.ChooseUpgrade(1);
        else if (keys.digit3Key.wasPressedThisFrame) match.Experience.ChooseUpgrade(2);
    }

    private void Styles()
    {
        if (title != null) return;
        title = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, wordWrap = true };
        title.normal.textColor = new Color(0.9f, 0.8f, 1f);
        body = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true };
        body.normal.textColor = new Color(0.92f, 0.92f, 0.97f);
        prompt = new GUIStyle(body) { alignment = TextAnchor.MiddleCenter, fontSize = 18 };
        button = new GUIStyle(GUI.skin.button) { fontSize = 17, wordWrap = true };
    }

    private void Panel(Rect rect)
    {
        Color old = GUI.color;
        GUI.color = panel;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = old;
    }


    private void DrawCompass(Vector2 delta)
    {
        Vector2 center = new Vector2(1216, 160);
        Color old = GUI.color;
        for (int i = 0; i < 48; i++)
        {
            float a = i * Mathf.PI * 2f / 48f;
            float b = (i + 1) * Mathf.PI * 2f / 48f;
            DrawLine(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 38f,
                center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * 38f,
                new Color(0.75f, 0.69f, 0.85f), 2f);
        }
        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI / 4f;
            Vector2 axis = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            DrawLine(center + axis * 30f, center + axis * 36f, Color.white, 2f);
        }
        Vector2 direction = new Vector2(delta.x, -delta.y).normalized;
        if (direction.sqrMagnitude > 0.01f)
        {
            Vector2 tip = center + direction * 27f;
            Vector2 side = new Vector2(-direction.y, direction.x);
            Color needle = new Color(1f, 0.65f, 0.25f);
            DrawLine(center - direction * 20f, tip, needle, 3f);
            DrawLine(tip, tip - direction * 11f + side * 7f, needle, 3f);
            DrawLine(tip, tip - direction * 11f - side * 7f, needle, 3f);
        }
        GUI.color = old;
    }

    private static void DrawLine(Vector2 from, Vector2 to, Color color, float width)
    {
        Matrix4x4 matrix = GUI.matrix;
        Color previous = GUI.color;
        Vector2 delta = to - from;
        GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, from);
        GUI.color = color;
        GUI.DrawTexture(new Rect(from.x, from.y - width * 0.5f, delta.magnitude, width), Texture2D.whiteTexture);
        GUI.color = previous;
        GUI.matrix = matrix;
    }

    private static void Bar(Rect rect, float progress, Color fill)
    {
        Color previous = GUI.color;
        GUI.color = new Color(0.16f, 0.18f, 0.26f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = fill;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(progress), rect.height),
            Texture2D.whiteTexture);
        GUI.color = previous;
    }

    private void DrawStatus()
    {
        PlayerHealth player = match.Player;
        if (player != null)
        {
            Panel(new Rect(18, 620, 290, 82));
            GUI.Label(new Rect(34, 630, 258, 30),
                $"VIDA   {player.CurrentHealth} / {player.MaxHealth}", title);
            Bar(new Rect(34, 674, 258, 12),
                player.MaxHealth > 0 ? (float)player.CurrentHealth / player.MaxHealth : 0f,
                new Color(0.84f, 0.42f, 0.53f));
        }

        PlayerExperience experience = match.Experience;
        if (experience != null)
        {
            Panel(new Rect(410, 18, 460, 90));
            Weapon weapon = experience.GetComponentInChildren<Weapon>();
            int damage = weapon != null ? weapon.CurrentDamage : 10 + experience.DamageBonus;
            GUI.Label(new Rect(428, 24, 424, 30),
                $"Nível {experience.CurrentLevel}  |  Dano {damage}", title);
            Bar(new Rect(428, 62, 424, 10),
                experience.NextLevelExperience > 0
                    ? (float)experience.CurrentExperience / experience.NextLevelExperience : 0f,
                new Color(0.55f, 0.36f, 0.96f));
            GUI.Label(new Rect(428, 77, 424, 25),
                $"{experience.CurrentExperience} / {experience.NextLevelExperience} XP", body);
        }

        Panel(new Rect(942, 18, 320, 90));
        int seconds = Mathf.FloorToInt(match.ElapsedSeconds);
        GUI.Label(new Rect(958, 24, 288, 28), $"FASE 1   {seconds / 60:00}:{seconds % 60:00}", title);
        string progress = match.BossDefeated ? "Gunter derrotado"
            : match.BossSpawned ? "Resta: Gunter"
            : $"Fantasmas: {match.GhostKills}/{MatchStats.GhostTarget}  |  Restam: {match.GhostsRemaining}";
        GUI.Label(new Rect(958, 60, 288, 40), progress, body);
    }

    private void OnGUI()
    {
        if (match == null || match.Objective == null) return;
        Styles();
        Matrix4x4 previous = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / 1280f, Screen.height / 720f, 1));
        DrawStatus();
        Panel(new Rect(18, 18, 325, 90));
        GUI.Label(new Rect(34, 30, 293, 68), match.Objective.Instruction, body);
        if ((!match.BossSpawned || match.BossDefeated) && !match.Finished)
            DrawCompass((Vector2)(match.Objective.GuidanceTarget - match.Player.transform.position));
        if (match.Boss != null && !match.BossDefeated && !match.Finished)
        {
            Panel(new Rect(425, 115, 430, 65));
            GUI.Label(new Rect(443, 122, 400, 28), $"GUNTER   {match.Boss.CurrentHealth} / {match.Boss.MaximumHealth}", title);
            Color old = GUI.color;
            GUI.color = new Color(0.7f, 0.25f, 0.8f);
            GUI.DrawTexture(new Rect(443, 159, 394f * match.Boss.CurrentHealth / match.Boss.MaximumHealth, 7), Texture2D.whiteTexture);
            GUI.color = old;
        }
        if (!match.Finished && !match.IsPaused && (match.Experience == null || !match.Experience.HasUpgradeChoice))
            GUI.Label(new Rect(390, 664, 500, 38), "Se necessário, pressione E", prompt);
        if (match.Victory || match.DeathMessageVisible)
        {
            Panel(new Rect(390, 235, 500, 270));
            GUI.Label(new Rect(415, 255, 450, 40), match.Victory ? "FASE 1 CONCLUÍDA" : "VOCÊ MORREU", title);
            GUI.Label(new Rect(415, 308, 450, 100), match.Victory
                ? "Você derrotou Gunter e recuperou a carta que aprisiona sua mãe. O Anel dos Mortos estará disponível desde o início das próximas tentativas."
                : "Os fragmentos serão espalhados novamente. Colete energia e escolha melhorias para enfrentar Gunter.", body);
            if (GUI.Button(new Rect(465, 430, 350, 48), "Jogar novamente", button)) match.Restart();
        }
        else if (!match.Finished && match.Experience != null && match.Experience.HasUpgradeChoice)
        {
            Panel(new Rect(205, 220, 870, 305));
            GUI.Label(new Rect(233, 240, 814, 40), $"NÍVEL {match.Experience.CurrentLevel} · ESCOLHA UMA MELHORIA", title);
            GUI.Label(new Rect(233, 286, 814, 34), "O jogo fica pausado enquanto você escolhe. Use 1, 2, 3 ou clique.", body);
            for (int i = 0; i < match.Experience.Choices.Count; i++)
            {
                if (GUI.Button(new Rect(232 + i * 278, 340, 258, 130),
                    $"{i + 1}\n{match.Experience.Describe(match.Experience.Choices[i])}", button))
                { match.Experience.ChooseUpgrade(i); break; }
            }
        }
        else if (match.IsPaused)
        {
            Panel(new Rect(465, 265, 350, 190));
            GUI.Label(new Rect(490, 288, 300, 40), "JOGO PAUSADO", title);
            if (GUI.Button(new Rect(490, 355, 300, 55), "Continuar", button)) match.Resume();
        }
        GUI.matrix = previous;
    }
}
