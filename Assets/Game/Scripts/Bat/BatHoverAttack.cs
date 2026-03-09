using Pada1.BBCore;
using Pada1.BBCore.Tasks;
using UnityEngine;

namespace BBUnity.Actions
{
    [Action("Bat/HoverAttack")]
    [Help("Hovers at player height, seeks the player if they move away, and attacks on cooldown.")]
    public class BatHoverAttack : GOAction
    {
        private const float RotationSpeed = 5f;

        private BatController _controller;
        private Transform _batTransform;
        private float _attackTimer;

        public override void OnStart()
        {
            _controller  = gameObject.GetComponent<BatController>();
            _batTransform = gameObject.transform;
            _attackTimer = 0f;
        }

        public override TaskStatus OnUpdate()
        {
            if (_controller == null || _controller.PlayerTarget == null)
                return TaskStatus.FAILED;

            TrackPlayerHeight();
            SeekOrAttack();
            FacePlayer();

            return TaskStatus.RUNNING;
        }

        // Always stay at player's height so hover follows jumps/movement
        private void TrackPlayerHeight()
        {
            Vector3 pos = _batTransform.position;
            pos.y = Mathf.Lerp(pos.y, _controller.PlayerTarget.position.y, _controller.MoveSpeed * Time.deltaTime);
            _batTransform.position = pos;
        }

        private void SeekOrAttack()
        {
            Vector3 toPlayer = _controller.PlayerTarget.position - _batTransform.position;
            toPlayer.y = 0f;
            float flatDist = toPlayer.magnitude;

            if (flatDist > _controller.HoverDistance)
                SeekPlayer(toPlayer, flatDist);
            else
                HandleAttack();
        }

        private void SeekPlayer(Vector3 horizontalToPlayer, float distance)
        {
            Vector3 step = horizontalToPlayer.normalized * _controller.MoveSpeed * Time.deltaTime;
            _batTransform.position += step;
        }

        private void HandleAttack()
        {
            _attackTimer += Time.deltaTime;
            if (_attackTimer < _controller.AttackCooldown) return;

            _attackTimer = 0f;
            _controller.TriggerAttack();
        }

        private void FacePlayer()
        {
            Vector3 lookDir = _controller.PlayerTarget.position - _batTransform.position;
            if (lookDir.sqrMagnitude < 0.001f) return;

            Quaternion targetRot = Quaternion.LookRotation(lookDir);
            _batTransform.rotation = Quaternion.Slerp(_batTransform.rotation, targetRot, RotationSpeed * Time.deltaTime);
        }
    }
}