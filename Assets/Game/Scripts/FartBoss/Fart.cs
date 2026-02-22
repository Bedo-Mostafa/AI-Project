using UnityEngine;
using Pada1.BBCore;
using Pada1.BBCore.Tasks;
using BBUnity.Actions;

[Action("FartBoss/Fart")]
public class Fart : GOAction
{
    [InParam("Player")] public GameObject player;
    [InParam("Total Damage")] public float totalDamage = 30f;
    [InParam("Duration")] public float duration = 3f;
    [InParam("Fart Effect")] public GameObject fartEffect;

    private float startTime;
    private float damagePerSecond;

    public override void OnStart()
    {
        damagePerSecond = totalDamage / duration;

        if (fartEffect != null)
            fartEffect.SetActive(true);

        startTime = Time.time;
    }

    public override TaskStatus OnUpdate()
    {
        if (player == null) return TaskStatus.FAILED;

        float elapsed = Time.time - startTime;

        if (elapsed < duration)
        {
            float frameDamage = damagePerSecond * Time.deltaTime;
            PlayerHealth ph = player.GetComponent<PlayerHealth>();
            if (ph != null)
                ph.TakeDamage(frameDamage);

            return TaskStatus.RUNNING;
        }

        Finish();
        return TaskStatus.COMPLETED;
    }

    public override void OnAbort()
    {
        Finish();
    }

    private void Finish()
    {
        if (fartEffect != null)
            fartEffect.SetActive(false);
    }
}
