using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class FirstStageObjective : MonoBehaviour
{
    private MatchStats match;
    private BoxCollider2D bedCollider;
    public Collider2D BedObstacle => bedCollider;
    private StageVisuals visuals;
    private Transform fragmentArrow;
    private LineRenderer fragmentArrowGold;
    private float fragmentArrowOrbit;
    private readonly List<StagePickup> fragments = new List<StagePickup>();
    private readonly List<Vector2> fragmentPositions = new List<Vector2>();
    private Transform ritual;
    private Transform exit;
    private LineRenderer ritualCircle;
    private LineRenderer exitOutline;
    public int CollectedFragments { get; private set; }
    public int RequiredFragments { get; private set; }
    public bool Assembled { get; private set; }
    public bool RingCollected { get; private set; }
    public bool LetterCollected { get; private set; }
    public Vector3 RitualPosition => ritual.position;
    public Vector3 ExitPosition => exit.position;
    public IReadOnlyList<Vector2> FragmentPositions => fragmentPositions;
    public IReadOnlyList<StagePickup> Fragments => fragments;

    public void Initialize(MatchStats stats)
    {
        match = stats;
        visuals = gameObject.AddComponent<StageVisuals>();
        visuals.Initialize(match.Player.GetComponent<SpriteRenderer>());
        Rect bounds = HouseFloor.Active != null ? HouseFloor.Active.Bounds : new Rect(-50, -50, 100, 100);
        BuildBedroom(bounds);
        Physics2D.SyncTransforms();
        Collider2D playerCollider = match.Player.GetComponent<Collider2D>();
        float playerHalfWidth = playerCollider != null ? playerCollider.bounds.extents.x : 0.25f;
        Vector2 spawn = BedroomBedPosition(bounds) + Vector2.right * 1.5f;
        if (bedCollider != null)
            spawn = new Vector2(bedCollider.bounds.max.x + playerHalfWidth + 0.25f, bedCollider.bounds.center.y);
        spawn = Clamp(spawn, bounds);
        Transform player = match.Player.transform;
        player.position = new Vector3(spawn.x, spawn.y, player.position.z);
        Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>();
        if (playerBody != null)
        {
            playerBody.position = spawn;
            playerBody.linearVelocity = Vector2.zero;
        }
        CameraFollow cameraFollow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        if (cameraFollow != null) cameraFollow.SnapToPlayer();
        ritual = new GameObject("Selo dos fragmentos").transform;
        ritual.SetParent(transform, false);
        ritual.position = Clamp((Vector2)match.Player.transform.position + Vector2.down * 4f, bounds);
        ritualCircle = visuals.Circle(ritual, "Selo", 1.4f, new Color(0.6f, 0.4f, 0.8f));
        visuals.Outline(ritual, "Runas", new[] { new Vector3(-1, -0.7f), new Vector3(0, 1.1f), new Vector3(1, -0.7f) }, Color.magenta);
        exit = new GameObject("Saída do quarto").transform;
        exit.SetParent(transform, false);
        exit.gameObject.SetActive(false);
        exitOutline = visuals.Outline(exit, "Porta", new[] {
            new Vector3(-1, -1.4f), new Vector3(-1, 1.4f), new Vector3(1, 1.4f), new Vector3(1, -1.4f)
        }, new Color(0.5f, 0.3f, 0.3f), 0.2f);
        RequiredFragments = Mathf.Clamp(match.Rules.fragmentCount, 2, 12);
        float separation = FragmentSeparation;
        for (int i = 0; i < RequiredFragments; i++)
        {
            bool placed = false;
            for (int attempt = 0; attempt < 1024; attempt++)
            {
                Vector2 point = new Vector2(Random.Range(bounds.xMin + 4f, bounds.xMax - 4f),
                    Random.Range(bounds.yMin + 4f, bounds.yMax - 4f));
                if (!IsFragmentPositionClear(point)) continue;
                bool clear = true;
                foreach (Vector2 other in fragmentPositions)
                    if (Vector2.Distance(point, other) < separation) { clear = false; break; }
                if (!clear) continue;
                AddFragment(point);
                placed = true;
                break;
            }
            if (!placed)
            {
                Debug.LogError("Não há espaço para os fragmentos: revise quantidade e distância no MatchStats.", this);
                RequiredFragments = fragments.Count;
                break;
            }
        }

    }

    private float FragmentSeparation => Mathf.Max(5f, match.Rules.minimumFragmentDistance,
        match.Rules.minimumFragmentPixels / Mathf.Max(1f, match.Rules.referencePixelsPerUnit));

    private bool IsFragmentPositionClear(Vector2 point)
    {
        return !IsFurnitureBlocked(point, 0.85f)
            && Vector2.Distance(point, ritual.position) >= 4f
            && Vector2.Distance(point, match.Player.transform.position) >= Mathf.Max(8f, match.Rules.pickupRadius + 2f);
    }

    public void RelocateRemainingFragments()
    {
        if (match == null || match.Finished || match.IsPaused || Assembled) return;
        var remaining = new List<StagePickup>();
        foreach (StagePickup fragment in fragments)
            if (fragment != null && fragment.gameObject.activeInHierarchy) remaining.Add(fragment);
        if (remaining.Count == 0) return;
        Rect bounds = HouseFloor.Active != null ? HouseFloor.Active.Bounds : new Rect(-50, -50, 100, 100);
        var positions = new List<Vector2>(remaining.Count);
        float separation = FragmentSeparation;
        float relocation = Mathf.Max(1f, match.Rules.fragmentRelocationDistance);
        // Plan a complete layout before moving anything; never partially relocate a group.
        for (int layout = 0; layout < 32; layout++)
        {
            positions.Clear();
            foreach (StagePickup fragment in remaining)
            {
                bool placed = false;
                for (int attempt = 0; attempt < 1024; attempt++)
                {
                    Vector2 point = new Vector2(Random.Range(bounds.xMin + 4f, bounds.xMax - 4f),
                        Random.Range(bounds.yMin + 4f, bounds.yMax - 4f));
                    if (!IsFragmentPositionClear(point)
                        || Vector2.Distance(point, fragment.transform.position) < relocation) continue;
                    bool clear = true;
                    foreach (Vector2 other in positions)
                        if (Vector2.Distance(point, other) < separation) { clear = false; break; }
                    if (!clear) continue;
                    positions.Add(point);
                    placed = true;
                    break;
                }
                if (!placed) break;
            }
            if (positions.Count != remaining.Count) continue;
            for (int i = 0; i < remaining.Count; i++)
            {
                Vector3 old = remaining[i].transform.position;
                remaining[i].transform.position = new Vector3(positions[i].x, positions[i].y, old.z);
            }
            fragmentPositions.Clear();
            fragmentPositions.AddRange(positions);
            return;
        }
        Debug.LogWarning("Fragment relocation has no valid layout; keeping existing positions. Check spacing and room size.", this);
    }

    private static Vector2 Clamp(Vector2 point, Rect bounds) => new Vector2(
        Mathf.Clamp(point.x, bounds.xMin + 3f, bounds.xMax - 3f),
        Mathf.Clamp(point.y, bounds.yMin + 3f, bounds.yMax - 3f));

    private void AddFragment(Vector2 position)
    {
        fragments.Add(CreatePickup(StagePickupKind.Fragment, position));
        fragmentPositions.Add(position);
    }

    private StagePickup CreatePickup(StagePickupKind kind, Vector2 position)
    {
        var obj = new GameObject(kind.ToString());
        obj.transform.SetParent(transform, false);
        obj.transform.position = position;
        if (kind == StagePickupKind.Ring)
        {
            visuals.Circle(obj.transform, "Anel dos mortos", 0.5f, new Color(1f, 0.82f, 0.25f));
            visuals.Block(obj.transform, "Pedra", new Vector2(0, 0.5f), Vector2.one * 0.22f, Color.magenta, 21);
        }
        else if (kind == StagePickupKind.Letter)
        {
            visuals.Block(obj.transform, "Carta da mãe", Vector2.zero, new Vector2(0.8f, 1f), new Color(1f, 0.88f, 0.65f), 20);
            visuals.Block(obj.transform, "Selo da carta", Vector2.zero, Vector2.one * 0.25f, new Color(0.7f, 0.1f, 0.2f), 21);
        }
        else
        {
            visuals.Outline(obj.transform, "Fragmento amaldiçoado", new[] {
                new Vector3(0, 0.7f), new Vector3(0.45f, 0), new Vector3(0, -0.7f), new Vector3(-0.45f, 0)
            }, new Color(0.85f, 0.5f, 1f), 0.13f);
            visuals.Block(obj.transform, "Núcleo", Vector2.zero, Vector2.one * 0.18f, Color.white, 21);
        }
        var pickup = obj.AddComponent<StagePickup>();
        pickup.Initialize(this, match, kind);
        return pickup;
    }

    public void Collect(StagePickup pickup)
    {
        if (pickup.Kind == StagePickupKind.Fragment)
        {
            CollectedFragments++;
            TryAssemble();
        }
        else if (pickup.Kind == StagePickupKind.Ring)
        {
            RingCollected = true;
            match.Player.GetComponent<PlayerRing>().Unlock();
        }
        else LetterCollected = true;
        if (RingCollected && LetterCollected)
            exitOutline.startColor = exitOutline.endColor = new Color(0.4f, 1f, 0.7f);
        match.NotifyChanged();
    }

    public bool TryAssemble()
    {
        if (match.Finished || match.IsPaused || Assembled || RequiredFragments <= 0
            || CollectedFragments < RequiredFragments) return false;
        Assembled = true;
        ritualCircle.startColor = ritualCircle.endColor = Color.red;
        match.PrepareBossEncounter();
        match.NotifyChanged();
        return true;
    }

    public void DropRewards(Vector3 position)
    {
        // MatchStats captures the boss position before removing enemies.
        // The exit uses that exact point, without the pickups' boundary clamping.
        exit.position = position;
        exit.gameObject.SetActive(true);
        Rect bounds = HouseFloor.Active != null ? HouseFloor.Active.Bounds : new Rect(-50, -50, 100, 100);
        CreatePickup(StagePickupKind.Ring, Clamp((Vector2)position + Vector2.left, bounds));
        CreatePickup(StagePickupKind.Letter, Clamp((Vector2)position + Vector2.right, bounds));
    }

    public string Instruction
    {
        get
        {
            if (match.Victory) return "Quarto concluído. Sua mãe está na carta.";
            if (match.GameOver) return "Winston caiu. Tente novamente.";
            if (match.BossDefeated)
                return !RingCollected || !LetterCollected ? "Recolha o Anel dos Mortos e a carta da mãe." : "Vá até a porta. Pressione E para sair.";
            if (match.BossSpawned) return "Derrote Gunter. Afaste-se do círculo vermelho!";
            return CollectedFragments < RequiredFragments
                ? $"Encontre os fragmentos amaldiçoados: {CollectedFragments}/{RequiredFragments}"
                : "Fragmentos completos. Gunter está chegando!";
        }
    }

    private void CreateFragmentArrow()
    {
        SpriteRenderer playerVisual = match.Player.GetComponent<SpriteRenderer>();
        fragmentArrow = new GameObject("Guia do fragmento mais proximo").transform;
        fragmentArrow.SetParent(transform, false);
        // Independent of the player's facing direction and animation scale.
        fragmentArrowOrbit = playerVisual != null
            ? Mathf.Max(1.2f, Mathf.Max(playerVisual.bounds.extents.x, playerVisual.bounds.extents.y) + 0.8f)
            : 1.5f;
        Vector3[] shape = {
            new Vector3(-0.45f, -0.1f), new Vector3(0f, -0.1f),
            new Vector3(0f, -0.3f), new Vector3(0.45f, 0f),
            new Vector3(0f, 0.3f), new Vector3(0f, 0.1f),
            new Vector3(-0.45f, 0.1f)
        };
        LineRenderer outline = visuals.Outline(fragmentArrow, "Contorno", shape, Color.black, 0.20f);
        fragmentArrowGold = visuals.Outline(fragmentArrow, "Seta dourada", shape, new Color(1f, 0.8f, 0.1f), 0.11f);
        outline.sortingLayerID = fragmentArrowGold.sortingLayerID = playerVisual != null ? playerVisual.sortingLayerID : 0;
        outline.sortingOrder = playerVisual != null ? Mathf.Max(100, playerVisual.sortingOrder + 10) : 100;
        fragmentArrowGold.sortingOrder = outline.sortingOrder + 1;
    }

    private void LateUpdate()
    {
        StagePickup nearest = null;
        float nearestDistance = float.PositiveInfinity;
        if (match != null && match.Player != null && !match.Finished && !Assembled && !match.IsBlackout)
        {
            foreach (StagePickup fragment in fragments)
            {
                if (fragment == null || !fragment.gameObject.activeInHierarchy) continue;
                float distance = ((Vector2)fragment.transform.position - (Vector2)match.Player.transform.position).sqrMagnitude;
                if (distance >= nearestDistance) continue;
                nearestDistance = distance;
                nearest = fragment;
            }
        }
        if (nearest == null || nearestDistance < 0.0001f)
        {
            if (fragmentArrow != null) fragmentArrow.gameObject.SetActive(false);
            return;
        }
        if (fragmentArrow == null) CreateFragmentArrow();
        fragmentArrow.gameObject.SetActive(true);
        Vector2 direction = ((Vector2)nearest.transform.position - (Vector2)match.Player.transform.position).normalized;
        fragmentArrow.position = match.Player.transform.position + (Vector3)(direction * fragmentArrowOrbit);
        fragmentArrow.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 3f);
        fragmentArrowGold.startColor = fragmentArrowGold.endColor =
            Color.Lerp(new Color(1f, 0.7f, 0.08f), new Color(1f, 0.95f, 0.45f), pulse);
    }

    private void OnDisable()
    {
        if (fragmentArrow != null) fragmentArrow.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (match == null || match.Finished || match.IsPaused || Keyboard.current == null) return;
        if (!Keyboard.current.eKey.wasPressedThisFrame) return;
        if (!Assembled) TryAssemble();
        if (match.BossDefeated && Vector2.Distance(match.Player.transform.position, exit.position) < 2.5f)
            match.CompleteStage();
    }

    private static Vector2 BedroomBedPosition(Rect bounds) => new Vector2(bounds.xMin + 7f, bounds.yMax - 7f);

    public bool IsFurnitureBlocked(Vector2 point, float margin)
    {
        if (bedCollider == null) return false;
        Bounds occupied = bedCollider.bounds;
        occupied.Expand(new Vector3(margin * 2f, margin * 2f, 0f));
        return point.x >= occupied.min.x && point.x <= occupied.max.x
            && point.y >= occupied.min.y && point.y <= occupied.max.y;
    }

    private void BuildBedroom(Rect bounds)
    {
        // Only the bed is solid; fragments and spawns keep clear of it.
        Transform room = new GameObject("Mobília do quarto").transform;
        room.SetParent(transform, false);
        room.position = Vector3.zero;
        Vector2 corner = BedroomBedPosition(bounds);
        visuals.Block(room, "Tapete", corner + Vector2.down * 2f, new Vector2(9, 11), new Color(0.22f, 0.12f, 0.26f), -90);
        Sprite[] bedSprites = Resources.LoadAll<Sprite>("Art/BedroomFurniture");
        if (bedSprites.Length > 0)
        {
            Sprite bedSprite = bedSprites[0];
            SpriteRenderer bed = visuals.Block(room, "Cama", corner, Vector2.one, Color.white);
            bed.sprite = bedSprite;
            PlayerSpriteAnimation playerAnimation = match.Player.GetComponent<PlayerSpriteAnimation>();
            float playerHeight = playerAnimation != null ? playerAnimation.StandingHeight : 1.48f;
            bed.transform.localScale = Vector3.one * (playerHeight * 1.35f / bedSprite.bounds.size.y);
            bedCollider = bed.gameObject.AddComponent<BoxCollider2D>();
            bedCollider.size = bedSprite.bounds.size * 0.9f;
            bedCollider.offset = bedSprite.bounds.center;
            Rigidbody2D bedBody = bed.gameObject.AddComponent<Rigidbody2D>();
            bedBody.bodyType = RigidbodyType2D.Static;
        }
        else
        {
            Debug.LogError("BedroomFurniture bed sprite is missing.", this);
        }
        visuals.Block(room, "Escrivaninha", new Vector2(bounds.xMax - 7, bounds.yMax - 5), new Vector2(7, 3), new Color(0.28f, 0.16f, 0.1f));
        visuals.Block(room, "Livro", new Vector2(bounds.xMax - 7, bounds.yMax - 5), new Vector2(1, 1.4f), new Color(0.48f, 0.18f, 0.2f), -49);
        visuals.Block(room, "Guarda-roupa", new Vector2(bounds.xMax - 4, bounds.yMin + 8), new Vector2(3, 9), new Color(0.2f, 0.12f, 0.1f));
    }
}

