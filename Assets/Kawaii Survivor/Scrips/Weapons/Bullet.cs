using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Bullet : MonoBehaviour
{
    [Header("Elements")]
    private Rigidbody2D rig;
    private Collider2D collider;
    private RangeWeapon rangeWeapon;

    [Header("Settings")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private LayerMask enemyMark;
    private int damage;
    private bool isCriticalHit;
    private Enemy target;

    private void Awake()
    {
        rig = GetComponent<Rigidbody2D>();
        collider = GetComponent<Collider2D>();

        //LeanTween.delayedCall(gameObject , 5,()=> rangeEnemyAttack.ReleaseBullet(this));
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Configure(RangeWeapon rangeWeapon)
    {
        this.rangeWeapon = rangeWeapon;
    }

    public virtual void Shoot(int damage, Vector2 direction, bool isCriticalHit)
    {
        Invoke("Release", 1);

        this.damage = damage;
        this.isCriticalHit = isCriticalHit;

        transform.right = direction;
        rig.velocity = direction * moveSpeed;
    }

    public virtual void Reload()
    {
        target = null;

        rig.velocity = Vector2.zero;
        collider.enabled = true;
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if(target != null)
            return;

        if (IsInLayerMark(collider.gameObject.layer, enemyMark))
        {
            target = collider.GetComponent<Enemy>();
            if (target == null || target.IsDead())
                return;

            CancelInvoke();

            Attack(target);
            Release();
        }
    }

    protected void Release()
    {
        if(!gameObject.activeSelf)
            return;
        rangeWeapon.ReleaseBullet(this);
    }

    private void Attack(Enemy enemy)
    {
        enemy.TakeDamage(damage, isCriticalHit);
    }

    private bool IsInLayerMark(int layer, LayerMask layerMask)
    {
        return (layerMask.value & (1 << layer)) != 0;
    }



}
