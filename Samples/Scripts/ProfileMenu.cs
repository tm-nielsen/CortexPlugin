using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EmotivUnityPlugin;
using TMPro;
using UnityEngine.UI;

public class ProfileMenu : MonoBehaviour
{
    public GameObject profileEntryPrefab;
    public Transform profileList;

    public GameObject splitView;
    public GameObject singleView;

    public Button newProfileButton, confirmButton, singleConfirmButton;
    public GameObject newProfileInput;

    TMP_InputField splitEditField, singleEditField;

    // profile, headset ID
    public static System.EventHandler<(string, string)> ProfileLoadedToHeadset;
    public static System.EventHandler<(string, string)> ProfileUnloadedFromHeadset;

    [HideInInspector]
    public string headsetID, profile;
    [HideInInspector]
    public TrainingMenu trainingMenu;

    // [profile name, headset ID]
    Dictionary<string, string> profileBlacklist = new Dictionary<string, string>();

    void Awake()
    {
        splitEditField = newProfileInput.GetComponentInChildren<TMP_InputField>(true);
        singleEditField = singleView.GetComponentInChildren<TMP_InputField>(true);

        newProfileButton.onClick.AddListener(() =>
        {
            newProfileButton.gameObject.SetActive(false);
            newProfileInput.SetActive(true);
            splitEditField.Select();
        });

        confirmButton.onClick.AddListener(() => CreateNewProfile(splitEditField.text));
        singleConfirmButton.onClick.AddListener(() => CreateNewProfile(singleEditField.text));

        newProfileInput.SetActive(false);
        singleView.SetActive(false);

        Cortex.profiles.ProfileLoaded += OnProfileLoaded;
        Cortex.DataStreamEnded += OnDataStreamEnded;
    }

    void OnEnable()
    {
        Cortex.profiles.ProfileQueryResult += OnProfileQueryResult;
        Cortex.profiles.QueryProfiles();
    }
    void OnDisable() => Cortex.profiles.ProfileQueryResult -= OnProfileQueryResult;

    void OnProfileQueryResult(List<string> names)
    {
        foreach (Transform child in profileList)
            Destroy(child.gameObject);

        bool noExtantProfiles = names.Count == 0;

        singleView.SetActive(noExtantProfiles);
        splitView.SetActive(!noExtantProfiles);

        foreach (string profileName in names)
        {
            if (profileBlacklist.ContainsKey(profileName))
                continue;

            Instantiate(profileEntryPrefab, profileList).GetComponent<ProfileListEntry>().Init(profileName, headsetID);
        }
    }

    public void CreateNewProfile(string profileName)
    {
        Cortex.profiles.CreateProfile(profileName);
        Cortex.profiles.ProfileCreated += LoadProfile;
    }

    public void LoadProfile(string profileName)
    {
        Cortex.profiles.LoadProfile(profileName, headsetID);

        //profile = profileName;
        //profileBlacklist.Add(profileName, headsetID);
    }

    public void LoadGuestProfile()
    {
        Cortex.profiles.LoadGuestProfile(headsetID);
        profile = "Guest Profile";

        if (ProfileLoadedToHeadset != null)
            ProfileLoadedToHeadset(this, ("Guest", headsetID));
    }

    public void UnloadProfile()
    {
        Cortex.profiles.UnloadProfile(profile, headsetID);

        profileBlacklist.Remove(profile);

        if (ProfileUnloadedFromHeadset != null)
            ProfileUnloadedFromHeadset(this, (profile, headsetID));
    }

    void OnProfileLoaded(string profileName)
    {
        profile = profileName;

        profileBlacklist.Add(profileName, headsetID);

        if (ProfileLoadedToHeadset != null)
            ProfileLoadedToHeadset(this, (profileName, headsetID));
    }

    void OnDataStreamEnded(string headsetID)
    {
        string profileToWhitelist = "";

        foreach(var kvp in profileBlacklist)
        {
            if (kvp.Value == headsetID)
                profileToWhitelist = kvp.Key;
        }

        if (!string.IsNullOrEmpty(profileToWhitelist))
            profileBlacklist.Remove(profileToWhitelist);
    }
}
