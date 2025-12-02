using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

[RequireComponent(typeof(RangeEnemyAttack))]
public class Zoomby : Enemy
{
    [Header("Elements")]
    [SerializeField] private Slider healthBar;
    [SerializeField] private TextMeshProUGUI healthText;

    enum State { None, Idle, Moving, Attacking }

    [Header("State Machine")]
    private State state;
    private float timer;

    [Header("Idle State")]
    [SerializeField] private float maxIdleDuration;
    private float idleDuration;

    [Header("Moving State")]
    [SerializeField] private float moveSpeed;
    private Vector2 targetPosition;

    [Header("Attack State")]
    private int attackCounter;
    private RangeEnemyAttack attack;

    private void Awake()
    {
        health = maxHealth;
        attack = GetComponent<RangeEnemyAttack>();

        state = State.None;

        healthBar.gameObject.SetActive(false);
        onSpawnSequenceCompleted += SpawnSequenceCompleted;
        onDamageTaken += DamageTakenCallback;
    }

    protected override void Start()
    {
        base.Start();
    }

    private void OnDestroy()
    {
        onSpawnSequenceCompleted -= SpawnSequenceCompleted;
        onDamageTaken -= DamageTakenCallback;

    }


    // Update is called once per frame
    void Update()
    {
        ManageStates();
    }

    private void ManageStates()
    {
        switch (state)
        {
            case State.Idle:
                ManageIdleState();
                break;
            case State.Moving:
                ManageMovingState();
                break;
            case State.Attacking:
                ManageAttackingState();
                break;

            default:
                break;
        }
    }

    private void OnEnable()
    {
        SetIdleState();
    }

    private void SetIdleState()
    {
        state = State.Idle;
        idleDuration = Random.Range(1f, maxIdleDuration);
    }

    private void ManageIdleState()
    {
        animator.Play("Idle");
        timer += Time.deltaTime;
        if (timer > idleDuration)
        {
            timer = 0;
            StartMovingState();
        }
    }

    private void StartMovingState()
    {
        state = State.Moving;
        targetPosition = GetRandomPosition();
    }

    private void ManageMovingState()
    {
        animator.Play("Move");
        transform.position = Vector2.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        if (Vector2.Distance(transform.position, targetPosition) < 0.01f)
        {
            StartAttackingState();
        }
    }

    private void StartAttackingState()
    {
        state = State.Attacking;
        animator.Play("Attack");
        attackCounter = 0;
    }

    private void ManageAttackingState()
    {
        
    }

    private void Attack()
    {
        Vector2 direction = Quaternion.Euler(0, 0, -45 * attackCounter) * Vector2.up;

        attack.InstanShoot(direction);

        attackCounter++;
    }


    private void SpawnSequenceCompleted()
    {
        healthBar.gameObject.SetActive(true);
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        healthBar.value = (float)health / maxHealth;
        healthText.text = $"{health} / {maxHealth}";
    }

    private void DamageTakenCallback(int damage, Vector2 position, bool isCriticalHit)
    {
        UpdateHealthBar();
    }

    public override void PassAway()
    {
        onBossPassAway?.Invoke(transform.position);
        PassAwayAfterWave();

    }

    private Vector2 GetRandomPosition()
    {
        Vector2 targetPosition = Vector2.zero;

        targetPosition.x = Random.Range(-Constants.arenaSize.x / 3, Constants.arenaSize.x / 3);
        targetPosition.y = Random.Range(-Constants.arenaSize.y / 3, Constants.arenaSize.y / 3);

        return targetPosition;
    }
}