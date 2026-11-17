using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(MatchStats))]
public class FirstStageHUD : MonoBehaviour
{
    private MatchStats match;
    private GUIStyle title, body, caption, centered, number, button;
    private Font interfaceFont;
    private Texture2D panelGradient;
    private GUIStyle hudTimer, hudKills, hudLevel, hudHealth, hudObjective;
    private Texture2D skullIcon;
    private Sprite fragmentIcon;
    private enum FrameKind { Objective, Vital, Experience, Quiet, Boss, Ornate }
    private GUIStyle[] fantasyFrames;
    private Texture2D menuDivider, bossDivider;
    private float viewWidth, viewHeight, uiScale = 1f;
    private static readonly Color Violet = new Color(0.68f, 0.49f, 1f);
    private static readonly Color Gold = new Color(0.72f, 0.59f, 0.35f);
    private static readonly Color Rose = new Color(0.78f, 0.19f, 0.29f);
    private static readonly Color Mint = new Color(0.43f, 0.91f, 0.72f);

    private void Start()
    {
        match = GetComponent<MatchStats>();
        GameObject legacyHUD = GameObject.Find("RiftbornHUD");
        if (legacyHUD != null) legacyHUD.SetActive(false);
    }

    private void Update()
    {
        if (match == null || match.Finished || match.Experience == null
            || !match.Experience.HasUpgradeChoice || Keyboard.current == null) return;
        Keyboard keys = Keyboard.current;
        if (keys.digit1Key.wasPressedThisFrame) match.Experience.ChooseUpgrade(0);
        else if (keys.digit2Key.wasPressedThisFrame) match.Experience.ChooseUpgrade(1);
        else if (keys.digit3Key.wasPressedThisFrame) match.Experience.ChooseUpgrade(2);
    }

    private GUIStyle TextStyle(int size, Color color, FontStyle weight = FontStyle.Normal,
        TextAnchor alignment = TextAnchor.UpperLeft)
    {
        var style = new GUIStyle(GUI.skin.label) {
            font = interfaceFont, fontSize = size,
            fontStyle = interfaceFont != null ? FontStyle.Normal : weight, wordWrap = true,
            alignment = alignment, padding = new RectOffset()
        };
        style.normal.textColor = color;
        return style;
    }

