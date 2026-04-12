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

        
            charMaster = GetComponentInChildren<CharCreationManager>(true); //It takes the CharMaster reference automatically. 

        allStats = GetComponentsInChildren<StatHandler>(true).ToList(); //It looks for all StatHandler scripts inside the UIManager children and adds them to the list as gameObjects.

        ValidateSetup();
    }

    private void ValidateSetup()
{
    if (charMaster == null) 
    {
        // We change Error to Log or Warning. 
        // This keeps your Console clean and doesn't "break" the execution.
        Debug.Log("UIManager: CharCreationManager not found yet. (Normal for Main Menu)");
    }
    else 
    {
        Debug.Log("<color=green>UIManager:</color> CharCreationManager linked successfully!");
    }
}

public void OpenMenu(string targetMenuName)
{
    // Remove the if (currentMenu.name == "NameUI") block entirely.
    // It is safer to let the Button handle the save.

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

       // SceneManager.LoadScene("OfficeScene");
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
public void SaveNameAndProceed(string nextMenuName)
{
    if (nameInputField != null)
    {
        string userTypedValue = nameInputField.text; 

        if (!string.IsNullOrWhiteSpace(userTypedValue))
        {
            playerStats.playerName = userTypedValue.Trim();
            Debug.Log($"<color=green>SUCCESS:</color> Saved {playerStats.playerName}");
            
            // Now that we ARE SURE it saved, change the menu
            OpenMenu(nextMenuName); 
        }
        else
        {
            Debug.LogWarning("Please enter a name!");
        }
    }
}
public void StartGame(string sceneName)
{
    
    SceneManager.LoadScene("OfficeScene");
    }
    
}