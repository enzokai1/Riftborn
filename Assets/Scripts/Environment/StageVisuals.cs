using UnityEngine;

// Small code-native symbols and furniture; existing character art is preserved.
public class StageVisuals : MonoBehaviour
{
    private Sprite pixel;
    private Material material;
    public void Initialize(SpriteRenderer reference)
    {
        pixel = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1f);
        material = reference.sharedMaterial;
    }

    public SpriteRenderer Block(Transform parent, string label, Vector2 position, Vector2 size, Color color, int order = -50)
    {
        var obj = new GameObject(label, typeof(SpriteRenderer));
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = position;
        obj.transform.localScale = size;
        var renderer = obj.GetComponent<SpriteRenderer>();
        renderer.sprite = pixel;
        renderer.sharedMaterial = material;
        renderer.color = color;
        renderer.sortingOrder = order;
        return renderer;
    }

    public LineRenderer Outline(Transform parent, string label, Vector3[] points, Color color, float width = 0.08f)
    {
        var obj = new GameObject(label, typeof(LineRenderer));
        obj.transform.SetParent(parent, false);
        var line = obj.GetComponent<LineRenderer>();
        line.sharedMaterial = material;
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = points.Length;
        line.SetPositions(points);
        line.startWidth = line.endWidth = width;
        line.startColor = line.endColor = color;
        line.sortingOrder = 20;
        return line;
    }

    public LineRenderer Circle(Transform parent, string label, float radius, Color color)
    {
        var points = new Vector3[64];
        for (int i = 0; i < points.Length; i++)
        {
            float angle = i * Mathf.PI * 2f / points.Length;
            points[i] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }
        return Outline(parent, label, points, color);
    }

    private void OnDestroy() { if (pixel != null) Destroy(pixel); }
}

