using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Random = UnityEngine.Random;

public class PlayerHealth : MonoBehaviour, IPlayerStatsDependency
{
    [Header("Settings")]
    [SerializeField] private int baseMaxHealth;
    private float maxHealth;
    private float health;
    private float armor;
    private float lifeSteal;
    private float dodge;
    private float healthRecoverySpeed;
    private float healthRecoverySpeedTimer;
    private float healthRecoverySpeedDuration;
    private bool isInvincible = false;


    [Header("Elements")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI healthText;

    [Header("Actions")]
    public static Action<Vector2> onAttackDodge;
    public static Action<int, Vector2> onPlayerDamaged;

    private void Awake()
    {
        Enemy.onDamageTaken += EnemyTookDamageCallback;
    }

    private void OnDestroy()
    {
        Enemy.onDamageTaken = EnemyTookDamageCallback;
    }

    private void EnemyTookDamageCallback(int damage, Vector2 enemyPos, bool isCriticalHit)
    {
        if (health >= maxHealth)
            return;

        float lifeStealValue = damage * lifeSteal;
        float healthToAdd = Math.Min(lifeStealValue, (maxHealth - health));

        health += healthToAdd;
        UpdateUI();
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    private void Update()
    {
        if (health < maxHealth)
        {
            RecoverHealth();
        }
    }

    private void RecoverHealth()
    {
        healthRecoverySpeedTimer += Time.deltaTime;

        if (healthRecoverySpeedTimer >= healthRecoverySpeedDuration)
        {
            healthRecoverySpeedTimer = 0;
            float healthToAdd = Mathf.Min(.1f, maxHealth - health);

            health += healthToAdd;
            UpdateUI(); 
        }
    }

    public void TakeDamage(int damage)
    {
        if (isInvincible)
            return;

        if (ShouldDodge())
        {
            onAttackDodge?.Invoke(transform.position);
            return;
        }

        float realDamage = damage * Mathf.Clamp(1 - (armor / 1000), 0, 1000);
        realDamage = Mathf.Min(realDamage, health);
        health -= realDamage;

        // notify listeners to show floating damage on player
        onPlayerDamaged?.Invoke((int)realDamage, transform.position);

        UpdateUI();

        if (health <= 0) PassAway();
    }

    public void SetInvincible(bool inv)
    {
        isInvincible = inv;
    }

    private bool ShouldDodge()
    {
        return Random.Range(0f, 101f) < dodge;
    }

    private void PassAway()
    {
        GameManager.instance.SetGameState(GameState.GAMEOVER);
    }

    private void UpdateUI()
    {
        float healthBarValue = health / maxHealth;
        healthSlider.value = healthBarValue;
        healthText.text = (int)health + " / " + maxHealth;
    }

    public void UpdateStats(PlayerStatsManager playerStatsManager)
    {
        float addedHealth = playerStatsManager.GetStatValue(Stat.MaxHealth);
        maxHealth = baseMaxHealth + (int)addedHealth;
        maxHealth = Mathf.Max(maxHealth, 1);

        health = maxHealth;
        UpdateUI();

        armor = playerStatsManager.GetStatValue(Stat.Armor);
        lifeSteal = playerStatsManager.GetStatValue(Stat.Lifesteal) / 100;
        dodge = playerStatsManager.GetStatValue(Stat.Dodge);

        healthRecoverySpeed = Mathf.Max(.0001f,playerStatsManager.GetStatValue(Stat.HealthRecoverySpeed));
        healthRecoverySpeedDuration = 1f / healthRecoverySpeed;
    }
}