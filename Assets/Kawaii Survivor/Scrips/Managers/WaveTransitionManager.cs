using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Random = UnityEngine.Random;

using NaughtyAttributes;

public class WaveTransitionManager : MonoBehaviour, IGameStateListener
{
    public static WaveTransitionManager instance;

    [Header("Player")]
    [SerializeField] private PlayerObjects playerObjects;

    [Header("Elements")]
    [SerializeField] private PlayerStatsManager playerStatsManager;
    [SerializeField] private GameObject upgradeContainersParent;
    [SerializeField] private UpgradeContainer[] upgradeContainers;

    [Header("Chest Related Stuff")]
    [SerializeField] private ChestObjectContainer chestContainerPrefab;
    [SerializeField] private Transform chestContainerParent;

    [Header("Settings")]
    private int chestCollected;

    private void Awake()
    {
        Chest.onCollected += ChestCollectedCallback;

        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    private void OnDestroy()
    {
        Chest.onCollected -= ChestCollectedCallback;

    }


    public void GameStateChangedCallback(GameState gameState)
    {
        switch (gameState)
        {
            case GameState.WAVETRANSITION:
                TryOpenChest();
                break;
        }
    }

    private void TryOpenChest()
    {
        // ensure upgrade UI hidden first so chest UI always appears on top and prevents overlap
        if (upgradeContainersParent != null)
            upgradeContainersParent.SetActive(false);

        chestContainerParent.Clear();

        if (chestCollected > 0)
            ShowObject();
        else
            ConfigureUpgradeContainers();

    }

    private System.Collections.IEnumerator ShowUpgradesWhenChestCleared()
    {
        // This coroutine is no longer used. Kept for compatibility but will immediately exit.
        yield break;
    }

    private void ShowObject()
    {
        chestCollected--;

        upgradeContainersParent.SetActive(false);

        ObjectDataSO[] objectDatas = ResourcesManager.Objects;
        ObjectDataSO randomObjectData = objectDatas[Random.Range(0, objectDatas.Length)];

        ChestObjectContainer containerInstance = Instantiate(chestContainerPrefab, chestContainerParent);
        containerInstance.Configure(randomObjectData);

        // Ensure the chest UI instance is removed immediately when player takes/recycles,
        // then continue the TryOpenChest flow so upgrades won't overlap the chest UI.
        containerInstance.TakeButton.onClick.AddListener(() => StartCoroutine(HandleChestClosedCoroutine(true, randomObjectData, containerInstance)));
        containerInstance.RecycleButton.onClick.AddListener(() => StartCoroutine(HandleChestClosedCoroutine(false, randomObjectData, containerInstance)));
    }

    private System.Collections.IEnumerator HandleChestClosedCoroutine(bool isTake, ObjectDataSO objectData, ChestObjectContainer containerInstance)
    {
        // perform the take or recycle action
        if (isTake)
            playerObjects.AddObject(objectData);
        else
            CurrencyManager.instance.AddCurrency(objectData.RecyclePrice);

        // destroy the UI instance so it's removed from screen
        if (containerInstance != null)
            Destroy(containerInstance.gameObject);

        // wait one frame to allow Destroy to process and Canvas to update
        yield return null;

        // continue flow: if there are more chests show next, otherwise show upgrades
        if (chestCollected > 0)
            ShowObject();
        else
            ConfigureUpgradeContainers();
    }

    [Button]
    private void ConfigureUpgradeContainers()
    {
        upgradeContainersParent.SetActive(true);

        for (int i = 0; i < upgradeContainers.Length; i++)
        {
            int randomIndex = Random.Range(0, Enum.GetValues(typeof(Stat)).Length);
            Stat stat = (Stat)Enum.GetValues(typeof(Stat)).GetValue(randomIndex);

            Sprite upgradeSprite = ResourcesManager.GetStatIcon(stat);

            string randomStatString = Enums.FormatStatName(stat);

            string buttonString;
            Action action = GetActionToPerform(stat, out buttonString);

            upgradeContainers[i].Configure(upgradeSprite, randomStatString, buttonString);

            upgradeContainers[i].Button.onClick.RemoveAllListeners();
            upgradeContainers[i].Button.onClick.AddListener(() => action?.Invoke());
            upgradeContainers[i].Button.onClick.AddListener(() => BonusSelectedCallback());

        }
    }

    private void BonusSelectedCallback()
    {
        GameManager.instance.WaveCompletedCallback();
    }

    private Action GetActionToPerform(Stat stat, out string buttonString)
    {
        buttonString = "";
        float value;
        switch (stat)
        {
            case Stat.Attack:
                value = Random.Range(1, 10);
                buttonString = "+" + value.ToString() + "%";
                break;
            case Stat.AttackSpeed:
                value = Random.Range(1, 10);
                buttonString = "+" + value.ToString() + "%";
                break;
            case Stat.CriticalChance:
                value = Random.Range(1, 10);
                buttonString = "+" + value.ToString() + "%";
                break;
            case Stat.CriticalPercent:
                value = Random.Range(1f, 2f);
                buttonString = "+" + value.ToString("F2") + "x";
                break;
            case Stat.MoveSpeed:
                value = Random.Range(1, 10);
                buttonString = "+" + value.ToString() + "%";
                break;
            case Stat.MaxHealth:
                value = Random.Range(1, 5);
                buttonString = "+" + value;
                break;
            case Stat.Range:
                value = Random.Range(1f, 5f);
                buttonString = "+" + value.ToString("F2");
                break;
            case Stat.HealthRecoverySpeed:
                value = Random.Range(1, 10);
                buttonString = "+" + value.ToString() + "%";
                break;
            case Stat.Armor:
                value = Random.Range(1, 10);
                buttonString = "+" + value.ToString() + "%";
                break;
            case Stat.Luck:
                value = Random.Range(1, 10);
                buttonString = "+" + value.ToString() + "%";
                break;
            case Stat.Dodge:
                value = Random.Range(1, 10);
                buttonString = "+" + value.ToString() + "%";
                break;
            case Stat.Lifesteal:
                value = Random.Range(1, 10);
                buttonString = "+" + value.ToString() + "%";
                break;
            default:
                return () => Debug.Log("Invalid stat");
        }

        //buttonString = Enums.FormatStatName(stat) + "\n" + buttonString;

        return () => playerStatsManager.AddPlayerStat(stat, value);
    }

    private void ChestCollectedCallback()
    {
        chestCollected++;

    }

    public bool HasCollectedChest()
    {
        return chestCollected > 0;
    }


}