using UnityEngine;
using BBUnity.Conditions;
using Pada1.BBCore;

[Condition("Screamer/IsDead")]
public class ScreamerIsDeadCondition : GOCondition
{
    private ScreamerController screamer;

    public override bool Check()
    {
        if (screamer == null)
        {
            screamer = gameObject.GetComponent<ScreamerController>();
        }

        if (screamer == null) return false;

        return screamer.health <= 0;
    }
}
