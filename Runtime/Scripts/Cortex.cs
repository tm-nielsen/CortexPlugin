using System;
using System.Collections.Generic;
using UnityEngine;

namespace CortexPlugin
{
    /// <summary>
    /// Primary interface for Emotiv Cortex integration
    /// </summary>
    public static class Cortex
    {
        static CortexClient ctxClient = CortexClient.Instance;
        static Authorizer authorizer = Authorizer.Instance;
        static DataStreamManager dataStreamManager = DataStreamManager.Instance;
        static DataSubscriber dataSubscriber;

        // verbose logs
        public static bool printLogs;
        public static bool printStreamData;

        public static bool isQuitting;

        public static ConnectToCortexStates connectionState = ConnectToCortexStates.Service_connecting;

        /// <summary>
        /// The plugin has been connected and successfully authorized.
        /// </summary>
        public static EventBuffer<License> Authorized;
        /// <summary>
        /// State of the connection to the Emotiv launcher has changed
        /// </summary>
        public static EventBuffer<ConnectToCortexStates> ConnectionStateChanged;
        /// <summary>
        /// Provides a list of headsets currently available to the Emotiv Launcher. Fired in response to QueryHeadsets.
        /// </summary>
        public static EventBuffer<List<Headset>> HeadsetQueryResult;
        /// <summary>
        /// A headset has been connected
        /// </summary>
        public static EventBuffer<HeadsetConnectEventArgs> DeviceConnected;
        /// <summary>
        /// The first object has been received by a new data stream. Effectively equivalent to the start of a session.
        /// </summary>
        public static EventBuffer<string> DataStreamStarted;
        /// <summary>
        /// A data stream has been closed
        /// </summary>
        public static EventBuffer<string> DataStreamEnded;
        /// <summary>
        /// An API error has been received
        /// </summary>
        public static EventBuffer<ErrorMsgEventArgs> ErrorRecieved;

        /// <summary>
        /// Provides events and methods to enable mental command training
        /// </summary>
        public static TrainingHandler training;
        /// <summary>
        /// Provides events and methods to enable profile management
        /// </summary>
        public static ProfileManager profiles;


        /// <summary>
        /// <para>Initiates the authorizer and cortex client,
        /// called externally at the beginning of the program.</para>
        /// It is best to call this in Awake() from a script with execution priority
        /// </summary>
        public static void Start(string clientId, string clientSecret,
            string appURL = "wss://localhost:6868",
            string emotivAppsPath = "C:\\Program Files\\EmotivApps",
            bool logs = false, bool streamPrint = false, string license = "")
        {
            Config.AppClientId = clientId;
            Config.AppClientSecret = clientSecret;
            Config.AppUrl = appURL;
            Config.EmotivAppsPath = emotivAppsPath;

            Config.TmpAppDataDir = Application.productName;
            Config.AppVersion = Application.version;

            printLogs = logs;
            printStreamData = streamPrint;

            EventBufferInstance eventBufferInstance = InstantiateEventBuffers();


            training = new TrainingHandler(eventBufferInstance);
            profiles = new ProfileManager(eventBufferInstance);

            dataStreamManager.Init();
            ctxClient.InitWebSocketClient();
            authorizer.StartAction(license);
        }

        /// <summary>
        /// Instantiates objects to buffer asynchronous websocket events into the main Unity thread
        /// </summary>
        static EventBufferInstance InstantiateEventBuffers()
        {
            // create Event Buffer GameObject to drive in engine events
            GameObject eventBufferObject = new GameObject();
            GameObject.DontDestroyOnLoad(eventBufferObject);
            eventBufferObject.name = "Event Buffer Object";
            EventBufferInstance eventBufferInstance = eventBufferObject.AddComponent<EventBufferInstance>();

            // add data subscriber (data stream event buffer handler)
            dataSubscriber = eventBufferObject.AddComponent<DataSubscriber>();


            // add buffers
            Authorized = new EventBuffer<License>();
            authorizer.GetLicenseInfoDone += Authorized.OnParentEvent;

            ConnectionStateChanged = new EventBuffer<ConnectToCortexStates>();
            authorizer.ConnectServiceStateChanged += ConnectionStateChanged.OnParentEvent;
            ConnectionStateChanged += (state) => connectionState = state;

            HeadsetQueryResult = new EventBuffer<List<Headset>>();
            ctxClient.QueryHeadsetOK += HeadsetQueryResult.OnParentEvent;

            DeviceConnected = new EventBuffer<HeadsetConnectEventArgs>();
            ctxClient.HeadsetConnectNotify += DeviceConnected.OnParentEvent;

            DataStreamStarted = new EventBuffer<string>();
            dataStreamManager.DataStreamStarted += DataStreamStarted.OnParentEvent;

            DataStreamEnded = new EventBuffer<string>();
            dataStreamManager.DataStreamEnded += DataStreamEnded.OnParentEvent;

            ErrorRecieved = new EventBuffer<ErrorMsgEventArgs>();
            ctxClient.ErrorMsgReceived += ErrorRecieved.OnParentEvent;

            EventBufferBase[] buffers = new EventBufferBase[]
            {
                Authorized,
                ConnectionStateChanged,
                HeadsetQueryResult,
                DeviceConnected,
                DataStreamStarted,
                DataStreamEnded,
                ErrorRecieved
            };
            eventBufferInstance.AddBuffers(buffers);

            return eventBufferInstance;
        }

