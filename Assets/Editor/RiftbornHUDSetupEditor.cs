using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class RiftbornHUDSetupEditor
{
    private const string ActionName = "Build Professional HUD";
    private static readonly Color Panel = new Color32(16, 18, 28, 230);
    private static readonly Color Text = new Color32(242, 244, 255, 255);
    private static readonly Color Muted = new Color32(182, 189, 211, 255);
    private static readonly Color Cyan = new Color32(103, 232, 249, 255);

    [MenuItem("Tools/Riftborn/Build Professional HUD")]
    private static void Build()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || !scene.IsValid()
            || !scene.isLoaded || PrefabStageUtility.GetCurrentPrefabStage() != null)
        {
            Debug.LogError("Riftborn HUD: abra a cena do jogo fora do Play Mode e Prefab Mode.");
            return;
        }

        if (TMP_Settings.defaultFontAsset == null)
        {
            Debug.LogError("Riftborn HUD: importe TMP Essential Resources e configure a fonte padrão do TMP.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(ActionName);
        try
        {
            GameObject[] roots = scene.GetRootGameObjects().Where(x => x.name == "Canvas").ToArray();
            if (roots.Length > 1)
                throw new InvalidOperationException("Existe mais de um Canvas raiz com esse nome.");
            RectTransform canvas = roots.Length == 0 ? Node(null, "Canvas") : Rect(roots[0].transform);
            Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject, ActionName);
            Edit<Canvas>(canvas).renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = Edit<CanvasScaler>(canvas);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform hud = Node(canvas, "RiftbornHUD");
            Stretch(hud);
            Transform legacy = Child(canvas, "ProgressionHUD");
            Transform existingTop = Child(hud, "TopProgression");
            if (legacy != null && existingTop != null)
                throw new InvalidOperationException("ProgressionHUD e TopProgression coexistem. Escolha qual manter antes de executar.");
            if (legacy != null)
            {
                Undo.SetTransformParent(legacy, hud, ActionName);
                Undo.RecordObject(legacy.gameObject, ActionName);
                legacy.name = "TopProgression";
            }

            RectTransform top = Node(hud, "TopProgression");
            Place(top, new Vector2(0.5f, 1), new Vector2(0, -28), new Vector2(520, 108));
            Surface(top);
            TMP_Text level = Label(top, "LevelText", "Nível 1", 30, Cyan,
                TextAlignmentOptions.Center, new Vector2(0, -10), new Vector2(480, 36));
            Slider xp = Bar(top, "ExperienceBar", new Vector2(0, -53), new Vector2(472, 12),
                new Color32(139, 92, 246, 255), 0);
            TMP_Text xpText = Label(top, "ExperienceText", "0 / 10 XP", 18, Muted,
                TextAlignmentOptions.Center, new Vector2(0, -73), new Vector2(472, 24));

            RectTransform status = Node(hud, "PlayerStatus");
            Place(status, new Vector2(0, 0), new Vector2(32, 32), new Vector2(300, 86));
            Surface(status);
            Label(status, "HealthLabel", "VIDA  — / —", 20, Text,
                TextAlignmentOptions.Left, new Vector2(0, -12), new Vector2(252, 28));
            Bar(status, "HealthBar", new Vector2(0, -52), new Vector2(252, 16),
                new Color32(214, 107, 135, 255), 0.75f);

            RectTransform info = Node(hud, "MatchInfo");
            Place(info, new Vector2(1, 1), new Vector2(-32, -28), new Vector2(240, 96));
            Surface(info);
            Label(info, "TimerText", "00:00", 28, Text,
                TextAlignmentOptions.Right, new Vector2(0, -10), new Vector2(200, 36));
            Label(info, "KillCountText", "— DERROTADOS", 18, Muted,
                TextAlignmentOptions.Right, new Vector2(0, -54), new Vector2(200, 26));

            PlayerProgressionUI[] presenters = hud.GetComponentsInChildren<PlayerProgressionUI>(true);
            if (presenters.Length > 1)
                throw new InvalidOperationException("Há mais de um PlayerProgressionUI no HUD. Revise antes de executar.");
            PlayerProgressionUI presenter = presenters.Length == 1 ? presenters[0] : Edit<PlayerProgressionUI>(top);
            Undo.RecordObject(presenter, ActionName);
            SerializedObject binding = new SerializedObject(presenter);
            binding.FindProperty("levelText").objectReferenceValue = level;
            binding.FindProperty("experienceBar").objectReferenceValue = xp;
            binding.FindProperty("experienceText").objectReferenceValue = xpText;
            bool needsPlayer = binding.FindProperty("playerExperience").objectReferenceValue == null;
            binding.ApplyModifiedProperties();

            foreach (Component component in canvas.GetComponentsInChildren<Component>(true))
                if (component != null && PrefabUtility.IsPartOfPrefabInstance(component))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(group);
            Debug.Log("Riftborn HUD configurada. Vida (75%), tempo e derrotados são placeholders. Salve a cena."
                + (needsPlayer ? " Atribua Player Experience no PlayerProgressionUI." : " Referência ao Player preservada."), hud);
        }
        catch (Exception error)
        {
            Undo.RevertAllDownToGroup(group);
            Debug.LogError($"Riftborn HUD: operação desfeita. {error.Message}");
        }
    }

    private static Transform Child(Transform parent, string name)
    {
        Transform[] matches = parent.Cast<Transform>().Where(x => x.name == name).ToArray();
        if (matches.Length > 1)
            throw new InvalidOperationException($"Nome duplicado: {parent.name}/{name}.");
        return matches.Length == 0 ? null : matches[0];
    }

    private static RectTransform Node(Transform parent, string name)
    {
        Transform existing = parent == null ? null : Child(parent, name);
        if (existing != null) return Rect(existing);
        GameObject obj = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(obj, ActionName);
        if (parent != null) Undo.SetTransformParent(obj.transform, parent, ActionName);
        return (RectTransform)obj.transform;
    }

    private static RectTransform Rect(Transform target)
    {
        if (!(target is RectTransform rect))
            throw new InvalidOperationException($"{target.name} precisa de RectTransform.");
        return rect;
    }

    private static T Edit<T>(RectTransform rect) where T : Component
    {
        T component = rect.GetComponent<T>();
        if (component == null) component = Undo.AddComponent<T>(rect.gameObject);
        Undo.RecordObject(component, ActionName);
        return component;
    }

    private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        Undo.RecordObject(rect, ActionName);
        rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
        rect.anchoredPosition3D = new Vector3(position.x, position.y, 0);
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private static void Stretch(RectTransform rect)
    {
        Place(rect, Vector2.zero, Vector2.zero, Vector2.zero);
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static void Surface(RectTransform rect)
    {
        Image image = Edit<Image>(rect);
        image.sprite = null;
        image.color = Panel;
        image.raycastTarget = false;
    }

    private static TMP_Text Label(RectTransform parent, string name, string preview, float size,
        Color color, TextAlignmentOptions alignment, Vector2 position, Vector2 dimensions)
    {
        RectTransform rect = Node(parent, name);
        Place(rect, new Vector2(0.5f, 1), position, dimensions);
        TMP_Text text = rect.GetComponent<TMP_Text>();
        if (text == null) text = Undo.AddComponent<TextMeshProUGUI>(rect.gameObject);
        Undo.RecordObject(text, ActionName);
        if (text.font == null) text.font = TMP_Settings.defaultFontAsset;
        text.text = preview;
        text.fontSize = size;
        text.enableAutoSizing = false;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private static Slider Bar(RectTransform parent, string name, Vector2 position, Vector2 size,
        Color color, float preview)
    {
        RectTransform rect = Node(parent, name);
        Place(rect, new Vector2(0.5f, 1), position, size);
        Slider slider = Edit<Slider>(rect);
        Transform area = Child(rect, "Fill Area");
        Transform oldFill = area == null ? null : Child(area, "Fill");
        if (oldFill != null)
        {
            if (Child(rect, "Fill") != null)
                throw new InvalidOperationException($"{name} contém dois objetos Fill.");
            Undo.SetTransformParent(oldFill, rect, ActionName);
        }
        if (area != null && area.childCount == 0) Undo.DestroyObjectImmediate(area.gameObject);
        Transform handle = Child(rect, "Handle Slide Area");
        slider.handleRect = null;
        slider.fillRect = null;
        if (handle != null) Undo.DestroyObjectImmediate(handle.gameObject);
        Image rootImage = rect.GetComponent<Image>();
        if (rootImage != null)
        {
            Undo.RecordObject(rootImage, ActionName);
            rootImage.color = Color.clear;
            rootImage.raycastTarget = false;
        }
        RectTransform background = Node(rect, "Background");
        RectTransform fill = Node(rect, "Fill");
        Stretch(background);
        Stretch(fill);
        Undo.RecordObject(background, ActionName);
        background.SetAsFirstSibling();
        Image track = Edit<Image>(background);
        track.color = new Color32(41, 45, 67, 255);
        track.raycastTarget = false;
        Image foreground = Edit<Image>(fill);
        foreground.color = color;
        foreground.raycastTarget = false;
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.wholeNumbers = false;
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;
        slider.navigation = new Navigation { mode = Navigation.Mode.None };
        slider.direction = Slider.Direction.LeftToRight;
        slider.targetGraphic = track;
        slider.fillRect = fill;
        slider.SetValueWithoutNotify(preview);
        return slider;
    }
}
