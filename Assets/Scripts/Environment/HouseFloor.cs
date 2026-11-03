using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class HouseFloor : MonoBehaviour
{
    [SerializeField] private Vector2 mapSize = new Vector2(100f, 100f);
    private SpriteRenderer floorRenderer;
    private bool sizeDirty;
    public static HouseFloor Active { get; private set; }
    public Rect Bounds => new Rect((Vector2)transform.position - mapSize * 0.5f, mapSize);

    private void OnEnable()
    {
        Active = this;
        floorRenderer = GetComponent<SpriteRenderer>();
        ApplySize();
        if (Application.isPlaying) CreateBorders();
    }

    private void OnDisable()
    {
        if (Active == this) Active = null;
    }

    private void OnValidate()
    {
        mapSize = new Vector2(Mathf.Max(20f, mapSize.x), Mathf.Max(20f, mapSize.y));
        sizeDirty = true;
    }

    private void Update()
    {
        // Sprite changes are deferred out of OnValidate to avoid Unity import callbacks.
        if (!sizeDirty) return;
        sizeDirty = false;
        floorRenderer = GetComponent<SpriteRenderer>();
        ApplySize();
    }

    private void ApplySize()
    {
        if (floorRenderer == null) return;
        floorRenderer.drawMode = SpriteDrawMode.Tiled;
        floorRenderer.tileMode = SpriteTileMode.Continuous;
        floorRenderer.sortingOrder = -100;
        floorRenderer.size = mapSize;
    }

    public Vector2 Clamp(Vector2 position, Vector2 margin)
    {
        Rect bounds = Bounds;
        margin = Vector2.Min(margin, bounds.size * 0.5f);
        return new Vector2(
            Mathf.Clamp(position.x, bounds.xMin + margin.x, bounds.xMax - margin.x),
            Mathf.Clamp(position.y, bounds.yMin + margin.y, bounds.yMax - margin.y));
    }

    public bool Contains(Vector2 position, float margin)
    {
        return (Clamp(position, Vector2.one * margin) - position).sqrMagnitude < 0.000001f;
    }

    private void CreateBorders()
    {
        if (transform.Find("MapBorders") != null) return;
        Transform root = new GameObject("MapBorders").transform;
        root.SetParent(transform, false);
        for (int i = 0; i < 4; i++)
        {
            bool vertical = i < 2;
            float sign = i % 2 == 0 ? -1f : 1f;
            GameObject edge = new GameObject("Border" + i, typeof(SpriteRenderer));
            edge.transform.SetParent(root, false);
            edge.transform.localPosition = vertical
                ? new Vector3(sign * (mapSize.x * 0.5f - 0.25f), 0f, 0f)
                : new Vector3(0f, sign * (mapSize.y * 0.5f - 0.25f), 0f);
            SpriteRenderer renderer = edge.GetComponent<SpriteRenderer>();
            renderer.sprite = floorRenderer.sprite;
            renderer.sharedMaterial = floorRenderer.sharedMaterial;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = vertical ? new Vector2(0.5f, mapSize.y) : new Vector2(mapSize.x, 0.5f);
            renderer.color = new Color(0.3f, 0.2f, 0.16f);
            renderer.sortingOrder = -99;
        }
    }
}

