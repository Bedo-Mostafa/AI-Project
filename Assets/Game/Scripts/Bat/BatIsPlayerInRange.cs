using Pada1.BBCore;
using UnityEngine;

namespace BBUnity.Conditions
{
    /// <summary>
    /// Checks whether the player is within detection radius ignoring height difference.
    /// </summary>
    [Condition("Bat/IsPlayerInRange")]
    [Help("True if the player is within detection radius ignoring height difference")]
    public class BatIsPlayerInRange : GOCondition
    {
        private BatController _controller;

        public override bool Check()
        {
            if (_controller == null)
                _controller = gameObject.GetComponent<BatController>();

            if (_controller == null || _controller.PlayerTarget == null) return false;

            Vector3 diff = gameObject.transform.position - _controller.PlayerTarget.position;
            diff.y = 0f;

            return diff.sqrMagnitude < _controller.DetectionRadius * _controller.DetectionRadius;
        }
    }
}