    private void Styles()
    {
        if (title != null) return;
        interfaceFont = Resources.Load<Font>("UI/Fonts/alagard-riftborn-pt");
        string[] frames = { "frame", "frame-014", "frame-001", "frame-015", "frame-023", "frame-002" };
        int[] corners = { 12, 10, 6, 2, 10, 12 };
        fantasyFrames = new GUIStyle[frames.Length];
        for (int i = 0; i < frames.Length; i++)
        {
            fantasyFrames[i] = new GUIStyle(GUIStyle.none) {
                border = new RectOffset(corners[i], corners[i], corners[i], corners[i])
            };
            fantasyFrames[i].normal.background = Resources.Load<Texture2D>("UI/Fantasy/" + frames[i]);
        }
        menuDivider = Resources.Load<Texture2D>("UI/Fantasy/divider-003");
        bossDivider = Resources.Load<Texture2D>("UI/Fantasy/divider-005");
        title = TextStyle(24, new Color(0.96f, 0.90f, 0.76f), FontStyle.Bold);
        body = TextStyle(18, Color.white, FontStyle.Bold);
        caption = TextStyle(14, new Color(0.93f, 0.90f, 1f), FontStyle.Bold);
        centered = TextStyle(18, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
        number = TextStyle(22, new Color(1f, 0.88f, 0.61f), FontStyle.Bold, TextAnchor.MiddleRight);
        Font hudFont = interfaceFont;
        hudTimer = HudStyle(hudFont, 42, TextAnchor.MiddleCenter);
        hudKills = HudStyle(hudFont, 34, TextAnchor.MiddleLeft);
        hudLevel = HudStyle(hudFont, 28, TextAnchor.MiddleRight);
        hudHealth = HudStyle(hudFont, 20, TextAnchor.MiddleCenter);
        hudObjective = HudStyle(hudFont, 22, TextAnchor.MiddleLeft);
        fragmentIcon = Resources.Load<Sprite>("Art/Fragments/Crystal00");
        skullIcon = Resources.Load<Texture2D>("UI/Icons/KillSkull");
        // Native button control retains mouse/keyboard behavior; its surface is drawn below.
        button = new GUIStyle(GUIStyle.none) { stretchWidth = false, stretchHeight = false };
        panelGradient = new Texture2D(1, 32, TextureFormat.RGBA32, false) {
            name = "Riftborn HUD gradient", hideFlags = HideFlags.HideAndDontSave,
            wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear
        };
        for (int y = 0; y < 32; y++)
            panelGradient.SetPixel(0, y, Color.Lerp(
                new Color(0.035f, 0.025f, 0.06f, 1f),
                new Color(0.065f, 0.045f, 0.09f, 0.94f), y / 31f));
        panelGradient.Apply(false, true);
    }

    private void Label(Rect rect, string text, GUIStyle style)
    {
        if (Event.current.type != EventType.Repaint) return;
        // Rasterize glyphs at their final size instead of stretching the font atlas.
        // Snap both edges to screen pixels to avoid fractional text placement.
        Rect pixels = Rect.MinMaxRect(Mathf.Round(rect.xMin * uiScale), Mathf.Round(rect.yMin * uiScale),
            Mathf.Round(rect.xMax * uiScale), Mathf.Round(rect.yMax * uiScale));
        Matrix4x4 previous = GUI.matrix;
        int fontSize = style.fontSize;
        Color faceColor = style.normal.textColor;
        try
        {
            GUI.matrix = Matrix4x4.identity;
            style.fontSize = Mathf.Max(1, Mathf.RoundToInt(fontSize * uiScale));
            // Draw every layer on the same screen-pixel grid. Keep the outline thin
            // so Alagard's small counters and decorative cuts remain readable.
            float edge = Mathf.Max(1f, Mathf.Round(style.fontSize / 32f));
            float depth = edge * 2f;
            style.normal.textColor = new Color(0.015f, 0.02f, 0.025f, faceColor.a);
            // Explicitly use the normal state for every layer. GUI.Label selects
            // hover under the cursor, which otherwise turns the dark passes white.
            var content = new GUIContent(text);
            style.Draw(new Rect(pixels.x + depth, pixels.y + depth, pixels.width, pixels.height), content, false, false, false, false);
            for (int y = -1; y <= 1; y++)
            for (int x = -1; x <= 1; x++)
                if (x != 0 || y != 0)
                    style.Draw(new Rect(pixels.x + x * edge, pixels.y + y * edge,
                        pixels.width, pixels.height), content, false, false, false, false);
            style.normal.textColor = faceColor;
            style.Draw(pixels, content, false, false, false, false);
        }
        finally
        {
            style.normal.textColor = faceColor;
            style.fontSize = fontSize;
            GUI.matrix = previous;
        }
    }

    private static void Rounded(Rect rect, Color color, float radius = 10f)
    {
        if (Event.current.type != EventType.Repaint || rect.width <= 0f || rect.height <= 0f) return;
        GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, color, 0f, radius);
    }

    private void Frame(Rect rect, Color tint, FrameKind kind = FrameKind.Objective)
    {
        if (Event.current.type != EventType.Repaint) return;
        Color previous = GUI.backgroundColor;
        GUI.backgroundColor = tint;
        fantasyFrames[(int)kind].Draw(rect, GUIContent.none, false, false, false, false);
        GUI.backgroundColor = previous;
    }

