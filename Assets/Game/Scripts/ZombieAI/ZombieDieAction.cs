using UnityEngine;
using UnityEngine.AI;
using BBUnity.Actions;
using Pada1.BBCore;
using Pada1.BBCore.Tasks;

[Action("Zombie/Die")]
public class ZombieDieAction : GOAction
{
    private NavMeshAgent agent;
    private Animator animator;
    private bool isDead = false;

    public override void OnStart()
    {
        agent = gameObject.GetComponent<NavMeshAgent>();
        animator = gameObject.GetComponent<Animator>();
    }

    public override TaskStatus OnUpdate()
    {
        if (!isDead)
        {
            isDead = true;
            if (agent != null && agent.isActiveAndEnabled)
            {
                agent.ResetPath();
                agent.isStopped = true;
            }
            animator.SetBool("IsDead", true);
        }
        return TaskStatus.RUNNING;
    }
}