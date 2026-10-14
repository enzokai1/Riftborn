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
        exit.position = new Vector3(bounds.center.x, bounds.yMax - 3f);
        exitOutline = visuals.Outline(exit, "Porta", new[] {
            new Vector3(-1, -1.4f), new Vector3(-1, 1.4f), new Vector3(1, 1.4f), new Vector3(1, -1.4f)
        }, new Color(0.5f, 0.3f, 0.3f), 0.2f);
        RequiredFragments = Mathf.Clamp(match.Rules.fragmentCount, 2, 12);
        float separation = match.Rules.minimumFragmentPixels / Mathf.Max(1f, match.Rules.referencePixelsPerUnit);
        Vector2 first = Clamp((Vector2)match.Player.transform.position + new Vector2(2.5f, 1f), bounds);
        AddFragment(first);
        for (int i = 1; i < RequiredFragments; i++)
        {
            bool placed = false;
            for (int attempt = 0; attempt < 1024; attempt++)
            {
                Vector2 point = new Vector2(Random.Range(bounds.xMin + 4f, bounds.xMax - 4f),
                    Random.Range(bounds.yMin + 4f, bounds.yMax - 4f));
                if (Vector2.Distance(point, ritual.position) < 4f || IsFurnitureBlocked(point, 0.85f)) continue;
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
        if (pickup.Kind == StagePickupKind.Fragment) CollectedFragments++;
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
        if (match.Finished || match.IsPaused || Assembled || CollectedFragments < RequiredFragments
            || Vector2.Distance(match.Player.transform.position, ritual.position) > 2.5f) return false;
        Assembled = true;
        ritualCircle.startColor = ritualCircle.endColor = Color.red;
        match.NotifyChanged();
        return true;
    }

    public void DropRewards(Vector3 position)
    {
        Rect bounds = HouseFloor.Active != null ? HouseFloor.Active.Bounds : new Rect(-50, -50, 100, 100);
        CreatePickup(StagePickupKind.Ring, Clamp((Vector2)position + Vector2.left, bounds));
        CreatePickup(StagePickupKind.Letter, Clamp((Vector2)position + Vector2.right, bounds));
    }

    public Vector3 GuidanceTarget
    {
        get
        {
            if (match.BossDefeated)
            {
                foreach (StagePickup pickup in GetComponentsInChildren<StagePickup>())
                    if (pickup.Kind != StagePickupKind.Fragment) return pickup.transform.position;
                return ExitPosition;
            }
            StagePickup nearest = null;
            float best = float.MaxValue;
            foreach (StagePickup fragment in fragments)
            {
                if (fragment == null || !fragment.gameObject.activeSelf) continue;
                float distance = (fragment.transform.position - match.Player.transform.position).sqrMagnitude;
                if (distance < best) { best = distance; nearest = fragment; }
            }
            return nearest != null ? nearest.transform.position : RitualPosition;
        }
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
                : "Volte ao selo e pressione E para unir os fragmentos.";
        }
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