    private void Panel(Rect rect, Color accent, FrameKind kind = FrameKind.Objective)
    {
        Rounded(new Rect(rect.x + 2, rect.y + 5, rect.width, rect.height),
            new Color(0f, 0f, 0f, 0.35f), 0);
        if (Event.current.type == EventType.Repaint)
        {
            Color previous = GUI.color;
            GUI.color = new Color(previous.r, previous.g, previous.b,
                previous.a * (kind == FrameKind.Ornate ? 1f : 0.84f));
            GUI.DrawTexture(new Rect(rect.x + 3, rect.y + 3, rect.width - 6, rect.height - 6),
                panelGradient, ScaleMode.StretchToFill, true);
            GUI.color = previous;
        }
        Frame(rect, Color.Lerp(Gold, accent, 0.25f), kind);
        Rounded(new Rect(rect.x + 20, rect.y + 9, rect.width - 40, 1),
            new Color(accent.r, accent.g, accent.b, 0.2f), 0);
    }

    private void Bar(Rect rect, float progress, Color fill, FrameKind kind = FrameKind.Quiet)
    {
        Rounded(rect, new Color(0.015f, 0.012f, 0.025f), 0);
        float width = (rect.width - 6) * Mathf.Clamp01(progress);
        if (width > 0f)
        {
            Rounded(new Rect(rect.x + 3, rect.y + 3, width, rect.height - 6), fill, 0);
            Rounded(new Rect(rect.x + 3, rect.y + 3, width, 2),
                new Color(1f, 1f, 1f, 0.25f), 0);
        }
        // Keep the frame visible even when the bar is empty.
        Frame(new Rect(rect.x - 2, rect.y - 2, rect.width + 4, rect.height + 4), Gold, kind);
    }
    private void Divider(Rect rect, Texture2D texture, Color tint, bool mirror = false)
    {
        if (Event.current.type != EventType.Repaint || texture == null) return;
        Color previous = GUI.color;
        GUI.color = tint;
        // Preserve the ornament's proportions; only extend its fading line.
        float cap = Mathf.Min(rect.width, rect.height * 2f);
        float sourceCap = Mathf.Min(1f, texture.height * 2f / texture.width);
        Rect line = new Rect(rect.x, rect.y, rect.width - cap, rect.height);
        Rect tip = new Rect(rect.xMax - cap, rect.y, cap, rect.height);
        if (mirror)
        {
            tip.x = rect.x;
            line.x = rect.x + cap;
        }
        GUI.DrawTextureWithTexCoords(line, texture,
            mirror ? new Rect(1f - sourceCap, 0, -(1f - sourceCap), 1)
                   : new Rect(0, 0, 1f - sourceCap, 1), true);
        GUI.DrawTextureWithTexCoords(tip, texture,
            mirror ? new Rect(1, 0, -sourceCap, 1)
                   : new Rect(1f - sourceCap, 0, sourceCap, 1), true);
        GUI.color = previous;
    }

    private static void Diamond(Vector2 center, float size, Color color)
    {
        Matrix4x4 previous = GUI.matrix;
        GUIUtility.RotateAroundPivot(45f, center);
        Rounded(new Rect(center.x - size / 2, center.y - size / 2, size, size), color, 1);
        GUI.matrix = previous;
    }

    private bool ActionButton(Rect rect, string text, Color accent, FrameKind kind = FrameKind.Experience)
    {
        bool hover = rect.Contains(Event.current.mousePosition);
        Rounded(rect, hover ? new Color(0.19f, 0.13f, 0.25f) : new Color(0.06f, 0.04f, 0.09f), 0);
        Frame(rect, hover ? Color.Lerp(accent, Color.white, 0.3f) : Color.Lerp(Gold, Color.gray, 0.35f), kind);
        bool clicked = GUI.Button(rect, GUIContent.none, button);
        Label(new Rect(rect.x + 14, rect.y + 8, rect.width - 28, rect.height - 16), text, centered);
        return clicked;
    }

    private GUIStyle HudStyle(Font font, int size, TextAnchor alignment)
    {
        GUIStyle style = TextStyle(size, Color.white, FontStyle.Normal, alignment);
        style.font = font;
        style.wordWrap = false;
        style.clipping = TextClipping.Clip;
        return style;
    }

