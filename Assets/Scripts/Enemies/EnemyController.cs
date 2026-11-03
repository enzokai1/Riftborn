using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyController : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField, Min(0f)] private float movementSpeed = 3f;
    [SerializeField, Min(1f)] private float animationFramesPerSecond = 10f;
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private Sprite[] walkFrames;
    [SerializeField] private Sprite[] attackFrames;

    private Rigidbody2D body;
    private Collider2D bodyCollider;
    private FirstStageObjective stage;
    private readonly Vector2[] routeNodes = new Vector2[6];
    private readonly float[] routeCosts = new float[6];
    private readonly int[] routeParents = new int[6];
    private readonly bool[] routeVisited = new bool[6];
    private SpriteRenderer spriteRenderer;
    private float animationElapsed;
    private float attackElapsed;
    private float attackDuration;
    private bool moving;
    public Transform Target => target;
    public bool IsAttacking { get; private set; }
    public bool IsDashing => dashRemaining > 0f;
    private float dashRemaining;
    private float dashSpeed;

    public void BeginDash(float speed, float duration)
    {
        if (!enabled || IsAttacking) return;
        dashSpeed = Mathf.Max(1f, speed);
        dashRemaining = Mathf.Max(0.1f, duration);
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    public void StopDash()
    {
        dashRemaining = 0f;
        body.linearVelocity = Vector2.zero;
    }

    private void Awake()
    {
        movementSpeed = Mathf.Max(0f, movementSpeed);
        body = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        stage = FindFirstObjectByType<FirstStageObjective>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void SetMovementSpeed(float speed) => movementSpeed = Mathf.Max(0f, speed);
    public void SetTarget(Transform newTarget) => target = newTarget;

    public void PlayAttack(float duration)
    {
        IsAttacking = true;
        attackElapsed = 0f;
        attackDuration = Mathf.Max(0.01f, duration);
        body.linearVelocity = Vector2.zero;
        FaceTarget();
        ShowFrame(attackFrames, 0);
    }

    private void FixedUpdate()
    {
        moving = false;
        if (target == null || IsAttacking)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }
        Vector2 offset = PursuitWaypoint() - body.position;
        float currentSpeed = IsDashing ? dashSpeed : movementSpeed;
        dashRemaining = Mathf.Max(0f, dashRemaining - Time.fixedDeltaTime);
        float speed = Mathf.Min(currentSpeed, offset.magnitude / Time.fixedDeltaTime);
        moving = speed > 0.01f;
        body.linearVelocity = offset.normalized * speed;
    }

    // Visibility graph around the bed, expanded by this enemy's collider.
    // Work in collider-center coordinates so offset colliders also clear the corners.
    private Vector2 PursuitWaypoint()
    {
        Vector2 destination = target.position;
        Collider2D obstacle = stage != null ? stage.BedObstacle : null;
        if (obstacle == null || !obstacle.enabled || !obstacle.gameObject.activeInHierarchy)
            return destination;
        Vector2 centerOffset = bodyCollider != null
            ? (Vector2)bodyCollider.bounds.center - body.position : Vector2.zero;
        Vector2 halfSize = bodyCollider != null
            ? (Vector2)bodyCollider.bounds.extents : Vector2.one * 0.5f;
        Bounds bounds = obstacle.bounds;
        Vector2 min = (Vector2)bounds.min - halfSize - Vector2.one * 0.03f;
        Vector2 max = (Vector2)bounds.max + halfSize + Vector2.one * 0.03f;
        Rect blocked = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        Vector2 start = body.position + centerOffset;
        Vector2 goal = destination + centerOffset;
        // Contact or a target beside the bed may lie inside the clearance margin.
        // Move out along the nearest face before planning, and approach reachable ground.
        if (blocked.Contains(start)) return OutsideBed(start, blocked) - centerOffset;
        if (blocked.Contains(goal)) goal = OutsideBed(goal, blocked);
        if (!CrossesBed(start, goal, blocked)) return goal - centerOffset;

        const float cornerMargin = 0.08f;
        routeNodes[0] = start;
        routeNodes[1] = goal;
        routeNodes[2] = new Vector2(min.x - cornerMargin, min.y - cornerMargin);
        routeNodes[3] = new Vector2(min.x - cornerMargin, max.y + cornerMargin);
        routeNodes[4] = new Vector2(max.x + cornerMargin, max.y + cornerMargin);
        routeNodes[5] = new Vector2(max.x + cornerMargin, min.y - cornerMargin);
        for (int i = 0; i < 6; i++)
        {
            routeCosts[i] = float.PositiveInfinity;
            routeParents[i] = -1;
            routeVisited[i] = false;
        }
        routeCosts[0] = 0f;
        for (int step = 0; step < 6; step++)
        {
            int current = -1;
            for (int i = 0; i < 6; i++)
                if (!routeVisited[i] && (current < 0 || routeCosts[i] < routeCosts[current])) current = i;
            if (current < 0 || float.IsPositiveInfinity(routeCosts[current])) break;
            if (current == 1) break;
            routeVisited[current] = true;
            for (int next = 1; next < 6; next++)
            {
                if (routeVisited[next] || CrossesBed(routeNodes[current], routeNodes[next], blocked)) continue;
                float cost = routeCosts[current] + Vector2.Distance(routeNodes[current], routeNodes[next]);
                if (cost >= routeCosts[next]) continue;
                routeCosts[next] = cost;
                routeParents[next] = current;
            }
        }
        if (routeParents[1] < 0) return body.position;
        int waypoint = 1;
        while (routeParents[waypoint] > 0) waypoint = routeParents[waypoint];
        return routeNodes[waypoint] - centerOffset;
    }

    private static Vector2 OutsideBed(Vector2 point, Rect bed)
    {
        float distance = point.x - bed.xMin;
        Vector2 result = new Vector2(bed.xMin - 0.08f, point.y);
        if (bed.xMax - point.x < distance)
        {
            distance = bed.xMax - point.x;
            result = new Vector2(bed.xMax + 0.08f, point.y);
        }
        if (point.y - bed.yMin < distance)
        {
            distance = point.y - bed.yMin;
            result = new Vector2(point.x, bed.yMin - 0.08f);
        }
        if (bed.yMax - point.y < distance) result = new Vector2(point.x, bed.yMax + 0.08f);
        return result;
    }

    private static bool CrossesBed(Vector2 from, Vector2 to, Rect bed)
    {
        Vector2 delta = to - from;
        float enter = 0f, exit = 1f;
        for (int axis = 0; axis < 2; axis++)
        {
            float min = axis == 0 ? bed.xMin : bed.yMin;
            float max = axis == 0 ? bed.xMax : bed.yMax;
            if (Mathf.Abs(delta[axis]) < 0.00001f)
            {
                if (from[axis] < min || from[axis] > max) return false;
                continue;
            }
            float a = (min - from[axis]) / delta[axis];
            float b = (max - from[axis]) / delta[axis];
            enter = Mathf.Max(enter, Mathf.Min(a, b));
            exit = Mathf.Min(exit, Mathf.Max(a, b));
            if (enter > exit) return false;
        }
        return true;
    }

    private void LateUpdate()
    {
        if (IsAttacking)
        {
            attackElapsed += Time.deltaTime;
            if (attackElapsed < attackDuration)
            {
                int count = attackFrames == null ? 0 : attackFrames.Length;
                ShowFrame(attackFrames, Mathf.Min(count - 1,
                    Mathf.FloorToInt(attackElapsed / attackDuration * count)));
                return;
            }
            IsAttacking = false;
        }
        FaceTarget();
        Sprite[] frames = moving ? walkFrames : idleFrames;
        if (frames == null || frames.Length == 0) return;
        float rate = Mathf.Max(1f, animationFramesPerSecond);
        // Keep the cycle advancing even when contact briefly changes the movement state.
        animationElapsed = (animationElapsed + Time.deltaTime) % (frames.Length / rate);
        ShowFrame(frames, Mathf.FloorToInt(animationElapsed * rate));
    }

    private void FaceTarget()
    {
        if (spriteRenderer == null || target == null) return;
        float horizontal = target.position.x - transform.position.x;
        if (Mathf.Abs(horizontal) > 0.05f) spriteRenderer.flipX = horizontal < 0f;
    }

    private void ShowFrame(Sprite[] frames, int index)
    {
        if (spriteRenderer == null || frames == null || frames.Length == 0) return;
        Sprite frame = frames[Mathf.Clamp(index, 0, frames.Length - 1)];
        if (frame != null) spriteRenderer.sprite = frame;
    }

    private void OnDisable()
    {
        dashRemaining = 0f;
        IsAttacking = false;
        animationElapsed = 0f;
        moving = false;
        if (body != null) body.linearVelocity = Vector2.zero;
    }
}
