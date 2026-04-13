using UnityEngine;
using System.Collections.Generic;

public class WorldTrigger : MonoBehaviour
{
    public enum StepType { Dialogue, NameInput, OpenMenu, StatMenuManual, TraitMenuManual, StartGame, CloseUI }

    [System.Serializable]
    public class TriggerStep
    {
        public StepType type;
        [TextArea(2, 5)] public string textContent; 
        public string menuOrSceneName; 
    }

    [Header("Settings")]
    public string triggerName = "New Trigger";
    public bool triggerOnlyOnce = true;
    
    [Header("The Sequence")]
    public List<TriggerStep> sequence = new List<TriggerStep>();

    private int currentStepIndex = 0;
    private bool hasTriggered = false;
    private UIManager ui;

    private void Start()
    {
        ui = Object.FindAnyObjectByType<UIManager>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (triggerOnlyOnce && hasTriggered) return;
            RunNextStep();
            hasTriggered = true;
        }
    }

    public void RunNextStep()
    {
        if (currentStepIndex >= sequence.Count)
        {
            ui.CloseDialogue();
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
                // We do NOT call RunNextStep here. 
                // The UI button will call it via UIManager.
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
        }
    }
}