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

        public float refreshPeriod = 1;

        Dictionary<string, Headset> availableHeadsets;
        string pairingHeadset = "";

        float refreshTimer;

        void Start()
        {
            Cortex.Authorized += OnAuthorize;
            Cortex.HeadsetQueryResult += OnHeadsetQueryResult;
            Cortex.DeviceConnected += PairWithNewlyConnectedHeadset;

            // disable the list until we can query headsets
            gameObject.SetActive(false);
        }

        void OnAuthorize(License license)
        {
            Cortex.QueryHeadsets();
            gameObject.SetActive(true);
        }

        void Update()
        {
            if (refreshPeriod <= 0)
                return;

            refreshTimer += Time.deltaTime;
            if(refreshTimer > refreshPeriod)
            {
                refreshTimer -= refreshPeriod;
                Cortex.QueryHeadsets();
            }
        }

        void OnHeadsetQueryResult(List<Headset> headsets)
        {
            availableHeadsets = new Dictionary<string, Headset>();
            string headsetList = "";

            foreach (Headset h in headsets)
            {
                availableHeadsets[h.headsetId] = h;
                headsetList += h.headsetId + "\n";
            }

            headsetListText.text = headsetList;
        }

        // called by UI
        public void PairWithHeadset(string headsetId)
        {
            headsetId = headsetId.ToUpper();
            if (availableHeadsets.ContainsKey(headsetId))
            {
                if (availableHeadsets[headsetId].status == "connected")
                {
                    Cortex.StartSession(headsetId);
                }
                else
                {
                    pairingHeadset = headsetId;
                    Cortex.ConnectDevice(headsetId);
                }
            }
            else
                Debug.LogWarning("Attempted to pair with a headset that is not available");
        }

        void PairWithNewlyConnectedHeadset(HeadsetConnectEventArgs args)
        {
            if (args.HeadsetId != pairingHeadset)
                return;

            Cortex.StartSession(args.HeadsetId);
            pairingHeadset = "";
        }
    }
}
