using System.Collections.Generic;
using UnityEngine;

// Original 5x7 bitmap lettering for the compact HUD. Independent of installed fonts.
public static class PixelHudText
{
    private static readonly Dictionary<char, string[]> Glyphs = new Dictionary<char, string[]> {
        {'0', new[]{"01110","11011","11011","11011","11011","11011","01110"}},
        {'1', new[]{"00110","01110","00110","00110","00110","00110","11111"}},
        {'2', new[]{"01110","11011","00011","00110","01100","11000","11111"}},
        {'3', new[]{"11110","00011","00011","01110","00011","00011","11110"}},
        {'4', new[]{"00011","00111","01111","11011","11111","00011","00011"}},
        {'5', new[]{"11111","11000","11000","11110","00011","11011","01110"}},
        {'6', new[]{"01110","11000","11000","11110","11011","11011","01110"}},
        {'7', new[]{"11111","00011","00110","00110","01100","01100","01100"}},
        {'8', new[]{"01110","11011","11011","01110","11011","11011","01110"}},
        {'9', new[]{"01110","11011","11011","01111","00011","00011","01110"}},
        {'N', new[]{"11001","11001","11101","11111","10111","10011","10011"}},
        {'I', new[]{"11111","00100","00100","00100","00100","00100","11111"}},
        {'V', new[]{"11011","11011","11011","11011","11011","01010","00100"}},
        {'E', new[]{"11111","11000","11000","11110","11000","11000","11111"}},
        {'L', new[]{"11000","11000","11000","11000","11000","11000","11111"}},
        {':', new[]{"00000","00110","00110","00000","00110","00110","00000"}},
        {'/', new[]{"00001","00011","00110","00110","01100","11000","10000"}}
    };

    public static void Draw(Rect rect, string text, float height, TextAnchor alignment = TextAnchor.MiddleLeft)
    {
        if (Event.current.type != EventType.Repaint) return;
        float pixel = height / 7f;
        float width = Mathf.Max(0, text.Length * 6 - 1) * pixel;
        float left = alignment == TextAnchor.MiddleRight ? rect.xMax - width
            : alignment == TextAnchor.MiddleCenter ? rect.center.x - width * 0.5f : rect.x;
        float top = rect.center.y - height * 0.5f;
        Color previous = GUI.color;
        for (int pass = 0; pass < 2; pass++)
        {
            GUI.color = pass == 0 ? new Color(0.025f, 0.025f, 0.03f) : Color.white;
            float border = pass == 0 ? Mathf.Max(1f, pixel * 0.45f) : 0f;
            for (int i = 0; i < text.Length; i++)
            {
                char character = text[i] == 'Í' ? 'I' : text[i];
                if (!Glyphs.TryGetValue(character, out string[] rows)) continue;
                float x = left + i * 6 * pixel;
                for (int y = 0; y < 7; y++)
                {
                    for (int column = 0; column < 5; column++)
                    {
                        if (rows[y][column] != '1') continue;
                        int first = column;
                        while (column + 1 < 5 && rows[y][column + 1] == '1') column++;
                        GUI.DrawTexture(new Rect(x + first * pixel - border, top + y * pixel - border,
                            (column - first + 1) * pixel + border * 2, pixel + border * 2), Texture2D.whiteTexture);
                    }
                }
                if (text[i] == 'Í')
                    GUI.DrawTexture(new Rect(x + 3 * pixel - border, top - 2 * pixel - border,
                        pixel + border * 2, pixel + border * 2), Texture2D.whiteTexture);
            }
        }
        GUI.color = previous;
    }

    public static void Ghost(Rect rect)
    {
        if (Event.current.type != EventType.Repaint) return;
        string[] rows = {"00111100","01111110","11111111","11011011","11011011","11111111","11111111","11011011"};
        float unit = rect.width / 8f;
        Color previous = GUI.color;
        for (int pass = 0; pass < 2; pass++)
        {
            GUI.color = pass == 0 ? Color.black : Color.white;
            float border = pass == 0 ? 1.5f : 0f;
            for (int y = 0; y < 8; y++)
            for (int x = 0; x < 8; x++)
                if (rows[y][x] == '1') GUI.DrawTexture(new Rect(rect.x + x * unit - border,
                    rect.y + y * unit - border, unit + border * 2, unit + border * 2), Texture2D.whiteTexture);
        }
        GUI.color = previous;
    }
}