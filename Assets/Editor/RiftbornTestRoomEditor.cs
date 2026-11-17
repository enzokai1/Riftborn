using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class RiftbornTestRoomEditor : EditorWindow
{
    private const string PendingKey = "Riftborn.TestRoom.Pending";
    private const string PreviousMatchKey = "Riftborn.TestRoom.PreviousMatch";
    [SerializeField] private string status = "Escolha um teste abaixo.";
    private const string ActiveKey = "Riftborn.TestRoom.Active";
    [SerializeField] private bool invulnerable = true;

    [MenuItem("Tools/Riftborn/Testar quarto")]
    public static void Open() => GetWindow<RiftbornTestRoomEditor>("Testar quarto").Show();

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Teste rapido do primeiro nivel", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Cada atalho inicia uma partida nova no ponto escolhido. Use WASD na aba Game. Nada e salvo na cena.", MessageType.Info);
        EditorGUILayout.HelpBox(status, MessageType.Info);
        invulnerable = EditorGUILayout.Toggle("Invulnerabilidade", invulnerable);
        using (new EditorGUI.DisabledScope(EditorApplication.isCompiling || EditorApplication.isUpdating))
        {
            if (GUILayout.Button("Explorar quarto sem inimigos")) Begin(1);
            if (GUILayout.Button("Testar XP sem inimigos")) Begin(4);
            if (GUILayout.Button("Ir direto para o chefe")) Begin(2);
            if (GUILayout.Button("Ir direto para a porta liberada")) Begin(3);
        }
        if (EditorApplication.isPlaying && GUILayout.Button("Mostrar porta agora (sem reiniciar)"))
        {
            SessionState.SetInt(PreviousMatchKey, 0);
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetInt(PendingKey, 3);
            status = "Preparando a porta...";
        }
        if (EditorApplication.isPlaying && GUILayout.Button("Encerrar teste")) EditorApplication.isPlaying = false;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Na porta: aproxime-se da imagem e pressione E.", EditorStyles.wordWrappedLabel);
    }

    private void Begin(int mode)
    {
        if (!EditorApplication.isPlaying)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            status = "Aguardando a nova partida...";
            SessionState.SetInt(PendingKey, mode);
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetInt(PreviousMatchKey, 0);
            EditorApplication.isPlaying = true;
        }
        else
        {
            status = "Aguardando a nova partida...";
            SessionState.SetInt(PendingKey, mode);
            SessionState.SetBool(ActiveKey, true);
            Time.timeScale = 1f;
            MatchStats previous = Object.FindFirstObjectByType<MatchStats>();
            SessionState.SetInt(PreviousMatchKey, previous != null ? previous.GetInstanceID() : 0);
            SceneManager.LoadScene("SampleScene");
        }
    }

    private void Update()
    {
        if (!EditorApplication.isPlaying)
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.SetInt(PendingKey, 0);
                SessionState.SetBool(ActiveKey, false);
            }
            return;
        }
        if (!SessionState.GetBool(ActiveKey, false)) return;
        MatchStats match = Object.FindFirstObjectByType<MatchStats>();
        if (match == null || match.Player == null || match.Objective == null || match.Finished) return;
        if (match.GetInstanceID() == SessionState.GetInt(PreviousMatchKey, 0)) return;
        match.Player.EditorInvulnerable = invulnerable;
        int mode = SessionState.GetInt(PendingKey, 0);
        if (mode == 0) return;
        if (match.IsPaused)
        {
            status = "Despause com Esc ou escolha a melhoria pendente para continuar.";
            Repaint();
            return;
        }
        SessionState.SetInt(PendingKey, 0);
        if (mode == 1 || mode == 4)
        {
            foreach (EnemySpawner spawner in Object.FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None)) spawner.enabled = false;
            foreach (EnemyHealth enemy in Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
            { enemy.gameObject.SetActive(false); Object.Destroy(enemy.gameObject); }
            match.enabled = false; // Freeze waves/blackouts while the player explores normally.
            if (mode == 4)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Progression/ExperiencePickup.prefab");
                float radius = match.Experience != null ? match.Experience.ExperienceAttractionRadius + 2f : 6f;
                for (int i = 0; i < 16; i++)
                {
                    float angle = i * Mathf.PI * 2f / 16f;
                    Vector3 point = match.Player.transform.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    if (prefab == null || match.Objective.IsFurnitureBlocked(point, 0.5f)
                        || (HouseFloor.Active != null && !HouseFloor.Active.Contains(point, 0.5f))) continue;
                    Object.Instantiate(prefab, point, Quaternion.identity);
                }
            }
        }
        else if (mode == 2) match.Objective.EditorCompleteFragments();
        else
        {
            match.Objective.EditorUnlockExit();
            if (!match.BossDefeated || !match.Objective.RingCollected || !match.Objective.LetterCollected)
            {
                status = "A porta ainda nao foi liberada. Tente novamente depois de despausar.";
                Repaint();
                return;
            }
            Vector3 position = match.Objective.ExitPosition + Vector3.down * 2.5f;
            match.Player.transform.position = position;
            if (match.Player.TryGetComponent(out Rigidbody2D body))
            { body.position = position; body.linearVelocity = Vector2.zero; }
            if (Camera.main != null && Camera.main.TryGetComponent(out CameraFollow camera)) camera.SnapToPlayer();
        }
        status = mode == 4 ? "Essencias de XP ao redor do jogador. Caminhe para atrair e coletar." : mode == 3 ? "Porta liberada a sua frente na aba Game. Aproxime-se e pressione E."
            : mode == 2 ? "Chefe liberado. Invulnerabilidade ativa se marcada acima." : "Exploracao sem inimigos pronta.";
        EditorApplication.ExecuteMenuItem("Window/General/Game");
        Repaint();
    }

    private void OnDisable()
    {
        if (!EditorApplication.isPlaying) return;
        MatchStats match = Object.FindFirstObjectByType<MatchStats>();
        if (match != null && match.Player != null) match.Player.EditorInvulnerable = false;
    }
}