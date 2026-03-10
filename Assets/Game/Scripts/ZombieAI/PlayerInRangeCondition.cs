using UnityEngine;
using Pada1.BBCore;
using BBUnity.Conditions;

[Condition("Zombie/PlayerInRange")]
public class PlayerInRangeCondition : GOCondition
{
    [InParam("Range Threshold")] public float rangeThreshold;

    private GameObject player;

    public override bool Check()
    {
        // Cache the controller reference the first time we check
        // if (controller == null)
        // {
        //     controller = gameObject.GetComponent<ZombieController>();
        // }
        // This one line replaces all the if/else checks!
        IEnemyController anyController = gameObject.GetComponent<IEnemyController>();
        if (anyController != null)
        {
            player = anyController.player;
        }

        // Safety check: ensure the controller exists and has found a player
        if (player == null)
        {
            return false;
        }

        // Use the dynamic player reference from the ZombieController
        float distance = Vector3.Distance(gameObject.transform.position, player.transform.position);
        return distance <= rangeThreshold;
    }
}