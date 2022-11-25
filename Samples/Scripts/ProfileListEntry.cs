using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EmotivUnityPlugin;

public class ProfileListEntry : MonoBehaviour
{
    string profileName;

    public Button selectButton, editButton, deleteButton, confirmButton, cancelButton;
    public GameObject primarySubmenu, renameSubmenu;

    TMP_InputField editText;

    public void Init(string name, string hID)
    {
        profileName = name;
        selectButton.GetComponentInChildren<TextMeshProUGUI>(true).text = name;
        editText = renameSubmenu.GetComponentInChildren<TMP_InputField>(true);

        selectButton.onClick.AddListener(() =>
        {
            Cortex.profiles.LoadProfile(profileName, hID);
        });
        editButton.onClick.AddListener(() =>
        {
            primarySubmenu.SetActive(false);
            renameSubmenu.SetActive(true);
            editText.Select();
        });
        deleteButton.onClick.AddListener(() =>
        {
            Cortex.training.DeleteProfile(profileName);
            Cortex.training.QueryProfiles();
        });
        confirmButton.onClick.AddListener(() =>
        {
            try
            {
                string newName = renameSubmenu.GetComponentInChildren<TMP_InputField>().text;
                Cortex.training.RenameProfile(profileName, newName);
                Cortex.training.QueryProfiles();
            }catch(System.Exception e)
            {
                Debug.LogWarning(e);
            }
        });
        cancelButton.onClick.AddListener(() =>
        {
            primarySubmenu.SetActive(true);

            renameSubmenu.GetComponentInChildren<TMP_InputField>().text = "";
            renameSubmenu.SetActive(false);
        });

        primarySubmenu.SetActive(true);
        renameSubmenu.SetActive(false);
    }
}
