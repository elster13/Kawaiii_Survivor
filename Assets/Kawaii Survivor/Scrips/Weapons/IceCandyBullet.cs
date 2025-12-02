using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IceCandyBullet : Bullet
{
    [Header("Motion")]
    [SerializeField] private float travelDistance = 4f;
    [SerializeField] private float travelDuration = 0.8f;

    [Header("Impact")]
    [SerializeField] private float impactRadius = 1.5f;
    [SerializeField] private int impactDamage = 6;
    [SerializeField] private float freezeDuration = 2.5f; // seconds to disable enemy movement
    [SerializeField] private bool freezeOnHit = true;

    [Header("Contact")]
    [SerializeField] private bool persistFreezeOnContact = true;
    [SerializeField] private int contactDamage = 2;

    [Header("References")]
    [SerializeField] private Collider2D damageCollider;
    [SerializeField] private ParticleSystem flightVfx;
    [SerializeField] private ParticleSystem impactVfx;
    [SerializeField] private TrailRenderer trail;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Vector3 startPos;
    private Vector3 targetPos;
    private bool hasLaunched = false;
    private bool hasExploded = false;
    private GameObject owner;
    private List<Coroutine> activeFreezes = new List<Coroutine>();

    public override void Shoot(int damage, Vector2 direction, bool isCriticalHit)
    {
        if (hasLaunched) return;
        hasLaunched = true;

        startPos = transform.position;
        targetPos = startPos + (Vector3)direction.normalized * travelDistance;

        if (damageCollider != null) damageCollider.enabled = true;
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
            float ease = 1f - Mathf.Pow(1f - t, 3f);
            transform.position = Vector3.Lerp(startPos, targetPos, ease);
            yield return null;
        }

        transform.position = targetPos;
        ExplodeAt(transform.position);

        // remain for a bit to allow contact freezing if configured
        yield return new WaitForSeconds(0.15f + 1.5f);

        // clear active freezes if any
        foreach (var c in activeFreezes)
            if (c != null) StopCoroutine(c);
        activeFreezes.Clear();

        Release();
    }

    private void ExplodeAt(Vector3 pos)
    {
        if (hasExploded) return;
        hasExploded = true;

        if (impactVfx != null) { impactVfx.transform.position = pos; impactVfx.Play(); }
        if (spriteRenderer != null) spriteRenderer.color = new Color(0.6f, 0.9f, 1f);

        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, impactRadius);
        foreach (var col in hits)
        {
            var enemy = col.GetComponent<Enemy>();
            if (enemy == null || enemy.IsDead()) continue;

            enemy.TakeDamage(impactDamage, false);

            if (freezeOnHit)
            {
                var em = enemy.GetComponent<EnemyMovement>();
                if (em != null)
                {
                    Coroutine c = StartCoroutine(FreezeRoutine(em, freezeDuration));
                    activeFreezes.Add(c);
                }
            }
        }

        if (damageCollider != null)
            damageCollider.enabled = persistFreezeOnContact;

        if (flightVfx != null) flightVfx.Stop();
        if (trail != null) trail.emitting = false;
    }

    private IEnumerator FreezeRoutine(EnemyMovement em, float duration)
    {
        if (em == null) yield break;
        em.enabled = false;
        yield return new WaitForSeconds(duration);
        if (em != null)
            em.enabled = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        var enemy = collision.GetComponent<Enemy>();
        if (enemy == null) return;

        if (!hasExploded)
        {
            // impact while flying
            ExplodeAt(transform.position);
            return;
        }

        // after exploded, apply contact damage and optional freeze
        if (hasExploded && persistFreezeOnContact)
        {
            if (!enemy.IsDead())
            {
                enemy.TakeDamage(contactDamage, false);
                if (freezeOnHit)
                {
                    var em = enemy.GetComponent<EnemyMovement>();
                    if (em != null)
                    {
                        Coroutine c = StartCoroutine(FreezeRoutine(em, freezeDuration));
                        activeFreezes.Add(c);
                    }
                }
            }
        }
    }

    public override void Reload()
    {
        base.Reload();
        hasLaunched = false;
        hasExploded = false;
        if (damageCollider != null) damageCollider.enabled = false;
        if (flightVfx != null) flightVfx.Stop();
        if (impactVfx != null) impactVfx.Stop();
        if (trail != null) { trail.Clear(); trail.emitting = false; }
        if (spriteRenderer != null) spriteRenderer.color = Color.white;

        foreach (var c in activeFreezes)
            if (c != null) StopCoroutine(c);
        activeFreezes.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, impactRadius);
    }
}
