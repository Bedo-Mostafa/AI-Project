using Pada1.BBCore;
using Pada1.BBCore.Tasks;
using UnityEngine;

namespace BBUnity.Actions
{
    [Action("Bat/FallToDeath")]
    [Help("Triggers bat death: raycasts for ground and begins falling. Completes when grounded.")]
    public class BatFallToDeath : GOAction
    {
        private BatController _controller;

        public override void OnStart()
        {
            _controller = gameObject.GetComponent<BatController>();
            _controller?.Die();
        }

        public override TaskStatus OnUpdate()
        {
            // Wait until BatController.FallToGround() finishes
            if (_controller == null) return TaskStatus.FAILED;
            return _controller.IsFalling ? TaskStatus.RUNNING : TaskStatus.COMPLETED;
        }
    }
}
