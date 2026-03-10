using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using XtremeFPS.Interfaces;

public class ScreamerController : MonoBehaviour, IShootableObject
{
    [Header("Stats")]
    public float health = 100f;
    public bool isDead = false;

    [Header("Dynamic References")]
    public GameObject player;

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

        if (agent != null) agent.enabled = false;

        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

        if (GameManager.Instance != null) GameManager.Instance.ZombieDied();

        Destroy(gameObject, 5f);
    }
}
