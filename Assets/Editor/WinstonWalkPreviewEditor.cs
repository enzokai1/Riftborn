using System.Linq;
using UnityEditor;
using UnityEngine;

public class WinstonWalkPreviewEditor : EditorWindow
{
    private const string SheetPath = "Assets/Art/Player/WinstonLantern/WinstonWalkDown.png";
    private const string ClipPath = "Assets/Art/Player/WinstonLantern/WinstonWalkDown.anim";
    private const string SessionKey = "Riftborn.WinstonWalkPreview.v2";
    private const string UpSheetPath = "Assets/Art/Player/WinstonLantern/WinstonWalkUp.png";
    private const string UpClipPath = "Assets/Art/Player/WinstonLantern/WinstonWalkUp.anim";
    [SerializeField] private int directionIndex = 1;
    private Sprite[] frames;
    private float fps = 8f;
    private int zoom = 4;
    private bool playing = true;
    private int selectedFrame;
    private double startTime;

    [InitializeOnLoadMethod]
    private static void Schedule()
    {
        if (!SessionState.GetBool(SessionKey, false))
            EditorApplication.update += PrepareWhenReady;
    }

    private static void PrepareWhenReady()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating
            || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(SheetPath) == null) return;
        EditorApplication.update -= PrepareWhenReady;
        PrepareAll();
        SessionState.SetBool(SessionKey, true);
        Open();
    }

    [MenuItem("Tools/Riftborn/Winston/Preview Walk")]
    public static void Open()
    {
        PrepareAll();
        var window = GetWindow<WinstonWalkPreviewEditor>("Winston - Caminhada");
        window.minSize = new Vector2(590, 560);
        window.LoadFrames();
        window.Show();
    }

    private static Sprite[] ReadFrames(string path = SheetPath)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
            .OrderBy(sprite => sprite.name).ToArray();
    }

    private static void PrepareAll()
    {
        PrepareAssets(SheetPath, ClipPath, "Down");
        PrepareAssets(UpSheetPath, UpClipPath, "Up");
    }

    private static void PrepareAssets(string sheetPath, string clipPath, string direction)
    {
        var importer = AssetImporter.GetAtPath(sheetPath) as TextureImporter;
        if (importer == null) return;
        var existing = ReadFrames(sheetPath);
        if (existing.Length != 8 || importer.spriteImportMode != SpriteImportMode.Multiple
            || importer.filterMode != FilterMode.Point || importer.mipmapEnabled
            || importer.textureCompression != TextureImporterCompression.Uncompressed
            || !Mathf.Approximately(importer.spritePixelsPerUnit, 32f))
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.spritePixelsPerUnit = 32f;
            var metadata = new SpriteMetaData[8];
            for (int i = 0; i < metadata.Length; i++)
                metadata[i] = new SpriteMetaData {
                    name = "Winston_Walk_" + direction + "_" + i,
                    rect = new Rect(i * 64, 0, 64, 64),
                    alignment = (int)SpriteAlignment.Custom,
                    pivot = new Vector2(0.5f, 5f / 64f)
                };
#pragma warning disable 0618
            importer.spritesheet = metadata;
#pragma warning restore 0618
            importer.SaveAndReimport();
        }

        var sprites = ReadFrames(sheetPath);
        if (sprites.Length != 8)
        {
            Debug.LogError("Winston: expected eight imported walking frames.");
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath) == null)
        {
            var clip = new AnimationClip { name = "WinstonWalk" + direction, frameRate = 8f };
            var keys = new ObjectReferenceKeyframe[9];
            for (int i = 0; i < 8; i++)
                keys[i] = new ObjectReferenceKeyframe { time = i / 8f, value = sprites[i] };
            // Explicit end key gives each of the eight frames a full 125 ms.
            keys[8] = new ObjectReferenceKeyframe { time = 1f, value = sprites[0] };
            AnimationUtility.SetObjectReferenceCurve(clip,
                EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AssetDatabase.CreateAsset(clip, clipPath);
            AssetDatabase.SaveAssets();
        }
    }

    private void OnEnable()
    {
        startTime = EditorApplication.timeSinceStartup;
        EditorApplication.update += Tick;
        LoadFrames();
    }

    private void OnDisable() { EditorApplication.update -= Tick; }
    private void Tick() { if (playing) Repaint(); }
    private void LoadFrames() { frames = ReadFrames(directionIndex == 0 ? SheetPath : UpSheetPath); }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("WINSTON / CAMINHADA", EditorStyles.boldLabel);
        int nextDirection = GUILayout.Toolbar(directionIndex, new[] { "Frente", "Costas" });
        if (nextDirection != directionIndex)
        {
            directionIndex = nextDirection;
            selectedFrame = 0;
            startTime = EditorApplication.timeSinceStartup;
            LoadFrames();
        }
        EditorGUILayout.HelpBox("Teste visual independente da partida. O sprite e o clipe estão em Assets/Art/Player/WinstonLantern.", MessageType.Info);
        if (frames == null || frames.Length != 8)
        {
            if (GUILayout.Button("Preparar sprites")) { PrepareAll(); LoadFrames(); }
            return;
        }
        bool nextPlaying = GUILayout.Toggle(playing, "Reproduzir caminhada em loop", "Button");
        if (nextPlaying != playing)
        {
            playing = nextPlaying;
            startTime = EditorApplication.timeSinceStartup - selectedFrame / fps;
        }
        EditorGUI.BeginChangeCheck();
        fps = EditorGUILayout.Slider("Quadros por segundo", fps, 4f, 14f);
        if (EditorGUI.EndChangeCheck())
            startTime = EditorApplication.timeSinceStartup - selectedFrame / fps;
        zoom = EditorGUILayout.IntSlider("Ampliação inteira", zoom, 1, 5);
        if (playing)
            selectedFrame = (int)((EditorApplication.timeSinceStartup - startTime) * fps) % 8;
        else
            selectedFrame = EditorGUILayout.IntSlider("Quadro", selectedFrame + 1, 1, 8) - 1;

        Rect area = GUILayoutUtility.GetRect(1, 330, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(area, new Color(0.12f, 0.14f, 0.18f));
        Rect target = new Rect(area.center.x - 32 * zoom, area.center.y - 32 * zoom, 64 * zoom, 64 * zoom);
        // A fixed baseline reveals vertical movement without stabilizing each frame artificially.
        EditorGUI.DrawRect(new Rect(area.x, target.y + 59 * zoom, area.width, 1), new Color(0.5f, 0.45f, 0.3f));
        DrawSprite(target, frames[selectedFrame]);
        EditorGUILayout.LabelField("Quadro " + (selectedFrame + 1) + " / 8 — célula 64 × 64 — ciclo " + (8f / fps).ToString("0.00") + " s");
        Rect strip = GUILayoutUtility.GetRect(512, 64);
        for (int i = 0; i < 8; i++)
        {
            Rect cell = new Rect(strip.x + i * 64, strip.y, 64, 64);
            EditorGUI.DrawRect(cell, i == selectedFrame ? new Color(0.27f, 0.22f, 0.37f) : new Color(0.15f, 0.16f, 0.19f));
            DrawSprite(cell, frames[i]);
        }
        if (GUILayout.Button("Selecionar clipe no Project"))
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<AnimationClip>(directionIndex == 0 ? ClipPath : UpClipPath);
    }

    private static void DrawSprite(Rect rect, Sprite sprite)
    {
        Rect source = sprite.rect;
        Texture2D texture = sprite.texture;
        GUI.DrawTextureWithTexCoords(rect, texture,
            new Rect(source.x / texture.width, source.y / texture.height,
                source.width / texture.width, source.height / texture.height), true);
    }
}