using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeWeapon : Weapon
{
    enum State
    {
        Idle,
        Attack
    }

    [Header("Elements")]
    [SerializeField] private Transform hitDetectionTransform;
    [SerializeField] private BoxCollider2D hitCollider;

    [Header("Settings")]
    private List<Enemy> damagedEnemies = new List<Enemy>();


    private State state;
    // Start is called before the first frame update
    void Start()
    {
        state = State.Idle;
    }

    // Update is called once per frame
    void Update()
    {
        switch (state)
        {
            case State.Idle:
                AutoArm();
                break;
            case State.Attack:
                Attacking();
                break;
        }
    }

    private void AutoArm()
    {
        Enemy closestEnemy = GetClosestEnemy();
        Vector2 targetUpVector = Vector3.up;
        if (closestEnemy != null)
        {
            targetUpVector = (closestEnemy.transform.position - transform.position).normalized;
            transform.up = targetUpVector;
            ManagerAttack();
        }

        transform.up = Vector3.Lerp(transform.up, targetUpVector, Time.deltaTime * armLerp);
        IncrementAttackTimer();
    }

    private void ManagerAttack()
    {
        if (attackTimer >= attackDelay)
        {
            attackTimer = 0;
            StartAttack();
        }
    }
    private void IncrementAttackTimer()
    {
        attackTimer += Time.deltaTime;
    }

    [NaughtyAttributes.Button]
    private void StartAttack()
    {
        animator.Play("Attack");
        state = State.Attack;

        animator.speed = 1f / attackDelay;

        PlayAttackSound();
    }

    private void Attacking()
    {
        Attack();
    }

    private void StopAttack()
    {
        state = State.Idle;
        damagedEnemies.Clear();
    }
    private void Attack()
    {
        Collider2D[] enemies = Physics2D.OverlapBoxAll
            (
                hitDetectionTransform.position,
                hitCollider.bounds.size,
                hitDetectionTransform.localEulerAngles.z,
                enemyMask
            );


        for (int i = 0; i < enemies.Length; i++)
        {
            Enemy enemy = enemies[i].GetComponent<Enemy>();

            if (enemy == null || enemy.IsDead())
                continue;

            if (!damagedEnemies.Contains(enemy))
            {
                int damage = GetDamage(out bool isCriticalHit);


                enemy.TakeDamage(damage, isCriticalHit);
                damagedEnemies.Add(enemy);
            }
        }

    }

    public override void UpdateStats(PlayerStatsManager playerStatsManager)
    {
        ConfigureStats();

        damage = Mathf.RoundToInt(damage * (1 + playerStatsManager.GetStatValue(Stat.Attack) / 100));
        attackDelay /= 1 + (playerStatsManager.GetStatValue(Stat.AttackSpeed) / 100);

        criticalChance = Mathf.RoundToInt(criticalChance * (1 + playerStatsManager.GetStatValue(Stat.CriticalChance) / 100));
        criticalPercent += playerStatsManager.GetStatValue(Stat.CriticalPercent);
    }
}
