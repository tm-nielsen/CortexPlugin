using Newtonsoft.Json.Linq;
using UnityEngine;

namespace EmotivUnityPlugin
{
    public class TrainingHandler
    {
        CortexClient ctxClient = CortexClient.Instance;
        Authorizer auth = Authorizer.Instance;

        // event buffers to enable engine synchronous callbacks
        public EventBuffer<TrainedActions> GetTrainedActionsResult;
        public EventBuffer<TrainingThreshold> TrainingThresholdResult;
        public EventBuffer<ActionSensitivity> ActionSensitivityResult;
        public EventBuffer<double> TrainingTimeResult;
        public EventBuffer<DetectionInfo> GetDetectionInfoResult;
        public EventBuffer<JObject> TrainingRequestResult;

        /// <summary>
        /// The target session Id for training, automatically set when a new session is started
        /// </summary>
        public string TargetSession
        {
            get
            {
                if (string.IsNullOrEmpty(_targetSession))
                    Debug.LogWarning("Attempted to train BCI without specifying a session");
                return _targetSession;
            }
            set { _targetSession = value; }
        }
        string _targetSession;

        string token { get => auth.CortexToken; }


        public void StartTraining(string action) => Training("start", action);
        public void AcceptTraining(string action) => Training("accept", action);
        public void RejectTraining(string action) => Training("reject", action);
        public void EraseTraining(string action) => Training("erase", action);
        public void CancelTraining(string action) => Training("reset", action);
        void Training(string status, string action) => ctxClient.Training(token, TargetSession, status, "mentalCommand", action);

        public void GetMentalCommandInfo() => GetDetectionInfo("mentalCommand");
        public void GetDetectionInfo(string detection) => ctxClient.GetDetectionInfo(detection);
        public void GetTrainingTime() => ctxClient.GetTrainingTime(token, "mentalCommand", TargetSession);
        public void GetTrainedActions(string profileName) => ctxClient.GetTrainedSignatureActions(token, "mentalCommand", profileName);
        public void GetTrainingThreshold() => ctxClient.MentalCommandTrainingThreshold(token, sessionId: TargetSession);
        public void GetActionSensitivity(string profileName) => ctxClient.MentalCommandActionSensitivity(token, "get", profileName);
        public void GetActionSensitivityBySession(string sessionID) => ctxClient.MentalCommandActionSensitivity(token, "get", sessionId: sessionID);
        public void GetActionSensitivity() => ctxClient.MentalCommandActionSensitivity(token, "get", sessionId: TargetSession);
        public void SetActionSensitivity(string profileName, int[] values) => ctxClient.MentalCommandActionSensitivity(token, "set", profileName, values: values);
        public void SetActionSensitivityBySession(string sessionID, int[] values) => ctxClient.MentalCommandActionSensitivity(token, "set", sessionId: sessionID, values: values);
        public void SetActionSensitivity(int[] values) => ctxClient.MentalCommandActionSensitivity(token, "set", sessionId: TargetSession, values: values);


        /// <summary>
        /// Instantiate all available event buffers to allow engine
        /// synchronous callbacks, called by Cortex in Start
        /// </summary>
        /// <param name="host">gameobject to attach event buffers to</param>
        public void InstantiateEventBuffers(EventBufferInstance host)
        {
            GetTrainedActionsResult = new EventBuffer<TrainedActions>();
            ctxClient.GetTrainedSignatureActionsOK += GetTrainedActionsResult.OnParentEvent;

            TrainingThresholdResult = new EventBuffer<TrainingThreshold>();
            ctxClient.MentalCommandTrainingThresholdOK += TrainingThresholdResult.OnParentEvent;

            ActionSensitivityResult = new EventBuffer<ActionSensitivity>();
            ctxClient.MentalCommandActionSensitivityOK += ActionSensitivityResult.OnParentEvent;

            TrainingTimeResult = new EventBuffer<double>();
            ctxClient.GetTrainingTimeDone += TrainingTimeResult.OnParentEvent;

            GetDetectionInfoResult = new EventBuffer<DetectionInfo>();
            ctxClient.GetDetectionInfoDone += ParseDetectionInfo;

            TrainingRequestResult = new EventBuffer<JObject>();
            ctxClient.TrainingOK += TrainingRequestResult.OnParentEvent;

            var buffers = new EventBufferBase[]
            {
                GetDetectionInfoResult,
                TrainingRequestResult,
                TrainingTimeResult,
                GetTrainedActionsResult,
                TrainingThresholdResult,
                ActionSensitivityResult
            };
            host.AddBuffers(buffers);
        }

        /// <summary>
        /// Wraps the get detection info event callback with a useful data type
        /// </summary>
        /// <param name="data">data to be parsed (raw from websocket)</param>
        void ParseDetectionInfo(object sender, JObject data)
        {
            try
            {
                DetectionInfo detectioninfo = new DetectionInfo("mentalCommand");

                JArray actions = (JArray)data["actions"];
                foreach (var ele in actions)
                {
                    detectioninfo.Actions.Add(ele.ToString());
                }
                JArray controls = (JArray)data["controls"];
                foreach (var ele in actions)
                {
                    detectioninfo.Controls.Add(ele.ToString());
                }
                JArray events = (JArray)data["events"];
                foreach (var ele in actions)
                {
                    detectioninfo.Events.Add(ele.ToString());
                }
                JArray signature = (JArray)data["signature"];
                foreach (var ele in actions)
                {
                    detectioninfo.Signature.Add(ele.ToString());
                }
                GetDetectionInfoResult.OnParentEvent(sender, detectioninfo);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning(e);
            }
        }
    }
}
