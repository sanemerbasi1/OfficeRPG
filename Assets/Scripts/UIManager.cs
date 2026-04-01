using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Required for .ToList()

public class UIManager : MonoBehaviour 
{
    [Header("Panel References")]
    public GameObject startMenuPanel;
    public GameObject charCreationPanel;

    [Header("Auto-Assigned (Do Not Drag)")]
    public CharCreationManager charMaster;
    public List<StatHandler> allStats = new List<StatHandler>();

    private void Awake()
    {
        ShowStartUI();
        // 1. Automatically find the Master script in children
        if (charMaster == null)
        {
            charMaster = GetComponentInChildren<CharCreationManager>(true);
        }

        // 2. Automatically find all 6 StatHandler prefabs in children
        // The 'true' allows it to find them even if the panel is currently hidden
        allStats = GetComponentsInChildren<StatHandler>(true).ToList();

        // 3. Simple Validation Check
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

    // Called by your "Back" button in the Character Creator
    public void BackToStart()
    {
        if (charMaster != null)
        {
            charMaster.ResetPoints();
        }

        foreach (StatHandler stat in allStats)
        {
            stat.ResetStat();
        }

        ShowStartUI();
    }

    public void ShowCharacterCreation()
    {
        startMenuPanel.SetActive(false);
        charCreationPanel.SetActive(true);
    }

    public void ShowStartUI()
    {
        startMenuPanel.SetActive(true);
        charCreationPanel.SetActive(false);
    }
    public void QuitGame()
    {
        Application.Quit();
    }
}