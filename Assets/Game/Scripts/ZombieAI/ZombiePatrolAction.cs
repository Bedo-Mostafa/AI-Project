using UnityEngine;
using UnityEngine.AI;
using Pada1.BBCore;
using Pada1.BBCore.Tasks;
using BBUnity.Actions;

[Action("Zombie/Patrol")]
public class ZombiePatrolAction : GOAction
{
    private NavMeshAgent agent;
    private Animator animator;
    private ZombieController controller;

    public override void OnStart()
    {
        agent = gameObject.GetComponent<NavMeshAgent>();
        animator = gameObject.GetComponent<Animator>();
        controller = gameObject.GetComponent<ZombieController>();
    }

    public override TaskStatus OnUpdate()
    {
        if (controller == null || controller.waypoints.Length == 0) return TaskStatus.FAILED;

        Transform target = controller.waypoints[controller.currentWaypointIndex];

        if (agent != null)
        {
            agent.SetDestination(target.position);

            // Just tell the Animator to Walk. Root Motion will handle the speed.
            if (animator != null) animator.SetFloat("Speed", 0f);
        }

        if (Vector3.Distance(gameObject.transform.position, target.position) < 1.5f)
        {
            controller.currentWaypointIndex = (controller.currentWaypointIndex + 1) % controller.waypoints.Length;
            return TaskStatus.COMPLETED;
        }

        return TaskStatus.RUNNING;
    }
}