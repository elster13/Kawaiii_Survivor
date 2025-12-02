using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponPosition : MonoBehaviour
{
    public Weapon Weapon { get; private set; }
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    [Header("Mount Offset")]
    [SerializeField] private Vector3 localPositionOffset = Vector3.zero;
    [SerializeField] private Vector3 localEulerOffset = Vector3.zero;
    [SerializeField] private Vector3 localScale = Vector3.one;

    public void AssignWeapon(Weapon weapon, int weaponLevel)
    {
        Weapon = Instantiate(weapon, transform);

        // Apply configurable local offsets so the Prefab's local origin can be adjusted
        Weapon.transform.localPosition = localPositionOffset;
        Weapon.transform.localRotation = Quaternion.Euler(localEulerOffset);
        Weapon.transform.localScale = localScale;

        Weapon.UpgradeTo(weaponLevel);
    }

    public void RemoveWeapon()
    {
        Destroy(Weapon.gameObject);
        Weapon = null;
    }
}
