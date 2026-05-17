using UnityEngine;
using System.Collections.Generic;

public class WorldTrigger : MonoBehaviour
{
    public static WorldTrigger ActiveInstance;
    

    [System.Serializable]
    public class TriggerStep
    {
        public StepType type;
        public string speakerName;
        [TextArea(2, 5)] public string textContent; 
        public string menuName; 
        
        public EncounterData encounterData; 
        public Transform enemyTransform;
    }

    [Header("Settings")]
    public string triggerName = "New Trigger";
    public bool triggerOnlyOnce = true;
    
    [Header("The Sequence")]
    public List<TriggerStep> sequence = new List<TriggerStep>();

    private int currentStepIndex = 0;
    private bool hasTriggered = false;
    [SerializeField] private UIManager ui;
    [SerializeField] private BattleManager battleManager;
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (triggerOnlyOnce && hasTriggered) return;

            ActiveInstance = this; 
            currentStepIndex = 0;
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
                ui.ShowDialogue(step.textContent, step.speakerName, () => RunNextStep());
                break;

            case StepType.NameInput:
                ui.ShowNameInputPanel(step.textContent);
                break;

            case StepType.StatMenuManual:
            case StepType.TraitMenuManual:
                ui.OpenMenu(step.menuName); 
                break;

            case StepType.OpenMenu:
                ui.OpenMenu(step.menuName);
                RunNextStep(); 
                break;

            case StepType.CloseUI:
                ui.CloseDialogue();
                RunNextStep();
                break;

            case StepType.Battle:
                if (step.encounterData != null)
                {
                    battleManager.StartBattle(step.encounterData, step.enemyTransform, () => RunNextStep());
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