using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using XtremeFPS.Interfaces;

public class FartBossController : MonoBehaviour, IShootableObject, IEnemyController
{
    // This satisfies the interface requirement
    [SerializeField] private GameObject _player;
    public GameObject player => _player;

    [Header("Stats")]
    public float health = 300f; // Give the boss more health!
    public bool isDead = false;

    private NavMeshAgent agent;
    private Animator animator;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (_player == null)
        {
            _player = GameObject.FindGameObjectWithTag("Player");
        }
    }

    // This is the bridge: The bullet script calls this specific method
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
        // Remove the collider so bullets pass through the corpse
        if (TryGetComponent<Rigidbody>(out Rigidbody rig)) rig.useGravity = false;
        if (TryGetComponent<Collider>(out Collider col)) col.enabled = false;

        isDead = true;

        // Disable AI navigation so it stops moving
        // if (agent != null) agent.enabled = false;

        // Change layer so bullets ignore the corpse
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

        // Tell the GameManager that a zombie has died
        if (GameManager.Instance != null) GameManager.Instance.ZombieDied();

        // Start the timer to safely hide the body
        StartCoroutine(HideCorpseRoutine());
    }

    private IEnumerator HideCorpseRoutine()
    {
        // Wait for 5 seconds so the death animation can finish and the body rests on the floor
        yield return new WaitForSeconds(5f);

        gameObject.SetActive(false);
    }
}