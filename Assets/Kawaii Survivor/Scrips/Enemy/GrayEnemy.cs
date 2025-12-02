using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EnemyMovement))]
public class GrayEnemy : Enemy
{
    enum State { Idle, Move, Warning, Sprint, Cooldown }

    [Header("Sprint Attack")]
    [SerializeField] private float attackRange = 3f;
    [SerializeField] private float warningDuration = 0.6f;
    [SerializeField] private float sprintForce = 18f;
    [SerializeField] private float sprintDuration = 0.4f;
    [SerializeField] private float sprintCooldown = 0.6f;
    [SerializeField] private int damage = 1;
    [SerializeField] private float hitRadius = 0.8f;

    private State state = State.Idle;
    private Rigidbody2D rb;
    private Color originalColor = Color.white;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    protected override void Start()
    {
        base.Start();

        if (enenmyRenderer == null)
            enenmyRenderer = GetComponentInChildren<SpriteRenderer>();

        if (enenmyRenderer != null)
            originalColor = enenmyRenderer.color;
    }

    void Update()
    {
        if (!CanAttack() || isDead || isHurt)
            return;

        float distToPlayer = Vector2.Distance(transform.position, player.transform.position);

        // If player is within attack range and we're in a state that can start an attack, begin sequence
        if (distToPlayer <= attackRange && (state == State.Idle || state == State.Move))
        {
            StartCoroutine(WarningThenSprint());
            return;
        }

        // Normal movement/animation
        if (movement != null && state == State.Idle)
        {
            // follow player if we're not idling in-place
            movement.FollowPlayer();
            animator?.Play("Move");
            state = State.Move;
        }

        if (state == State.Move)
        {
            // keep moving each frame and face the player horizontally
            animator?.Play("Move");
            if (movement != null)
                movement.FollowPlayer();

            if (enenmyRenderer != null && player != null)
                enenmyRenderer.flipX = player.transform.position.x < transform.position.x;
        }
    }

    private IEnumerator WarningThenSprint()
    {
        state = State.Warning;

        // stop standard movement
        if (movement != null)
            movement.enabled = false;

        // play idle (brief pause) and tint red as warning
        animator?.Play("Idle");
        if (enenmyRenderer != null)
            enenmyRenderer.color = Color.red;

        yield return new WaitForSeconds(warningDuration);

        // restore color and sprint
        if (enenmyRenderer != null)
            enenmyRenderer.color = originalColor;

        state = State.Sprint;
        animator?.Play("Run");

        // compute direction toward player's center at time of sprint start
        Vector2 dir = Vector2.zero;
        if (player != null)
            dir = ((Vector2)player.GetCenter() - (Vector2)transform.position).normalized;

        // flip sprite to face sprint direction
        if (enenmyRenderer != null && Mathf.Abs(dir.x) > 0.0001f)
            enenmyRenderer.flipX = dir.x < 0f;

        if (rb != null && dir.sqrMagnitude > 0.0001f)
        {
            rb.velocity = Vector2.zero;
            rb.AddForce(dir * sprintForce, ForceMode2D.Impulse);
        }
        else if (movement != null)
        {
            // fallback: move transform directly for duration (we'll check hits inside the loop below)
        }

        // during sprint, check each frame for hitting the player (only once per sprint)
        bool dealtDamage = false;
        float sprintElapsed = 0f;
        while (sprintElapsed < sprintDuration)
        {
            // if player within hit radius, deal damage once
            if (!dealtDamage && player != null)
            {
                float d = Vector2.Distance(transform.position, player.GetCenter());
                if (d <= hitRadius)
                {
                    player.TakeDamage(damage);
                    dealtDamage = true;
                }
            }

            // fallback transform movement if no rigidbody
            if (rb == null && movement != null && player != null)
            {
                transform.position = Vector3.MoveTowards(transform.position, (Vector3)player.transform.position, (sprintForce * Time.deltaTime));
            }

            sprintElapsed += Time.deltaTime;
            yield return null;
        }

        // stop any residual movement
        if (rb != null)
            rb.velocity = Vector2.zero;

        // end sprint
        state = State.Cooldown;
        if (movement != null)
            movement.enabled = true;

        animator?.Play("Idle");

        yield return new WaitForSeconds(sprintCooldown);

        state = State.Idle;
    }
}
