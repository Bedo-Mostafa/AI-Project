using UnityEngine;
using BBUnity.Conditions;
using Pada1.BBCore;

[Condition("Zombie/IsAlerted")]
public class IsAlertedCondition : GOCondition
{
    private ZombieBase zombieBase;

    public override bool Check()
    {
        if (zombieBase == null)
        {
            zombieBase = gameObject.GetComponent<ZombieBase>();
        }

        if (zombieBase == null) return false;

        return zombieBase.IsAlerted;
    }
}