        public static void Stop()
        {
            isQuitting = true;
            dataStreamManager.Stop();
            ctxClient.ForceCloseWSC();
        }

        /// <summary>
        /// Start a session with the given headset,
        /// will automatically subscribe to basic data streams
        /// </summary>
        public static void StartSession(string headsetId) => dataStreamManager.StartSession(headsetId);
        /// <summary>
        /// Ends the sessions specified by the given id
        /// </summary>
        public static void EndSession(string sessionId) => dataStreamManager.CloseSession(sessionId);
        /// <summary>
        /// Ends the session specified by the given headset id
        /// </summary>
        public static void EndSessionByHeadset(string headsetId) => dataStreamManager.CloseSessionByHeadset(headsetId);
        /// <summary>
        /// Gets the active session associated with the given headset id
        /// </summary>
        public static string GetSessionByHeadset(string headsetId) => dataStreamManager.GetSessionByHeadset(headsetId);
        /// <summary>
        /// Ends the session created most recently
        /// </summary>
        public static void EndMostRecentSession() => dataStreamManager.CloseMostRecentSession();

        /// <summary>
        /// Trigger a query into the available headsets,
        /// <para>subscribe to QueryHeadsetResult for result</para>
        /// </summary>
        public static void QueryHeadsets()
        {
            ctxClient.QueryHeadsets("");
        }

        /// <summary>
        /// Connect a Device that is discovered, but unavailable
        /// (bluetooth pairing, basically)
        /// </summary>
        public static void ConnectDevice(string headsetId)
        {
            ctxClient.ConnectDevice(headsetId);
        }

        /// <summary>
        /// Returns true if a data stream currently exists for the given headset
        /// </summary>
        public static bool DataStreamExists(string headsetId) => dataSubscriber.DataStreamExists(headsetId);

        /// <summary>
        /// Subscribe to incoming data stream events for the given headset,
        /// the provided function will be called when new data is received.
        /// The headset must first be paired, and have an active session
        /// </summary>
        /// <typeparam name="T">type of data to recieve, can be MentalCommand, DeviceInfo, or SystemEventArgs</typeparam>
        /// <param name="headsetId">headset to get data from</param>
        /// <param name="action">method to be called when new data is recieved</param>
        /// <returns>true if subscription was successful</returns>
        public static bool SubscribeDataStream<T>(string headsetId, Action<T> action) where T : DataStreamEventArgs
            => dataSubscriber.SubscribeDataStream(headsetId, action);
        /// <summary>
        /// Unsubscribe from incoming data stream events for the given headset,
        /// all subscriptions will be cleared automatically on session closure,
        /// but it is efficient to unsubscribe when the data feed is uneccesary
        /// </summary>
        /// <typeparam name="T">type of data subscription, can be MentalCommand, DeviceInfo, or SystemEventArgs</typeparam>
        /// <param name="headsetId">headset of the desired stream</param>
        /// <param name="action">method to remove from callback</param>
        /// <returns>true if unsubscription was successful</returns>
        public static bool UnsubscribeDataStream<T>(string headsetId, Action<T> action) where T : DataStreamEventArgs
            => dataSubscriber.UnsubscribeDataStream(headsetId, action);

        /// <summary>
        /// Simplified interface to SubscribeDataStream for mental commands
        /// </summary>
        public static bool SubscribeMentalCommands(string headsetId, Action<MentalCommand> action)
            => SubscribeDataStream(headsetId, action);
        /// <summary>
        /// Simplified interface to UnsubscribeDataStream for mental commands
        /// </summary>
        public static bool UnsubscribeMentalCommands(string headsetId, Action<MentalCommand> action)
            => UnsubscribeDataStream(headsetId, action);

        /// <summary>
        /// Simplified interface to SubscribeDataStream for device information
        /// </summary>
        public static bool SubscribeDeviceInfo(string headsetId, Action<DeviceInfo> action)
            => SubscribeDataStream(headsetId, action);
        /// <summary>
        /// Simplified interface to UnsubscribeDataStream for devide information
        /// </summary>
        public static bool UnsubscribeDeviceInfo(string headsetId, Action<DeviceInfo> action)
            => UnsubscribeDataStream(headsetId, action);

        /// <summary>
        /// Simplified interface to SubscribeDataStream for system events
        /// </summary>
        public static bool SubscribeSystemEvents(string headsetId, Action<SystemEventArgs> action)
            => SubscribeDataStream(headsetId, action);
        /// <summary>
        /// Simplified interface to UnsubscribeDataStream for system events
        /// </summary>
        public static bool UnsubscribeSystemEvents(string headsetId, Action<SystemEventArgs> action)
            => UnsubscribeDataStream(headsetId, action);
    }
}
