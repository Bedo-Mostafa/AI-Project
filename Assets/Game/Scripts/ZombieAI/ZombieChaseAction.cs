using UnityEngine;
using UnityEngine.AI;
using Pada1.BBCore;
using Pada1.BBCore.Tasks;
using BBUnity.Actions;

[Action("Zombie/Chase")]
public class ZombieChaseAction : GOAction
{
    private GameObject player;
    private ZombieController controller;
    private NavMeshAgent agent;
    private Animator animator;

    public override void OnStart()
    {
        agent = gameObject.GetComponent<NavMeshAgent>();
        animator = gameObject.GetComponent<Animator>();
        controller = gameObject.GetComponent<ZombieController>();

        // Grab the player reference that the controller found at Start
        if (controller != null)
        {
            player = controller.player;
        }
    }

    public override TaskStatus OnUpdate()
    {
        // If the controller couldn't find a player, the action fails
        if (player == null) return TaskStatus.FAILED;

        if (agent != null && agent.isActiveAndEnabled)
        {
            // Update the NavMesh destination to the player's current position
            agent.SetDestination(player.transform.position);

            // Tell the Animator to play the movement animation
            if (animator != null) animator.SetFloat("Speed", 1f);
        }

        return TaskStatus.RUNNING;
    }
}