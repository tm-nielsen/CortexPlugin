using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using CortexPlugin;
using UnityEngine.UI;

namespace CortexExamples
{
    public class HeadsetManager : MonoBehaviour
    {
        public TextMeshProUGUI headsetListText;

        Dictionary<string, Headset> availableHeadsets;


        void Start()
        {
            Cortex.HeadsetQueryResult += OnHeadsetQueryResult;
        }

        void OnHeadsetQueryResult(List<Headset> headsets)
        {
            availableHeadsets = new Dictionary<string, Headset>();
            string headsetList = "";

            foreach (Headset h in headsets)
            {
                availableHeadsets[h.headsetID] = h;
                headsetList += h.headsetID + "\n";
            }

            headsetListText.text = headsetList;
        }

        // called by UI
        public void PairWithHeadset(string headsetId)
        {
            if (availableHeadsets.ContainsKey(headsetId))
            {
                if (availableHeadsets[headsetId].status == "connected")
                {
                    Cortex.StartSession(headsetId);
                }
                else
                {
                    Cortex.ConnectDevice(headsetId);
                    Cortex.HeadsetConnected += PairWithNewlyConnectedHeadset;
                }
            }
            else
                Debug.LogWarning("Attempted to pair with a headset that is not available");
        }

        void PairWithNewlyConnectedHeadset(HeadsetConnectEventArgs args)
        {
            Cortex.StartSession(args.HeadsetId);
            Cortex.HeadsetConnected -= PairWithNewlyConnectedHeadset;
        }
    }
}
