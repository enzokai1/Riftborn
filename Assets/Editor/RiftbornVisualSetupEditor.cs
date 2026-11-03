using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RiftbornVisualSetupEditor
{
    private const string AppliedKey = "Riftborn.VisualSetup.20260915.v3";
    private const string FramePath = "Assets/Art/Player/Frames/";
    private const string FloorPath = "Assets/Art/Environment/HouseWoodFloor.png";
    private static readonly string[] Directions = { "Down", "Up", "Left", "Right" };

    [InitializeOnLoadMethod]
    private static void ScheduleSetup()
    {
        if (!SessionState.GetBool(AppliedKey, false))
            EditorApplication.update += ApplyWhenReady;
    }

    private static void ApplyWhenReady()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating
            || EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/SampleScene.unity" || !AssetsReady()) return;
        EditorApplication.update -= ApplyWhenReady;
        Apply();
    }

    private static bool AssetsReady()
    {
        if (AssetDatabase.LoadAssetAtPath<Sprite>(FloorPath) == null) return false;
        foreach (string direction in Directions)
        {
            for (int i = 0; i < 4; i++)
                if (LoadFrame("Walk", direction, i) == null) return false;
            for (int i = 0; i < 3; i++)
                if (LoadFrame("Shoot", direction, i) == null) return false;
        }
        return true;
    }

    private static Sprite LoadFrame(string action, string direction, int index)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>($"{FramePath}{action}{direction}_{index}.png");
    }

    [MenuItem("Tools/Riftborn/Apply Character Visuals and House Floor")]
    public static void Apply()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || !scene.IsValid()
            || !scene.isLoaded || PrefabStageUtility.GetCurrentPrefabStage() != null) return;
        if (!AssetsReady())
        {
            Debug.LogError("Riftborn: aguarde a importação dos sprites em Assets/Art/Player/Frames.");
            return;
        }
        PlayerController[] players = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<PlayerController>(true)).ToArray();
        if (players.Length != 1)
        {
            Debug.LogError("Riftborn: a cena precisa ter exatamente um PlayerController.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Configure character and house floor");
        try
        {
            PlayerController player = players[0];
            PlayerSpriteAnimation animation = player.GetComponent<PlayerSpriteAnimation>();
            if (animation == null) animation = Undo.AddComponent<PlayerSpriteAnimation>(player.gameObject);
            Undo.RecordObject(animation, "Configure character animation");
            SerializedObject binding = new SerializedObject(animation);
            foreach (string direction in Directions)
            {
                Assign(binding, "walk" + direction, "Walk", direction, 4);
                Assign(binding, "shoot" + direction, "Shoot", direction, 3);
            }
            Sprite[] sheetSprites = AssetDatabase.LoadAllAssetsAtPath(
                "Assets/Art/Player/ChatGPT Image 15 de set. de 2026, 21_49_35.png").OfType<Sprite>().ToArray();
            SerializedProperty deathFrames = binding.FindProperty("deathFrames");
            deathFrames.arraySize = 5;
            for (int i = 0; i < 5; i++)
                deathFrames.GetArrayElementAtIndex(i).objectReferenceValue =
                    sheetSprites.FirstOrDefault(sprite => sprite.name == $"Player_Death_{i}");
            binding.FindProperty("deadSprite").objectReferenceValue =
                sheetSprites.FirstOrDefault(sprite => sprite.name == "Player_Death_4");
            binding.ApplyModifiedProperties();
            SpriteRenderer playerRenderer = player.GetComponent<SpriteRenderer>();
            Undo.RecordObject(playerRenderer, "Set player sprite");
            playerRenderer.sprite = LoadFrame("Walk", "Down", 0);
            playerRenderer.flipX = false;

            GameObject floor = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "HouseFloor");
            if (floor == null)
            {
                floor = new GameObject("HouseFloor", typeof(SpriteRenderer));
                SceneManager.MoveGameObjectToScene(floor, scene);
                Undo.RegisterCreatedObjectUndo(floor, "Create house floor");
            }
            SpriteRenderer floorRenderer = floor.GetComponent<SpriteRenderer>();
            if (floorRenderer == null) floorRenderer = Undo.AddComponent<SpriteRenderer>(floor);
            Undo.RecordObject(floorRenderer, "Configure house floor");
            floorRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(FloorPath);
            floorRenderer.sharedMaterial = playerRenderer.sharedMaterial;
            floorRenderer.drawMode = SpriteDrawMode.Tiled;
            floorRenderer.tileMode = SpriteTileMode.Continuous;
            floorRenderer.size = new Vector2(40f, 40f);
            floorRenderer.sortingOrder = -100;
            if (floor.GetComponent<HouseFloor>() == null) Undo.AddComponent<HouseFloor>(floor);

            EditorSceneManager.MarkSceneDirty(scene);
            SessionState.SetBool(AppliedKey, true);
            Undo.CollapseUndoOperations(group);
            Debug.Log("Riftborn: 16 quadros de caminhada, 12 de tiro e piso de casa configurados. Salve a cena.");
        }
        catch (Exception error)
        {
            Undo.RevertAllDownToGroup(group);
            Debug.LogException(error);
        }
    }

    private static void Assign(SerializedObject binding, string field, string action, string direction, int count)
    {
        SerializedProperty frames = binding.FindProperty(field);
        frames.arraySize = count;
        for (int i = 0; i < count; i++)
            frames.GetArrayElementAtIndex(i).objectReferenceValue = LoadFrame(action, direction, i);
    }
}
