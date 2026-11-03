using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerHealth))]
public class PlayerRing : MonoBehaviour
{
    public const string UnlockKey = "WinstonsWay.RingOfTheDead.Unlocked";
    public const float Duration = 5f;
    private MatchStats match;
    private PlayerHealth health;
    private SpriteRenderer visual;
    private Color originalColor;
    private Collider2D[] colliders;
    private LayerMask[] originalMasks;
    private float activeUntil;
    private float readyAt;
    private bool phased;
    public bool Unlocked { get; private set; }
    public bool IsActive => phased;
    public float Remaining => Mathf.Max(0, activeUntil - Time.time);
    public float CooldownRemaining => Mathf.Max(0, readyAt - Time.time);

    public void Configure(MatchStats stats)
    {
        match = stats;
        health = GetComponent<PlayerHealth>();
        visual = GetComponent<SpriteRenderer>();
        originalColor = visual != null ? visual.color : Color.white;
        colliders = GetComponents<Collider2D>();
        originalMasks = new LayerMask[colliders.Length];
        for (int i = 0; i < colliders.Length; i++) originalMasks[i] = colliders[i].excludeLayers;
        Unlocked = PlayerPrefs.GetInt(UnlockKey, 0) == 1;
    }

    public void Unlock()
    {
        Unlocked = true;
        PlayerPrefs.SetInt(UnlockKey, 1);
        PlayerPrefs.Save();
    }

    public bool TryActivate()
    {
        if (!Unlocked || phased || match == null || match.Finished || match.IsPaused
            || health.CurrentHealth <= 0 || CooldownRemaining > 0) return false;
        phased = true;
        activeUntil = Time.time + Duration;
        readyAt = Time.time + Mathf.Max(Duration, match.Rules.ringCooldown);
        health.SetInvulnerable(true);
        int enemyMask = LayerMask.GetMask("Enemy");
        for (int i = 0; i < colliders.Length; i++) colliders[i].excludeLayers = originalMasks[i].value | enemyMask;
        if (visual != null) visual.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0.4f);
        return true;
    }

    private void Update()
    {
        if (phased && (Time.time >= activeUntil || match == null || match.Finished)) EndEffect();
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) TryActivate();
    }

    private void EndEffect()
    {
        phased = false;
        if (health != null) health.SetInvulnerable(false);
        if (visual != null) visual.color = originalColor;
        if (colliders != null)
            for (int i = 0; i < colliders.Length; i++)
                if (colliders[i] != null) colliders[i].excludeLayers = originalMasks[i];
    }

    private void OnDisable() { if (phased) EndEffect(); }
}

