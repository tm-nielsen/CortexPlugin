using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace CortexPlugin
{
    /// <summary>
    /// Provides events and methods to enable profile management
    /// </summary>
    public class ProfileManager
    {
        CortexClient ctxClient = CortexClient.Instance;

        /// <summary>
        /// Provides a list of names beloning to all extant profiles
        /// </summary>
        public EventBuffer<List<string>> ProfileListQueryResult;
        /// <summary>
        /// Provides the profile name currently loaded to the headset provided in the related request
        /// </summary>
        public EventBuffer<string> CurrentProfileResult;
        /// <summary>
        /// A new profile has been created with the provided name
        /// </summary>
        public EventBuffer<string> ProfileCreated;
        /// <summary>
        /// The profile with this name has been loaded to a headset
        /// </summary>
        public EventBuffer<string> ProfileLoaded;
        /// <summary>
        /// A guest profile has been loaded, argument is not meaningful
        /// </summary>
        public EventBuffer<string> GuestProfileLoaded;
        /// <summary>
        /// A profile has been unloaded from a headset
        /// </summary>
        public EventBuffer<bool> ProfileUnloaded;
        /// <summary>
        /// Changes to the profile with this name have been saved
        /// </summary>
        public EventBuffer<string> ProfileSaved;

        string token { get => Authorizer.Instance.CortexToken; }

        /// <summary>
        /// Trigger a query for available profiles
        /// <para>subscribe to ProfileListQueryResult for result</para>
        /// </summary>
        public void QueryProfileList() { try { ctxClient.QueryProfile(token); } catch (System.Exception e) { Debug.LogWarning(e); } }
        /// <summary>
        /// Get the profile loaded on this headset
        /// <para>subscribe to getCurrentProfileRersult for result</para>
        /// </summary>
        public void GetCurrent(string headsetId) => ctxClient.GetCurrentProfile(token, headsetId);
        /// <summary>
        /// Create a new profile with this name
        /// </summary>
        public void Create(string profileName) => ctxClient.SetupProfile(token, profileName, "create");
        /// <summary>
        /// Delete the profile with this name
        /// </summary>
        public void Delete(string profileName) => ctxClient.SetupProfile(token, profileName, "delete");
        /// <summary>
        /// Rename the profile with this name to this new name
        /// </summary>
        public void Rename(string oldName, string newName) => ctxClient.SetupProfile(token, oldName, "rename", newProfileName: newName);
        /// <summary>
        /// Load the profile with this name to this headset
        /// </summary>
        public void Load(string profileName, string headsetId) => ctxClient.SetupProfile(token, profileName, "load", headsetId);
        /// <summary>
        /// Unload this profile from this headset
        /// </summary>
        public void Unload(string profileName, string headsetId) => ctxClient.SetupProfile(token, profileName, "unload", headsetId);
        /// <summary>
        /// Save the potentially modified contents of the profile with htis name currently loaded to this headset.
        /// Necessary to save training progress.
        /// </summary>
        public void Save(string profileName, string headsetId) => ctxClient.SetupProfile(token, profileName, "save", headsetId);

        /// <summary>
        /// Load a guest profile to this headset
        /// </summary>
        public void LoadGuest(string headsetId) => ctxClient.LoadGuestProfile(token, headsetId);

        /// <summary>
        /// Instantite all available event buffers to allow engine
        /// synchronous callbacks, called by Cortex in Start
        /// </summary>
        public ProfileManager(EventBufferInstance host)
        {
            ProfileListQueryResult = new EventBuffer<List<string>>();
            ctxClient.QueryProfileOK += ParseProfileList;

            CurrentProfileResult = new EventBuffer<string>();
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
                ProfileListQueryResult,
                CurrentProfileResult,
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
                ProfileListQueryResult.OnParentEvent(this, profileLists);
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
                Debug.LogWarning("No profile loaded with the headset");
            }
            else
            {
                string profileName = data["name"].ToString();
                bool loadByThisApp = (bool)data["loadedByThisApp"];

                if (!loadByThisApp)
                    Debug.LogWarning($"Profile: {profileName} is loaded, but by another app");

                CurrentProfileResult.OnParentEvent(sender, profileName);
            }
        }
    }
}
