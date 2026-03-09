using Pada1.BBCore;
using UnityEngine;

namespace BBUnity.Conditions
{
    [Condition("Bat/IsAtPlayerHeight")]
    [Help("True if the bat's Y position is within threshold of the player's Y")]
    public class BatIsAtPlayerHeight : GOCondition
    {
        private BatController _controller;

        public override bool Check()
        {
            if (_controller == null)
                _controller = gameObject.GetComponent<BatController>();

            if (_controller == null || _controller.PlayerTarget == null) return false;

            float verticalDiff = Mathf.Abs(gameObject.transform.position.y - _controller.PlayerTarget.position.y);
            _controller.IsAtPlayerHeight = verticalDiff <= _controller.HeightThreshold;
            return _controller.IsAtPlayerHeight;
        }
    }
}
