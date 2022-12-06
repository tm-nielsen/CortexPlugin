using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace CortexPlugin
{
    /// <summary>
    /// Manages the creation and data subcription of mutliple headset sessios at once
    /// </summary>
    public class DataStreamManager
    {
        public static DataStreamManager Instance { get; } = new DataStreamManager();
        static readonly object locker = new object();
        CortexClient ctxClient = CortexClient.Instance;
        Authorizer authorizer = Authorizer.Instance;
        DataSubscriber dataSubscriber = DataSubscriber.Instance;

        /// <summary>
        /// Active DataStreams from all open sessions
        /// </summary>
        Dictionary<string, DataStream> sessions;
        /// <summary>
        /// Maps each open session ID to the respective headset, used to enable subscriptions by headset ID
        /// </summary>
        Dictionary<string, string> headsetToSessionID;
        /// <summary>
        /// set when creating a session, used to send our headsetConnected event when first data comes in
        /// </summary>
        string connectingHeadset = null;

        /// <summary>
        /// Sent out when a new headset has been connected and stream data has been recieved
        /// </summary>
        public event EventHandler<string> DataStreamStarted;
        /// <summary>
        /// Sends our when a data stream has been automatically stopped,
        /// normally due to a headset being disconnected
        /// </summary>
        public event EventHandler<string> DataStreamEnded;

        public void Init()
        {
            dataSubscriber = DataSubscriber.Instance;
            sessions = new Dictionary<string, DataStream>();
            headsetToSessionID = new Dictionary<string, string>();

            // Connect event handlers
            ctxClient.StreamDataReceived += OnStreamDataRecieved;
            ctxClient.CreateSessionOK += OnCreateSessionOk;
            ctxClient.SubscribeDataDone += OnSubscribeDataDone;
            // Automatically refreshes headset list when a headset is paired over bluetooth
            ctxClient.HeadsetConnectNotify +=
                (object sender, HeadsetConnectEventArgs e) =>
                {
                    if (Cortex.printLogs)
                        Debug.Log($"Headset paired: {e.HeadsetId}");
                    ctxClient.QueryHeadsets("");
                };
            ctxClient.StreamStopNotify += OnStreamStop;
        }

        /// <summary>
        /// Routes incoming stream data from CortexClient to the relevant DataStream(s)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnStreamDataRecieved(object sender, StreamDataEventArgs e)
        {
            if (connectingHeadset != null)
            {
                if (headsetToSessionID[connectingHeadset] == e.Sid)
                {
                    DataStreamStarted(this, connectingHeadset);
                    connectingHeadset = null;
                }
            }
            lock (locker) if (sessions.ContainsKey(e.Sid))
                    sessions[e.Sid].OnStreamDataRecieved(e);
        }

        /// <summary>
        /// Called by Cortexclient when a session has been successfully created,
        /// creates a DataStream object to handle incoming data
        /// </summary>
        private void OnCreateSessionOk(object sender, SessionEventArgs e)
        {
            try
            {
                string id = e.SessionId;
                // automatically set up training handler with new session
                Cortex.training.TargetSession = id;
                ctxClient.Subscribe(authorizer.CortexToken, id, Config.dataStreams);
                lock (locker)
                {
                    sessions[id] = new DataStream(id);
                    headsetToSessionID[e.HeadsetId] = id;
                    dataSubscriber.AddStream(sessions[id], id, e.HeadsetId);
                }

                if (Cortex.printLogs)
                    Debug.Log($"Session created successfuly, new session id: {id}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex);
            }
        }

        /// <summary>
        /// Called by CortexClient when a session has successfully subscribed to data,
        /// prepares any relevant DataStream(s) for incoming data
        /// </summary>
        private void OnSubscribeDataDone(object sender, MultipleResultEventArgs e)
        {
            if (Cortex.printLogs)
                Debug.Log("DataStreamManager: SubscribeDataOK");
            foreach (JObject i in e.FailList)
            {
                ErrorMsgEventArgs args = new ErrorMsgEventArgs((int)i["code"],
                    $"Failed to subscribe to {i["stream name"]} stream, {i["message"]}. " +
                    $"Flex mapping may be unsupported for mental commands.","subscribe");
                Cortex.ErrorRecieved.OnParentEvent(this, args);
            }

            foreach (JObject stream in e.SuccessList)
            {
                if ((string)stream["streamName"] == "dev")
                {
                    string sid = (string)stream["sid"];
                    lock (locker) if (sessions.ContainsKey(sid))
                            sessions[sid].ConfigureDevHeaders((JArray)stream["cols"][2]);
                }
            }
        }

        /// <summary>
        /// Called by CortexClient when a data stream is automatically close, internally remove session
        /// and notify externals through event
        /// </summary>
        private void OnStreamStop(object sender, string sessionId)
        {
            string headsetId = "";
            foreach (var item in headsetToSessionID)
                if (item.Value == sessionId)
                    headsetId = item.Key;

            sessions.Remove(sessionId);
            dataSubscriber.RemoveStreamBySessionID(sessionId);

            if (!string.IsNullOrEmpty(headsetId))
            {
                headsetToSessionID.Remove(headsetId);
                DataStreamEnded(this, headsetId);
            }
        }

        /// <summary>
        /// Stops the connection to the Cortex service,
        /// called externally (preferably in OnApplicationQuit)
        /// </summary>
        public void Stop()
        {
            foreach (var k in sessions)
                ctxClient.UpdateSession(authorizer.CortexToken, k.Key, "close");
            sessions.Clear();
            headsetToSessionID.Clear();
        }

        /// <summary>
        /// Starts a cortex session with a given headset,
        /// triggers a callback that will also subscribe to data streams
        /// </summary>
        public void StartSession(string headsetId)
        {
            if (Cortex.printLogs)
                Debug.Log($"Attempting to start session with headset: {headsetId}");
            ctxClient.CreateSession(authorizer.CortexToken, headsetId, "open");
            connectingHeadset = headsetId;
        }
        /// <summary>
        /// Closes an individual session
        /// </summary>
        /// <param name="sessionId">the session to close</param>
        public void CloseSession(string sessionId)
        {
            ctxClient.UpdateSession(authorizer.CortexToken, sessionId, "close");
            sessions.Remove(sessionId);
            dataSubscriber.RemoveStreamBySessionID(sessionId);

            string toRemove = null;
            foreach (var item in headsetToSessionID)
                if (item.Value == sessionId)
                    toRemove = item.Key;

            if (!string.IsNullOrEmpty(toRemove))
            {
                headsetToSessionID.Remove(toRemove);
                DataStreamEnded(this, toRemove);
            }
        }
        /// <summary>
        /// Closes an individual session by the associated headsetId
        /// </summary>
        /// <param name="headsetId">the id of the headset associated with the session</param>
        public void CloseSessionByHeadset(string headsetId)
        {
            string sessionId = headsetToSessionID[headsetId];
            ctxClient.UpdateSession(authorizer.CortexToken, sessionId, "close");
            sessions.Remove(sessionId);
            dataSubscriber.RemoveStreamByHeadsetID(headsetId);
            headsetToSessionID.Remove(headsetId);
            DataStreamEnded(this, headsetId);
        }
        /// <summary>
        /// Gets the active session id associated with the headset
        /// </summary>
        /// <param name="headsetId"></param>
        /// <returns>the session ID if it exists, otherwise null</returns>
        public string GetSessionByHeadset(string headsetId)
        {
            if (headsetToSessionID.ContainsKey(headsetId))
                return headsetToSessionID[headsetId];
            else
                return null;
        }
        /// <summary>
        /// Closes the session that was created most recently
        /// </summary>
        public void CloseMostRecentSession()
        {
            string sessionId = headsetToSessionID.Last().Value;
            CloseSession(sessionId);
        }
    }
}
