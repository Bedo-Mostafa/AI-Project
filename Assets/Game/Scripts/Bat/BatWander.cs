using Pada1.BBCore;
using Pada1.BBCore.Tasks;
using UnityEngine;

namespace BBUnity.Actions
{
    [Action("Bat/Wander")]
    [Help("Flies the bat randomly within a BoxCollider's bounds. Runs forever until interrupted.")]
    public class BatWander : GOAction
    {
        private const float ArrivalThreshold = 0.4f;
        private const float RotationSpeed = 5f;

        private Vector3 _currentTarget;
        private BatController _controller;

        public override void OnStart()
        {
            _controller = gameObject.GetComponent<BatController>();
            _currentTarget = PickRandomPoint();
        }

        public override TaskStatus OnUpdate()
        {
            MoveTowardTarget();

            if (HasReachedTarget())
                _currentTarget = PickRandomPoint();

            return TaskStatus.RUNNING;
        }

        private void MoveTowardTarget()
        {
            if (_controller == null) return;

            var t = gameObject.transform;
            t.position = Vector3.MoveTowards(t.position, _currentTarget, _controller.MoveSpeed * Time.deltaTime);

            Vector3 dir = _currentTarget - t.position;
            if (dir.sqrMagnitude < 0.001f) return;

            Quaternion targetRot = Quaternion.LookRotation(dir);
            t.rotation = Quaternion.Slerp(t.rotation, targetRot, RotationSpeed * Time.deltaTime);
        }

        private bool HasReachedTarget()
        {
            return Vector3.Distance(gameObject.transform.position, _currentTarget) < ArrivalThreshold;
        }

        private Vector3 PickRandomPoint()
        {
            if (_controller == null || _controller.WanderBounds == null)
                return gameObject.transform.position;

            Bounds b = _controller.WanderBounds.bounds;
            return new Vector3(
                Random.Range(b.min.x, b.max.x),
                Random.Range(b.min.y, b.max.y),
                Random.Range(b.min.z, b.max.z)
            );
        }
    }
}
