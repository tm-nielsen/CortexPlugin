using CortexPlugin;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CortexExamples
{
    /// <summary>
    /// Initializes the connection to the Cortex API
    /// </summary>
    public class ConnectToCortex : MonoBehaviour
    {
        [Header("credentials")]
        public string clientId;
        public string clientSecret;

        [Header("settings")]
        public bool printLogs;
        public bool printDataStreams;

        void Awake()
        {
            Cortex.Start(clientId, clientSecret, logs: printLogs, streamPrint: printDataStreams);
        }

        private void OnApplicationQuit()
        {
            Cortex.Stop();
        }
    }
}
