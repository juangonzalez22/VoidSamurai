using UnityEngine;

public class AttackHitbox : MonoBehaviour
{
    private FighterCombat ownerCombat;
    private Collider2D hitboxCollider;

    private bool hasHit;

    void Start()
    {
        ownerCombat = GetComponentInParent<FighterCombat>();
        hitboxCollider = GetComponent<Collider2D>();
        hitboxCollider.enabled = false;
    }

    void Update()
    {
        UpdateHitboxState();
    }

    void UpdateHitboxState()
    {
        bool attacking = ownerCombat != null &&
                         ownerCombat.IsAttacking &&
                         !ownerCombat.IsDead &&
                         !ownerCombat.MatchEnded;

        if (attacking)
        {
            hitboxCollider.enabled = true;
        }
        else
        {
            hitboxCollider.enabled = false;
            hasHit = false;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit || ownerCombat == null || ownerCombat.IsDead || ownerCombat.MatchEnded)
            return;

        FighterCombat enemyCombat = other.GetComponentInParent<FighterCombat>();

        if (enemyCombat == null)
            return;

        if (enemyCombat == ownerCombat || enemyCombat.IsDead || enemyCombat.MatchEnded)
            return;

        bool ownerAttacking = ownerCombat.IsAttacking;
        bool enemyAttacking = enemyCombat.IsAttacking;

        if (ownerAttacking && enemyAttacking)
        {
            hasHit = true;
            enemyCombat.StartClash(ownerCombat.transform);
            ownerCombat.StartClash(enemyCombat.transform);
            return;
        }

        hasHit = true;

        if (ownerCombat.currentState == FighterCombat.FighterState.AttackHigh)
        {
            bool blocked = enemyCombat.currentState == FighterCombat.FighterState.BlockHigh;
            enemyCombat.ReceiveHit(blocked, ownerCombat.transform);
        }
        else if (ownerCombat.currentState == FighterCombat.FighterState.AttackLow)
        {
            bool blocked = enemyCombat.currentState == FighterCombat.FighterState.BlockLow;
            enemyCombat.ReceiveHit(blocked, ownerCombat.transform);
        }
    }
}