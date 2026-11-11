using System.Collections.Generic;
using UnityEngine;

// First Dread Rooms step: perimeter only. Floor and furniture stay independent.
public static class HauntedRoomWalls
{
    public static bool TryBuild(Transform parent, SpriteRenderer reference, Vector2 size)
    {
        string[] names = { "wall", "wall_cracked", "wall_damaged", "wall_mouldy", "window-06" };
        var sprites = new Dictionary<string, Sprite>();
        foreach (string name in names)
        {
            Sprite sprite = Resources.Load<Sprite>("Art/DreadRoom/" + name);
            if (sprite == null)
            {
                Debug.LogWarning("Dread Rooms wall asset missing: " + name + ". Using original borders.");
                return false;
            }
            sprites.Add(name, sprite);
        }

        Transform root = new GameObject("MapBorders").transform;
        root.SetParent(parent, false);
        float left = -size.x * 0.5f;
        float right = size.x * 0.5f;
        float bottom = -size.y * 0.5f;
        float top = size.y * 0.5f;
        // Each face uses the complete floral wall, with its cornice facing outside.
        BuildFace(root, reference, sprites, "Back", new Vector2(0, top - 1f), size.x, 0f, 0);
        BuildFace(root, reference, sprites, "Front", new Vector2(0, bottom + 1f), size.x, 180f, 5);
        BuildFace(root, reference, sprites, "Left", new Vector2(left + 1f, 0), size.y - 4f, 90f, 11);
        BuildFace(root, reference, sprites, "Right", new Vector2(right - 1f, 0), size.y - 4f, -90f, 17);

        Solid(root, "Back wall collision", new Vector2(0, top - 1f), new Vector2(size.x, 2f));
        Solid(root, "Front wall collision", new Vector2(0, bottom + 1f), new Vector2(size.x, 2f));
        Solid(root, "Left wall collision", new Vector2(left + 1f, 0), new Vector2(2f, size.y));
        Solid(root, "Right wall collision", new Vector2(right - 1f, 0), new Vector2(2f, size.y));
        return true;
    }

    private static void BuildFace(Transform root, SpriteRenderer reference,
        Dictionary<string, Sprite> sprites, string label, Vector2 center, float length, float angle, int phase)
    {
        Transform face = new GameObject(label + " floral wall").transform;
        face.SetParent(root, false);
        face.localPosition = center;
        face.localRotation = Quaternion.Euler(0, 0, angle);
        int columns = Mathf.Max(1, Mathf.RoundToInt(length));
        float width = length / columns;
        for (int i = 0; i < columns; i++)
        {
            int variation = i + phase;
            string tile = variation % 17 == 8 ? "wall_damaged" : variation % 11 == 5 ? "wall_mouldy"
                : variation % 7 == 3 ? "wall_cracked" : "wall";
            Draw(face, reference, sprites[tile], "Wallpaper " + i,
                new Vector2(-length * 0.5f + (i + 0.5f) * width, 0), new Vector2(width, 2f));
        }
        for (float x = -length * 0.5f + 7f; x <= length * 0.5f - 4f; x += 14f)
            Draw(face, reference, sprites["window-06"], "Damaged window",
                new Vector2(x, 0), new Vector2(1, 2), false, -79);
    }
    private static void Draw(Transform root, SpriteRenderer reference, Sprite sprite,
        string label, Vector2 position, Vector2 size, bool tiled = false, int order = -80)
    {
        var obj = new GameObject(label, typeof(SpriteRenderer));
        obj.transform.SetParent(root, false);
        obj.transform.localPosition = position;
        var renderer = obj.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sharedMaterial = reference.sharedMaterial;
        renderer.sortingLayerID = reference.sortingLayerID;
        renderer.sortingOrder = order;
        if (tiled)
        {
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            renderer.size = size;
        }
        else obj.transform.localScale = new Vector3(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y, 1);
    }

    private static void Solid(Transform root, string label, Vector2 position, Vector2 size)
    {
        var obj = new GameObject(label, typeof(BoxCollider2D));
        obj.transform.SetParent(root, false);
        obj.transform.localPosition = position;
        obj.GetComponent<BoxCollider2D>().size = size;
    }
}