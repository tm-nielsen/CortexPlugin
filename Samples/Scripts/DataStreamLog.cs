using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using CortexPlugin;

namespace CortexExamples
{
    public class DataStreamLog : MonoBehaviour
    {
        [Header("references")]
        public ScrollRect scrollRect;
        public TextMeshProUGUI textLog;

        [Header("settings")]
        public bool printMentalCommands = true;
        public bool printDeviceInfo = true;
        public bool printSystemEvents = true;

        void Start()
        {
            Cortex.DataStreamStarted += OnDataStreamStarted;
        }

        void OnDataStreamStarted(string headsetId)
        {
            Cortex.SubscribeMentalCommands(headsetId, OnMentalCommandReceived);
            Cortex.SubscribeDeviceInfo(headsetId, OnDeviceInfoReceived);
            Cortex.SubscribeSystemEvents(headsetId, OnSystemEventReceived);
        }

        void OnMentalCommandReceived(MentalCommand mentalCommand)
        {
            if (printMentalCommands)
                AddTextToLog(mentalCommand.ToString());
        }

        void OnDeviceInfoReceived(DeviceInfo deviceInfo)
        {
            if (printDeviceInfo)
                AddTextToLog(deviceInfo.ToString());
        }

        void OnSystemEventReceived(SystemEventArgs args)
        {
            if (printSystemEvents)
                AddTextToLog(args.ToString());
        }

        void AddTextToLog(string text)
        {
            textLog.text += text + "\n";

            Canvas.ForceUpdateCanvases();
            scrollRect.normalizedPosition = Vector2.zero;
        }
    }
}