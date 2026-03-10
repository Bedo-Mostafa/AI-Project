using UnityEngine;
using System.Collections;
using XtremeFPS.Interfaces;

/// <summary>
/// Central state hub for the bat enemy AI.
/// Shared by all Behavior Bricks actions and conditions.
/// </summary>
[RequireComponent(typeof(Animator))]
public class BatController : MonoBehaviour, IShootableObject
{
    private const string TakeDamageTrigger = "TakeDamage";
    private const string AttackTrigger = "Attack";
    private const string FallTrigger = "Dead";

    [SerializeField] private int _maxHealth = 100;
    [SerializeField] private float _fallSpeed = 5f;
    [SerializeField] private float _groundCheckDist = 50f;
    [SerializeField] private LayerMask _groundMask;
    [SerializeField] private float _despawnDelay = 10f;

    [Header("AI Parameters")]
    public Transform PlayerTarget;     // Assign the Player in the Inspector
    public float DetectionRadius = 12f;
    public float escapeRadius = 20f;
    public BoxCollider WanderBounds;
    public float HoverDistance = 2f;
    public float AttackCooldown = 1.5f;
    public float MoveSpeed = 4f;
    public float DiveSpeed = 6f;
    public float HeightThreshold = 1f;
    public float SeparationRadius = 3f;   // Min distance between bats
    public float SeparationForce = 5f;   // How strongly they push apart

    public bool IsAtPlayerHeight = false;
    public bool playerDetected = false;

    public int CurrentHealth = 100;
    public bool IsAlive = true;
    public bool IsFalling { get; private set; }
    public bool IsPlayerDead { get; set; }

    public Animator _animator;
    private Vector3 _groundTarget;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        CurrentHealth = _maxHealth;
    }

    private void Update()
    {
        if (IsFalling)
            FallToGround();

        if (IsAtPlayerHeight)
        {
            _animator.SetBool("CanAttackPlayer", true);
        }
        else
        {
            _animator.SetBool("CanAttackPlayer", false);
        }
        if (CurrentHealth <= 0)
            Die();

        CheckDetection();
        CheckPlayerHealth();
        ApplySeparation();
    }

    private void ApplySeparation()
    {
        if (!IsAlive) return;

        Collider[] nearby = Physics.OverlapSphere(transform.position, SeparationRadius);
        Vector3 push = Vector3.zero;

        foreach (Collider col in nearby)
        {
            if (col.gameObject == gameObject) continue;
            if (!col.TryGetComponent<BatController>(out _)) continue;

            Vector3 away = transform.position - col.transform.position;
            float strength = 1f - (away.magnitude / SeparationRadius); // Stronger when closer
            push += away.normalized * strength;
        }

        transform.position += push * SeparationForce * Time.deltaTime;
    }

    private void CheckDetection()
    {
        if (PlayerTarget == null || !IsAlive) return;

        Vector3 diff = transform.position - PlayerTarget.position;
        diff.y = 0f; // Ignore height for detection

        if (diff.sqrMagnitude <= DetectionRadius * DetectionRadius)
        {
            playerDetected = true;
        }
        else if (diff.sqrMagnitude >= escapeRadius * escapeRadius)
        {
            _animator.SetBool("CanAttackPlayer", false);
            playerDetected = false;
        }
    }

    public void OnHit(RaycastHit hit, float damage)
    {
        if (!IsAlive) return;

        TakeDamage(damage);
    }

    public void TakeDamage(float amount)
    {
        if (!IsAlive) return;

        CurrentHealth -= (int)amount;
        _animator.SetTrigger(TakeDamageTrigger);

        if (CurrentHealth <= 0)
            Die();
    }

    public void CauseDamage()
    {
        PlayerTarget.GetComponent<PlayerHealth>().TakeDamage(20);
    }

    public void TriggerAttack()
    {
        _animator.SetTrigger(AttackTrigger);
    }

    public void Die()
    {
        if (!IsAlive) return;

        IsAlive = false;
        IsFalling = true;

        _groundTarget = FindGroundPoint();

        // Calculate height from ground
        float heightFromGround = transform.position.y - _groundTarget.y;

        // Send to animator
        _animator.SetFloat("FallHeight", heightFromGround);

        _animator.SetTrigger(FallTrigger);

        var executor = GetComponent<BehaviorExecutor>();
        if (executor != null)
            executor.enabled = false;
    }

    private Vector3 FindGroundPoint()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, _groundCheckDist, _groundMask))
            return hit.point;

        return transform.position + Vector3.down * _groundCheckDist;
    }

    private void FallToGround()
    {
        transform.position = Vector3.MoveTowards(
            transform.position, _groundTarget, _fallSpeed * Time.deltaTime);

        // Gradually zero out X position offset and all rotation while falling
        Vector3 pos = transform.position;
        pos.x = Mathf.Lerp(pos.x, _groundTarget.x, _fallSpeed * Time.deltaTime);
        transform.position = pos;

        transform.rotation = Quaternion.Lerp(
            transform.rotation, Quaternion.identity, _fallSpeed * Time.deltaTime);

        if (transform.position == _groundTarget)
        {
            IsFalling = false;
            _animator.SetBool("isGrounded", true);

            StartCoroutine(DespawnAfterDelay());
        }
    }

    private IEnumerator DespawnAfterDelay()
    {
        yield return new WaitForSeconds(_despawnDelay);
        Destroy(gameObject);
    }

    private void CheckPlayerHealth()
    {
        if (PlayerTarget == null) return;

        if (PlayerTarget.GetComponent<PlayerHealth>().health <= 0)
        {
            IsPlayerDead = true;
            playerDetected = false;
            _animator.SetBool("CanAttackPlayer", false);
        }
    }
}
