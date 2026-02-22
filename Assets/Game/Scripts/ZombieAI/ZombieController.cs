using UnityEngine;
using UnityEngine.AI;

public class ZombieController : MonoBehaviour
{
    public float health = 100f;
    public Transform[] waypoints;
    [HideInInspector] public int currentWaypointIndex = 0;

    private NavMeshAgent agent;
    private Animator animator;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        // IMPORTANT: Tell the NavMeshAgent NOT to push the character forward.
        // We are letting the animation handle the physical movement now!
        if (agent != null) agent.updatePosition = false;
    }

    public void TakeDamage(float amount)
    {
        health -= amount;
    }

    // This built-in function runs every frame right after the Animator calculates Root Motion
    void OnAnimatorMove()
    {
        if (animator != null && agent != null && agent.isActiveAndEnabled)
        {
            // 1. Physically move the GameObject using the animation's movement data
            transform.position = animator.rootPosition;

            // 2. Tell the NavMeshAgent brain where the body just moved, so they stay synced
            agent.nextPosition = transform.position;
        }
    }
}