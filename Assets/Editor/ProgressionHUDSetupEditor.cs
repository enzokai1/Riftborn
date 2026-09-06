using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ProgressionHUDSetupEditor
{
    private const string MenuPath = "Tools/Riftborn/Setup Progression HUD";
    private const string UndoName = "Setup Progression HUD";

    [MenuItem(MenuPath)]
    private static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("Setup Progression HUD: saia do Play Mode antes de executar.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded || PrefabStageUtility.GetCurrentPrefabStage() != null)
        {
            Debug.LogError("Setup Progression HUD: abra a cena do jogo fora do Prefab Mode.");
            return;
        }

        // Validate the entire structure before recording or changing anything.
        GameObject canvasObject;
        CanvasScaler scaler;
        RectTransform hud;
        RectTransform textRect;
        TMP_Text levelText;
        RectTransform barRect;
        Slider slider;
        RectTransform fillArea;
        RectTransform fillRect;
        Image background;
        Image fill;
        Transform handle;

        try
        {
            GameObject[] canvases = scene.GetRootGameObjects().Where(obj => obj.name == "Canvas").ToArray();
            if (canvases.Length != 1)
                throw new InvalidOperationException("a cena ativa precisa ter exatamente um objeto raiz chamado Canvas.");

            canvasObject = canvases[0];
            RequireComponent<Canvas>(canvasObject.transform);
            scaler = RequireComponent<CanvasScaler>(canvasObject.transform);
            Transform hudObject = FindChild(canvasObject.transform, "ProgressionHUD");
            Transform textObject = FindChild(hudObject, "LevelText");
            Transform barObject = FindChild(hudObject, "ExperienceBar");
            Transform backgroundObject = FindChild(barObject, "Background");
            Transform fillAreaObject = FindChild(barObject, "Fill Area");
            Transform fillObject = FindChild(fillAreaObject, "Fill");
            handle = FindChild(barObject, "Handle Slide Area", optional: true);

            RequireOnlyChildren(barObject, "Background", "Fill Area", "Handle Slide Area");
            RequireOnlyChildren(fillAreaObject, "Fill");
            hud = RequireComponent<RectTransform>(hudObject);
            textRect = RequireComponent<RectTransform>(textObject);
            levelText = RequireComponent<TMP_Text>(textObject);
            barRect = RequireComponent<RectTransform>(barObject);
            slider = RequireComponent<Slider>(barObject);
            fillArea = RequireComponent<RectTransform>(fillAreaObject);
            fillRect = RequireComponent<RectTransform>(fillObject);
            background = RequireComponent<Image>(backgroundObject);
            fill = RequireComponent<Image>(fillObject);
        }
        catch (InvalidOperationException error)
        {
            Debug.LogError($"Setup Progression HUD: {error.Message} Nenhuma alteração foi aplicada.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(UndoName);
        Undo.RegisterFullObjectHierarchyUndo(canvasObject, UndoName);

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        ConfigureRect(hud, new Vector2(0f, -20f), new Vector2(500f, 100f));
        ConfigureRect(textRect, Vector2.zero, new Vector2(400f, 40f));
        levelText.fontSize = 30f;
        levelText.enableAutoSizing = false;
        levelText.alignment = TextAlignmentOptions.Center;
        levelText.raycastTarget = false;

        ConfigureRect(barRect, new Vector2(0f, -50f), new Vector2(420f, 20f));
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;
        slider.direction = Slider.Direction.LeftToRight;
        slider.fillRect = fillRect;
        slider.handleRect = null;
        slider.targetGraphic = background;

        if (handle != null)
            Undo.DestroyObjectImmediate(handle.gameObject);

        // Remove the horizontal padding reserved for the former slider handle.
        fillArea.offsetMin = new Vector2(0f, fillArea.offsetMin.y);
        fillArea.offsetMax = new Vector2(0f, fillArea.offsetMax.y);
        background.raycastTarget = false;
        fill.raycastTarget = false;

        // Persist component changes as overrides when the HUD is a prefab instance.
        foreach (Component component in canvasObject.GetComponentsInChildren<Component>(true))
        {
            if (component != null && PrefabUtility.IsPartOfPrefabInstance(component))
                PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Undo.CollapseUndoOperations(undoGroup);
        Debug.Log("HUD de progressão configurado. Salve a cena para manter os ajustes. Use Undo para desfazer.", canvasObject);
    }

    private static void ConfigureRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static Transform FindChild(Transform parent, string name, bool optional = false)
    {
        Transform[] matches = parent.Cast<Transform>().Where(child => child.name == name).ToArray();
        if (matches.Length == 1)
            return matches[0];
        if (optional && matches.Length == 0)
            return null;

        throw new InvalidOperationException($"esperado exatamente um filho '{name}' em '{parent.name}'; encontrados {matches.Length}.");
    }

    private static T RequireComponent<T>(Transform target) where T : Component
    {
        T component = target.GetComponent<T>();
        if (component == null)
            throw new InvalidOperationException($"'{target.name}' precisa do componente {typeof(T).Name}.");
        return component;
    }

    private static void RequireOnlyChildren(Transform parent, params string[] allowedNames)
    {
        foreach (Transform child in parent)
        {
            if (!allowedNames.Contains(child.name))
                throw new InvalidOperationException($"filho inesperado '{child.name}' em '{parent.name}'. Revise a estrutura antes de executar.");
        }
    }
}
