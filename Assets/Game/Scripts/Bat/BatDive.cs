using Pada1.BBCore;
using Pada1.BBCore.Tasks;
using UnityEngine;

namespace BBUnity.Actions
{
    [Action("Bat/Dive")]
    [Help("Dives the bat toward the player's position while descending to player height.")]
    public class BatDive : GOAction
    {
        private BatController _controller;

        public override void OnStart()
        {
            _controller = gameObject.GetComponent<BatController>();
        }

        public override TaskStatus OnUpdate()
        {
            if (_controller == null || _controller.PlayerTarget == null) return TaskStatus.FAILED;

            Vector3 diveTarget = _controller.PlayerTarget.position;
            gameObject.transform.position = Vector3.MoveTowards(
                gameObject.transform.position, diveTarget, _controller.DiveSpeed * Time.deltaTime);

            Quaternion targetRotation = Quaternion.LookRotation(diveTarget - gameObject.transform.position);
            gameObject.transform.rotation = Quaternion.Slerp(gameObject.transform.rotation, targetRotation, 5f * Time.deltaTime);

            return HasReachedPlayerHeight() ? TaskStatus.COMPLETED : TaskStatus.RUNNING;
        }

        private bool HasReachedPlayerHeight()
        {
            float diff = Mathf.Abs(gameObject.transform.position.y - _controller.PlayerTarget.position.y);
            _controller.IsAtPlayerHeight = diff <= _controller.HeightThreshold;
            return _controller.IsAtPlayerHeight;
        }
    }
}
