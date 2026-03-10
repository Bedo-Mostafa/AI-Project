using UnityEngine;
using UnityEngine.AI;
using Pada1.BBCore;
using Pada1.BBCore.Tasks;
using BBUnity.Actions;

[Action("Zombie/Attack")]
public class ZombieAttackAction : GOAction
{
    [InParam("Attack Damage")] public float attackDamage = 10f;
    [InParam("Clip Name")] public string clipName = "Attack";
    [InParam("Hit Timing (0 to 1)")] public float hitTiming = 0.5f;

    private GameObject player;
    private ZombieController controller; // Reference to our main script
    private float attackDuration = 1.0f;
    private float attackStartTime;
    private bool hasDealtDamage;
    private NavMeshAgent agent;
    private Animator animator;

    public override void OnStart()
    {
        agent = gameObject.GetComponent<NavMeshAgent>();
        animator = gameObject.GetComponent<Animator>();
        controller = gameObject.GetComponent<ZombieController>();

        // Get the dynamic player reference from the controller
        if (controller != null)
        {
            player = controller.player;
        }
        hasDealtDamage = false;

        FindAnimationDuration();

        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        if (animator != null)
        {
            animator.SetFloat("Speed", 0);
            animator.SetBool("IsAttacking", true);
        }

        // Face the player flatly right at the start
        FacePlayerFlat();

        attackStartTime = Time.time;
    }

    public override TaskStatus OnUpdate()
    {
        if (player == null) return TaskStatus.FAILED;

        // Smoothly rotate toward the player without tilting up or down
        FacePlayerFlat();

        float elapsedTime = Time.time - attackStartTime;

        if (!hasDealtDamage && elapsedTime >= (attackDuration * hitTiming))
        {
            PlayerHealth ph = player.GetComponent<PlayerHealth>();

            // Safety check: Only deal damage and log health if the target actually has a health script
            if (ph != null)
            {
                ph.TakeDamage(attackDamage);
                Debug.Log($"Player Health: {ph.health}");
            }
            else
            {
                // This tells us what the zombie is mistakenly targeting!
                Debug.LogWarning($"[Zombie] Tried to attack {player.name}, but it has no PlayerHealth script!");
            }

            hasDealtDamage = true;
        }

        if (elapsedTime >= attackDuration)
        {
            if (animator != null) animator.SetBool("IsAttacking", false);
            if (agent != null && agent.isActiveAndEnabled) agent.isStopped = false;

            return TaskStatus.COMPLETED;
        }

        return TaskStatus.RUNNING;
    }

    public override void OnAbort()
    {
        if (animator != null) animator.SetBool("IsAttacking", false);
        if (agent != null && agent.isActiveAndEnabled) agent.isStopped = false;
    }

    private void FindAnimationDuration()
    {
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip.name.Contains(clipName))
                {
                    attackDuration = clip.length;
                    return;
                }
            }
        }
    }

    // Helper method to keep the zombie flat on the ground while turning
    private void FacePlayerFlat()
    {
        if (player == null) return;

        Vector3 direction = player.transform.position - gameObject.transform.position;
        direction.y = 0; // This is the magic line that stops the zombie from tilting!

        if (direction.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            // Slerp makes the turning smooth instead of violently snapping
            gameObject.transform.rotation = Quaternion.Slerp(gameObject.transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
    }
}