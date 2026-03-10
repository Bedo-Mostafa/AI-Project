using UnityEngine;
using BBUnity.Conditions;
using Pada1.BBCore;

[Condition("FartBoss/IsDead")]
public class FartBossIsDeadCondition : GOCondition
{
    private FartBossController boss;

    public override bool Check()
    {
        // Fetch the component only if we haven't already
        if (boss == null)
        {
            boss = gameObject.GetComponent<FartBossController>();
        }

        // Safety check: if boss is STILL null, the condition shouldn't pass
        if (boss == null) return false;

        // Return the actual health evaluation
        return boss.health <= 0;
    }
}