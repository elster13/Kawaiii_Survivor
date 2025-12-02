using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cotton Candy Bullet
/// Behavior:
/// - 被发射后沿给定方向移动固定距离（position 从 start -> start + dir*distance）
/// - 移动使用 ease-out 插值，使速度由快到慢
/// - 到达目标后停止，开启伤害判定：对触碰到的敌人周期性造成伤害
/// - 停留若干秒（默认 3s）后自动销毁
/// </summary>
public class CottonCandyBullet : Bullet
{
    [Header("Motion")]
    [SerializeField] private float travelDistance = 4f;
    [SerializeField] private float travelDuration = 1.0f; // time to reach target (seconds) - increased for slower flight

    [Header("Lifetime")]
    [SerializeField] private float stayDuration = 3f; // stay time after stopping

    [Header("Damage")]
    [SerializeField] private int damagePerTick = 5;
    [SerializeField] private float damageTickInterval = 0.5f; // seconds per tick (not used for fireball)
    [SerializeField] private LayerMask enemyMask;

    [Header("Fireball (Impact)")]
    [SerializeField] private bool explodeOnImpact = true;
    [SerializeField] private float impactRadius = 1.5f;
    [SerializeField] private int impactDamage = 10;
    [SerializeField] private bool igniteOnHit = true;
    [SerializeField] private float igniteDuration = 3f;
    [SerializeField] private float igniteTickInterval = 1f;
    [SerializeField] private int igniteDamagePerTick = 2;
    [SerializeField] private bool persistDamageOnContact = true; // keep area damaging after explosion
    [SerializeField] private int contactDamage = 3; // damage applied when enemy touches the stopped fireball

    [Header("References")]
    [SerializeField] private Collider2D damageCollider; // should be trigger, disabled during travel
    [SerializeField] private ParticleSystem flightVfx;
    [SerializeField] private ParticleSystem impactVfx;
    [SerializeField] private TrailRenderer trail;
    [SerializeField] private SpriteRenderer spriteRenderer;

    // runtime
    private Vector3 startPos;
    private Vector3 targetPos;
    private bool hasLaunched = false;
    private HashSet<Enemy> enemiesInRange = new HashSet<Enemy>();
    private Coroutine damageCoroutine;
    private GameObject owner; // optional: avoid damaging owner
    private bool isReleased = false;
    private List<Coroutine> igniteCoroutines = new List<Coroutine>();

    // This method is called by RangeWeapon via virtual dispatch
    public override void Shoot(int damage, Vector2 direction, bool isCriticalHit)
    {
        if (hasLaunched)
            return;

        hasLaunched = true;
        // store incoming damage if needed (not used for periodic tick by default)
        // initialDamage = damage; // optional

        startPos = transform.position;
        targetPos = startPos + (Vector3)direction.normalized * travelDistance;

        // enable damage collider during flight
        if (damageCollider != null)
            damageCollider.enabled = true;

        // play flight VFX/trail
        if (flightVfx != null) flightVfx.Play();
        if (trail != null) trail.emitting = true;

        StartCoroutine(MoveRoutine());
    }

    private IEnumerator MoveRoutine()
    {
        float elapsed = 0f;
        while (elapsed < travelDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / travelDuration);
            // ease-out cubic: 1 - (1-t)^3
            float ease = 1f - Mathf.Pow(1f - t, 3f);
            transform.position = Vector3.Lerp(startPos, targetPos, ease);
            yield return null;
        }

        transform.position = targetPos;

        // arrived: explode at target position
        ExplodeAt(transform.position);

        // wait a short moment to let impact VFX play and lingering effects
        yield return new WaitForSeconds(0.15f + stayDuration);

        // stop any ignite coroutines
        foreach (var c in igniteCoroutines)
        {
            if (c != null)
                StopCoroutine(c);
        }
        igniteCoroutines.Clear();

        // return to pool
        Release();
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & enemyMask) == 0)
            return;

        Enemy enemy = collision.GetComponent<Enemy>();
        if (enemy == null)
            return;

        // ignore owner if provided
        if (owner != null && collision.gameObject == owner)
            return;

        // If still flying and hit enemy, explode on impact
        if (explodeOnImpact && !isReleased)
        {
            ExplodeAt(transform.position);
            // small delay handled in ExplodeAt before releasing
            return;
        }

        // If already exploded and configured to persist damage on contact, apply contact damage
        if (isReleased && persistDamageOnContact)
        {
            if (enemy != null && !enemy.IsDead())
            {
                enemy.TakeDamage(contactDamage, false);
                if (igniteOnHit)
                {
                    var c = StartCoroutine(IgniteEnemy(enemy));
                    igniteCoroutines.Add(c);
                }
            }
        }
    }

    public override void Reload()
    {
        // called when popped from pool
        base.Reload();

        // reset state
        hasLaunched = false;
        isReleased = false;
        enemiesInRange.Clear();

        if (damageCoroutine != null)
        {
            StopCoroutine(damageCoroutine);
            damageCoroutine = null;
        }

        if (damageCollider != null)
            damageCollider.enabled = false;

        if (flightVfx != null) flightVfx.Stop();
        if (impactVfx != null) impactVfx.Stop();
        if (trail != null) { trail.Clear(); trail.emitting = false; }
        if (spriteRenderer != null) spriteRenderer.color = Color.white;

        // ensure any lingering ignite coroutines are stopped and cleared
        foreach (var c in igniteCoroutines)
        {
            if (c != null)
                StopCoroutine(c);
        }
        igniteCoroutines.Clear();
    }

    private void ExplodeAt(Vector3 pos)
    {
        if (isReleased) return;
        isReleased = true;

        // play impact VFX
        if (impactVfx != null)
        {
            impactVfx.transform.position = pos;
            impactVfx.Play();
        }

        // optionally tint sprite briefly
        if (spriteRenderer != null)
            spriteRenderer.color = Color.red;

        // damage enemies in radius
        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, impactRadius, enemyMask);
        foreach (var col in hits)
        {
            var enemy = col.GetComponent<Enemy>();
            if (enemy == null || enemy.IsDead()) continue;

            enemy.TakeDamage(impactDamage, false);

            if (igniteOnHit)
            {
                Coroutine c = StartCoroutine(IgniteEnemy(enemy));
                igniteCoroutines.Add(c);
            }
        }

        // disable collider so it doesn't trigger again
        if (damageCollider != null)
        {
            // If we want persistent contact damage, keep collider enabled; otherwise disable
            damageCollider.enabled = persistDamageOnContact;
        }

        // stop flight VFX and trail
        if (flightVfx != null) flightVfx.Stop();
        if (trail != null) trail.emitting = false;
    }

    private IEnumerator IgniteEnemy(Enemy enemy)
    {
        float elapsed = 0f;
        while (elapsed < igniteDuration && enemy != null && !enemy.IsDead())
        {
            enemy.TakeDamage(igniteDamagePerTick, false);
            yield return new WaitForSeconds(igniteTickInterval);
            elapsed += igniteTickInterval;
        }
    }

    // optional helper to auto-launch when spawned (if desired)
    private void Start()
    {
        // no-op by default; user should call Launch(...) after instantiating
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(transform.position, transform.position + transform.up * travelDistance);
        Gizmos.DrawWireSphere(transform.position + transform.up * travelDistance, 0.2f);
    }
}
