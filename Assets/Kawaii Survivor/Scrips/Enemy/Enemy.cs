using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Enemy : MonoBehaviour
{
    [Header("Compenents")]
    protected EnemyMovement movement;

    [Header("Health")]
    [SerializeField] protected int maxHealth;
    protected int health;


    [Header("Elements")]
    protected Player player;

    [Header("Spawn Sequence Relasted")]
    [SerializeField] protected SpriteRenderer enenmyRenderer;
    [SerializeField] protected SpriteRenderer spawnIndicator;
    [SerializeField] protected Collider2D collider;
    protected bool hasSpawned;

    [Header("Effects")]
    [SerializeField] protected ParticleSystem passAwayParticles;
    [Header("Animator")]
    [SerializeField] protected Animator animator;


    [Header("Attack")]
    [SerializeField] protected float playerDetectionRadius;

    [Header("Actions")]
    public static Action<int, Vector2, bool> onDamageTaken;
    public static Action<Vector2> onPassAway;
    public static Action<Vector2> onBossPassAway;
    protected static Action onSpawnSequenceCompleted;

    [Header("Damage Reaction")]
    [SerializeField] protected float hurtStunDuration = 0.18f;
    [SerializeField] protected float knockbackForce = 3f;
    [SerializeField] protected float hurtPlayVelocityThreshold = 0.1f;

    // runtime state
    protected bool isDead = false;
    protected bool isHurt = false;

    [Header("DEBUG")]
    [SerializeField] protected bool gizmos;


    // Start is called before the first frame update
    protected virtual void Start()
    {
        health = maxHealth;
        movement = GetComponent<EnemyMovement>();
        player = FindFirstObjectByType<Player>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (player == null)
        {
            Debug.LogWarning("No player found, Auto-Destroying...");
            Destroy(gameObject);
        }

        StartSpawnSepuence();
    }

    // Update is called once per frame
    protected bool CanAttack()
    {
        return enenmyRenderer.enabled;
    }

    protected void StartSpawnSepuence()
    {
        SetRenderersVisibility(false);

        //Scale up & down the spawn indicator
        Vector2 targetScale = spawnIndicator.transform.localScale * 1.2f;
        LeanTween.scale(spawnIndicator.gameObject, targetScale, .3f)
            .setLoopPingPong(4)
            .setOnComplete(SpawnSequenceComplete);
    }

    private void SpawnSequenceComplete()
    {
        SetRenderersVisibility(true);
        hasSpawned = true;

        collider.enabled = true;

        if (movement != null)
            movement.StorePlayer(player);

        onSpawnSequenceCompleted?.Invoke();
    }

    private void SetRenderersVisibility(bool visibility)
    {
        enenmyRenderer.enabled = visibility;
        spawnIndicator.enabled = !visibility;
    }

    public void TakeDamage(int damage, bool isCriticalHit)
    {
        if (isDead)
            return;

        int realDamage = Mathf.Min(damage, health);
        health -= realDamage;

        onDamageTaken?.Invoke(damage, transform.position, isCriticalHit);

        if (health <= 0)
        {
            PassAway();
            return;
        }

        StartHurtReaction();
    }

    private void StartHurtReaction()
    {
        if (isDead || isHurt)
            return;

        StartCoroutine(HurtRoutine());
    }

    private System.Collections.IEnumerator HurtRoutine()
    {
        isHurt = true;

        // compute knockback direction away from player when possible
        Vector2 knockDir = Vector2.zero;
        if (player != null)
            knockDir = ((Vector2)transform.position - player.GetCenter()).normalized;

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null && knockDir.sqrMagnitude > 0.0001f)
        {
            rb.velocity = Vector2.zero;
            rb.AddForce(knockDir * knockbackForce, ForceMode2D.Impulse);
        }

        if (movement != null)
            movement.enabled = false;

        // play hurt if enemy is not currently moving fast
        if (animator != null)
        {
            float currentVel = rb != null ? rb.velocity.magnitude : 0f;
            if (currentVel <= hurtPlayVelocityThreshold)
                animator.Play("Hurt");
        }

        yield return new WaitForSeconds(hurtStunDuration);

        // end stun: restore movement and ensure animator returns to idle
        if (!isDead)
        {
            // stop residual knockback so idle/movement blending looks correct
            if (rb != null)
                rb.velocity = Vector2.zero;

            if (movement != null)
                movement.enabled = true;

            if (animator != null)
                animator.Play("Idle");
        }

        isHurt = false;
    }

    public virtual void PassAway()
    {
        if (isDead)
            return;

        isDead = true;

        onPassAway?.Invoke(transform.position);

        // 立刻停止移动与 AI 更新：禁用 movement（若有）、将刚体速度清零，并禁用此组件以停止子类的 Update/状态机
        if (movement != null)
            movement.enabled = false;

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.velocity = Vector2.zero;

        // 禁用此组件（这是派生类实例），以阻止派生类的 Update/FixedUpdate 等继续移动逻辑
        this.enabled = false;

        // 如果存在 Animator 并有 death 动画，则先播放再销毁
        if (animator != null)
        {
            animator.Play("Death");
            float deathLength = GetAnimationClipLength("death");
            if (deathLength > 0f)
            {
                StartCoroutine(PlayDeathAndDestroy(deathLength));
                return;
            }
        }

        PassAwayAfterWave();
    }

    private System.Collections.IEnumerator PlayDeathAndDestroy(float wait)
    {
        yield return new WaitForSeconds(wait);
        PassAwayAfterWave();
    }

    private float GetAnimationClipLength(string clipName)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return 0f;

        foreach (var clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name.ToLower().Contains(clipName.ToLower()))
                return clip.length;
        }

        return 0f;
    }

    public void PassAwayAfterWave()
    {
        if (passAwayParticles != null)
        {
            passAwayParticles.transform.SetParent(null);
            passAwayParticles.Play();
        }
        else
        {
            Debug.LogWarning($"passAwayParticles not assigned on '{gameObject.name}'. Destroying without particles.");
        }

        Destroy(gameObject);
    }

    public Vector2 GetCenter()
    {
        return (Vector2)transform.position + collider.offset;
    }

    public bool IsDead()
    {
        return isDead;
    }


    void OnDrawGizmos()
    {
        if (!gizmos)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, playerDetectionRadius);

    }
}
