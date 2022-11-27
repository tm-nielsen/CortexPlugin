using System;
using System.Collections.Generic;
using UnityEngine;

namespace CortexPlugin
{
    /// <summary>
    /// Primary interface for Emotiv integration
    /// </summary>
    public static class Cortex
    {
        static CortexClient ctxClient = CortexClient.Instance;
        static Authorizer authorizer = Authorizer.Instance;
        static DataStreamManager dataStreamManager = DataStreamManager.Instance;
        static HeadsetFinder headsetFinder = HeadsetFinder.Instance;
        static DataSubscriber dataSubscriber;

        // verbose logs
        public static bool printLogs;
        public static bool printStreamData;

        public static bool isQuitting;

        public static ConnectToCortexStates connectionState = ConnectToCortexStates.Service_connecting;

        // Event buffers, enable event calls within Unity from other threads
        // remember to unsubscribe from events in OnDestroy
        public static EventBuffer<List<Headset>> HeadsetQueryResult;
        public static EventBuffer<string> DataStreamStarted;
        public static EventBuffer<string> DataStreamEnded;
        public static EventBuffer<HeadsetConnectEventArgs> HeadsetConnected;
        public static EventBuffer<ConnectToCortexStates> ConnectionStateChanged;
        public static EventBuffer<ErrorMsgEventArgs> ErrorRecieved;

        public static TrainingHandler training;
        public static ProfileManager profiles;


        /// <summary>
        /// <para>Initiates the authorizer and cortex client,
        /// called externally at the beginning of the program.</para>
        /// It is best to call this in Awake() from a script with execution priority
        /// </summary>
        /// <param name="logs">enable verbose logs</param>
        /// <param name="streamPrint">enable continous prints of incoming stream data</param>
        /// <param name="license">uneccessary in most cases,
        /// if you need this you probably know what you are doing and will be changing this code anyways</param>
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

            // initialize Training handler
            training = new TrainingHandler();
            training.InstantiateEventBuffers(eventBufferInstance);

            // initialize profile manager
            profiles = new ProfileManager();
            profiles.InstantiateEventBuffers(eventBufferInstance);

            // Initiate data stream manager
            dataStreamManager.Init();
            // Initialize websocket client
            ctxClient.InitWebSocketClient();
            // Start connecting to cortex service
            authorizer.StartAction(license);
        }

        /// <summary>
        /// Instantiates objects to buffer asynchronous websocket events into the main Unity thread
        /// </summary>
        /// <returns></returns>
        static EventBufferInstance InstantiateEventBuffers()
        {
            // create Event Buffer GameObject to drive in engine events
            GameObject eventBufferObject = new GameObject();
            GameObject.DontDestroyOnLoad(eventBufferObject);
            eventBufferObject.name = "Event Buffer Object";
            EventBufferInstance eventBufferInstance = eventBufferObject.AddComponent<EventBufferInstance>();

            // add data subscriber (data stream event buffer handler)
            dataSubscriber = eventBufferObject.AddComponent<DataSubscriber>();

            // add buffer for headset query completion
            HeadsetQueryResult = new EventBuffer<List<Headset>>();
            headsetFinder.QueryHeadsetOK += HeadsetQueryResult.OnParentEvent;

            // add buffer for successful start of data stream (initating session with headset)
            DataStreamStarted = new EventBuffer<string>();
            dataStreamManager.DataStreamStarted += DataStreamStarted.OnParentEvent;

            // add buffer for when a headset is unexpectedly disconnected (sends headset id)
            DataStreamEnded = new EventBuffer<string>();
            dataStreamManager.DataStreamEnded += DataStreamEnded.OnParentEvent;

            // add buffer for headset connection (pairing with computer)
            HeadsetConnected = new EventBuffer<HeadsetConnectEventArgs>();
            ctxClient.HeadsetConnectNotify += HeadsetConnected.OnParentEvent;

            // add buffer for connection state changing
            ConnectionStateChanged = new EventBuffer<ConnectToCortexStates>();
            authorizer.ConnectServiceStateChanged += ConnectionStateChanged.OnParentEvent;
            // keep track of connection state
            ConnectionStateChanged += (state) => connectionState = state;

            // add buffer for error recieved
            ErrorRecieved = new EventBuffer<ErrorMsgEventArgs>();
            ctxClient.ErrorMsgReceived += ErrorRecieved.OnParentEvent;

            EventBufferBase[] buffers = new EventBufferBase[]
            {
                HeadsetQueryResult,
                DataStreamStarted,
                HeadsetConnected,
                DataStreamEnded,
                ConnectionStateChanged,
                ErrorRecieved
            };
            eventBufferInstance.AddBuffers(buffers);

            return eventBufferInstance;
        }

        public static void Stop()
        {
            isQuitting = true;
            dataStreamManager.Stop();
            HeadsetFinder.Instance.StopQueryHeadset();
            ctxClient.ForceCloseWSC();
        }

        /// <summary>
        /// Start a session with the given headset,
        /// will automatically subscribe to basic data streams
        /// and trigger HeadsetConnected event
        /// </summary>
        /// <param name="headsetId"></param>
        public static void StartSession(string headsetId) => dataStreamManager.StartSession(headsetId);
        /// <summary>
        /// Ends the sessions specified by the given ID
        /// </summary>
        public static void EndSession(string sessionId) => dataStreamManager.CloseSession(sessionId);
        /// <summary>
        /// Ends the session specified by the given headsetId
        /// </summary>
        public static void EndSessionByHeadset(string headsetId) => dataStreamManager.CloseSessionByHeadset(headsetId);
        /// <summary>
        /// Gets the active session associated with the given headsetId
        /// </summary>
        public static string GetSessionByHeadset(string headsetId) => dataStreamManager.GetSessionByHeadset(headsetId);
        /// <summary>
        /// Ends the session created most recently
        /// </summary>
        public static void EndMostRecentSession() => dataStreamManager.CloseMostRecentSession();

        /// <summary>
        /// Trigger a query into the availaale headsets,
        /// subscribe to QueryHeadsetOK for result
        /// </summary>
        public static void QueryHeadsets()
        {
            headsetFinder.TriggerQuery();
        }

        /// <summary>
        /// Checks if a given headset is already in use
        /// </summary>
        /// <param name="headsetId">headset to check</param>
        /// <returns>true if there is already an extant session for the headset</returns>
        public static bool HeadsetIsAlreadyInUse(string headsetId)
        {
            return dataStreamManager.HeadsetIsAlreadyInUse(headsetId);
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
        /// Check if a data stream currently exists for the given headset
        /// </summary>
        /// <param name="headsetId"></param>
        /// <returns>ID of desired headset stream</returns>
        public static bool DataStreamExists(string headsetId) => dataSubscriber.DataStreamExists(headsetId);

        /// <summary>
        /// Subscribe to incoming data stream events for the given headset,
        /// the provided function will be called when new data is received.
        /// The headset must first be paired, and have an active session
        /// </summary>
        /// <typeparam name="T">type of data to recieve</typeparam>
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
        /// <typeparam name="T">type of data subscription</typeparam>
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
        public static bool SubscribeSysEvents(string headsetId, Action<SystemEventArgs> action)
            => SubscribeDataStream(headsetId, action);
        /// <summary>
        /// Simplified interface to UnsubscribeDataStream for system events
        /// </summary>
        public static bool UnsubscribeSysEvents(string headsetId, Action<SystemEventArgs> action)
            => UnsubscribeDataStream(headsetId, action);
    }
}
