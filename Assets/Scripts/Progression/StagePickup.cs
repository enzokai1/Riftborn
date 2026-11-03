using UnityEngine;

public enum StagePickupKind { Fragment, Ring, Letter }

public class StagePickup : MonoBehaviour
{
    public StagePickupKind Kind { get; private set; }
    private FirstStageObjective objective;
    private MatchStats match;
    private bool collected;
    public void Initialize(FirstStageObjective owner, MatchStats stats, StagePickupKind kind)
    { objective = owner; match = stats; Kind = kind; }

    private void Update()
    {
        if (match == null || match.Finished || match.IsPaused || match.Player == null) return;
        if (Vector2.Distance(transform.position, match.Player.transform.position) <= match.Rules.pickupRadius)
            TryCollect();
    }

    public bool TryCollect()
    {
        if (collected || match == null || match.Finished || match.IsPaused
            || match.Player == null || match.Player.CurrentHealth <= 0
            || Vector2.Distance(transform.position, match.Player.transform.position) > match.Rules.pickupRadius)
            return false;
        collected = true;
        objective.Collect(this);
        gameObject.SetActive(false);
        Destroy(gameObject);
        return true;
    }
}

