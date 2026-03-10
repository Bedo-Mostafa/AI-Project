using UnityEngine;

[DefaultExecutionOrder(-10)]
public class ZombieBase : MonoBehaviour
{
    [SerializeField] private float alertDuration;

    public bool IsAlerted { get; private set; }

    private float alertTimer;

    public void Alert()
    {
        IsAlerted = true;
        alertTimer = alertDuration;
    }

    private void Update()
    {
        if (!IsAlerted)
            return;

        alertTimer -= Time.deltaTime;
        if (alertTimer <= 0f)
        {
            IsAlerted = false;
        }
    }
}
