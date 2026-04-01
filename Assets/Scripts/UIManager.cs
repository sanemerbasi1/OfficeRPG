using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour 
{
    [Header("Data Table")]
    public PlayerStats playerStats;
    [Header("Panel References")]
    public GameObject startMenuPanel;
    public GameObject charStatPanel;
    public GameObject charTraitPanel;

    [Header("Auto-Assigned (Do Not Drag)")]
    public CharCreationManager charMaster;
    public List<StatHandler> allStats = new List<StatHandler>();

    private void Awake()
    {
        ShowStartUI();

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

    public void ShowStatPanel()
    {
        startMenuPanel.SetActive(false);
        charTraitPanel.SetActive(false);
        charStatPanel.SetActive(true);
    }

    public void ShowStartUI()
    {
        startMenuPanel.SetActive(true);
        charTraitPanel.SetActive(false);
        charStatPanel.SetActive(false);
    }
    public void ShowTraitPanel()
    {
        charStatPanel.SetActive(false);
        startMenuPanel.SetActive(false);
        charTraitPanel.SetActive(true);
    }
    public void QuitGame()
    {
        Application.Quit();
    }
    public void FinalizeAndStartGame()
    {
        if (charMaster.pointsRemaining > 0)
        {
            Debug.LogWarning("You still have points to spend!");
            return;
        }

        foreach (StatHandler stat in allStats)
        {
            stat.SaveToData(playerStats);
        }

        SceneManager.LoadScene("OfficeScene");
    }
}