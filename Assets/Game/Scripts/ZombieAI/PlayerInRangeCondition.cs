using UnityEngine;
using Pada1.BBCore;
using BBUnity.Conditions;

[Condition("Zombie/PlayerInRange")]
public class PlayerInRangeCondition : GOCondition
{
    [InParam("Player")] public GameObject player;
    [InParam("Range Threshold")] public float rangeThreshold;

    public override bool Check()
    {
        if (player == null) return false;
        return Vector3.Distance(gameObject.transform.position, player.transform.position) <= rangeThreshold;
    }
}