using UnityEngine;
using UnityEngine.UI;
using Pada1.BBCore;
using Pada1.BBCore.Tasks;
using BBUnity.Actions;
using DG.Tweening;

[Action("FartBoss/Fart")]
public class Fart : GOAction
{
    [InParam("Player")] public GameObject player;
    [InParam("Total Damage")] public float totalDamage = 30f;
    [InParam("Duration")] public float duration = 3f;
    [InParam("Fart Effect")] public GameObject fartEffect;

    [InParam("Max Opacity")] public float maxOpacity = 1f;
    [InParam("Fade Duration")] public float fadeDuration = 0.5f;

    private float startTime;
    private float damagePerSecond;

    public override void OnStart()
    {
        damagePerSecond = totalDamage / duration;

        if (fartEffect != null)
        {
            fartEffect.SetActive(true);

            Image fartImg = fartEffect.GetComponent<Image>();

            if (fartImg != null)
            {
                fartImg.DOKill();

                Color c = fartImg.color;
                c.a = 0f;
                fartImg.color = c;

                fartImg.DOFade(maxOpacity, fadeDuration).SetEase(Ease.InOutSine);
            }
        }

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
        {
            Image fartImg = fartEffect.GetComponent<Image>();

            if (fartImg != null)
            {
                fartImg.DOKill();

                fartImg.DOFade(0f, fadeDuration)
                    .SetEase(Ease.InOutSine)
                    .OnComplete(() => fartEffect.SetActive(false));
            }
            else
            {
                fartEffect.SetActive(false);
            }
        }
    }
}