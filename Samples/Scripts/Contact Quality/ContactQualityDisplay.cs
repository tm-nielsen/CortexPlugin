using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using EmotivUnityPlugin;
using TMPro;
using Text = TMPro.TextMeshProUGUI;

public class ContactQualityDisplay : MonoBehaviour
{
    public CQNodeSet[] displays;
    public CQNodeSet fallback;
    public Color[] nodeColours;
    public Text contactQualityPercentage;

    CQNodeSet activeDisplay = null;
    string headsetID;
    bool shouldFindDisplay;
    bool shouldSubscribeOnEnable;

    private void Awake()
    {
        foreach(var display in displays)
        {
            display.Init(nodeColours);
        }
        fallback.Init(nodeColours);
    }

    private void OnEnable()
    {
        if (shouldSubscribeOnEnable && Cortex.DataStreamExists(headsetID))
        {
            Cortex.SubscribeDeviceInfo(headsetID, OnDevDataRecieved);
            shouldSubscribeOnEnable = false;
        }
    }
    private void OnDisable()
    {
        if (Cortex.DataStreamExists(headsetID))
        {
            Cortex.UnsubscribeDeviceInfo(headsetID, OnDevDataRecieved);
            shouldSubscribeOnEnable = true;
        }
    }

    public void Activate(string id)
    {
        if (Cortex.DataStreamExists(headsetID))
            Cortex.UnsubscribeDeviceInfo(headsetID, OnDevDataRecieved);

        headsetID = id;
        Cortex.SubscribeDeviceInfo(headsetID, OnDevDataRecieved);
        activeDisplay = null;

        foreach (CQNodeSet nodeset in displays)
            nodeset.gameObject.SetActive(false);
        fallback.gameObject.SetActive(false);

        shouldFindDisplay = true;

        contactQualityPercentage.text = "00%";
    }

    void OnDevDataRecieved(DeviceInfo data)
    {
        FindApplicableDisplay(data);

        activeDisplay?.OnCQUpdate(data);
        contactQualityPercentage.text = $"{data.cqOverall}%";
    }

    void FindApplicableDisplay(DeviceInfo data)
    {
        if (!shouldFindDisplay)
            return;
        shouldFindDisplay = false;

        foreach(CQNodeSet display in displays)
        {
            if (display.CanDisplay(data.cqHeaders.ToList()))
            {
                activeDisplay?.gameObject.SetActive(false);
                display.gameObject.SetActive(true);
                activeDisplay = display;
                return;
            }
        }

        fallback.gameObject.SetActive(true);
        activeDisplay = fallback;
    }
}
