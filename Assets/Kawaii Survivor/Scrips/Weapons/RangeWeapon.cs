using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using System;

public class RangeWeapon : Weapon
{
    [Header("Elements")]
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private Transform shootingPoint;

    [Header("Pooling")]
    private ObjectPool<Bullet> bulletPool;

    [Header("Actions")]
    public static Action onBulletShot;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    // runtime debug
    private Enemy lastTarget = null;

    // Start is called before the first frame update
    void Start()
    {
        if (bulletPrefab == null)
        {
            Debug.LogError("RangeWeapon: bulletPrefab is not assigned on " + gameObject.name + ". Assign a Bullet prefab in the inspector.");
            this.enabled = false;
            return;
        }

        bulletPool = new ObjectPool<Bullet>(CreateFunction, ActionOnGet, ActionOnRelease, ActionOnDestroy);
    }

    // Update is called once per frame
    void Update()
    {
        AutoArm();
    }

    private Bullet CreateFunction()
    {
        Bullet bulletInstance = Instantiate(bulletPrefab, shootingPoint.position, Quaternion.identity);
        bulletInstance.Configure(this);

        return bulletInstance;
    }
    private void ActionOnGet(Bullet bullet)
    {
        bullet.Reload();
        bullet.transform.position = shootingPoint.position;
        bullet.gameObject.SetActive(true);
    }

    private void ActionOnRelease(Bullet bullet)
    {
        bullet.gameObject.SetActive(false);
    }

    private void ActionOnDestroy(Bullet bullet)
    {
        Destroy(bullet.gameObject);
    }

    public void ReleaseBullet(Bullet bullet)
    {
        bulletPool.Release(bullet);
    }

    private void AutoArm()
    {
        Enemy closestEnemy = GetClosestEnemy();
        Vector2 targetUpVector = Vector3.up;
        if (closestEnemy != null)
        {
            targetUpVector = (closestEnemy.GetCenter() -(Vector2) transform.position).normalized;
            transform.up = targetUpVector;

            // debug: detect when we have a target vs not
            if (debugLogs && lastTarget != closestEnemy)
            {
                Debug.Log($"[RangeWeapon:{gameObject.name}] Acquired target: {closestEnemy.name}");
            }
            lastTarget = closestEnemy;

            ManagerShooting();
            return;
        }
        // no target
        if (debugLogs && lastTarget != null)
        {
            Debug.Log($"[RangeWeapon:{gameObject.name}] Lost target");
            lastTarget = null;
        }

        transform.up = Vector3.Lerp(transform.up, targetUpVector, Time.deltaTime * armLerp);

    }

    private void ManagerShooting()
    {
        attackTimer += Time.deltaTime;

        if (attackTimer >= attackDelay)
        {
            attackTimer = 0;
            Shoot();
        }
    }
    private void Shoot()
    {
        int damage = GetDamage(out bool isCriticalHit);

        Bullet bulletInstance = bulletPool.Get();
        bulletInstance.Shoot(damage, transform.up, isCriticalHit);

        if (debugLogs)
            Debug.Log($"[RangeWeapon:{gameObject.name}] Shoot called (damage={damage}, crit={isCriticalHit})");

        onBulletShot?.Invoke();

        PlayAttackSound();
    }

    public override void UpdateStats(PlayerStatsManager playerStatsManager)
    {
        ConfigureStats();

        damage = Mathf.RoundToInt(damage * (1 + playerStatsManager.GetStatValue(Stat.Attack) / 100));
        attackDelay /= 1 + (playerStatsManager.GetStatValue(Stat.AttackSpeed) / 100);

        criticalChance = Mathf.RoundToInt(criticalChance * (1 + playerStatsManager.GetStatValue(Stat.CriticalChance) / 100));
        criticalPercent += playerStatsManager.GetStatValue(Stat.CriticalPercent);

        range += playerStatsManager.GetStatValue(Stat.Range) / 10;
    }
}
