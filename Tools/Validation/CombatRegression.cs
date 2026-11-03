using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

// Copied into the isolated validation project's Assets/Editor by the runner.
[InitializeOnLoad]
public static class CombatRegression
{
    private const string Running = "Riftborn.CombatRegression.Running";
    private static IEnumerator checks;
    private static double nextUpdate;
    private static double deadline;
    private static int passed;

    static CombatRegression()
    {
        if (SessionState.GetBool(Running, false))
        {
            deadline = EditorApplication.timeSinceStartup + 90;
            EditorApplication.update += Tick;
        }
    }

    public static void Begin()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        foreach (var spawner in Object.FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None))
            spawner.enabled = false;
        SessionState.SetBool(Running, true);
        EditorApplication.isPlaying = true;
    }

    private static void Tick()
    {
        if (EditorApplication.timeSinceStartup > deadline)
        {
            Finish(2, "Timed out entering play mode or running checks.");
            return;
        }
        if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup < nextUpdate) return;
        try
        {
            if (checks == null) checks = Run();
            if (!checks.MoveNext()) { Finish(0, passed + " checks passed."); return; }
            nextUpdate = EditorApplication.timeSinceStartup + (checks.Current is float delay ? delay : 0.02f);
        }
        catch (Exception error) { Finish(2, error.ToString()); }
    }

    private static void Finish(int code, string message)
    {
        SessionState.SetBool(Running, false);
        EditorApplication.update -= Tick;
        if (code == 0) Debug.Log("COMBAT_REGRESSION_PASS: " + message);
        else Debug.LogError("COMBAT_REGRESSION_FAIL: " + message);
        EditorApplication.Exit(code);
    }

    private static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
        passed++;
        Debug.Log("CHECK PASS: " + message);
    }

    private static void Set(object target, string name, object value) =>
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

    private static T Get<T>(object target, string name) =>
        (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);

    private static void Call(object target, string name, params object[] args) =>
        target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);

    private static EnemyHealth Enemy(Vector2 position)
    {
        var obj = new GameObject("Regression enemy", typeof(CircleCollider2D), typeof(EnemyHealth));
        obj.layer = LayerMask.NameToLayer("Enemy");
        obj.transform.position = position;
        obj.GetComponent<CircleCollider2D>().radius = 0.2f;
        EnemyHealth health = obj.GetComponent<EnemyHealth>();
        health.ConfigureHealth(100);
        return health;
    }

    private static void Aim(Vector3 world)
    {
        Mouse mouse = Mouse.current ?? InputSystem.AddDevice<Mouse>();
        InputState.Change(mouse.position, (Vector2)Camera.main.WorldToScreenPoint(world));
    }

    private static IEnumerator Run()
    {
        yield return 0.3f;
        foreach (var enemy in Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
            Object.Destroy(enemy.gameObject);
        yield return 0.1f;
        var match = Object.FindFirstObjectByType<MatchStats>();
        var player = Object.FindFirstObjectByType<PlayerController>();
        var body = player.GetComponent<Rigidbody2D>();
        var health = player.GetComponent<PlayerHealth>();
        var animation = player.GetComponent<PlayerSpriteAnimation>();
        var renderer = player.GetComponent<SpriteRenderer>();
        var weapon = player.GetComponentInChildren<Weapon>();
        health.SetInvulnerable(true);
        var bed = GameObject.Find("Cama");
        Check(bed != null, "Bed exists using imported furniture.");
        var bedCollider = bed.GetComponent<BoxCollider2D>();
        var feet = player.GetComponent<BoxCollider2D>();
        Check(bed.GetComponent<Rigidbody2D>().bodyType == RigidbodyType2D.Static && !bedCollider.isTrigger,
            "Bed is a fixed solid object.");
        float ratio = bed.GetComponent<SpriteRenderer>().bounds.size.y / animation.StandingHeight;
        Check(Mathf.Abs(ratio - 1.35f) < 0.02f, "Bed height is proportional to the player (1.35x).");
        Physics2D.SyncTransforms();
        Check(!feet.Distance(bedCollider).isOverlapped, "Player spawns outside the bed.");
        foreach (Vector2 fragment in match.Objective.FragmentPositions)
            Check(!match.Objective.IsFurnitureBlocked(fragment, 0.7f), "Fragment remains reachable outside bed.");

        int starts = 0, shots = 0;
        weapon.ShotStarted += _ => starts++;
        weapon.ShotFired += _ => shots++;
        Aim(player.transform.position + Vector3.right * 4f);
        Call(weapon, "LateUpdate");
        Check(!weapon.HasEnemyInRange && starts == 0 && shots == 0, "No enemies means no automatic fire.");
        var target = Enemy((Vector2)player.transform.position + Vector2.right * 8f);
        Physics2D.SyncTransforms();
        Call(weapon, "LateUpdate");
        Check(!weapon.HasEnemyInRange && starts == 0, "An enemy beyond six units does not start a cast.");
        target.transform.position = player.transform.position + Vector3.right * 4f;
        Physics2D.SyncTransforms();
        Aim(target.transform.position);
        Set(weapon, "nextShotTime", 0f);
        Call(weapon, "LateUpdate");
        Check(weapon.HasEnemyInRange && starts == 1 && shots == 0, "Nearby enemy starts windup before projectile release.");
        Set(player, "movementDirection", Vector2.zero);
        body.linearVelocity = Vector2.zero;
        Call(animation, "LateUpdate");
        Check(renderer.sprite.name.Contains("Shoot"), "Standing still shows the casting animation.");
        Set(player, "movementDirection", Vector2.right);
        body.linearVelocity = Vector2.right * 3f;
        Call(animation, "LateUpdate");
        Check(renderer.sprite.name.Contains("Shoot"), "Casting animation is also shown while moving.");
        Set(weapon, "releaseTime", -1f);
        Call(weapon, "LateUpdate");
        Check(shots == 1, "Windup releases exactly one shot.");
        var projectile = Object.FindFirstObjectByType<Projectile>();
        Check(projectile != null && projectile.GetComponent<SpriteRenderer>().sprite.name == "Player_Projectile",
            "Projectile uses the original blue fireball sprite.");
        Check(Vector2.Distance(projectile.transform.position, animation.CastOrigin(Vector2.right)) < 0.01f,
            "Fireball starts at the scroll.");
        Call(weapon, "LateUpdate");
        Check(shots == 1, "Cooldown prevents another immediate shot.");
        Set(animation, "shotElapsed", 1f);
        Call(animation, "LateUpdate");
        Check(renderer.sprite.name.Contains("Walk"), "Walking resumes when the cast ends.");
        Set(player, "movementDirection", Vector2.zero);
        body.linearVelocity = Vector2.zero;
        Call(animation, "LateUpdate");
        Check(renderer.sprite.name.Contains("Walk"), "Stopping does not replay an old cast.");

        Call(animation, "OnShotStarted", Vector2.left);
        Call(animation, "LateUpdate");
        Check(renderer.flipX, "Left-facing casting is mirrored consistently.");
        Set(weapon, "nextShotTime", 0f);
        Call(weapon, "LateUpdate");
        int beforePause = shots;
        Time.timeScale = 0f;
        Call(weapon, "LateUpdate");
        Check(shots == beforePause, "Pause prevents projectile release.");
        Time.timeScale = 1f;
        target.transform.position = player.transform.position + Vector3.right * 9f;
        Physics2D.SyncTransforms();
        Call(weapon, "LateUpdate");
        Check(!Get<bool>(weapon, "pendingShot"), "Leaving range cancels a pending cast.");
        target.transform.position = player.transform.position + Vector3.right * 4f;
        target.TakeDamage(100);
        Physics2D.SyncTransforms();
        Call(weapon, "LateUpdate");
        Check(!weapon.HasEnemyInRange, "Dead enemies cannot trigger shooting.");
        weapon.enabled = false;
        Object.Destroy(target.gameObject);
        foreach (var shot in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))
            Object.Destroy(shot.gameObject);
        yield return 0.1f;

        player.enabled = false;
        Vector2 bedStart = bed.transform.position;
        SimulationMode2D previousMode = Physics2D.simulationMode;
        Physics2D.simulationMode = SimulationMode2D.Script;
        body.position = new Vector2(bedCollider.bounds.max.x + 0.7f, bedCollider.bounds.center.y + 0.52f);
        Physics2D.SyncTransforms();
        Set(player, "movementDirection", Vector2.left);
        for (int i = 0; i < 35; i++) { Call(player, "FixedUpdate"); Physics2D.Simulate(0.02f); }
        Check(body.position.x >= bedCollider.bounds.max.x + feet.bounds.extents.x - 0.03f,
            "Player cannot walk through the bed.");
        Check(Vector2.Distance(bed.transform.position, bedStart) < 0.0001f, "Walking into the bed cannot push it.");
        float startY = body.position.y;
        Set(player, "movementDirection", new Vector2(-1f, 1f).normalized);
        for (int i = 0; i < 10; i++) { Call(player, "FixedUpdate"); Physics2D.Simulate(0.02f); }
        Check(body.position.y > startY + 0.2f, "Player slides along furniture without friction sticking.");
        Set(player, "movementDirection", Vector2.zero);
        Call(player, "FixedUpdate");
        Check(body.linearVelocity.sqrMagnitude < 0.0001f, "Releasing movement stops the player.");
        body.position = bedStart + Vector2.right * 4f;
        Set(player, "movementDirection", Vector2.right);
        Call(player, "FixedUpdate");
        float cardinalSpeed = body.linearVelocity.magnitude;
        Set(player, "movementDirection", Vector2.one.normalized);
        Call(player, "FixedUpdate");
        Check(Mathf.Abs(body.linearVelocity.magnitude - cardinalSpeed) < 0.001f, "Diagonal and cardinal speeds match.");
        body.linearVelocity = Vector2.zero;
        Physics2D.simulationMode = previousMode;

        var enemyHit = Enemy((Vector2)player.transform.position + Vector2.right * 2f);
        var extraCollider = new GameObject("Second hit collider", typeof(CircleCollider2D));
        extraCollider.layer = LayerMask.NameToLayer("Enemy");
        extraCollider.transform.SetParent(enemyHit.transform, false);
        extraCollider.GetComponent<CircleCollider2D>().radius = 0.2f;
        Projectile prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Weapons/Projectile.prefab").GetComponent<Projectile>();
        var bolt = Object.Instantiate(prefab, player.transform.position, Quaternion.identity);
        bolt.Initialize(Vector2.right, 10f, 7, 6f);
        yield return 0.6f;
        Check(enemyHit.CurrentHealth == 93, "Compound enemy colliders receive exactly one damage application.");
        Check(bolt == null, "Fireball disappears on impact.");
        Object.Destroy(enemyHit.gameObject);
        var miss = Object.Instantiate(prefab, player.transform.position + Vector3.down * 3f, Quaternion.identity);
        miss.Initialize(Vector2.right, 10f, 7, 1f);
        yield return 0.5f;
        Check(miss == null, "Missed fireballs expire at their maximum travel range.");
        health.SetInvulnerable(false);
        health.TakeDamage(health.MaxHealth);
        Check(!player.enabled && !weapon.enabled, "Death stops movement and attacks.");
    }
}
