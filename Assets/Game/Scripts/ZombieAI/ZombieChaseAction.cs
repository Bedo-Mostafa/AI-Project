using UnityEngine;
using UnityEngine.AI;
using Pada1.BBCore;
using Pada1.BBCore.Tasks;
using BBUnity.Actions;

[Action("Zombie/Chase")]
public class ZombieChaseAction : GOAction
{
    [InParam("Player")] public GameObject player;

    private NavMeshAgent agent;
    private Animator animator;

    public override void OnStart()
    {
        agent = gameObject.GetComponent<NavMeshAgent>();
        animator = gameObject.GetComponent<Animator>();
    }

    public override TaskStatus OnUpdate()
    {
        if (player == null) return TaskStatus.FAILED;

        if (agent != null)
        {
            agent.SetDestination(player.transform.position);

            // Tell the Animator to Walk/Run
            if (animator != null) animator.SetFloat("Speed", 1f);
        }

        return TaskStatus.RUNNING;
    }
}