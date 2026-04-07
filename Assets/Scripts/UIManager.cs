using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour 

{
    public TMP_InputField nameInputField;
    [Header("Data Table")]
    public PlayerStats playerStats;

    [Header("All Menus (Drag all panels here)")]
    public List<GameObject> allMenus = new List<GameObject>();

    [Header("Auto-Assigned (Do Not Drag)")]
    public CharCreationManager charMaster;
    public List<StatHandler> allStats = new List<StatHandler>();

    private void Awake()
    {
       
        if (playerStats != null) playerStats.ResetToDefaults(); //Its calling a function from PlayerStats script, I already created a reference for it with the playerStats variable and put the PlayerStats scriptable object in the inspector.

        OpenMenu("BeginUI");  // I need to change this if I rename the first menu.

        if (charMaster == null) 
        {
            charMaster = GetComponentInChildren<CharCreationManager>(true); //It takes the CharMaster reference automatically. 
        }

        allStats = GetComponentsInChildren<StatHandler>(true).ToList(); //It looks for all StatHandler scripts inside the UIManager children and adds them to the list as gameObjects.

        ValidateSetup();
    }

    private void ValidateSetup()
    {
        if (charMaster == null) 
            Debug.LogError($"<color=red>UIManager Error:</color> CharCreationManager not found in children of {name}!");
        
        if (allStats.Count == 0) 
            Debug.LogWarning($"<color=yellow>UIManager Warning:</color> No StatHandlers found in children of {name}!");
        else
            Debug.Log($"<color=green>UIManager Success:</color> Found {allStats.Count} stats to manage.");
    }

    public void OpenMenu(string targetMenuName)
{
    GameObject currentMenu = allMenus.Find(m => m.activeSelf); //It gives menus a nickname as m and checks if they are checked as active in the inspector to find the current menu. ActiveSelf is an Unity command. 

    if (currentMenu != null && currentMenu.name == "NameUI") 
    {
        if (string.IsNullOrWhiteSpace(nameInputField.text))
        {
            Debug.LogWarning("Access Denied: Name cannot be empty!");
            return; 
        }
        
        SavePlayerName();
    }

    foreach (GameObject menu in allMenus) //It uses foreach to loop through all the menus and checks if the name of the menu is the same as the targetMenuName which is the menu name we written, if it is it opens that menu and closes the others.
    {
        menu.SetActive(menu.name.ToLower() == targetMenuName.ToLower());
    }
}

    public void ResetAndGoHome()
    {
        if (charMaster != null) charMaster.ResetPoints();

        foreach (StatHandler stat in allStats)
        {
            stat.ResetStat();
        }

        OpenMenu("BeginUI");
    }

    public void QuitGame()
    {
        Application.Quit();
       
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    public void FinalizeAndStartGame()
    {
        if (charMaster != null && charMaster.traitPointsRemaining > 0)
        {
            Debug.LogWarning("You still have points to spend!");
            return;
        }
        else if (charMaster != null && charMaster.traitPointsRemaining == 0)
        {
            Debug.Log("All points spent, proceeding to save stats and load game.");
        }

        foreach (StatHandler stat in allStats)
        {
            stat.SaveToData(playerStats);
        }

        SceneManager.LoadScene("OfficeScene");
    }
    public void GoToTraitMenu()
    {
        if (charMaster != null && charMaster.statPointsRemaining > 0)
        {
            Debug.LogWarning($"<color=orange>UI Blocked:</color> Spend all points ({charMaster.statPointsRemaining} left) before picking traits!");
            
            return; 
        }

        OpenMenu("traitUI");
    }
public void AddTraitToStats(TraitData trait)
{
    if (playerStats.slot1 == null) 
    {
        playerStats.slot1 = trait;
    }
    else if (playerStats.slot2 == null) 
    {
        playerStats.slot2 = trait;
    }
}

public void RemoveTraitFromStats(TraitData trait)
{
    if (playerStats.slot1 == trait) 
    {
        playerStats.slot1 = null;
    }
    else if (playerStats.slot2 == trait) 
    {
        playerStats.slot2 = null;
    }
}
public void SavePlayerName()
{
    if (nameInputField != null)
    {
        playerStats.playerName = nameInputField.text;
        Debug.Log("Player Name Saved: " + playerStats.playerName);
    }
}
}