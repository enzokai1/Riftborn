using UnityEngine;

// World-space reticle at the weapon's cursor aim position.
[DisallowMultipleComponent]
[DefaultExecutionOrder(200)]
[RequireComponent(typeof(Weapon))]
public class WeaponAimIndicator : MonoBehaviour
{
    private Weapon weapon;
    private PlayerController player;
    private LineRenderer reticle;
    private readonly Vector3[] circle = new Vector3[33];

    private void Awake()
    {
        weapon = GetComponent<Weapon>();
        player = GetComponentInParent<PlayerController>();
        SpriteRenderer owner = GetComponentInParent<SpriteRenderer>();
        if (owner == null) { enabled = false; return; }
        reticle = CreateLine("AimReticle", owner, 0.035f, 2);
        reticle.positionCount = circle.Length;
    }

    private LineRenderer CreateLine(string label, SpriteRenderer owner, float width, int order)
    {
        GameObject visual = new GameObject(label);
        visual.transform.SetParent(transform, false);
        LineRenderer line = visual.AddComponent<LineRenderer>();
        line.sharedMaterial = owner.sharedMaterial;
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.startWidth = line.endWidth = width;
        line.textureMode = LineTextureMode.Stretch;
        line.alignment = LineAlignment.View;
        line.sortingLayerID = owner.sortingLayerID;
        line.sortingOrder = owner.sortingOrder + order;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.enabled = false;
        return line;
    }

    private void LateUpdate()
    {
        bool visible = weapon != null && weapon.isActiveAndEnabled && weapon.HasAim
            && player != null && player.isActiveAndEnabled;
        SetVisible(visible);
        if (!visible) return;

        Vector3 end = weapon.AimPosition;
        Color color = new Color(0.55f, 0.95f, 1f);

        const float radius = 0.2f;
        for (int i = 0; i < circle.Length; i++)
        {
            float angle = i * Mathf.PI * 2f / (circle.Length - 1);
            circle[i] = end + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
        }
        reticle.SetPositions(circle);
        Tint(reticle, color, 0.9f);
    }

    private static void Tint(LineRenderer line, Color color, float alpha)
    {
        color.a = alpha;
        line.startColor = line.endColor = color;
    }

    private void SetVisible(bool visible)
    {
        if (reticle == null) return;
        reticle.enabled = visible;
    }

    private void OnDisable() => SetVisible(false);
}
