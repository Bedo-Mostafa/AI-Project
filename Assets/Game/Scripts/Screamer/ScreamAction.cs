using UnityEngine;
using Pada1.BBCore;
using Pada1.BBCore.Tasks;
using BBUnity.Actions;

[Action("Screamer/Scream")]
public class ScreamAction : GOAction
{
    [InParam("Scream Radius")] public float screamRadius = 20f;
    [InParam("Zombie Layer")] public LayerMask zombieLayer;

    private Animator anim;

    public override void OnStart()
    {
        if (anim == null) anim = gameObject.GetComponent<Animator>();
        if (anim != null) anim.SetTrigger("Scream");

        ScreamerController controller = gameObject.GetComponent<ScreamerController>();
        if (controller != null) controller.Scream();
    }

    public override TaskStatus OnUpdate()
    {
        Collider[] hits = Physics.OverlapSphere(gameObject.transform.position, screamRadius, zombieLayer);

        foreach (Collider hit in hits)
        {
            ZombieBase zombie = hit.GetComponent<ZombieBase>();

            if (zombie == null || zombie == gameObject.GetComponent<ZombieBase>())
                continue;

            zombie.Alert();
        }

        return TaskStatus.COMPLETED;
    }
}
