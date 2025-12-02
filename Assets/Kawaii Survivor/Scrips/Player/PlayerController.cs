using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http.Headers;
using UnityEngine;
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour, IPlayerStatsDependency
{
    public ParticleSystem dashEffect;

    [Header("Elements")]
    private Rigidbody2D rig;
    private PlayerHealth playerHealth;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Dash Feedback")]
    [Tooltip("冲刺冷却完成时精灵闪烁时长（秒）")]
    [SerializeField] private float cooldownFlashDuration = 0.18f;
    [Tooltip("闪烁白化强度（0-1，1 为完全白）")]
    [Range(0f, 1f)]
    [SerializeField] private float cooldownFlashIntensity = 1f;
    private bool dashCooldownReadyNotified = true;

    [Header("Settings")]
    [SerializeField] private float baseMoveSpeed;
    private float moveSpeed;
    [Header("Compatibility")]
    [Tooltip("Global speed scale to tune movement feel without changing baseMoveSpeed values (use <1 to slow down).")]
    [SerializeField] private float speedScale = 1f;

    [Header("Dash")]
    [Tooltip("冲刺时使用的固定移动速度（用于覆盖普通移动速度），单位与 baseMoveSpeed 保持一致")]
    [SerializeField] private float dashSpeed = 12f;
    [Tooltip("冲刺持续时间（秒）")]
    [SerializeField] private float dashDuration = 0.4f;
    [Tooltip("冲刺冷却时间（秒）")]
    [SerializeField] private float dashCooldown = 2f;

    private bool isDashing = false;
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;
    private Vector2 dashDirection = Vector2.zero;
    private Vector2 lastNonZeroInput = Vector2.up;


    void Start()
    {
        rig = GetComponent<Rigidbody2D>();
        playerHealth = GetComponent<PlayerHealth>();
        dashCooldownTimer = 0f;
        // 初始通知状态：当冷却已就绪则标记为已通知
        dashCooldownReadyNotified = dashCooldownTimer <= 0f;
    }


    private void FixedUpdate()
    {
        Vector2 input = Vector2.zero;
        if (InputManager.instance != null)
            input = InputManager.instance.GetMoveVector();

        // 如果有有效输入则更新 lastNonZeroInput，用于无输入时冲刺方向回退
        if (input.sqrMagnitude > 0.0001f)
            lastNonZeroInput = input.normalized;

        float currentSpeed;
        Vector2 movementDir;

        if (isDashing)
        {
            currentSpeed = dashSpeed;
            movementDir = dashDirection.sqrMagnitude > 0.0001f ? dashDirection.normalized : lastNonZeroInput;
        }
        else
        {
            currentSpeed = moveSpeed;
            movementDir = input.sqrMagnitude > 0.0001f ? input.normalized : Vector2.zero;
        }

        // 直接设置 velocity（units/sec）。`speedScale` 用于快速调整整体速度平衡。
        rig.velocity = movementDir * currentSpeed * speedScale;

        // 处理Sprite翻转，实现左右转向效果
        if (movementDir.x < 0)
        {
            // 向左移动，翻转Sprite
            spriteRenderer.flipX = true;
        }
        else if (movementDir.x > 0)
        {
            // 向右移动，恢复正常方向
            spriteRenderer.flipX = false;
        }

        // Dash timer handled in Update (input/timing), but velocity applied here for physics consistency
    }

    private void Update()
    {
        HandleDashInput();
    }

    private void HandleDashInput()
    {
        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.deltaTime;
        // 冷却进行中重置通知标志
        if (dashCooldownTimer > 0f)
            dashCooldownReadyNotified = false;
        if (isDashing)
        {   
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
                EndDash();
        }
        else
        {
            if (dashCooldownTimer <= 0f && Input.GetKeyDown(KeyCode.LeftShift))
                StartDash();
            // 冷却刚完成，且还未通知，触发闪烁一次
            if (dashCooldownTimer <= 0f && !dashCooldownReadyNotified)
            {
                dashCooldownReadyNotified = true;
                PlayCooldownFlash();
            }
        }
    }

    private void PlayCooldownFlash()
    {
        if (spriteRenderer == null)
        {
            Debug.LogWarning("PlayerController: spriteRenderer 未设置，无法播放冷却闪烁。", this);
            return;
        }
        StopCoroutine(nameof(CooldownFlashCoroutine));
        StartCoroutine(nameof(CooldownFlashCoroutine));
    }

    private IEnumerator CooldownFlashCoroutine()
    {
        if (spriteRenderer == null)
            yield break;

        Color original = spriteRenderer.color;
        Color target = Color.Lerp(original, Color.black, cooldownFlashIntensity);
        float half = Mathf.Max(0.001f, cooldownFlashDuration * 0.5f);
        float t = 0f;

        // 渐变到较白
        while (t < half)
        {
            t += Time.deltaTime;
            spriteRenderer.color = Color.Lerp(original, target, t / half);
            yield return null;
        }

        // 渐变回原色
        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            spriteRenderer.color = Color.Lerp(target, original, t / half);
            yield return null;
        }

        spriteRenderer.color = original;
    }

    private void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        
        if (dashEffect == null)
        {
            Debug.LogWarning($"dashEffect not assigned on {name}", this);
        }
        else
        {
            if (!dashEffect.gameObject.activeInHierarchy)
                dashEffect.gameObject.SetActive(true);
            dashEffect.Clear();
            dashEffect.Play(true); // withChildren = true
            Debug.Log("Dash effect Play called", dashEffect.gameObject);
        }

        // 记录启动时的方向（优先当前输入，没有则使用上一次有效输入或朝向）
        Vector2 input = Vector2.zero;
        if (InputManager.instance != null)
            input = InputManager.instance.GetMoveVector();
        if (input.sqrMagnitude > 0.0001f)
            dashDirection = input.normalized;
        else if (lastNonZeroInput.sqrMagnitude > 0.0001f)
            dashDirection = lastNonZeroInput;
        else
            dashDirection = transform.up; // 回退：使用角色朝向

        if (playerHealth != null)
            playerHealth.SetInvincible(true);
    }

    private void EndDash()
    {
        isDashing = false;
        
        if (dashEffect != null)
        {
            // 停止发射但保留已发射粒子，或使用 StopEmittingAndClear 根据需要
            dashEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
        
        if (playerHealth != null)
        {
            playerHealth.SetInvincible(false);
        }
    }

    public void UpdateStats(PlayerStatsManager playerStatsManager)
    {
        float _moveSpeedPercent = playerStatsManager.GetStatValue(Stat.MoveSpeed) / 100;
        moveSpeed = baseMoveSpeed * (1 + _moveSpeedPercent);
    }
}
