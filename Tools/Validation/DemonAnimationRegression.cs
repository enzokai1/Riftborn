using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class DemonAnimationRegression
{
    private static void Set(object target, string field, object value) =>
        target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    private static T Get<T>(object target, string field) =>
        (T)target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
    private static void Call(object target, string method) =>
        target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, null);

    public static IEnumerator Run(Action<bool, string> check)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TestPrefabs/Enemy.prefab");
        check(prefab != null, "Actual enemy prefab imports successfully");
        var prefabMovement = prefab.GetComponent<EnemyController>();
        var prefabDeath = prefab.GetComponent<EnemyDeath>();
        Sprite[] second = Get<Sprite[]>(prefabMovement, "secondAttackFrames");
        Sprite[] hurt = Get<Sprite[]>(prefabMovement, "hurtFrames");
        Sprite[] death = Get<Sprite[]>(prefabDeath, "deathFrames");
        check(second.Length == 7 && second.All(frame => frame != null), "All seven Attack02 frames are bound");
        check(hurt.Length == 4 && hurt.All(frame => frame != null), "All four Hurt frames are bound");
        check(death.Length == 4 && death.All(frame => frame != null), "Death uses four complete poses, not disconnected fragments");
        foreach (Sprite[] frames in new[] { second, hurt, death })
        {
            for (int i = 0; i < frames.Length; i++)
            {
                Sprite frame = frames[i];
                Vector2 anchor = frame.rect.position + frame.pivot;
                check(Mathf.Abs(frame.pixelsPerUnit - 22) < 0.001f
                    && Mathf.Abs(anchor.x - (i * 100 + 53)) < 0.001f
                    && Mathf.Abs(anchor.y - 46.35066674f) < 0.001f,
                    frame.name + " preserves the standing sprite's scale and anchor");
            }
        }
        check(death[1].rect.xMax >= 168 && death[2].rect.xMax >= 268 && death[3].rect.x >= 300,
            "Death poses include detached artwork from the same animation cell");

        var statsObject = new GameObject("Animation test match");
        var match = statsObject.AddComponent<MatchStats>();
        match.enabled = false;
        var player = new GameObject("Animation test player", typeof(Rigidbody2D), typeof(BoxCollider2D));
        player.transform.position = Vector3.right * 20;
        player.AddComponent<PlayerController>().enabled = false;
        player.GetComponent<Rigidbody2D>().gravityScale = 0;
        player.AddComponent<PlayerHealth>();
        var enemy = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
        var movement = enemy.GetComponent<EnemyController>();
        var health = enemy.GetComponent<EnemyHealth>();
        var visual = enemy.GetComponent<SpriteRenderer>();
        var contact = enemy.GetComponent<EnemyContactDamage>();
        var dying = enemy.GetComponent<EnemyDeath>();
        health.ConfigureHealth(1000);
        movement.SetTarget(player.transform);
        contact.enabled = false;
        enemy.GetComponent<EnemyKillReporter>().SetMatchStats(match);

        for (int attack = 0; attack < 3; attack++)
        {
            movement.PlayAttack(0.7f, Vector2.right, 1, 0.55f);
            string name = attack == 1 ? "Demon_A_Attack02_" : "Demon_A_Attack01_";
            check(visual.sprite.name == name + "0", "Attacks alternate 01 / 02 / 01: " + attack);
            Set(movement, "attackElapsed", movement.AttackImpactTime - 0.001f);
            Call(movement, "LateUpdate");
            check(visual.sprite.name == name + (attack == 1 ? "2" : "3"), "Windup precedes impact: " + attack);
            Set(movement, "attackElapsed", movement.AttackImpactTime);
            Call(movement, "LateUpdate");
            check(visual.sprite.name == name + (attack == 1 ? "3" : "4")
                && Mathf.Abs(movement.AttackImpactTime - 0.4f) < 0.0001f, "Impact pose keeps the existing 0.4s hit: " + attack);
            Sprite impactSprite = visual.sprite;
            health.TakeDamage(1);
            check(movement.IsAttacking && visual.sprite == impactSprite
                && movement.AttackElapsed == movement.AttackImpactTime, "Receiving damage does not interrupt the attack: " + attack);
            movement.CancelAttack();
        }

        health.TakeDamage(1);
        check(visual.sprite == hurt[0], "Nonlethal damage starts Hurt when the enemy is free");
        Set(movement, "hurtElapsed", 0.1f);
        health.TakeDamage(1);
        check(Get<float>(movement, "hurtElapsed") == 0.1f, "Continuous fire does not restart Hurt");
        Call(movement, "FixedUpdate");
        check(enemy.GetComponent<Rigidbody2D>().linearVelocity.sqrMagnitude > 0,
            "Hurt does not stop movement or introduce stagger");
        Set(movement, "hurtElapsed", 0.24f);
        Call(movement, "LateUpdate");
        check(visual.sprite.name.Contains("Walk"), "Movement animation resumes after Hurt");
        health.TakeDamage(1);
        check(!Get<bool>(movement, "showingHurt"), "Hurt cooldown prevents immediate replay");
        Set(movement, "nextHurtAt", -1f);
        movement.BeginDash(12, 0.6f);
        health.TakeDamage(1);
        check(movement.IsDashing && !Get<bool>(movement, "showingHurt"), "Damage never interrupts the dash");
        movement.StopDash();
        movement.PlayAttack(0.7f);
        visual.flipX = true;
        health.TakeDamage(health.CurrentHealth);
        check(enemy.activeSelf && visual.sprite == death[0] && visual.flipX,
            "Death starts visibly and preserves facing");
        check(!movement.enabled && !contact.enabled && !enemy.GetComponent<Collider2D>().enabled
            && !enemy.GetComponent<Rigidbody2D>().simulated, "Dying enemy cannot attack, move or block the player");
        check(match.KillCount == 1 && Object.FindObjectsByType<ExperiencePickup>(FindObjectsSortMode.None).Length == 1,
            "Normal death reports a kill and drops XP immediately");
        health.TakeDamage(1000);
        check(match.KillCount == 1 && Object.FindObjectsByType<ExperiencePickup>(FindObjectsSortMode.None).Length == 1,
            "Corpse hits cannot duplicate kills or XP");
        Time.timeScale = 0;
        yield return null;
        Sprite pausedFrame = visual.sprite;
        float pausedTime = Get<float>(dying, "elapsed");
        yield return null;
        yield return null;
        yield return null;
        check(enemy != null && visual.sprite == pausedFrame && Get<float>(dying, "elapsed") == pausedTime,
            "Pause freezes the death animation");
        Time.timeScale = 1;
        for (int i = 1; i < death.Length; i++)
        {
            Set(dying, "elapsed", i / 8f - Time.deltaTime + 0.001f);
            Call(dying, "Update");
            check(visual.sprite == death[i], "Death displays complete pose " + i);
        }
        Set(dying, "elapsed", death.Length / 8f);
        Call(dying, "Update");
        yield return null;
        check(enemy == null, "Body is removed after the last death pose");
        foreach (var xp in Object.FindObjectsByType<ExperiencePickup>(FindObjectsSortMode.None)) Object.DestroyImmediate(xp.gameObject);

        // Exercise the real boss kill -> StopCombat -> rewards path, in both callback orders.
        for (int reporterFirst = 0; reporterFirst < 2; reporterFirst++)
        {
            if (reporterFirst > 0)
            {
                Object.DestroyImmediate(statsObject);
                statsObject = new GameObject("Animation test match 2");
                match = statsObject.AddComponent<MatchStats>();
                match.enabled = false;
            }
            var boss = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
            boss.transform.localScale *= 2.5f;
            var bossHealth = boss.GetComponent<EnemyHealth>();
            var bossDeath = boss.GetComponent<EnemyDeath>();
            var reporter = boss.GetComponent<EnemyKillReporter>();
            reporter.SetMatchStats(match);
            reporter.MarkAsBoss();
            match.SetBoss(bossHealth);
            boss.GetComponent<EnemyController>().SetTarget(player.transform);
            if (reporterFirst > 0) { bossDeath.enabled = false; bossDeath.enabled = true; }
            var objective = statsObject.AddComponent<FirstStageObjective>();
            objective.enabled = false;
            var stageVisuals = statsObject.AddComponent<StageVisuals>();
            stageVisuals.Initialize(boss.GetComponent<SpriteRenderer>());
            var exit = new GameObject("Test exit").transform;
            exit.SetParent(statsObject.transform);
            Set(objective, "match", match);
            Set(objective, "visuals", stageVisuals);
            Set(objective, "exit", exit);
            Set(match, "<Objective>k__BackingField", objective);
            int previousKills = match.KillCount;
            bossHealth.TakeDamage(bossHealth.CurrentHealth);
            check(match.BossDefeated && match.KillCount == previousKills + 1,
                "Boss defeat registers once, callback order " + reporterFirst);
            check(boss.activeSelf && boss.GetComponent<SpriteRenderer>().sprite == death[0],
                "StopCombat preserves the boss death animation, callback order " + reporterFirst);
            check(!boss.GetComponent<Collider2D>().enabled && !boss.GetComponent<Rigidbody2D>().simulated,
                "Boss corpse cannot block rewards, callback order " + reporterFirst);
            check(Object.FindObjectsByType<StagePickup>(FindObjectsSortMode.None).Length == 2
                && Object.FindObjectsByType<ExperiencePickup>(FindObjectsSortMode.None).Length == 0,
                "Boss still drops only its ring and letter, callback order " + reporterFirst);
            Set(bossDeath, "elapsed", 0.5f);
            Call(bossDeath, "Update");
            yield return null;
            check(boss == null, "Boss body is removed after animation, callback order " + reporterFirst);
        }
        Object.DestroyImmediate(statsObject);
        Object.DestroyImmediate(player);
    }
}