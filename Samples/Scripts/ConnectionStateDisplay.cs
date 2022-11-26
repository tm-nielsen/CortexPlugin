using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CortexPlugin;
using TMPro;

namespace CortexExamples
{
    /// <summary>
    /// Keeps track of connection state to update UI
    /// </summary>
    public class ConnectionStateDisplay : MonoBehaviour
    {
        public TextMeshProUGUI stateText;
        public GameObject installApp, Login;

        private void Start()
        {
            Cortex.ConnectionStateChanged += OnConnectionStateChanged;
        }

        public void OnConnectionStateChanged(ConnectToCortexStates connectionState)
        {
            installApp.SetActive(connectionState == ConnectToCortexStates.EmotivApp_NotFound);
            Login.SetActive(connectionState == ConnectToCortexStates.Login_notYet);

            switch (connectionState)
            {
                case ConnectToCortexStates.Service_connecting:
                    {
                        stateText.text = "Connecting to Service...";
                        break;
                    }
                case ConnectToCortexStates.EmotivApp_NotFound:
                    {
                        stateText.text = "";
                        break;
                    }
                case ConnectToCortexStates.Login_waiting:
                    {
                        stateText.text = "Waiting for login";
                        break;
                    }
                case ConnectToCortexStates.Login_notYet:
                    {
                        stateText.text = "";
                        break;
                    }
                case ConnectToCortexStates.Authorizing:
                    {
                        stateText.text = "Please open Emotiv Launcher to authorize this application";
                        break;
                    }
                case ConnectToCortexStates.Authorize_failed:
                    {
                        stateText.text = "Authentication Failed";
                        break;
                    }
                case ConnectToCortexStates.Authorized:
                    {
                        stateText.text = "Connected!";
                        break;
                    }
                case ConnectToCortexStates.LicenseExpried:
                    {
                        stateText.text = "Trial Expired";
                        break;
                    }
                case ConnectToCortexStates.License_HardLimited:
                    {
                        stateText.text = "Offline use limit reached";
                        break;
                    }
            }
        }

        public void InstallApp()
        {
            Application.OpenURL("https://www.emotiv.com/emotiv-launcher/#download");
        }
    }
}
