using Newtonsoft.Json.Linq;
using System.Collections;

namespace CortexPlugin
{
    public class Headset
    {
        public string headsetID;
        public string status;
        public string serialId;
        public string firmwareVersion;
        public string dongleSerial;
        public ArrayList sensors;
        public ArrayList motionSensors;
        public JObject settings;
        public ConnectionType connectedBy;
        public HeadsetTypes headsetType;
        public string mode;

        // Contructor
        public Headset()
        {
        }
        public Headset(JObject jHeadset)
        {
            headsetID = (string)jHeadset["id"];

            if (headsetID.Contains(HeadsetNames.epoc_plus))
            {
                headsetType = HeadsetTypes.HEADSET_TYPE_EPOC_PLUS;
            }
            else if (headsetID.Contains(HeadsetNames.epoc_flex))
            {
                headsetType = HeadsetTypes.HEADSET_TYPE_EPOC_FLEX;
            }
            else if (headsetID.Contains(HeadsetNames.epoc_x))
            {
                headsetType = HeadsetTypes.HEADSET_TYPE_EPOC_X;
            }
            else if (headsetID.Contains(HeadsetNames.insight2))
            {
                headsetType = HeadsetTypes.HEADSET_TYPE_INSIGHT2;
            }
            else if (headsetID.Contains(HeadsetNames.insight))
            {
                headsetType = HeadsetTypes.HEADSET_TYPE_INSIGHT;
            }
            else if (headsetID.Contains(HeadsetNames.mn8))
            {
                headsetType = HeadsetTypes.HEADSET_TYPE_MN8;
            }
            else if (headsetID.Contains(HeadsetNames.epoc))
            {
                headsetType = HeadsetTypes.HEADSET_TYPE_EPOC_STD;
            }

            status = (string)jHeadset["status"];
            firmwareVersion = (string)jHeadset["firmware"];
            dongleSerial = (string)jHeadset["dongle"];
            sensors = new ArrayList();

            foreach (JToken sensor in (JArray)jHeadset["sensors"])
            {
                sensors.Add(sensor.ToString());
            }
            motionSensors = new ArrayList();
            foreach (JToken sensor in (JArray)jHeadset["motionSensors"])
            {
                motionSensors.Add(sensor.ToString());
            }
            mode = (string)jHeadset["mode"];
            string cnnBy = (string)jHeadset["connectedBy"];
            if (cnnBy == "dongle")
            {
                connectedBy = ConnectionType.CONN_TYPE_DONGLE;
            }
            else if (cnnBy == "bluetooth")
            {
                connectedBy = ConnectionType.CONN_TYPE_BTLE;
            }
            else if (cnnBy == "extender")
            {
                connectedBy = ConnectionType.CONN_TYPE_EXTENDER;
            }
            else if (cnnBy == "usb cable")
            {
                connectedBy = ConnectionType.CONN_TYPE_USB_CABLE;
            }
            else
            {
                connectedBy = ConnectionType.CONN_TYPE_UNKNOWN;
            }
            settings = (JObject)jHeadset["settings"];
        }

        public bool Equals(Headset rhs)
        {
            return headsetID == rhs.headsetID && status == rhs.status && connectedBy == rhs.connectedBy;
        }
    }
}
