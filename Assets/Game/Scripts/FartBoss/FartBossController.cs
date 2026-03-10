using UnityEngine;
using UnityEngine.AI;
using XtremeFPS.Interfaces;

public class FartBossController : MonoBehaviour, IShootableObject
{
    [Header("Stats")]
    public float health = 300f; // Give the boss more health!
    public bool isDead = false;

    private NavMeshAgent agent;
    private Animator animator;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        // Disable automatic position updates so the Animator's Root Motion controls movement
        if (agent != null) agent.updatePosition = false;
    }

    void OnAnimatorMove()
    {
        // Sync the NavMeshAgent position with the physical animation movement
        if (animator != null && agent != null && agent.isActiveAndEnabled && !isDead)
        {
            transform.position = animator.rootPosition;
            agent.nextPosition = transform.position;
        }
    }

    // This is the bridge: The bullet script calls this specific method
    public void OnHit(RaycastHit hit, float damage)
    {
        if (isDead) return;

        Debug.Log($"Fart Boss hit at {hit.point} for {damage} damage!");
        TakeDamage(damage);
    }

    public void TakeDamage(float amount)
    {
        health -= amount;

        if (health <= 0 && !isDead)
        {
            Die();
        }
    }

    private void Die()
    {
        // Optional: Remove the collider so bullets pass through the corpse
        if (TryGetComponent<Rigidbody>(out Rigidbody rig)) rig.useGravity = false;
        if (TryGetComponent<Collider>(out Collider col)) col.enabled = false;
        
        isDead = true;
        Debug.Log("Fart Boss Died!");

        // Disable AI navigation so it stops moving
        if (agent != null) agent.enabled = false;
        
        // IMPORTANT: Change layer so bullets ignore the corpse
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

        // Tell the GameManager that a zombie has died (this lowers the zombie count!)
        if (GameManager.Instance != null) GameManager.Instance.ZombieDied();
    }
}