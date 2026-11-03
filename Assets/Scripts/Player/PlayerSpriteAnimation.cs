using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(150)]
[RequireComponent(typeof(PlayerController), typeof(SpriteRenderer))]
public class PlayerSpriteAnimation : MonoBehaviour
{
    [SerializeField, Min(1f)] private float framesPerSecond = 8f;
    [SerializeField, Min(0.01f)] private float shotDuration = 0.24f;
    [SerializeField] private Sprite[] walkDown;
    [SerializeField] private Sprite[] walkUp;
    [SerializeField] private Sprite[] walkLeft;
    [SerializeField] private Sprite[] walkRight;
    [SerializeField] private Sprite[] shootDown;
    [SerializeField] private Sprite[] shootUp;
    [SerializeField] private Sprite[] shootLeft;
    [SerializeField] private Sprite[] shootRight;
    [SerializeField] private Sprite[] deathFrames;
    [SerializeField] private Sprite deadSprite;

    private enum Facing { Down, Up, Left, Right }
    private PlayerController controller;
    private SpriteRenderer spriteRenderer;
    private Weapon weapon;
    private Facing facing;
    private Facing shotFacing;
    private float walkElapsed;
    private float shotElapsed;
    private float currentShotDuration;
    private float deathElapsed;
    private bool dying;
    private bool shooting;
    private bool wasMoving;

    public float AttackDuration => Mathf.Min(Mathf.Max(0.01f, shotDuration),
        weapon != null ? weapon.ShotInterval * 0.85f : shotDuration);
    public float StandingHeight => walkDown != null && walkDown.Length > 0 && walkDown[0] != null
        ? walkDown[0].bounds.size.y * Mathf.Abs(transform.lossyScale.y) : 1.48f;

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        weapon = GetComponentInChildren<Weapon>();
    }

    private void OnEnable()
    {
        if (weapon != null) weapon.ShotStarted += OnShotStarted;
    }

    private void OnDisable()
    {
        if (weapon != null) weapon.ShotStarted -= OnShotStarted;
        shooting = false;
        wasMoving = false;
        walkElapsed = 0f;
    }

    public void PlayDeath()
    {
        dying = true;
        deathElapsed = 0f;
        shooting = false;
        spriteRenderer.flipX = false;
    }

    // The scroll is on the character's right in the front/back artwork.
    // Side casts use the same right-facing frames, mirrored for the left.
    public Vector3 CastOrigin(Vector2 direction)
    {
        Facing castFacing = DirectionToFacing(direction);
        float side = castFacing == Facing.Left ? -1f : 1f;
        Vector3 offset = new Vector3(side * 0.38f, 0.08f) * (StandingHeight / 1.48f);
        return transform.position + offset;
    }

    private void OnShotStarted(Vector2 direction)
    {
        if (dying) return;
        shotFacing = DirectionToFacing(direction);
        shotElapsed = 0f;
        currentShotDuration = AttackDuration;
        shooting = true;
    }

    private void LateUpdate()
    {
        if (dying)
        {
            deathElapsed += Time.unscaledDeltaTime;
            if (deadSprite != null && (deathElapsed >= 0.8f || deathFrames == null || deathFrames.Length == 0))
            {
                spriteRenderer.flipX = false;
                spriteRenderer.sprite = deadSprite;
            }
            else if (deathFrames != null && deathFrames.Length > 0)
                ShowFrame(deathFrames, Mathf.Min(deathFrames.Length - 1,
                    Mathf.FloorToInt(deathElapsed / 0.8f * deathFrames.Length)));
            return;
        }
        if (Time.timeScale == 0f) return;

        // Input is sampled every rendered frame; physics velocity may briefly be zero
        // between steps or during contact. Keep the walk cycle continuous on turns.
        Vector2 direction = controller.MovementDirection;
        bool moving = controller.isActiveAndEnabled && direction.sqrMagnitude > 0.001f;
        if (!moving || !wasMoving) walkElapsed = 0f;
        if (moving)
        {
            facing = DirectionToFacing(direction);
            Sprite[] walkFrames = Frames(facing, false);
            int frameCount = walkFrames != null ? walkFrames.Length : 0;
            if (frameCount > 0)
                walkElapsed = (walkElapsed + Time.deltaTime)
                    % (frameCount / Mathf.Max(1f, framesPerSecond));
        }
        wasMoving = moving;

        if (shooting && (weapon == null || !weapon.isActiveAndEnabled || !weapon.HasEnemyInRange))
            shooting = false;
        // Casting changes the pose without interrupting movement.
        // The walk cycle keeps advancing and resumes after the shot.
        if (shooting)
        {
            Sprite[] shots = Frames(shotFacing, true);
            if (shotElapsed < currentShotDuration && shots != null && shots.Length > 0)
            {
                ShowFrame(shots, Mathf.Min(shots.Length - 1,
                    Mathf.FloorToInt(shotElapsed / currentShotDuration * shots.Length)),
                    shotFacing == Facing.Left);
                shotElapsed += Time.deltaTime;
                if (!moving) facing = shotFacing;
                return;
            }
            shooting = false;
        }

        ShowFrame(Frames(facing, false),
            moving ? Mathf.FloorToInt(walkElapsed * Mathf.Max(1f, framesPerSecond)) : 0);
    }

    private static Facing DirectionToFacing(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y) && direction.x != 0f)
            return direction.x > 0f ? Facing.Right : Facing.Left;
        return direction.y > 0f ? Facing.Up : Facing.Down;
    }

    private Sprite[] Frames(Facing direction, bool attack)
    {
        switch (direction)
        {
            case Facing.Up: return attack ? shootUp : walkUp;
            case Facing.Left: return attack ? shootRight : walkLeft;
            case Facing.Right: return attack ? shootRight : walkRight;
            default: return attack ? shootDown : walkDown;
        }
    }

    private void ShowFrame(Sprite[] frames, int frame, bool flip = false)
    {
        if (frames == null || frames.Length == 0) return;
        Sprite sprite = frames[frame % frames.Length];
        if (sprite == null) return;
        spriteRenderer.flipX = flip;
        spriteRenderer.sprite = sprite;
    }
}
