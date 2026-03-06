using UnityEngine;
using Pada1.BBCore;
using Pada1.BBCore.Tasks;
using BBUnity.Actions;

[Action("Zombie/Alert")]
public class AlertZombieAction : GOAction
{
    private ZombieBase zombieBase;

    public override void OnStart()
    {
        if (zombieBase == null)
        {
            zombieBase = gameObject.GetComponent<ZombieBase>();
        }

        if (zombieBase != null)
        {
            zombieBase.Alert();
        }
    }

    public override TaskStatus OnUpdate()
    {
        return TaskStatus.COMPLETED;
    }
}
