using UnityEngine;
using Pada1.BBCore;
using BBUnity.Conditions;

[Condition("Screamer/PlayerInRange")]
public class ScreamerPlayerInRangeCondition : GOCondition
{
    [InParam("Range Threshold")] public float rangeThreshold;

    private ScreamerController controller;

    public override bool Check()
    {
        if (controller == null)
        {
            controller = gameObject.GetComponent<ScreamerController>();
        }

        if (controller == null || controller.player == null)
        {
            return false;
        }

        float distance = Vector3.Distance(gameObject.transform.position, controller.player.transform.position);
        return distance <= rangeThreshold;
    }
}
