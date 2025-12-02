using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class DamageTextManager : MonoBehaviour
{
    [Header("Elements")]
    [SerializeField] private DamageText damageTextPrefab;

    [Header("Pooling")]
    private ObjectPool<DamageText> damageTextPool;


    private void Awake()
    {
        Enemy.onDamageTaken += EnemyHitCallBack;
        PlayerHealth.onAttackDodge += AttackDodgeCallback;
        PlayerHealth.onPlayerDamaged += PlayerDamagedCallback;
    }

    private void OnDestroy()
    {
        Enemy.onDamageTaken -= EnemyHitCallBack;
        PlayerHealth.onAttackDodge -= AttackDodgeCallback;
        PlayerHealth.onPlayerDamaged -= PlayerDamagedCallback;
    }

    // Start is called before the first frame update
    void Start()
    {
        damageTextPool = new ObjectPool<DamageText>(CreateFunction, ActionOnGet, ActionOnRelease, ActionOnDestroy);
    }

    private DamageText CreateFunction()
    {
        return Instantiate(damageTextPrefab, transform);
    }
    private void ActionOnGet(DamageText damageText)
    {
        damageText.gameObject.SetActive(true);
    }

    private void ActionOnRelease(DamageText damageText)
    {
        if(damageText != null)
            damageText.gameObject.SetActive(false);
    }

    private void ActionOnDestroy(DamageText damageText)
    {
        Destroy(damageText.gameObject);
    }

    private void EnemyHitCallBack(int damage, Vector2 enemyPos, bool isCriticalHit)
    {
        DamageText damageTextInstance = damageTextPool.Get();

        Vector3 spawnPosition = enemyPos + Vector2.up * 1.5f;
        damageTextInstance.transform.position = spawnPosition;

        damageTextInstance.Animate(damage.ToString(), isCriticalHit);

        LeanTween.delayedCall(1, () => damageTextPool.Release(damageTextInstance));
    }

    private void AttackDodgeCallback(Vector2 playerPosition)
    {
        DamageText damageTextInstance = damageTextPool.Get();

        Vector3 spawnPosition = playerPosition + Vector2.up * 1.5f;
        damageTextInstance.transform.position = spawnPosition;

        damageTextInstance.Animate("Dodged",false);

        LeanTween.delayedCall(1, () => damageTextPool.Release(damageTextInstance));
    }

    private void PlayerDamagedCallback(int damage, Vector2 playerPos)
    {
        DamageText damageTextInstance = damageTextPool.Get();

        Vector3 spawnPosition = playerPos + Vector2.up * 1.5f;
        damageTextInstance.transform.position = spawnPosition;

        damageTextInstance.Animate(damage.ToString(), false, Color.red);

        LeanTween.delayedCall(1, () => damageTextPool.Release(damageTextInstance));
    }
}
