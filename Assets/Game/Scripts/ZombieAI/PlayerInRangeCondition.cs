// using UnityEngine;
// using Pada1.BBCore;
// using BBUnity.Conditions;

// [Condition("Zombie/PlayerInRange")]
// public class PlayerInRangeCondition : GOCondition
// {
//     [InParam("Player")] public GameObject player;
//     [InParam("Range Threshold")] public float rangeThreshold;

//     public override bool Check()
//     {
//         if (player == null) return false;
//         return Vector3.Distance(gameObject.transform.position, player.transform.position) <= rangeThreshold;
//     }
// }

using UnityEngine;
using Pada1.BBCore;
using BBUnity.Conditions;

[Condition("Zombie/PlayerInRange")]
public class PlayerInRangeCondition : GOCondition
{
    // Removed [InParam("Player")] as it is now found dynamically
    [InParam("Range Threshold")] public float rangeThreshold;

    private ZombieController controller;

    public override bool Check()
    {
        // Cache the controller reference the first time we check
        if (controller == null)
        {
            controller = gameObject.GetComponent<ZombieController>();
        }

        // Safety check: ensure the controller exists and has found a player
        if (controller == null || controller.player == null)
        {
            return false;
        }

        // Use the dynamic player reference from the ZombieController
        float distance = Vector3.Distance(gameObject.transform.position, controller.player.transform.position);
        return distance <= rangeThreshold;
    }
}