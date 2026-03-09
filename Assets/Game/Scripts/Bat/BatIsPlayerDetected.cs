using Pada1.BBCore;
using UnityEngine;

namespace BBUnity.Conditions
{
    [Condition("Bat/IsPlayerDetected")]
    [Help("True if the BatController has flagged the player as detected")]
    public class BatIsPlayerDetected : GOCondition
    {
        private BatController _controller;

        public override bool Check()
        {
            if (_controller == null)
                _controller = gameObject.GetComponent<BatController>();

            return _controller != null && _controller.playerDetected;
        }
    }
}