using System;
using System.Collections;
using System.Collections.Generic;
using Tabsil.Sijil;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour, IWantToBeSaved
{
    [Header("Elements")]
    [SerializeField] private Button sfxButton;
    [SerializeField] private Button musicButton;


    [Header("Settings")]
    [SerializeField] private Color onColor;
    [SerializeField] private Color offColor;

    [Header("Data")]
    private bool sfxState;
    private bool musicState;

    [Header("Actions")]
    public static Action<bool> onSFXStateChanged;
    public static Action<bool> onMusicStateChanged;

    private void Awake()
    {
        sfxButton.onClick.RemoveAllListeners();
        sfxButton.onClick.AddListener(SFXButtonCallback);

        musicButton.onClick.RemoveAllListeners();
        musicButton.onClick.AddListener(MusicButtonCallback);


    }

    // Start is called before the first frame update
    void Start()
    {

        onSFXStateChanged?.Invoke(sfxState);
        onMusicStateChanged?.Invoke(musicState);
    }

    // Update is called once per frame
    void Update()
    {

    }
    private void SFXButtonCallback()
    {
        sfxState = !sfxState;

        UpdateSFXVisuals();

        Save();

        onSFXStateChanged?.Invoke(sfxState);
    }

    private void UpdateSFXVisuals()
    {
        if (sfxState)
        {
            sfxButton.image.color = onColor;
            sfxButton.GetComponentInChildren<TextMeshProUGUI>().text = "ON";
        }
        else
        {
            sfxButton.image.color = offColor;
            sfxButton.GetComponentInChildren<TextMeshProUGUI>().text = "OFF";
        }
    }

    private void MusicButtonCallback()
    {
        musicState = !musicState;

        UpdateMusicVisuals();

        Save();

        onMusicStateChanged?.Invoke(musicState);

    }

    private void UpdateMusicVisuals()
    {
        if (musicState)
        {
            musicButton.image.color = onColor;
            musicButton.GetComponentInChildren<TextMeshProUGUI>().text = "ON";
        }
        else
        {
            musicButton.image.color = offColor;
            musicButton.GetComponentInChildren<TextMeshProUGUI>().text = "OFF";
        }
    }

    private void PrivacyPolicyButtonCallback()
    {
        Application.OpenURL("https://www.youtube.com/");
    }

    private void AskButtonCallback()
    {
        string email = "ptg3012@gmail.com";
        string subject = MyEscapeUrl("Help");
        string body = MyEscapeUrl("Hey! I need help with this ....");

        Application.OpenURL("mailto:" + email + "?subject=" + subject + "&body=" + body);
    }
    private string MyEscapeUrl(string s)
    {
        return UnityWebRequest.EscapeURL(s).Replace("+", "%20");
    }


    public void Load()
    {
        sfxState = true;
        musicState = true;

        if (Sijil.TryLoad(this, "sfx", out object sfxStateObject))
            sfxState = (bool)sfxStateObject;
        if (Sijil.TryLoad(this, "music", out object musicStateObject))
            musicState = (bool)musicStateObject;

        UpdateSFXVisuals();
        UpdateMusicVisuals();
    }

    public void Save()
    {
        Sijil.Save(this, "sfx", sfxState);
        Sijil.Save(this, "music", musicState);
    }
}
