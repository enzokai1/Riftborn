using System;
using UnityEngine;

// Animates only child visuals: the pickup position and collection radius stay fixed.
[DisallowMultipleComponent]
public sealed class FragmentVisual : MonoBehaviour
{
    [SerializeField, Min(1f)] private float framesPerSecond = 10f;
    [SerializeField, Min(0f)] private float floatHeight = 0.12f;
    private static Sprite[] frames;
    private MatchStats match;
    private SpriteRenderer crystal;
    private SpriteRenderer shadow;
    private SpriteRenderer glow;
    private SpriteRenderer glintHorizontal;
    private SpriteRenderer glintVertical;
    private Texture2D softTexture;
    private Sprite softSprite;
    private float elapsed;

    public void Initialize(StageVisuals visuals, MatchStats stats)
    {
        match = stats;
        if (frames == null || frames.Length == 0 || frames[0] == null)
        {
            frames = Resources.LoadAll<Sprite>("Art/Fragments");
            Array.Sort(frames, (a, b) => string.CompareOrdinal(a.name, b.name));
        }
        if (frames.Length == 0)
        {
            Debug.LogError("Missing fragment sprites in Resources/Art/Fragments.", this);
            enabled = false;
            return;
        }

        CreateSoftDisc();
        shadow = visuals.Block(transform, "Shadow", new Vector2(0, -0.55f),
            new Vector2(0.95f, 0.28f), new Color(0.02f, 0.01f, 0.06f, 0.4f), 18);
        shadow.sprite = softSprite;
        glow = visuals.Block(transform, "Crystal glow", Vector2.zero,
            Vector2.one * 1.8f, new Color(0.35f, 0.22f, 1f, 0.18f), 19);
        glow.sprite = softSprite;
        crystal = visuals.Block(transform, "Spinning crystal", Vector2.zero, Vector2.one, Color.white, 20);
        crystal.sprite = frames[0];
        glintHorizontal = visuals.Block(crystal.transform, "Glint horizontal", Vector2.zero,
            new Vector2(0.15f, 0.025f), Color.white, 21);
        glintVertical = visuals.Block(crystal.transform, "Glint vertical", Vector2.zero,
            new Vector2(0.025f, 0.15f), Color.white, 21);

        // Derive phase from position without consuming the gameplay random sequence.
        elapsed = Mathf.Repeat(transform.position.x * 0.73f + transform.position.y * 0.37f, 5f);
        Animate();
    }

    private void Update()
    {
        if (crystal == null || (match != null && (match.IsPaused || match.Finished))) return;
        elapsed += Time.deltaTime;
        Animate();
    }

    private void Animate()
    {
        float wave = Mathf.Sin(elapsed * 2.2f);
        crystal.sprite = frames[Mathf.FloorToInt(elapsed * framesPerSecond) % frames.Length];
        crystal.transform.localPosition = new Vector3(0, 0.12f + wave * floatHeight, 0);
        glow.transform.localPosition = crystal.transform.localPosition;
        glow.color = new Color(0.35f, 0.22f, 1f, 0.16f + wave * 0.035f);
        shadow.transform.localScale = new Vector3(0.9f - wave * 0.08f, 0.25f - wave * 0.025f, 1);
        shadow.color = new Color(0.02f, 0.01f, 0.06f, 0.35f - wave * 0.05f);

        float sparkle = Mathf.Pow(Mathf.Max(0, Mathf.Sin(elapsed * 2.8f)), 12);
        Vector3 glintPosition = new Vector3(-0.18f, 0.32f, 0);
        glintHorizontal.transform.localPosition = glintVertical.transform.localPosition = glintPosition;
        glintHorizontal.color = glintVertical.color = new Color(0.85f, 0.95f, 1f, sparkle * 0.85f);
    }

    private void CreateSoftDisc()
    {
        const int size = 32;
        softTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        softTexture.name = "Fragment soft light";
        softTexture.wrapMode = TextureWrapMode.Clamp;
        softTexture.filterMode = FilterMode.Bilinear;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float radius = new Vector2((x + 0.5f) / size * 2f - 1f, (y + 0.5f) / size * 2f - 1f).magnitude;
            float alpha = Mathf.Clamp01(1f - radius);
            pixels[y * size + x] = new Color(1, 1, 1, alpha * alpha);
        }
        softTexture.SetPixels(pixels);
        softTexture.Apply(false, true);
        softSprite = Sprite.Create(softTexture, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
    }

    private void OnDestroy()
    {
        if (softSprite != null) Destroy(softSprite);
        if (softTexture != null) Destroy(softTexture);
    }
}
