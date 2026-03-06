using UnityEngine;
using Pada1.BBCore;
using Pada1.BBCore.Tasks;
using BBUnity.Actions;

[Action("Screamer/Scream")]
public class ScreamAction : GOAction
{
    [InParam("Scream Radius")] public float screamRadius = 20f;
    [InParam("Zombie Layer")] public LayerMask zombieLayer;

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
