using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using XtremeFPS.Interfaces;

[DefaultExecutionOrder(-10)]
public class ScreamerController : MonoBehaviour, IShootableObject
{
    [Header("Stats")]
    public float health = 100f;
    public bool isDead = false;

    [Header("Dynamic References")]
    public GameObject player;

    [Header("Audio")]
    public AudioClip screamClip;

    [Header("Scream Settings")]
    public float rotateDuration = 0.5f;
    public float soundDelay = 1f;

    private NavMeshAgent agent;
    private Animator animator;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) Debug.LogError($"{gameObject.name} could not find an object tagged 'Player'!");
    }

    public void OnHit(RaycastHit hit, float damage)
    {
        if (isDead) return;

        TakeDamage(damage);
    }

    public void TakeDamage(float amount)
    {
        health -= amount;

        if (health <= 0 && !isDead)
        {
            Die();
        }
    }

    private void Die()
    {
        if (TryGetComponent<Rigidbody>(out Rigidbody rig)) rig.useGravity = false;
        if (TryGetComponent<Collider>(out Collider col)) col.enabled = false;

        isDead = true;

        // if (agent != null) agent.enabled = false;

        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

        if (GameManager.Instance != null) GameManager.Instance.ZombieDied();

        StartCoroutine(HideCorpseRoutine());
    }

    public void Scream()
    {
        if (player != null)
            StartCoroutine(RotateTowardsPlayer());

        if (screamClip != null)
            StartCoroutine(DelayedScreamSound());
    }

    private IEnumerator HideCorpseRoutine()
    {
        yield return new WaitForSeconds(5f);
        gameObject.SetActive(false);
    }

    private IEnumerator RotateTowardsPlayer()
    {
        Vector3 direction = player.transform.position - transform.position;
        direction.y = 0;

        if (direction.magnitude < 0.1f) yield break;

        Quaternion startRot = transform.rotation;
        Quaternion targetRot = Quaternion.LookRotation(direction);
        float elapsed = 0f;

        while (elapsed < rotateDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / rotateDuration);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        transform.rotation = targetRot;
    }

    private IEnumerator DelayedScreamSound()
    {
        yield return new WaitForSeconds(soundDelay);
        AudioSource.PlayClipAtPoint(screamClip, transform.position);
    }
}
