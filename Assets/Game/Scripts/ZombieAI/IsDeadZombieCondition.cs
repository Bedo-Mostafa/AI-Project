using UnityEngine;
using BBUnity.Conditions;
using Pada1.BBCore;

[Condition("Zombie/IsDeadZombie")]
public class IsDeadZombieCondition : GOCondition
{
    private ZombieController zombie;

    public override bool Check()
    {
        if (zombie == null)
        {
            zombie = gameObject.GetComponent<ZombieController>();
        }

        if (zombie == null) return false;

        return zombie.health <= 0;
    }
}