    private void HudSprite(Rect rect, Sprite sprite)
    {
        if (sprite == null || Event.current.type != EventType.Repaint) return;
        Rect source = sprite.textureRect;
        Texture2D texture = sprite.texture;
        GUI.DrawTextureWithTexCoords(rect, texture, new Rect(source.x / texture.width,
            source.y / texture.height, source.width / texture.width, source.height / texture.height));
    }
    private void CompactBar(Rect rect, float progress, Color fill)
    {
        Rounded(rect, new Color(0.28f, 0.28f, 0.28f), 0);
        Rounded(new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4), new Color(0.015f, 0.015f, 0.02f), 0);
        float width = (rect.width - 6) * Mathf.Clamp01(progress);
        if (width > 0) Rounded(new Rect(rect.x + 3, rect.y + 3, width, rect.height - 6), fill, 0);
    }

    private void OutlinedLabel(Rect rect, string text, GUIStyle style)
    {
        // Label applies the same pixel outline and depth to HUD and menu text.
        Label(rect, text, style);
    }
    private void DrawStatus()
    {
        PlayerExperience experience = match.Experience;
        float progress = experience != null && experience.NextLevelExperience > 0
            ? (float)experience.CurrentExperience / experience.NextLevelExperience : 0f;
        CompactBar(new Rect(0, 0, viewWidth, 22), progress, new Color(0f, 0.72f, 0.9f));

        // The approved layout keeps all three counters free of background panels.
        if (Event.current.type == EventType.Repaint && skullIcon != null)
            GUI.DrawTexture(new Rect(14, 36, 34, 40), skullIcon, ScaleMode.ScaleToFit, true);
        OutlinedLabel(new Rect(54, 32, viewWidth * 0.3f - 54, 48), match.GhostKills.ToString(), hudKills);
        int seconds = Mathf.FloorToInt(match.ElapsedSeconds);
        OutlinedLabel(new Rect(viewWidth * 0.35f, 30, viewWidth * 0.3f, 52),
            $"{seconds / 60:00}:{seconds % 60:00}", hudTimer);
        // The level stays smaller and visually secondary to the central timer.
        OutlinedLabel(new Rect(viewWidth - 300, 32, 280, 48),
            $"NÍVEL {(experience != null ? experience.CurrentLevel : 1)}", hudLevel);

        PlayerHealth player = match.Player;
        if (player != null)
        {
            Rect health = new Rect(16, 90, 224, 30);
            CompactBar(health, player.MaxHealth > 0 ? (float)player.CurrentHealth / player.MaxHealth : 0f,
                new Color(0.76f, 0.035f, 0.12f));
            OutlinedLabel(health, $"{player.CurrentHealth} / {player.MaxHealth}", hudHealth);
        }

        bool fragments = !match.BossReady && !match.BossSpawned && !match.BossDefeated;
        string objective = fragments
            ? $"Fragmentos: {match.Objective.CollectedFragments} / {match.Objective.RequiredFragments}"
            : match.Objective.Instruction;
        float objectiveWidth = fragments ? 254f : 320f;
        float objectiveHeight = fragments ? 42f : 64f;
        Rounded(new Rect(16, 145, objectiveWidth, objectiveHeight), new Color(0, 0, 0, 0.78f), 0);
        if (fragments) HudSprite(new Rect(24, 154, 16, 24), fragmentIcon);
        OutlinedLabel(new Rect(fragments ? 49 : 28, 153, objectiveWidth - (fragments ? 42 : 24),
            objectiveHeight - 12), objective, fragments ? hudObjective : body);

        if (match.Boss != null && !match.BossDefeated && !match.Finished)
        {
            float x = (viewWidth - 430) / 2;
            Panel(new Rect(x, 142, 430, 100), Rose, FrameKind.Boss);
            Divider(new Rect(x + 24, 162, 120, 12), bossDivider, Gold);
            Divider(new Rect(x + 286, 162, 120, 12), bossDivider, Gold, true);
            Label(new Rect(x + 150, 152, 130, 32), "GUNTER", centered);
            Label(new Rect(x + 20, 183, 390, 24),
                $"{match.Boss.CurrentHealth} / {match.Boss.MaximumHealth}", centered);
            Bar(new Rect(x + 20, 212, 390, 18),
                match.Boss.MaximumHealth > 0 ? (float)match.Boss.CurrentHealth / match.Boss.MaximumHealth : 0f, Rose, FrameKind.Boss);
        }
    }

    private void DrawExitPrompt()
    {
        if (!match.Objective.CanExit) return;
        Camera camera = Camera.main;
        if (camera == null) return;
        Vector3 screen = camera.WorldToViewportPoint(match.Objective.ExitPosition + Vector3.up * 2.1f);
        if (screen.z <= 0 || screen.x < 0 || screen.x > 1 || screen.y < 0 || screen.y > 1) return;
        float x = Mathf.Clamp(screen.x * viewWidth - 150, 20, viewWidth - 320);
        float y = Mathf.Clamp((1f - screen.y) * viewHeight - 56, 220, viewHeight - 180);
        Panel(new Rect(x, y, 300, 64), Mint);
        Rounded(new Rect(x + 14, y + 13, 36, 36), new Color(0.16f, 0.33f, 0.29f), 7);
        Label(new Rect(x + 14, y + 13, 36, 36), "E", centered);
        Label(new Rect(x + 64, y + 12, 220, 18), "SAÍDA LIBERADA", caption);
        Label(new Rect(x + 64, y + 32, 220, 24), "Pressione E para sair", body);
    }

    private Texture2D blackoutMask;

    private void DrawBlackout()
    {
        float darkness = match.BlackoutDarkness;
        Camera camera = Camera.main;
        if (darkness <= 0f || camera == null || match.Player == null) return;
        if (blackoutMask == null)
        {
            const int size = 256;
            blackoutMask = new Texture2D(size, size, TextureFormat.RGBA32, false);
            blackoutMask.name = "Blackout vision mask";
            blackoutMask.hideFlags = HideFlags.HideAndDontSave;
            blackoutMask.wrapMode = TextureWrapMode.Clamp;
            blackoutMask.filterMode = FilterMode.Bilinear;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float distance = new Vector2((x + 0.5f) / size * 2f - 1f,
                    (y + 0.5f) / size * 2f - 1f).magnitude;
                float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1f, distance));
                pixels[y * size + x] = new Color(0.015f, 0.01f, 0.025f, alpha);
            }
            blackoutMask.SetPixels(pixels);
            blackoutMask.Apply(false, true);
        }
        Vector3 world = match.Player.transform.position;
        Vector3 screen = camera.WorldToScreenPoint(world);
        Vector3 edge = camera.WorldToScreenPoint(world + camera.transform.right * Mathf.Max(2f, match.Rules.blackoutVisionRadius));
        float diameter = Mathf.Max(1f, Vector2.Distance(screen, edge) * 2f);
        Rect uv = new Rect(0.5f - screen.x / diameter, 0.5f - screen.y / diameter,
            Screen.width / diameter, Screen.height / diameter);
        Matrix4x4 matrix = GUI.matrix;
        Color color = GUI.color;
        GUI.matrix = Matrix4x4.identity;
        GUI.color = new Color(1f, 1f, 1f, darkness);
        GUI.DrawTextureWithTexCoords(new Rect(0, 0, Screen.width, Screen.height), blackoutMask, uv, true);
        GUI.color = color;
        GUI.matrix = matrix;
    }


    private void OnDestroy()
    {
        if (blackoutMask != null) Destroy(blackoutMask);
        if (panelGradient != null) Destroy(panelGradient);
    }

    private void DrawMenus()
    {
        bool result = match.Victory || match.DeathMessageVisible;
        bool upgrade = !match.Finished && match.Experience != null && match.Experience.HasUpgradeChoice;
        if (!result && !upgrade && !match.IsPaused) return;
        Rounded(new Rect(0, 0, viewWidth, viewHeight), new Color(0.015f, 0.01f, 0.03f, 0.72f), 0);
        if (result)
        {
            float x = (viewWidth - 560) / 2, y = (viewHeight - 334) / 2;
            Color accent = match.Victory ? Gold : Rose;
            Panel(new Rect(x, y, 560, 334), accent, FrameKind.Ornate);
            Divider(new Rect(x + 28, y + 106, 504, 10), menuDivider, accent);
            Diamond(new Vector2(x + 32, y + 32), 10, accent);
            Label(new Rect(x + 50, y + 25, 480, 20),
                match.Victory ? "O QUARTO  /  CONCLUÍDO" : "A JORNADA AINDA NÃO ACABOU", caption);
            Label(new Rect(x + 28, y + 66, 504, 40),
                match.Victory ? "FASE 1 CONCLUÍDA" : "VOCÊ MORREU", title);
            Label(new Rect(x + 28, y + 118, 504, 104), match.Victory
                ? "Você derrotou Gunter e recuperou a carta que aprisiona sua mãe. O Anel dos Mortos estará disponível desde o início das próximas tentativas."
                : "Os fragmentos serão espalhados novamente. Colete energia e escolha melhorias para enfrentar Gunter.", body);
            int seconds = Mathf.FloorToInt(match.ElapsedSeconds);
            Label(new Rect(x + 28, y + 229, 504, 20),
                $"TEMPO  {seconds / 60:00}:{seconds % 60:00}    /    FANTASMAS  {match.GhostKills}", caption);
            if (ActionButton(new Rect(x + 28, y + 270, 504, 44), "Jogar novamente", accent)) match.Restart();
        }
        else if (upgrade)
        {
            float x = (viewWidth - 900) / 2, y = (viewHeight - 330) / 2;
            Panel(new Rect(x, y, 900, 330), Violet, FrameKind.Ornate);
            Divider(new Rect(x + 28, y + 91, 844, 8), menuDivider, Gold);
            Label(new Rect(x + 28, y + 24, 844, 20), $"NÍVEL {match.Experience.CurrentLevel}  /  EVOLUÇÃO", caption);
            Label(new Rect(x + 28, y + 57, 844, 35), "Escolha seu próximo poder", title);
            Label(new Rect(x + 28, y + 101, 844, 28), "O jogo está pausado. Use 1, 2, 3 ou clique em uma melhoria.", body);
            for (int i = 0; i < match.Experience.Choices.Count; i++)
            {
                if (ActionButton(new Rect(x + 28 + i * 286, y + 151, 272, 148),
                    $"{i + 1}\n\n{match.Experience.Describe(match.Experience.Choices[i])}",
                    i == 0 ? Rose : i == 1 ? Violet : Gold, FrameKind.Ornate))
                { match.Experience.ChooseUpgrade(i); break; }
            }
        }
        else
        {
            float x = (viewWidth - 400) / 2, y = (viewHeight - 240) / 2;
            Panel(new Rect(x, y, 400, 240), Violet, FrameKind.Ornate);
            Divider(new Rect(x + 28, y + 96, 344, 8), menuDivider, Gold);
            Label(new Rect(x + 28, y + 25, 344, 20), "UM INSTANTE PARA RESPIRAR", caption);
            Label(new Rect(x + 28, y + 61, 344, 35), "JOGO PAUSADO", title);
            Label(new Rect(x + 28, y + 106, 344, 28), "Pressione Esc para voltar à aventura.", body);
            if (ActionButton(new Rect(x + 28, y + 166, 344, 48), "Continuar", Violet)) match.Resume();
        }
    }

    private void OnGUI()
    {
        if (match == null || match.Objective == null) return;
        Styles();
        DrawBlackout();
        Matrix4x4 previous = GUI.matrix;
        // Uniform scaling keeps text and rounded corners proportional at any aspect ratio.
        float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
        if (scale <= 0f) return;
        uiScale = scale;
        viewWidth = Screen.width / scale;
        viewHeight = Screen.height / scale;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
        DrawStatus();
        DrawExitPrompt();
        DrawMenus();
        GUI.matrix = previous;
    }
}
