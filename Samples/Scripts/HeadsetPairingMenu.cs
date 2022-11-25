using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EmotivUnityPlugin;
using System;
using WebSocket4Net;
using Newtonsoft.Json.Linq;
using System.Linq;

public class HeadsetPairingMenu : MonoBehaviour
{
    public GameObject headsetEntryPrefab;
    public Transform headsetList;

    public ContactQualityDisplay contactQualityDisplay;

    public static string connectingHeadset = "";

    List<Headset> previousData = new List<Headset>();

    void OnEnable()
    {
        Cortex.HeadsetQueryResult += OnHeadsetChanged;
        Cortex.ErrorRecieved += OnErrorMessageRecieved;

        TriggerHeadsetQuery();
    }
    private void OnDisable()
    {
        Cortex.HeadsetQueryResult -= OnHeadsetChanged;
        Cortex.ErrorRecieved -= OnErrorMessageRecieved;

        // destroy all headsets in list
        foreach (Transform child in headsetList)
            Destroy(child.gameObject);

        previousData = new List<Headset>();
        connectingHeadset = "";
    }

    // called by the event system when there is a change in the list of available headsets
    private void OnHeadsetChanged(List<Headset> headsets)
    {
        headsets = headsets.Where((Headset h) => !Cortex.HeadsetIsAlreadyInUse(h.headsetID)).ToList();

        // Only update display if incoming data is new
        if (!IsNewHeadsets(headsets))
            return;
        previousData = headsets;

        // destroy all headsets in list
        foreach (Transform child in headsetList)
            Destroy(child.gameObject);

        if (headsets.Count == 0)
        {
            print("No Headsets detected");
            return;
        }

        // loop through detected headsets and add them to the UI list
        foreach (var item in headsets)
        {
            // this should never happen, but is included in the example so it is here just in case
            if (item == null)
            {
                print("headset list item value was null");
                continue;
            }

            ConnectableHeadset newHeadset = Instantiate(headsetEntryPrefab, headsetList).GetComponent<ConnectableHeadset>();
            newHeadset.Init(item);
            if (item.headsetID == connectingHeadset)
                newHeadset.SetAsConnecting();
        }
    }

    bool IsNewHeadsets(List<Headset> headsets)
    {
        if(headsets.Count != previousData.Count)
        {
            previousData = headsets;
            return true;
        }

        for (int i = 0; i < headsets.Count; i++)
        {
            if (!headsets[i].Equals(previousData[i]))
            {
                previousData = headsets;
                return true;
            }
        }
        return false;
    }

    public void TriggerHeadsetQuery()
    {
        Cortex.QueryHeadsets();
    }

    void OnErrorMessageRecieved(ErrorMsgEventArgs args)
    {
        if(args.MethodName == "createSession")
        {
            // destroy all headsets in list
            foreach (Transform child in headsetList)
                Destroy(child.gameObject);

            previousData = new List<Headset>();
            connectingHeadset = "";
            TriggerHeadsetQuery();
        }
    }
}
