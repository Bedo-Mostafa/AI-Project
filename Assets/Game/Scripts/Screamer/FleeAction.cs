using UnityEngine;
using UnityEngine.AI;
using Pada1.BBCore;
using Pada1.BBCore.Tasks;
using BBUnity.Actions;

[Action("Screamer/Flee")]
public class FleeAction : GOAction
{
    [InParam("Flee Distance")] public float fleeDistance = 15f;
    [InParam("Safe Distance")] public float safeDistance = 20f;

    private NavMeshAgent agent;
    private Animator animator;
    private Vector3 fleeTarget;
    private ZombieController controller;
    private GameObject player;

    public override void OnStart()
    {
        agent = gameObject.GetComponent<NavMeshAgent>();
        animator = gameObject.GetComponent<Animator>();
        controller = gameObject.GetComponent<ZombieController>();
        player = controller.player;

        PickFleeTarget();
    }

    public override TaskStatus OnUpdate()
    {
        if (agent == null || player == null) return TaskStatus.FAILED;

        agent.SetDestination(fleeTarget);
        animator?.SetBool("IsMoving", true);

        if (!agent.pathPending && agent.remainingDistance < agent.stoppingDistance)
        {
            float distToPlayer = Vector3.Distance(gameObject.transform.position, player.transform.position);

            if (distToPlayer >= safeDistance)
            {
                animator?.SetBool("IsMoving", false);
                return TaskStatus.COMPLETED;
            }

            PickFleeTarget();
        }

        return TaskStatus.RUNNING;
    }

    private void PickFleeTarget()
    {
        if (player == null) return;

        Vector3 awayDir = (gameObject.transform.position - player.transform.position).normalized;

        Vector3 randomOffset = Random.insideUnitSphere * (fleeDistance * 0.5f);
        randomOffset.y = 0f;

        Vector3 rawTarget = gameObject.transform.position + awayDir * fleeDistance + randomOffset;

        if (NavMesh.SamplePosition(rawTarget, out NavMeshHit hit, fleeDistance, NavMesh.AllAreas))
        {
            fleeTarget = hit.position;
        }
    }
}
