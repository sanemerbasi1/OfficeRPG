using UnityEngine;
using System.Collections.Generic;

public class WorldTrigger : MonoBehaviour
{
    public static WorldTrigger ActiveInstance;
    

    [System.Serializable]
    public class TriggerStep
    {
        public StepType type;
        [TextArea(2, 5)] public string textContent; 
        public string menuOrSceneName; 
        
        // Added this so the Level Designer can drag an Encounter SO here
        public EncounterData encounterData; 
    }

    [Header("Settings")]
    public string triggerName = "New Trigger";
    public bool triggerOnlyOnce = true;
    
    [Header("The Sequence")]
    public List<TriggerStep> sequence = new List<TriggerStep>();

    private int currentStepIndex = 0;
    private bool hasTriggered = false;
    private UIManager ui;
    private BattleManager battleManager; // Reference to your Battle System

    private void Start()
    {
        ui = Object.FindAnyObjectByType<UIManager>();
        // Assuming your BattleManager is in the scene
        battleManager = Object.FindAnyObjectByType<BattleManager>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (triggerOnlyOnce && hasTriggered) return;

            ActiveInstance = this; 
            currentStepIndex = 0; // Reset index to start from beginning
            RunNextStep();
            hasTriggered = true;
        }
    }

    public void RunNextStep()
    {
        if (currentStepIndex >= sequence.Count)
        {
            ui.CloseDialogue();
            if (ActiveInstance == this) ActiveInstance = null;
            return;
        }

        TriggerStep step = sequence[currentStepIndex];
        currentStepIndex++;

        switch (step.type)
        {
            case StepType.Dialogue:
                ui.ShowDialogue(step.textContent, () => RunNextStep());
                break;

            case StepType.NameInput:
                ui.ShowNameInputPanel(step.textContent);
                break;

            case StepType.StatMenuManual:
            case StepType.TraitMenuManual:
                ui.OpenMenu(step.menuOrSceneName); 
                break;

            case StepType.OpenMenu:
                ui.OpenMenu(step.menuOrSceneName);
                RunNextStep(); 
                break;

            case StepType.StartGame:
                ui.StartGame(step.menuOrSceneName);
                break;

            case StepType.CloseUI:
                ui.CloseDialogue();
                RunNextStep();
                break;

            // --- NEW BATTLE CASE ---
            case StepType.Battle:
                if (step.encounterData != null)
                {
                    // We pass the callback () => RunNextStep() so the battle 
                    // triggers the next part of the sequence ONLY when it ends.
                    battleManager.StartBattle(step.encounterData, () => RunNextStep());
                }
                else
                {
                    Debug.LogWarning("Battle step triggered but no EncounterData assigned!");
                    RunNextStep();
                }
                break;
        }
    }
}