using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CortexPlugin
{
    /// <summary>
    /// Handles the data stream of a single session
    /// </summary>
    public class DataStream
    {
        public event EventHandler<MentalCommand> MentalCommandReceived;
        public event EventHandler<SysEventArgs> SysEventReceived;
        public event EventHandler<DeviceInfo> DevDataReceived;

        DeviceInfo devData;

        string sessionID;

        public DataStream(string id)
        {
            sessionID = id;
        }
        public void Close()
        {
            DataStreamManager.Instance.CloseSession(sessionID);
        }


        public void ConfigureDevHeaders(JArray cols)
        {
            devData = new DeviceInfo(cols);
        }

        public void OnStreamDataRecieved(StreamDataEventArgs e)
        {
            try
            {
                // flattened array of incoming data, starting with timestamp, then relevant data
                // nested arrays are inserted as individual elements
                ArrayList data = e.Data;
                double time = Convert.ToDouble(data[0]);
                switch (e.StreamName)
                {
                    case DataStreamName.MentalCommands:
                        string act = Convert.ToString(data[1]);
                        double pow = Convert.ToDouble(data[2]);
                        MentalCommand comEvent = new MentalCommand(time, act, pow);

                        MentalCommandReceived(this, comEvent);

                        if (Cortex.printStreamData)
                            Debug.Log($"Mental Command Recieved | {comEvent}");
                        break;

                    case DataStreamName.SysEvents:
                        string detection = Convert.ToString(data[1]);
                        string eventMsg = Convert.ToString(data[2]);
                        SysEventArgs sysEvent = new SysEventArgs(time, detection, eventMsg);

                        SysEventReceived(this, sysEvent);

                        if (Cortex.printStreamData)
                            Debug.Log($"System Event Recieved | {detection} | {eventMsg}");
                        break;

                    case DataStreamName.DevInfos:
                        devData.UpdateInfo(data);
                        if (DevDataReceived != null) DevDataReceived(this, devData);

                        if (Cortex.printStreamData)
                            Debug.Log($"Dev Info Received | Battery: {devData.battery}" +
                                $", Overall CQ: {devData.cqOverall}, Signal: {devData.signalStrength}");
                        break;

                    default:
                        Debug.Log($"unused stream data on {e.StreamName}");
                        break;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex);
            }
        }
    }


    /// <summary>
    /// Represents and handles data from the devinfo channel
    /// </summary>
    public class DeviceInfo : DataStreamEventArgs
    {
        public List<string> cqHeaders;
        public Dictionary<Channel_t, float> contactQuality;
        public float battery;
        public float cqOverall;
        public float signalStrength;

        /// <summary>
        /// Initialize object with data headers
        /// </summary>
        public DeviceInfo(JArray cols)
        {
            cqHeaders = new List<string>();
            contactQuality = new Dictionary<Channel_t, float>();

            string headerDisplay = "Contact Quality headers: ";
            foreach (var header in cols)
            {
                cqHeaders.Add((string)header);
                headerDisplay += (string)header + ", ";
            }
        }

        public void UpdateInfo(ArrayList data)
        {
            signalStrength = (float)Convert.ToDouble(data[2]);
            int i;
            for (i = 0; i < cqHeaders.Count; i++)
            {
                var key = ChannelStringList.StringToChannel(cqHeaders[i]);
                contactQuality[key] = (float)Convert.ToDouble(data[i + 3]);
            }
            battery = (float)Convert.ToDouble(data[i + 3]);
            cqOverall = contactQuality[Channel_t.CHAN_CQ_OVERALL];
        }

        public override string ToString()
        {
            string res = "Contact Quality: {";
            foreach (var kvp in contactQuality)
            {
                res += $"[{ChannelStringList.ChannelToString(kvp.Key)}, {kvp.Value}], ";
            }
            res += $"}},  Battery: {battery}, signalStrength: {signalStrength}";
            return res;
        }
    }
}