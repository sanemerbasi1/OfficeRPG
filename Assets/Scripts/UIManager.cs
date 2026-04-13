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
    private bool nameInputActivated = false;

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
            menu.SetActive(menu.name.ToLower() == targetMenuName.ToLower());
        }
    }

    // --- DIALOGUE SYSTEM ---
    public void ShowDialogue(string message, UnityAction onContinueAction = null)
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(true);

        string processedMessage = message;

        if (playerStats != null && !string.IsNullOrEmpty(playerStats.playerName))
        {
            processedMessage = message.Replace("{name}", playerStats.playerName);
        }

        if (dialogueText != null) dialogueText.text = processedMessage;

        TogglePlayerMovement(false);

        if (dialogueContinueButton != null)
        {
            dialogueContinueButton.onClick.RemoveAllListeners();
            
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
        
        // Also ensure all other panels in allMenus are closed
        foreach (GameObject menu in allMenus)
        {
            menu.SetActive(false);
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

    public void SaveNameAndProceed(string fallbackMenu)
    {
        if (nameInputField != null && !string.IsNullOrWhiteSpace(nameInputField.text))
        {
            playerStats.playerName = nameInputField.text.Trim();
            
            // Clean up the input panel and move to next step
            CloseDialogue(); 

            CheckForActiveSequence(fallbackMenu);
        }
    }

    // --- STATS & TRAITS SEPARATED ---

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

        // IMPORTANT: Close the menu before proceeding to the next sequence step
        CloseDialogue(); 

        CheckForActiveSequence(fallbackMenu);
    }

    public void FinalizeTraitSelection(string fallbackMenu)
    {
        if (charMaster != null && charMaster.traitPointsRemaining > 0)
        {
            Debug.LogWarning("Select all traits first!");
            return;
        }

        // IMPORTANT: Close the menu before proceeding to the next sequence step
        CloseDialogue(); 

        CheckForActiveSequence(fallbackMenu);
    }

    private void CheckForActiveSequence(string fallbackMenu)
    {
        WorldTrigger activeTrigger = Object.FindAnyObjectByType<WorldTrigger>();
        if (activeTrigger != null)
        {
            activeTrigger.RunNextStep();
        }
        else if (!string.IsNullOrEmpty(fallbackMenu) && fallbackMenu != "none")
        {
            OpenMenu(fallbackMenu);
        }
    }

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

    // --- UTILS ---
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
        SceneManager.LoadScene("OfficeScene");
    }

    public void QuitGame()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}