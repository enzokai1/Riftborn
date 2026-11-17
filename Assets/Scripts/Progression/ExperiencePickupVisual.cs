using UnityEngine;

// One shared pixel-art circular mask for all XP drops; no real-time light per drop.
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer), typeof(ExperiencePickup))]
public sealed class ExperiencePickupVisual : MonoBehaviour
{
    private static Sprite orb;
    private static Texture2D orbTexture;
    private static int users;
    private SpriteRenderer original;
    private SpriteRenderer aura;
    private SpriteRenderer body;
    private SpriteRenderer core;
    private Transform floating;
    private ExperiencePickup pickup;
    private MatchStats match;
    private float elapsed;
    private float phase;

    private void Awake()
    {
        original = GetComponent<SpriteRenderer>();
        pickup = GetComponent<ExperiencePickup>();
        match = FindFirstObjectByType<MatchStats>();
        if (orb == null) orb = CreateOrb();
        users++;
        floating = new GameObject("XP essence").transform;
        floating.SetParent(transform, false);
        aura = Layer("Aura", new Color(0.15f, 0.9f, 0.7f, 0.10f), 0.95f, 1);
        body = Layer("XP orb", new Color(0.13f, 0.83f, 0.68f), 0.60f, 2);
        core = Layer("Bright core", new Color(0.8f, 1f, 0.86f), 0.24f, 3);
        core.transform.localPosition = new Vector3(-0.06f, 0.06f, 0f);
        phase = Mathf.Repeat(transform.position.x * 1.7f + transform.position.y * 0.83f, 20f);
        original.enabled = false;
        Animate();
    }

    private SpriteRenderer Layer(string label, Color color, float scale, int order)
    {
        var obj = new GameObject(label, typeof(SpriteRenderer));
        obj.transform.SetParent(floating, false);
        obj.transform.localScale = Vector3.one * scale;
        SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();
        renderer.sprite = orb;
        renderer.sharedMaterial = original.sharedMaterial;
        renderer.sortingLayerID = original.sortingLayerID;
        renderer.sortingOrder = original.sortingOrder + order;
        renderer.color = color;
        return renderer;
    }

    private void Update()
    {
        if (match != null && (match.IsPaused || match.Finished)) return;
        elapsed += Time.deltaTime;
        Animate();
    }

    private void Animate()
    {
        float wave = Mathf.Sin(elapsed * 2.6f + phase);
        floating.localPosition = new Vector3(0f, 0.10f + wave * 0.06f, 0f);
        floating.localScale = Vector3.one * (1f + wave * 0.025f);
        aura.color = new Color(0.15f, 0.9f, 0.7f, (pickup.IsAttracted ? 0.14f : 0.10f) + wave * 0.015f);
        body.color = Color.Lerp(new Color(0.13f, 0.83f, 0.68f), new Color(0.3f, 1f, 0.8f), pickup.IsAttracted ? 0.7f : (wave + 1f) * 0.15f);
    }

    private static Sprite CreateOrb()
    {
        // A small, hard-edged mask keeps the round silhouette visibly pixelated.
        // Matching pixels-per-unit to size preserves the orb's world-space dimensions.
        const int size = 16;
        orbTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        orbTexture.name = "XP pixel circle alpha mask";
        orbTexture.filterMode = FilterMode.Point;
        orbTexture.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float distance = new Vector2(x + 0.5f - size * 0.5f, y + 0.5f - size * 0.5f).magnitude;
            float alpha = distance <= size * 0.46875f ? 1f : 0f;
            pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
        }
        orbTexture.SetPixels(pixels);
        orbTexture.Apply(false, true);
        Sprite sprite = Sprite.Create(orbTexture, new Rect(0, 0, size, size),
            Vector2.one * 0.5f, size, 0, SpriteMeshType.FullRect);
        sprite.name = "XP small round orb";
        return sprite;
    }
    private void OnDisable()
    {
        if (floating != null) floating.gameObject.SetActive(false);
        if (original != null) original.enabled = true;
    }

    private void OnEnable()
    {
        if (floating != null) floating.gameObject.SetActive(true);
        if (original != null) original.enabled = false;
    }

    private void OnDestroy()
    {
        if (floating != null) Destroy(floating.gameObject);
        users--;
        if (users == 0)
        {
            if (orb != null) Destroy(orb);
            if (orbTexture != null) Destroy(orbTexture);
            orb = null;
            orbTexture = null;
        }
    }
}
