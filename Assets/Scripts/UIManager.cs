using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Events;

public class UIManager : MonoBehaviour 
{
    public TMP_InputField nameInputField;
    
    [Header("Data Table")]
    public PlayerStats playerStats;

    [Header("All Menus (Drag all panels here)")]
    public List<GameObject> allMenus = new List<GameObject>();

    [Header("Dialogue & Name Input UI")]
    public GameObject nameInputPanel;
    public GameObject dialoguePanel;
    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI dialogueText;
    public Button dialogueContinueButton; 

    [Header("Auto-Assigned (Do Not Drag)")]
    public CharCreationManager charMaster;
    public List<StatHandler> allStats = new List<StatHandler>();

    private void Awake()
    {
        if (playerStats != null) playerStats.ResetToDefaults();
        OpenMenu("BeginUI");  
        charMaster = GetComponentInChildren<CharCreationManager>(true); 
        allStats = GetComponentsInChildren<StatHandler>(true).ToList(); 
    }

    public void OpenMenu(string targetMenuName)
    {
        foreach (GameObject menu in allMenus)
        {
            if(menu != null)
                menu.SetActive(menu.name.ToLower() == targetMenuName.ToLower());
        }
    }

    public void ShowDialogue(string message, UnityAction onContinueAction = null)
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(true);

        string processedMessage = message;

        // Swaps {name} with the player's name (and respects any <b> tags you put in the inspector)
        if (playerStats != null && !string.IsNullOrEmpty(playerStats.playerName))
        {
            processedMessage = message.Replace("{name}", playerStats.playerName);
        }

        if (dialogueText != null) dialogueText.text = processedMessage;

        TogglePlayerMovement(false);

        if (dialogueContinueButton != null)
        {
            dialogueContinueButton.onClick.RemoveAllListeners();
            
            // This links back to the Lambda in WorldTrigger: () => RunNextStep()
            if (onContinueAction != null)
                dialogueContinueButton.onClick.AddListener(onContinueAction);
            else
                dialogueContinueButton.onClick.AddListener(CloseDialogue);
        }
    }

    public void CloseDialogue()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (nameInputPanel != null) nameInputPanel.SetActive(false);
        
        foreach (GameObject menu in allMenus)
        {
            if (menu != null) menu.SetActive(false);
        }

        TogglePlayerMovement(true);
    }

    // --- NAME INPUT ---
    public void ShowNameInputPanel(string message)
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (nameInputPanel != null) nameInputPanel.SetActive(true);
        if (playerNameText != null) playerNameText.text = message;
        TogglePlayerMovement(false);
    }

    // Link this to your Name Save Button
    public void SaveNameAndProceed(string fallbackMenu)
    {
        if (nameInputField != null && !string.IsNullOrWhiteSpace(nameInputField.text))
        {
            playerStats.playerName = nameInputField.text.Trim();
            CloseDialogue(); 
            CheckForActiveSequence(fallbackMenu);
        }
    }

    // --- STATS & TRAITS ---
    // Link this to your Stat "Done" Button
    public void FinalizeStatSelection(string fallbackMenu)
    {
        if (charMaster != null && charMaster.statPointsRemaining > 0)
        {
            Debug.LogWarning("Spend all points first!");
            return;
        }

        foreach (StatHandler stat in allStats)
        {
            stat.SaveToData(playerStats);
        }

        CloseDialogue(); 
        CheckForActiveSequence(fallbackMenu);
    }

    // Link this to your Trait "Done" Button
    public void FinalizeTraitSelection(string fallbackMenu)
    {
        if (charMaster != null && charMaster.traitPointsRemaining > 0)
        {
            Debug.LogWarning("Select all traits first!");
            return;
        }

        CloseDialogue(); 
        CheckForActiveSequence(fallbackMenu);
    }

    // --- THE "UNPAUSE" BUTTON ---
    private void CheckForActiveSequence(string fallbackMenu)
{
    // We call the class name directly because ActiveInstance is static
    if (WorldTrigger.ActiveInstance != null)
    {
        WorldTrigger.ActiveInstance.RunNextStep();
    }
    else if (!string.IsNullOrEmpty(fallbackMenu) && fallbackMenu != "none")
    {
        OpenMenu(fallbackMenu);
    }
}

    // ... (rest of your Add/Remove trait and movement logic) ...
    public void AddTraitToStats(TraitData trait)
    {
        if (playerStats.slot1 == null) playerStats.slot1 = trait;
        else if (playerStats.slot2 == null) playerStats.slot2 = trait;
    }

    public void RemoveTraitFromStats(TraitData trait)
    {
        if (playerStats.slot1 == trait) playerStats.slot1 = null;
        else if (playerStats.slot2 == trait) playerStats.slot2 = null;
    }

    private void TogglePlayerMovement(bool canMove)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var controller = player.GetComponent<PlayerController>();
            if (controller != null) controller.enabled = canMove;
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }
    }

    public void StartGame(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}