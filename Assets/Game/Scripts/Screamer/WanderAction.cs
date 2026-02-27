using UnityEngine;
using UnityEngine.AI;
using Pada1.BBCore;
using Pada1.BBCore.Tasks;
using BBUnity.Actions;

[Action("Screamer/Wander")]
public class WanderAction : GOAction
{
    [InParam("Wander Radius")] public float wanderRadius = 10f;
    [InParam("Wait Time")] public float waitTime = 2f;

    private NavMeshAgent agent;
    private Animator animator;
    private Vector3 wanderTarget;
    private bool isWaiting;
    private float waitTimer;

    public override void OnStart()
    {
        agent = gameObject.GetComponent<NavMeshAgent>();
        animator = gameObject.GetComponent<Animator>();
        isWaiting = false;
        waitTimer = 0f;

        PickNewWanderTarget();
    }

    public override TaskStatus OnUpdate()
    {
        if (agent == null) return TaskStatus.FAILED;

        if (isWaiting)
        {
            animator.SetBool("IsMoving", false);

            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                isWaiting = false;
                PickNewWanderTarget();
            }
            return TaskStatus.RUNNING;
        }

        agent.SetDestination(wanderTarget);
        animator.SetBool("IsMoving", true);

        if (!agent.pathPending && agent.remainingDistance < agent.stoppingDistance)
        {
            isWaiting = true;
            waitTimer = waitTime;
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