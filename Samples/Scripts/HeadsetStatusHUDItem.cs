using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EmotivUnityPlugin;

public class HeadsetStatusHUDItem : MonoBehaviour
{
    public Gradient colourGradient;
    [Header("references")]
    public Image batteryIndicator;
    public TextMeshProUGUI contactQualityText;
    public Image commandStrengthIndicator;
    [Header("secondary references")]
    public Image contactQualityBackground;
    public Image batteryBackground;

    [HideInInspector]
    public string headsetID;
    [HideInInspector]
    public string profileName;

    public Color BackgroundColor
    {
        set { GetComponent<Image>().color = value; }
    }

    System.Action<string, string, bool> ActivateDetailedDisplay;


    public void Init(string headset, System.Action<string, string, bool> activateDetailedDisplay)
    {
        headsetID = headset;
        Cortex.SubscribeDeviceInfo(headset, OnDeviceInfoReceived);
        Cortex.SubscribeMentalCommands(headset, OnMentalCommandReceived);
        ProfileMenu.ProfileLoadedToHeadset += OnProfileLoadedToHeadset;
        ProfileMenu.ProfileUnloadedFromHeadset += OnProfileUnloadedFromHeadset;

        ActivateDetailedDisplay = activateDetailedDisplay;
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    private void OnDestroy()
    {
        if (Cortex.DataStreamExists(headsetID))
        {
            Cortex.UnsubscribeDeviceInfo(headsetID, OnDeviceInfoReceived);
            Cortex.UnsubscribeMentalCommands(headsetID, OnMentalCommandReceived);
        }

        ProfileMenu.ProfileLoadedToHeadset -= OnProfileLoadedToHeadset;
        ProfileMenu.ProfileUnloadedFromHeadset -= OnProfileUnloadedFromHeadset;
    }

    void OnDeviceInfoReceived(DeviceInfo data)
    {
        DisplayContactQuality(data.cqOverall);
        DisplayBatteryLevel(data.battery);
    }

    void DisplayContactQuality(float quality)
    {
        contactQualityText.text = $"{quality}";

        Color cqColour = colourGradient.Evaluate(quality / 100);
        contactQualityText.color = cqColour;
        contactQualityBackground.color = cqColour;
    }

    void DisplayBatteryLevel(float battery)
    {
        batteryIndicator.fillAmount = battery / 100;

        Color battColour = colourGradient.Evaluate(battery / 100);
        batteryIndicator.color = battColour;
        batteryBackground.color = battColour;
    }

    void OnMentalCommandReceived(MentalCommand command)
    {
        commandStrengthIndicator.fillAmount = (float)command.power;
    }

    void OnClick() => ActivateDetailedDisplay(headsetID, profileName, true);

    void OnProfileLoadedToHeadset(object sender, (string, string) args) => OnProfileChange(args.Item2, args.Item1);
    void OnProfileUnloadedFromHeadset(object sender, (string, string) args) => OnProfileChange(args.Item2, null);
    void OnProfileChange(string headset, string profile)
    {
        if (headset == headsetID)
        {
            profileName = profile;
            ActivateDetailedDisplay(headsetID, profileName, false);
        }
    }
}
