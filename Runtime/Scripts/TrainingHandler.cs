using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace CortexPlugin
{
    /// <summary>
    /// Provides events and methods to enable mental command training
    /// </summary>
    public class TrainingHandler
    {
        CortexClient ctxClient = CortexClient.Instance;

        /// <summary>
        /// Provides a list of trained actions with names and times trained
        /// </summary>
        public EventBuffer<TrainedActions> TrainedActionsResult;
        /// <summary>
        /// Provides the current threshold and last training score for a mental action.
        /// Threshold is the goal a training score should reach to be accepted. both are (0.0 - 1.0)
        /// </summary>
        public EventBuffer<TrainingThreshold> TrainingThresholdResult;
        /// <summary>
        /// Provides the current sensitivity information for a set of active actions
        /// </summary>
        public EventBuffer<ActionSensitivity> ActionSensitivityResult;
        /// <summary>
        /// Provides the approximate length of a training session in seconds
        /// </summary>
        public EventBuffer<double> TrainingTimeResult;
        /// <summary>
        /// Provides information about available actions, controls and events
        /// </summary>
        public EventBuffer<DetectionInfo> DetectionInfoResult;
        /// <summary>
        /// Sent in response to training requests
        /// </summary>
        public EventBuffer<JObject> TrainingRequestResult;

        // proxy events through the data stream
        // https://emotiv.gitbook.io/cortex-api/bci/getdetectioninfo#mental-command
        /// <summary>
        /// A training sample has started
        /// </summary>
        public EventBuffer<string> TrainingStarted;
        /// <summary>
        /// A training sample has successfully finished, must accept or reject the sample
        /// </summary>
        public EventBuffer<string> TrainingSucceeded;
        /// <summary>
        /// A training sample has failed, often due to lack of signal or contact quality
        /// </summary>
        public EventBuffer<string> TrainingFailed;
        /// <summary>
        /// A training sample was started, succeeded, and has now been accepted
        /// </summary>
        public EventBuffer<string> TrainingCompleted;
        /// <summary>
        /// A training sample was started, succeeded, and has now been rejected
        /// </summary>
        public EventBuffer<string> TrainingRejected;
        /// <summary>
        /// Training was successfully cancelled
        /// </summary>
        public EventBuffer<string> TrainingCancelled;

        /// <summary>
        /// The data for a mental command has been successfully erased
        /// </summary>
        public EventBuffer<string> DataErased;
        public EventBuffer<string> AutoSamplingNeutralCompleted;
        public EventBuffer<string> SignatureUpdated;

        Dictionary<string, SystemEventWrapper> systemEventWrappers;

        /// <summary>
        /// The target session Id for training, automatically set to the newest session
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

        string token { get => Authorizer.Instance.CortexToken; }


        /// <summary>
        /// Initiate a training sample for this action
        /// <para>Must be in the set specified by the detection info, use MentalCommandNames for simplicity</para>
        /// </summary>
        public void Start(string action) => Training("start", action);
        /// <summary>
        /// Accept a successful training sample for this action
        /// <para>Must be in the set specified by the detection info, use MentalCommandNames for simplicity</para>
        /// </summary>
        public void Accept(string action) => Training("accept", action);
        /// <summary>
        /// Reject a successful training sample for this action
        /// <para>Must be in the set specified by the detection info, use MentalCommandNames for simplicity</para>
        /// </summary>
        public void Reject(string action) => Training("reject", action);
        /// <summary>
        /// Cancel an in progress sample for this action
        /// <para>Must be in the set specified by the detection info, use MentalCommandNames for simplicity</para>
        /// </summary>
        public void Cancel(string action) => Training("reset", action);
        /// <summary>
        /// Erase all training data for this action
        /// <para>Must be in the set specified by the detection info, use MentalCommandNames for simplicity</para>
        /// </summary>
        public void Erase(string action) => Training("erase", action);

        void Training(string status, string action) => ctxClient.Training(token, TargetSession, status, "mentalCommand", action);

        /// <summary>
        /// Save the training progress of this profile being used used with this headset
        /// </summary>
        public void SaveProfile(string profileName, string headsetId) => ctxClient.SetupProfile(token, profileName, "save", headsetId);


        /// <summary>
        /// Get the names of actions, controls, and events available for mental commands
        /// <para>subscribe to GetDetectionInfoResult for result</para>
        /// </summary>
        public void GetMentalCommandInfo() => GetDetectionInfo("mentalCommand");
        /// <summary>
        /// Get the names of actions, controls, and events available for the specified detection category
        /// <para>subscribe to GetDetectionInfoResult for result</para>
        /// </summary>
        /// <param name="detection">must be either "mentalCommand" or "facialExpression"</param>
        public void GetDetectionInfo(string detection) => ctxClient.GetDetectionInfo(detection);
        /// <summary>
        /// Get the approximate length of a training sample in seconds
        /// <para>subscribe to TrainingTimeResult for result</para>
        /// </summary>
        public void GetTrainingTime() => ctxClient.GetTrainingTime(token, "mentalCommand", TargetSession);
        /// <summary>
        /// Get a list of trained actions for this profile, including names and times trained
        /// <para>subscribe to GetTrainedActionsResult for result</para>
        /// </summary>
        public void GetTrainedActions(string profileName) => ctxClient.GetTrainedSignatureActions(token, "mentalCommand", profileName);
        /// <summary>
        /// Get the current threshold and last training score for the target session
        /// <para>subscribe to GetTrainingThresholdResult for result</para>
        /// </summary>
        public void GetTrainingThreshold() => ctxClient.MentalCommandTrainingThreshold(token, sessionId: TargetSession);

        /// <summary>
        /// Get the current sensitivity information for a set of actions active on this profile
        /// <para>subscribe to GetActionSensitivity for result</para>
        /// </summary>
        public void GetActionSensitivity(string profileName) => ctxClient.MentalCommandActionSensitivity(token, "get", profileName);
        /// <summary>
        /// Get the current sensitivity information for a set of actions active in this session
        /// <para>subscribe to GetActionSensitivity for result</para>
        /// </summary>
        public void GetActionSensitivityBySession(string sessionId) => ctxClient.MentalCommandActionSensitivity(token, "get", sessionId: sessionId);
        /// <summary>
        /// Get the current sensitivity information for a set of actions active in the target session
        /// <para>subscribe to GetActionSensitivity for result</para>
        /// </summary>
        public void GetActionSensitivity() => ctxClient.MentalCommandActionSensitivity(token, "get", sessionId: TargetSession);

        /// <summary>
        /// Assign the sensitivity for a set of action active on this profile
        /// </summary>
        public void SetActionSensitivity(string profileName, int[] values) => ctxClient.MentalCommandActionSensitivity(token, "set", profileName, values: values);
        /// <summary>
        /// Assign the sensitivity for a set of action active in this session
        /// </summary>
        public void SetActionSensitivityBySession(string sessionId, int[] values) => ctxClient.MentalCommandActionSensitivity(token, "set", sessionId: sessionId, values: values);
        /// <summary>
        /// Assign the sensitivity for a set of action active in the target session
        /// </summary>
        public void SetActionSensitivity(int[] values) => ctxClient.MentalCommandActionSensitivity(token, "set", sessionId: TargetSession, values: values);


        /// <summary>
        /// Instantiate all available event buffers to allow engine
        /// synchronous callbacks, called by Cortex in Start
        /// </summary>
        public TrainingHandler(EventBufferInstance host)
        {
            TrainedActionsResult = new EventBuffer<TrainedActions>();
            ctxClient.GetTrainedSignatureActionsOK += TrainedActionsResult.OnParentEvent;

            TrainingThresholdResult = new EventBuffer<TrainingThreshold>();
            ctxClient.MentalCommandTrainingThresholdOK += TrainingThresholdResult.OnParentEvent;

            ActionSensitivityResult = new EventBuffer<ActionSensitivity>();
            ctxClient.MentalCommandActionSensitivityOK += ActionSensitivityResult.OnParentEvent;

            TrainingTimeResult = new EventBuffer<double>();
            ctxClient.GetTrainingTimeDone += TrainingTimeResult.OnParentEvent;

            DetectionInfoResult = new EventBuffer<DetectionInfo>();
            ctxClient.GetDetectionInfoDone += ParseDetectionInfo;

            TrainingRequestResult = new EventBuffer<JObject>();
            ctxClient.TrainingOK += TrainingRequestResult.OnParentEvent;


            TrainingStarted = new EventBuffer<string>();
            TrainingSucceeded = new EventBuffer<string>();
            TrainingFailed = new EventBuffer<string>();
            TrainingCompleted = new EventBuffer<string>();
            TrainingRejected = new EventBuffer<string>();

            DataErased = new EventBuffer<string>();
            TrainingCancelled = new EventBuffer<string>();
            AutoSamplingNeutralCompleted = new EventBuffer<string>();
            SignatureUpdated = new EventBuffer<string>();

            var buffers = new EventBufferBase[]
            {
                DetectionInfoResult,
                TrainingRequestResult,
                TrainingTimeResult,
                TrainedActionsResult,
                TrainingThresholdResult,
                ActionSensitivityResult,

                TrainingStarted,
                TrainingSucceeded,
                TrainingFailed,
                TrainingCompleted,
                TrainingRejected,

                DataErased,
                TrainingCancelled,
                AutoSamplingNeutralCompleted,
                SignatureUpdated
            };
            host.AddBuffers(buffers);

            Cortex.DataStreamStarted += OnDataStreamStarted;
            Cortex.DataStreamEnded += OnDataStreamEnded;
        }

        /// <summary>
        /// Wraps the get detection info event callback with a useful data type
        /// </summary>
        void ParseDetectionInfo(object sender, JObject data)
        {
            DetectionInfoResult.OnParentEvent(sender, new DetectionInfo(data));
        }


        void OnDataStreamStarted(string headsetId)
        {
            if (systemEventWrappers == null)
                systemEventWrappers = new Dictionary<string, SystemEventWrapper>();

            systemEventWrappers[headsetId] = new SystemEventWrapper(headsetId, OnSystemEventReceived);
        }
        void OnDataStreamEnded(string headsetId)
        {
            systemEventWrappers.Remove(headsetId);
        }

        void OnSystemEventReceived(string headsetId, SystemEventCode systemEventCode)
        {
            switch (systemEventCode)
            {
                case SystemEventCode.Started:
                    TrainingStarted.OnParentEvent(this, headsetId);
                    break;
                case SystemEventCode.Suceeded:
                    TrainingSucceeded.OnParentEvent(this, headsetId);
                    break;
                case SystemEventCode.Failed:
                    TrainingFailed.OnParentEvent(this, headsetId);
                    break;
                case SystemEventCode.Completed:
                    TrainingCompleted.OnParentEvent(this, headsetId);
                    break;
                case SystemEventCode.Rejected:
                    TrainingRejected.OnParentEvent(this, headsetId);
                    break;

                case SystemEventCode.DataErased:
                    DataErased.OnParentEvent(this, headsetId);
                    break;
                case SystemEventCode.Reset:
                    TrainingCancelled.OnParentEvent(this, headsetId);
                    break;
                case SystemEventCode.AutoSamplingNeutralCompleted:
                    AutoSamplingNeutralCompleted.OnParentEvent(this, headsetId);
                    break;
                case SystemEventCode.SignatureUpdated:
                    SignatureUpdated.OnParentEvent(this, headsetId);
                    break;
            }
        }

        /// <summary>
        /// Wraps data stream system events with a headset id
        /// </summary>
        private class SystemEventWrapper
        {
            System.Action<string, SystemEventCode> sendSystemEvent;
            string headsetId;

            public SystemEventWrapper(string headsetId, System.Action<string, SystemEventCode> action)
            {
                this.headsetId = headsetId;
                sendSystemEvent = action;
                Cortex.SubscribeSystemEvents(headsetId, OnSystemEventReceived);
            }
            ~SystemEventWrapper()
            {
                Cortex.UnsubscribeSystemEvents(headsetId, OnSystemEventReceived);
            }

            void OnSystemEventReceived(SystemEventArgs args)
            {
                sendSystemEvent(headsetId, args.code);
            }
        }
    }

    /// <summary>
    /// Simplified proxy for mental command names to avoid having to reuse string literals
    /// </summary>
    public static class MentalCommandNames
    {
        public const string Neutral = "neutral";
        public const string Push = "push";
        public const string Pull = "pull";
        public const string Lift = "lift";
        public const string Drop = "drop";
        public const string Left = "left";
        public const string Right = "right";
    }
}
