using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using XtremeFPS.Interfaces;

public class ZombieController : MonoBehaviour, IShootableObject, IEnemyController
{
    [SerializeField] private GameObject _player;
    public GameObject player => _player;
    #region Variables
    [Header("Stats")]
    public float health = 100f;
    public bool isDead = false;

    [Header("Dynamic References")]
    public Transform[] waypoints;
    [HideInInspector] public int currentWaypointIndex = 0;

    private NavMeshAgent agent;
    private Animator animator;
    #endregion

    #region MonoBehaviour Callbacks
    void Start()
    {

        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (_player == null)
        {
            _player = GameObject.FindGameObjectWithTag("Player");
        }

        GameObject[] waypointObjects = GameObject.FindGameObjectsWithTag("Waypoint");

        waypoints = waypointObjects
            .OrderBy(go => go.name)
            .Select(go => go.transform)
            .ToArray();

        if (waypoints.Length == 0) Debug.LogWarning($"{gameObject.name} found 0 waypoints!");

        if (agent != null) agent.updatePosition = false;
    }

    void OnAnimatorMove()
    {
        if (animator != null && agent != null && agent.isActiveAndEnabled && !isDead)
        {
            transform.position = animator.rootPosition;
            agent.nextPosition = transform.position;
        }
    }
    #endregion

    #region IShootableObject Implementation
    public void OnHit(RaycastHit hit, float damage)
    {
        if (isDead) return;

        TakeDamage(damage);
    }
    #endregion

    #region Private Methods
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

    private IEnumerator HideCorpseRoutine()
    {
        yield return new WaitForSeconds(5f);

        gameObject.SetActive(false);
    }
    #endregion
}