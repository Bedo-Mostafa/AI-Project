using UnityEngine;
using BBUnity.Conditions;
using Pada1.BBCore;

[Condition("Zombie/IsDead")]
public class IsDeadCondition : GOCondition
{
    private ZombieController zombie;

    public override bool Check()
    {
        // 1. Fetch the component only if we haven't already (Lazy Initialization)
        if (zombie == null)
        {
            zombie = gameObject.GetComponent<ZombieController>();
        }

        // 2. Safety check: if zombie is STILL null, the condition shouldn't pass
        if (zombie == null) return false;

        // 3. Return the actual health evaluation
        return zombie.health <= 0;
    }
}