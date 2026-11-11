using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// Run in the isolated project created by RunGunterSwingRegression.ps1.
[InitializeOnLoad]
public static class GunterSwingRegression
{
    private const string Running = "Riftborn.GunterSwingRegression";
    private static IEnumerator checks;
    private static double deadline;
    private static double nextTick;
    private static int passed;

    static GunterSwingRegression()
    {
        if (!SessionState.GetBool(Running, false)) return;
        deadline = EditorApplication.timeSinceStartup + 90;
        EditorApplication.update += Tick;
    }

    public static void Begin()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(Running, true);
        EditorApplication.isPlaying = true;
    }

    private static void Tick()
    {
        if (EditorApplication.timeSinceStartup > deadline) { Finish(2, "Timeout"); return; }
        if (!Application.isPlaying || EditorApplication.timeSinceStartup < nextTick) return;
        try
        {
            if (checks == null) checks = Run();
            if (!checks.MoveNext()) { Finish(0, passed + " checks passed"); return; }
            nextTick = EditorApplication.timeSinceStartup + 0.15;
        }
        catch (Exception error) { Finish(2, error.ToString()); }
    }

    private static void Finish(int code, string message)
    {
        SessionState.SetBool(Running, false);
        EditorApplication.update -= Tick;
        Debug.Log((code == 0 ? "GUNTER_REGRESSION_PASS: " : "GUNTER_REGRESSION_FAIL: ") + message);
        EditorApplication.Exit(code);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        passed++;
        Debug.Log("CHECK PASS: " + message);
    }

    private static void Set(object target, string field, object value) =>
        target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    private static void Call(object target, string method) =>
        target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, null);

    private sealed class Fight : IDisposable
    {
        public readonly GameObject boss, hero, stats;
        public readonly Rigidbody2D bossBody, playerBody;
        public readonly EnemyController movement;
        public readonly EnemyContactDamage contact;
        public readonly PlayerHealth health;
        public readonly MatchStats match;
        public readonly GunterAreaAttack area;

        public Fight(Vector2 playerCenter, bool isBoss = true, bool secondAttack = false)
        {
            stats = new GameObject("Test match");
            match = stats.AddComponent<MatchStats>();
            match.enabled = false;
            hero = new GameObject("Test player", typeof(Rigidbody2D), typeof(BoxCollider2D));
            playerBody = hero.GetComponent<Rigidbody2D>();
            playerBody.gravityScale = 0;
            playerBody.freezeRotation = true;
            playerBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var feet = hero.GetComponent<BoxCollider2D>();
            // Same feet dimensions as PlayerController.Awake.
            feet.size = new Vector2(0.5f, 0.32f);
            feet.offset = new Vector2(0, -0.52f);
            playerBody.position = playerCenter - feet.offset;
            hero.AddComponent<PlayerController>().enabled = false;
            health = hero.AddComponent<PlayerHealth>();
            boss = new GameObject("Test Gunter", typeof(Rigidbody2D), typeof(BoxCollider2D));
            bossBody = boss.GetComponent<Rigidbody2D>();
            bossBody.gravityScale = 0;
            bossBody.freezeRotation = true;
            // Enemy.prefab's box, offset and the boss's 2.5x spawn scale.
            boss.transform.localScale = Vector3.one * 2.5f;
            boss.GetComponent<BoxCollider2D>().offset = new Vector2(0, 0.21133333f);
            bossBody.position = new Vector2(0, -0.21133333f * 2.5f);
            movement = boss.AddComponent<EnemyController>();
            movement.SetTarget(hero.transform);
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TestPrefabs/Enemy.prefab").GetComponent<EnemyController>();
            foreach (string field in new[] { "attackFrames", "secondAttackFrames" })
                Set(movement, field, typeof(EnemyController).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(source));
            if (secondAttack) { movement.PlayAttack(0.7f); movement.CancelAttack(); }
            var enemyHealth = boss.AddComponent<EnemyHealth>();
            contact = boss.AddComponent<EnemyContactDamage>();
            contact.SetDamage(10);
            area = boss.AddComponent<GunterAreaAttack>();
            area.enabled = false;
            Set(area, "match", match);
            Set(area, "player", health);
            Set(area, "health", enemyHealth);
            Set(area, "movement", movement);
            Set(area, "body", bossBody);
            Set(area, "contact", contact);
            Set(area, "warning", boss.AddComponent<LineRenderer>());
            if (isBoss) contact.ConfigureBossDash(match, area);
            Set(contact, "nextAttackTime", float.PositiveInfinity);
            Physics2D.SyncTransforms();
            Call(contact, "Update"); // Cache the real target components.
        }

        public void Swing() => Call(contact, "StartSwing");
        public void Step(int count, Vector2 playerVelocity)
        {
            for (int i = 0; i < count; i++)
            {
                playerBody.linearVelocity = playerVelocity;
                Call(movement, "FixedUpdate");
                Physics2D.Simulate(Time.fixedDeltaTime);
                Call(contact, "Update");
                Call(movement, "LateUpdate");
            }
        }
        public void Dispose()
        {
            Object.DestroyImmediate(boss);
            Object.DestroyImmediate(hero);
            Object.DestroyImmediate(stats);
        }
    }

    private static IEnumerator Run()
    {
        yield return null;
        Time.fixedDeltaTime = 0.02f;
        Physics2D.simulationMode = SimulationMode2D.Script;
        for (int variant = 0; variant < 2; variant++)
        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI / 4;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            float x = Mathf.Abs(direction.x) > 0.001f ? 1.5f / Mathf.Abs(direction.x) : float.PositiveInfinity;
            float y = Mathf.Abs(direction.y) > 0.001f ? 1.41f / Mathf.Abs(direction.y) : float.PositiveInfinity;
            Vector2 start = direction * (Mathf.Min(x, y) + 0.3f);
            using (var fight = new Fight(start, true, variant == 1))
            {
                fight.Swing();
                fight.Step(25, direction * 2f);
                Check(fight.health.CurrentHealth == 90, "Straight retreat takes one hit, direction " + i + ", attack " + variant);
                fight.Step(30, Vector2.zero);
                Check(fight.health.CurrentHealth == 90, "No repeated damage during recovery, direction " + i + ", attack " + variant);
            }
            using (var fight = new Fight(start, true, variant == 1))
            {
                fight.Swing();
                fight.Step(25, new Vector2(-direction.y, direction.x) * 2f);
                Check(fight.health.CurrentHealth == 100, "Sidestep avoids committed stab, direction " + i + ", attack " + variant + "; boss=" + fight.bossBody.position + "; player=" + fight.playerBody.position);
            }
        }
        using (var fight = new Fight(Vector2.right * 1.8f))
        {
            fight.match.Rules.bossSwingAdvance = 0;
            fight.Swing();
            fight.Step(25, Vector2.right * 2);
            Check(fight.health.CurrentHealth == 100, "Baseline without advance reproduces safe retreat");
        }
        using (var fight = new Fight(Vector2.right * 8))
        {
            Vector2 start = fight.bossBody.position;
            fight.Swing();
            fight.Step(10, Vector2.zero);
            Check(Vector2.Distance(start, fight.bossBody.position) < 0.001f, "Windup stays still");
            fight.playerBody.position = new Vector2(-8, 8);
            fight.Step(15, Vector2.zero);
            Check(Vector2.Distance(fight.bossBody.position, start + Vector2.right) < 0.002f,
                "Lunge travels exactly one unit and does not retarget");
            fight.Step(15, Vector2.zero);
            Check(fight.movement.IsAttacking && fight.contact.IsBusy && fight.bossBody.linearVelocity == Vector2.zero,
                "Recovery keeps Gunter stationary and busy after the animation");
            Set(fight.area, "elapsed", 100f);
            Call(fight.area, "Update");
            Check(!fight.area.IsWarning, "Area attack cannot interrupt recovery");
            fight.Step(24, Vector2.zero);
            Check(!fight.movement.IsAttacking && !fight.contact.IsBusy, "Recovery ends after 1.25 seconds total");
            Call(fight.area, "Update");
            Check(fight.area.IsWarning, "Area attack becomes available after recovery");
        }
        using (var fight = new Fight(Vector2.right * 8))
        {
            var wall = new GameObject("Wall", typeof(BoxCollider2D));
            wall.transform.position = new Vector2(1.5f, 0);
            wall.GetComponent<BoxCollider2D>().size = new Vector2(0.2f, 6);
            Physics2D.SyncTransforms();
            fight.Swing();
            fight.Step(25, Vector2.zero);
            Check(fight.bossBody.position.x < 0.2f, "Lunge cannot pass through a solid obstacle");
            Object.DestroyImmediate(wall);
        }
        using (var fight = new Fight(Vector2.right * 1.8f, false))
        {
            Vector2 start = fight.bossBody.position;
            fight.Swing();
            fight.Step(25, Vector2.zero);
            Check(fight.health.CurrentHealth == 90 && Vector2.Distance(start, fight.bossBody.position) < 0.001f,
                "Normal enemy still attacks without a lunge");
            fight.Step(11, Vector2.zero);
            Check(!fight.contact.IsBusy, "Normal enemy has no extra boss recovery");
        }
        using (var fight = new Fight(Vector2.right * 8))
        {
            fight.Swing();
            fight.Step(15, Vector2.zero);
            Vector2 position = fight.bossBody.position;
            float elapsed = fight.movement.AttackElapsed;
            Time.timeScale = 0;
            yield return null;
            Call(fight.contact, "Update");
            Call(fight.movement, "LateUpdate");
            Check(fight.movement.AttackElapsed == elapsed && fight.bossBody.position == position,
                "Pause freezes swing movement and timing");
            Time.timeScale = 1;
            fight.contact.enabled = false;
            Check(!fight.movement.IsAttacking && fight.bossBody.linearVelocity == Vector2.zero,
                "Disabling combat cancels the lunge immediately");
        }
        IEnumerator animations = DemonAnimationRegression.Run(Check);
        while (animations.MoveNext()) yield return animations.Current;
    }
}
