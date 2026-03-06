using UnityEngine;
using UnityEngine.AI;
using XtremeFPS.Interfaces;

public class ZombieController : MonoBehaviour, IShootableObject
{
    #region Variables
    [Header("Stats")]
    public float health = 100f;
    public bool isDead = false;

    [Header("Movement")]
    public Transform[] waypoints;
    [HideInInspector] public int currentWaypointIndex = 0;

    private NavMeshAgent agent;
    private Animator animator;
    #endregion

    #region MonoBehaviour Callbacks
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
    #endregion

    #region IShootableObject Implementation
    // This is the bridge: The bullet script calls this specific method
    public void OnHit(RaycastHit hit, float damage)
    {
        if (isDead) return;

        Debug.Log($"Zombie hit at {hit.point} for {damage} damage!");
        TakeDamage(damage);
    }
    #endregion

    #region Private Methods
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
        //     // Optional: Remove the collider so bullets pass through the corpse
        //     if (TryGetComponent<Rigidbody>(out Rigidbody rig)) rig.useGravity = false;
        //     if (TryGetComponent<Collider>(out Collider col)) col.enabled = false;
        isDead = true;
        Debug.Log("Zombie Died!");

        // Disable AI navigation so it stops moving
        if (agent != null) agent.enabled = false;
        // IMPORTANT: Change layer so bullets ignore the corpse
        // This prevents the bullet from hitting a "dead" object and trying to parent to it
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
    }
    #endregion
}