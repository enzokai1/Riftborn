using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class FirstStageObjective : MonoBehaviour
{
    private MatchStats match;
    private BoxCollider2D bedCollider;
    private BoxCollider2D wardrobeCollider;
    private Collider2D playerFootprint;
    public Collider2D BedObstacle => bedCollider;
    public Collider2D WardrobeObstacle => wardrobeCollider;
    private StageVisuals visuals;
    private Transform fragmentArrow;
    private LineRenderer fragmentArrowGold;
    private float fragmentArrowOrbit;
    private readonly List<StagePickup> fragments = new List<StagePickup>();
    private readonly List<Vector2> fragmentPositions = new List<Vector2>();
    private Transform ritual;
    private Transform exit;
    public int CollectedFragments { get; private set; }
    public int RequiredFragments { get; private set; }
    public bool Assembled { get; private set; }
    public bool RingCollected { get; private set; }
    public bool LetterCollected { get; private set; }
    public Vector3 RitualPosition => ritual.position;
    public Vector3 ExitPosition => exit.position;
    public IReadOnlyList<Vector2> FragmentPositions => fragmentPositions;
    public IReadOnlyList<StagePickup> Fragments => fragments;

    // Matches the visible doorway using the center of the player's feet collider.
    private static readonly Vector2 ExitHalfSize = new Vector2(1f, 1.4f);
    public bool CanExit
    {
        get
        {
            if (match == null || match.Player == null || match.Finished || match.IsPaused
                || !match.BossDefeated || !RingCollected || !LetterCollected
                || exit == null || !exit.gameObject.activeInHierarchy) return false;
            Vector3 feet = playerFootprint != null ? playerFootprint.bounds.center : match.Player.transform.position;
            Vector3 local = exit.InverseTransformPoint(feet);
            return Mathf.Abs(local.x) <= ExitHalfSize.x && Mathf.Abs(local.y) <= ExitHalfSize.y;
        }
    }

    public void Initialize(MatchStats stats)
    {
        match = stats;
        visuals = gameObject.AddComponent<StageVisuals>();
        visuals.Initialize(match.Player.GetComponent<SpriteRenderer>());
        Rect bounds = HouseFloor.Active != null ? HouseFloor.Active.Bounds : new Rect(-50, -50, 100, 100);
        BuildBedroom(bounds);
        Physics2D.SyncTransforms();
        Collider2D playerCollider = match.Player.GetComponent<Collider2D>();
        playerFootprint = playerCollider;
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
        exit = new GameObject("Saída do quarto").transform;
        exit.SetParent(transform, false);
        exit.gameObject.SetActive(false);
        Sprite doorSprite = Resources.Load<Sprite>("Art/DreadRoom/exit-door");
        if (doorSprite != null)
        {
            SpriteRenderer door = visuals.Block(exit, "Porta envelhecida", Vector2.zero, Vector2.one, Color.white, 19);
            door.sprite = doorSprite;
            door.transform.localScale = new Vector3(ExitHalfSize.x * 2f / doorSprite.bounds.size.x,
                ExitHalfSize.y * 2f / doorSprite.bounds.size.y, 1f);
        }
        else Debug.LogError("Missing Dread Rooms exit door sprite.", this);
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
            obj.AddComponent<FragmentVisual>().Initialize(visuals, match);
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
        match.NotifyChanged();
    }

#if UNITY_EDITOR
    public void EditorCompleteFragments()
    {
        if (match == null || match.Finished || match.IsPaused) return;
        foreach (StagePickup fragment in fragments)
            if (fragment != null) { fragment.gameObject.SetActive(false); Destroy(fragment.gameObject); }
        CollectedFragments = RequiredFragments;
        TryAssemble();
        match.NotifyChanged();
    }

    public void EditorUnlockExit()
    {
        if (match == null || match.Finished || match.IsPaused) return;
        if (!match.BossDefeated) match.RegisterKill(true);
        foreach (StagePickup pickup in GetComponentsInChildren<StagePickup>())
        {
            if (pickup.Kind == StagePickupKind.Fragment) continue;
            Collect(pickup);
            pickup.gameObject.SetActive(false);
            Destroy(pickup.gameObject);
        }
    }
#endif
    public bool TryAssemble()
    {
        if (match.Finished || match.IsPaused || Assembled || RequiredFragments <= 0
            || CollectedFragments < RequiredFragments) return false;
        Assembled = true;
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
                return !RingCollected || !LetterCollected ? "Recolha o Anel dos Mortos e a carta da mãe." : "A saída está liberada. Vá até a porta.";
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
        // Reduce both geometry and stroke: LineRenderer width is measured in world units.
        const float arrowSize = 0.7f;
        for (int i = 0; i < shape.Length; i++) shape[i] *= arrowSize;
        LineRenderer outline = visuals.Outline(fragmentArrow, "Contorno", shape, new Color(0.08f, 0.05f, 0.02f), 0.20f * arrowSize);
        fragmentArrowGold = visuals.Outline(fragmentArrow, "Seta dourada", shape, new Color(1f, 0.8f, 0.1f), 0.11f * arrowSize);
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
        if (CanExit)
            match.CompleteStage();
    }

    private static Vector2 BedroomBedPosition(Rect bounds) => new Vector2(bounds.xMin + 7f, bounds.yMax - 7f);

    public bool IsFurnitureBlocked(Vector2 point, float margin)
    {
        return OverlapsFurniture(bedCollider, point, margin) || OverlapsFurniture(wardrobeCollider, point, margin);
    }

    private static bool OverlapsFurniture(Collider2D furniture, Vector2 point, float margin)
    {
        if (furniture == null) return false;
        Bounds occupied = furniture.bounds;
        occupied.Expand(new Vector3(margin * 2f, margin * 2f, 0f));
        return point.x >= occupied.min.x && point.x <= occupied.max.x
            && point.y >= occupied.min.y && point.y <= occupied.max.y;
    }

    private void BuildBedroom(Rect bounds)
    {
        // Solid furniture is also excluded from fragment placement and enemy spawns.
        Transform room = new GameObject("Mobília do quarto").transform;
        room.SetParent(transform, false);
        room.position = Vector3.zero;
        Vector2 corner = BedroomBedPosition(bounds);
        visuals.Block(room, "Tapete", corner + Vector2.down * 2f, new Vector2(9, 11), new Color(0.22f, 0.12f, 0.26f), -90);
        PlayerSpriteAnimation playerAnimation = match.Player.GetComponent<PlayerSpriteAnimation>();
        float playerHeight = playerAnimation != null ? playerAnimation.StandingHeight : 1.48f;
        bedCollider = CreateBedroomFurniture(room, "Cama", "bed-decayed", corner,
            playerHeight * 1.35f, new Vector2(20f, 30f), new Vector2(0f, -1f));
        wardrobeCollider = CreateBedroomFurniture(room, "Guarda-roupa", "wardrobe-decayed",
            corner + new Vector2(-3f, 1f), playerHeight * 1.65f,
            new Vector2(23f, 29f), new Vector2(-0.5f, -1.5f));
        visuals.Block(room, "Escrivaninha", new Vector2(bounds.xMax - 7, bounds.yMax - 5), new Vector2(7, 3), new Color(0.28f, 0.16f, 0.1f));
        visuals.Block(room, "Livro", new Vector2(bounds.xMax - 7, bounds.yMax - 5), new Vector2(1, 1.4f), new Color(0.48f, 0.18f, 0.2f), -49);
        BuildCandelabras(room, bounds, corner);
        BuildClock(room, bounds);
    }

    private void BuildClock(Transform room, Rect bounds)
    {
        Sprite sprite = Resources.Load<Sprite>("Art/DreadRoom/grandfather-clock");
        if (sprite == null)
        {
            Debug.LogError("Missing Dread Rooms grandfather clock sprite.", this);
            return;
        }
        SpriteRenderer clock = visuals.Block(room, "Relogio antigo",
            new Vector2(bounds.xMin + 14f, bounds.yMax - 3.5f), Vector2.one, Color.white, -45);
        clock.sprite = sprite;
        clock.transform.localScale = Vector3.one * (2.6f / sprite.bounds.size.y);
        clock.gameObject.AddComponent<HauntedClock>().Initialize(match);
    }
    private void BuildCandelabras(Transform room, Rect bounds, Vector2 bedPosition)
    {
        Sprite sprite = Resources.Load<Sprite>("Art/DreadRoom/candelabra");
        if (sprite == null)
        {
            Debug.LogError("Missing Dread Rooms candelabra sprite.", this);
            return;
        }
        Vector2[] positions = {
            bedPosition + new Vector2(2.5f, 2.5f),
            new Vector2(bounds.xMax - 4f, bounds.yMax - 4f),
            new Vector2(bounds.xMin + 4f, bounds.center.y),
            new Vector2(bounds.xMax - 4f, bounds.center.y),
            new Vector2(bounds.xMin + 4f, bounds.yMin + 4f),
            new Vector2(bounds.xMax - 4f, bounds.yMin + 4f)
        };
        foreach (Vector2 position in positions)
        {
            // Decorative fixtures near the edges leave combat paths unobstructed.
            var fixture = new GameObject("Candelabro");
            fixture.transform.SetParent(room, false);
            fixture.transform.localPosition = position;
            SpriteRenderer renderer = visuals.Block(fixture.transform, "Candelabro sprite",
                Vector2.zero, Vector2.one * 0.75f, Color.white, -45);
            renderer.sprite = sprite;
            fixture.AddComponent<CandelabraLight>().Initialize(match);
        }
    }
    private BoxCollider2D CreateBedroomFurniture(Transform room, string label, string asset,
        Vector2 position, float height, Vector2 opaquePixelSize, Vector2 opaquePixelCenter)
    {
        Sprite sprite = Resources.Load<Sprite>("Art/DreadRoom/" + asset);
        if (sprite == null)
        {
            Debug.LogError("Missing Dread Rooms furniture: " + asset, this);
            return null;
        }
        SpriteRenderer renderer = visuals.Block(room, label, position, Vector2.one, Color.white);
        renderer.sprite = sprite;
        // Size and collision use the artwork, excluding the transparent 32x32 canvas padding.
        float ppu = sprite.pixelsPerUnit;
        renderer.transform.localScale = Vector3.one * (height * ppu / opaquePixelSize.y);
        BoxCollider2D obstacle = renderer.gameObject.AddComponent<BoxCollider2D>();
        obstacle.size = opaquePixelSize / ppu * 0.9f;
        obstacle.offset = opaquePixelCenter / ppu;
        Rigidbody2D body = renderer.gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Static;
        return obstacle;
    }
}

