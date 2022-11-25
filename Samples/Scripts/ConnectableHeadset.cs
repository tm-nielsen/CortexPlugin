using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EmotivUnityPlugin;

public class ConnectableHeadset : MonoBehaviour
{
    public TextMeshProUGUI deviceName;
    public Button connectButton;
    public GameObject connectingText;

    [Header("Sprites")]
    public Sprite dongleSprite;

    string headsetID;
    bool connected;

    void Start()
    {
        connectButton.onClick.AddListener(Pair);
    }

    private void OnEnable()
    {
        Cortex.HeadsetConnected += OnHeadsetConnected;
    }

    private void OnDisable()
    {
        Cortex.HeadsetConnected -= OnHeadsetConnected;
    }

    public void Init(Headset info)
    {
        headsetID = info.headsetID;
        deviceName.text = headsetID;

        connected = info.status == "connected";

        if (info.connectedBy == ConnectionType.CONN_TYPE_DONGLE)
        {
            Image connectButtonImage = connectButton.GetComponent<Image>();
            connectButtonImage.sprite = dongleSprite;
        }
    }

    void Pair()
    {
        if (!connected)
        {
            Cortex.ConnectDevice(headsetID);
            connectButton.gameObject.SetActive(false);
            connectingText.SetActive(true);
            HeadsetPairingMenu.connectingHeadset = headsetID;
        }
        else
            InitiateConnection();
    }

    void OnHeadsetConnected(HeadsetConnectEventArgs args)
    {
        if (args.HeadsetId == headsetID)
            InitiateConnection();
    }

    void InitiateConnection()
    {
        connectButton.gameObject.SetActive(false);
        connectingText.SetActive(true);

        Cortex.StartSession(headsetID);
    }

    public void SetAsConnecting()
    {
        connected = true;
        connectButton.gameObject.SetActive(false);
        connectingText.SetActive(true);
    }
}
