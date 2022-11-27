using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace CortexPlugin
{
    public class ProfileManager
    {
        CortexClient ctxClient = CortexClient.Instance;
        Authorizer auth = Authorizer.Instance;

        // event buffers to enable engine synchronous callbacks
        public EventBuffer<List<string>> ProfileQueryResult;
        public EventBuffer<string> GetCurrentProfileResult;
        public EventBuffer<string> ProfileCreated;
        public EventBuffer<string> ProfileLoaded;
        public EventBuffer<string> GuestProfileLoaded;
        public EventBuffer<bool> ProfileUnloaded;
        public EventBuffer<string> ProfileSaved;

        string token { get => auth.CortexToken; }

        public void QueryProfiles() { try { ctxClient.QueryProfile(token); } catch (System.Exception e) { Debug.LogWarning(e); } }
        public void GetCurrentProfile(string headsetId) => ctxClient.GetCurrentProfile(token, headsetId);
        public void CreateProfile(string profileName) => ctxClient.SetupProfile(token, profileName, "create");
        public void DeleteProfile(string profileName) => ctxClient.SetupProfile(token, profileName, "delete");
        public void RenameProfile(string oldName, string newName) => ctxClient.SetupProfile(token, oldName, "rename", newProfileName: newName);
        public void LoadProfile(string profileName, string headsetId) => ctxClient.SetupProfile(token, profileName, "load", headsetId);
        public void UnloadProfile(string profileName, string headsetId) => ctxClient.SetupProfile(token, profileName, "unload", headsetId);
        public void SaveProfile(string profileName, string headsetId) => ctxClient.SetupProfile(token, profileName, "save", headsetId);

        public void LoadGuestProfile(string headsetId) => ctxClient.LoadGuestProfile(token, headsetId);

        /// <summary>
        /// Instantite all available event buffers to allow engine
        /// synchronous callbacks, called by Cortex in Start
        /// </summary>
        /// <param name="host">gameobject to attach event buffers to</param>
        public void InstantiateEventBuffers(EventBufferInstance host)
        {
            ProfileQueryResult = new EventBuffer<List<string>>();
            ctxClient.QueryProfileOK += ParseProfileList;

            GetCurrentProfileResult = new EventBuffer<string>();
            ctxClient.GetCurrentProfileDone += OnGetCurrentProfileOK;

            ProfileCreated = new EventBuffer<string>();
            ctxClient.CreateProfileOK += ProfileCreated.OnParentEvent;

            ProfileLoaded = new EventBuffer<string>();
            ctxClient.LoadProfileOK += ProfileLoaded.OnParentEvent;

            GuestProfileLoaded = new EventBuffer<string>();
            ctxClient.LoadGuestProfileOK += GuestProfileLoaded.OnParentEvent;

            ProfileUnloaded = new EventBuffer<bool>();
            ctxClient.UnloadProfileDone += ProfileUnloaded.OnParentEvent;

            ProfileSaved = new EventBuffer<string>();
            ctxClient.SaveProfileOK += ProfileSaved.OnParentEvent;

            var buffers = new EventBufferBase[]
            {
                ProfileQueryResult,
                GetCurrentProfileResult,
                ProfileCreated,
                ProfileLoaded,
                GuestProfileLoaded,
                ProfileUnloaded,
                ProfileSaved,
            };
            host.AddBuffers(buffers);
        }

        /// <summary>
        /// Wraps the get profile list event callback with a readable type
        /// </summary>
        /// <param name="profiles">data to be parsed into a list of profiles</param>
        void ParseProfileList(object sender, JArray profiles)
        {
            try
            {
                List<string> profileLists = new List<string>();
                foreach (JObject ele in profiles)
                {
                    string name = (string)ele["name"];
                    profileLists.Add(name);
                }
                ProfileQueryResult.OnParentEvent(this, profileLists);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning(e);
            }
        }

        /// <summary>
        /// Wraps the get current profile event callback with a readable type and extra functionality
        /// </summary>
        void OnGetCurrentProfileOK(object sender, JObject data)
        {
            if (data["name"].Type == JTokenType.Null)
            {
                Debug.Log("OnGetCurrentProfileDone: no profile loaded with the headset");
            }
            else
            {
                string profileName = data["name"].ToString();
                bool loadByThisApp = (bool)data["loadedByThisApp"];

                if (!loadByThisApp)
                    Debug.LogWarning($"Profile: {profileName} is loaded, but by another app");

                GetCurrentProfileResult.OnParentEvent(sender, profileName);
            }
        }
    }
}
