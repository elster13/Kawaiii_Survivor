using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes.Test;
using UnityEngine;

public abstract class Weapon : MonoBehaviour, IPlayerStatsDependency
{
    [field: SerializeField] public WeaponDataSO WeaponData { get; private set; }

    [Header("Settings")]
    [SerializeField] protected float range;
    [SerializeField] protected LayerMask enemyMask;

    [Header("Attacks")]
    [SerializeField] protected int damage;
    [SerializeField] protected float attackDelay;
    [SerializeField] protected Animator animator;

    [Header("Audio")]
    protected AudioSource audioSource;


    protected float attackTimer;

    [Header("Critical")]
    protected int criticalChance;
    protected float criticalPercent;

    [field: Header("Level")]
    public int Level { get; private set; }


    [Header("Animations")]
    [SerializeField] protected float armLerp;

    protected void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.clip = WeaponData.AttackSound;

        if (animator != null && WeaponData.AnimatorOverride != null)
            animator.runtimeAnimatorController = WeaponData.AnimatorOverride;
    }

    protected void PlayAttackSound()
    {
        if (!AudioManager.instance.IsSfxOn)
            return;

        audioSource.pitch = Random.Range(.95f, 1.05f);
        audioSource.Play();
    }

    protected Enemy GetClosestEnemy()
    {
        Enemy closestEnemy = null;
        Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, range, enemyMask);

        if (enemies.Length <= 0)
            return null;


        float minDistance = range;

        for (int i = 0; i < enemies.Length; i++)
        {
            Enemy enmyCheck = enemies[i].GetComponent<Enemy>();
            if (enmyCheck == null || enmyCheck.IsDead())
                continue;

            float distanceToEnemy = Vector2.Distance(transform.position, enmyCheck.transform.position);

            if (distanceToEnemy < minDistance)
            {
                closestEnemy = enmyCheck;
                minDistance = distanceToEnemy;
            }
        }

        return closestEnemy;
    }

    protected int GetDamage(out bool isCriticalHit)
    {
        isCriticalHit = false;

        if (Random.Range(0, 101) <= criticalChance)
        {
            isCriticalHit = true;
            return Mathf.RoundToInt(damage * criticalPercent);
        }

        return damage;
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, range);
    }

    public abstract void UpdateStats(PlayerStatsManager playerStatsManager);

    protected void ConfigureStats()
    {
        Dictionary<Stat, float> calculatorStats = WeaponStatsCalculator.GetStats(WeaponData, Level);


        damage = Mathf.RoundToInt(calculatorStats[Stat.Attack]);
        attackDelay = 1f / calculatorStats[Stat.AttackSpeed];
        criticalChance = Mathf.RoundToInt(calculatorStats[Stat.CriticalChance]);
        criticalPercent = calculatorStats[Stat.CriticalPercent];
        range = calculatorStats[Stat.Range];
    }

    public void UpgradeTo(int targetLevel)
    {
        Level = targetLevel;
        ConfigureStats();
    }

    public int GetRecyclePrice()
    {
        return WeaponStatsCalculator.GetRecyclePrice(WeaponData, Level);
    }
    public void Upgrade() => UpgradeTo(Level + 1);

}
