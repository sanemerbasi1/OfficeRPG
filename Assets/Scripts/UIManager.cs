using UnityEngine;
using System.Collections.Generic;
using System.Collections;
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

    [Header("New Day Transition UI")]
    [SerializeField] private GameObject newDayPanel;         
    [SerializeField] private CanvasGroup newDayCanvasGroup;  
    [SerializeField] private TextMeshProUGUI newDayText;      
    
    [Header("Dialogue & Name Input UI")]
    public GameObject nameInputPanel;
    public TextMeshProUGUI nameInputText;
    public TextMeshProUGUI inputSpeakerNameText;
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI speakerNameText;
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

    public void ShowDialogue(string message, string speakerName = " ", UnityAction onContinueAction = null)
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        
        if (speakerNameText != null) speakerNameText.text = speakerName;

        string processedMessage = message;

        if (nameInputText != null && !string.IsNullOrEmpty(nameInputText.text))        
        {
            processedMessage = message.Replace("{name}", nameInputText.text);
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
        
        foreach (GameObject menu in allMenus) 
        {
            if (menu != null) menu.SetActive(false);
        }

        TogglePlayerMovement(true);

        // Check if DayManager has a pending calendar rollout sequence waiting
        if (DayManager.Instance != null)
        {
            DayManager.Instance.CompleteDayChange();
        }
    }

    public void ShowNameInputPanel(string message, string speakerName = " ")
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (nameInputPanel != null) nameInputPanel.SetActive(true);
        if (nameInputText != null) nameInputText.text = message;
        if (inputSpeakerNameText != null) inputSpeakerNameText.text = speakerName;
        TogglePlayerMovement(false);
    }

    public void SaveNameAndProceed(string fallbackMenu)
    {
        if (nameInputField != null && !string.IsNullOrWhiteSpace(nameInputField.text))
        {
            playerStats.playerName = nameInputField.text.Trim();
            CloseDialogue(); 
            CheckForActiveSequence(fallbackMenu);
        }
    }

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

    private void CheckForActiveSequence(string fallbackMenu)
    {
        if (WorldTrigger.ActiveInstance != null)
        {
            WorldTrigger.ActiveInstance.RunNextStep();
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

    public void TogglePlayerMovement(bool canMove)
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

    public void ShowNewDayTransition(int dayNumber, System.Action onTransitionComplete)
    {
        if (newDayPanel == null)
        {
            Debug.LogWarning("[UI WARNING] New Day Panel reference is missing! Skipping animation.");
            onTransitionComplete?.Invoke();
            return;
        }

        if (newDayText != null)
        {
            newDayText.text = $"DAY {dayNumber}";
        }

        StartCoroutine(NewDaySequence(onTransitionComplete));
    }

    // Explicit System.Collections reference prevents namespace conflict errors with Generic Lists
    private System.Collections.IEnumerator NewDaySequence(System.Action onTransitionComplete)
    {
        // STEP 1: Pop the screen up to solid black instantly
        newDayPanel.SetActive(true);
        if (newDayCanvasGroup != null)
        {
            newDayCanvasGroup.alpha = 1f; 
        }

        // STEP 2: Execute player location teleportation NOW while screen is 100% pitch black
        onTransitionComplete?.Invoke();

        // Hold on the black screen briefly for calendar card readability
        yield return new WaitForSeconds(1.5f);

        // STEP 3: Smoothly fade out the black mask to reveal the player back at start position
        float fadeDuration = 1.0f;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            if (newDayCanvasGroup != null)
            {
                newDayCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            }
            yield return null; 
        }

        if (newDayCanvasGroup != null) newDayCanvasGroup.alpha = 0f;
        newDayPanel.SetActive(false);
    }
}