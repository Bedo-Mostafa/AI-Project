using Pada1.BBCore;
using UnityEngine;

namespace BBUnity.Conditions
{
    [Condition("Bat/IsDead")]
    [Help("True if the player is dead according to BatController's shared flag")]
    public class BatIsDead : GOCondition
    {
        private BatController _controller;

        public override bool Check()
        {
            if (_controller == null)
                _controller = gameObject.GetComponent<BatController>();

            return _controller != null && !_controller.IsAlive;
        }
    }
}
