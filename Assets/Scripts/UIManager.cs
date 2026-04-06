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
       
        if (playerStats != null) playerStats.ResetToDefaults();

     
        OpenMenu("BeginUI"); 

        if (charMaster == null)
        {
            charMaster = GetComponentInChildren<CharCreationManager>(true);
        }

        allStats = GetComponentsInChildren<StatHandler>(true).ToList();

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
    // 1. FIND THE CURRENT MENU
    GameObject currentMenu = allMenus.Find(m => m.activeSelf);

    // 2. IF WE ARE LEAVING THE NAME UI...
    if (currentMenu != null && currentMenu.name == "NameUI") 
    {
        if (string.IsNullOrWhiteSpace(nameInputField.text))
        {
            Debug.LogWarning("Access Denied: Name cannot be empty!");
            return; 
        }
        
        SavePlayerName();
    }

    foreach (GameObject menu in allMenus)
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