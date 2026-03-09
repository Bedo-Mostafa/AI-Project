using UnityEngine;
using UnityEngine.AI;
using Pada1.BBCore;
using Pada1.BBCore.Tasks;
using BBUnity.Actions;

[Action("Screamer/Wander")]
public class WanderAction : GOAction
{
    [InParam("Wander Radius")] public float wanderRadius = 10f;

    private NavMeshAgent agent;
    private Animator animator;
    private Vector3 wanderTarget;

    public override void OnStart()
    {
        agent = gameObject.GetComponent<NavMeshAgent>();
        animator = gameObject.GetComponent<Animator>();

        PickNewWanderTarget();
    }

    public override TaskStatus OnUpdate()
    {
        if (agent == null) return TaskStatus.FAILED;

        agent.SetDestination(wanderTarget);
        animator?.SetBool("IsMoving", true);

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            animator?.SetBool("IsMoving", false);
            return TaskStatus.COMPLETED;
        }

        return TaskStatus.RUNNING;
    }

    private void PickNewWanderTarget()
    {
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection += gameObject.transform.position;
        randomDirection.y = gameObject.transform.position.y;

        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
        {
            wanderTarget = hit.position;
        }
        else
        {
            wanderTarget = gameObject.transform.position;
        }
    }
}