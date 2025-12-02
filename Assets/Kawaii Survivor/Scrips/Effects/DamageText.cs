using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DamageText : MonoBehaviour
{
    [Header("Elements")]
    [SerializeField] private Animator animator;
    [SerializeField] private TextMeshPro damageText;

    [NaughtyAttributes.Button]
    public void Animate(string damage, bool isCriticalHit, Color? forcedColor = null)
    {
        damageText.text = damage.ToString();
        if (isCriticalHit)
            damageText.color = Color.yellow;
        else
            damageText.color = forcedColor ?? Color.white;

        animator.Play("Animate");
    }
}
