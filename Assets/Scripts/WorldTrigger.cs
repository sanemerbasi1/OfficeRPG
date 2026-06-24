using UnityEngine;
using System.Collections.Generic;

public class WorldTrigger : MonoBehaviour
{
    public static WorldTrigger ActiveInstance;

    [System.Serializable]
    public class DialogueChoice
    {
        [TextArea(1, 2)] 
        public string choiceText = "New Choice";
        
        [Header("Reward")]
        public StatType statReward; 
        public int rewardAmount = 0; 

        [Header("Branching")]
        [Tooltip("Leave empty to go to the next step. Enter a Step ID to jump to a specific branch.")]
        public string targetStepID = ""; 
    }

    [System.Serializable]
    public class TriggerStep
    {
        [Header("Branching Identifiers")]
        [Tooltip("Assign an ID here if you want a choice button (or another step) to jump directly to this step.")]
        public string stepID = "";

        [Tooltip("NEW: After this specific step finishes playing, jump directly to this destination Step ID. Perfect for merging back to the main path!")]
        public string jumpToAfterStep = "";

        [Header("Step Settings")]
        public StepType type;
        public string speakerName;
        [TextArea(2, 5)] public string textContent; 
        public string menuName; 
        public EncounterData encounterData; 
        
        [Header("For Choice Menus Only")]
        public List<DialogueChoice> choices = new List<DialogueChoice>();
    }

    [Header("Settings")]
    public string triggerName = "New Trigger";
    public bool triggerOnlyOnce = true;
    
    [Header("Grid Combat Links")]
    [Tooltip("Drag the overworld enemy sprite/prefab instance here so it snaps to the grid when the fight starts!")]
    [SerializeField] private GridUnit fieldEnemyUnit;

    [Header("The Sequence")]
    public List<TriggerStep> sequence = new List<TriggerStep>();

    private int currentStepIndex = 0;
    private bool hasTriggered = false;
    
    [Header("Dependencies")]
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
                // MODIFIED: The continue button callback now checks if it needs to merge/jump somewhere else!
                ui.ShowDialogue(step.textContent, step.speakerName, () => 
                {
                    if (!string.IsNullOrEmpty(step.jumpToAfterStep))
                    {
                        JumpToStep(step.jumpToAfterStep);
                    }
                    else
                    {
                        RunNextStep();
                    }
                });
                break;

            case StepType.NameInput:
                ui.ShowNameInputPanel(step.textContent, step.speakerName);
                break;

            case StepType.StatMenuManual:
            case StepType.TraitMenuManual:
                ui.OpenMenu(step.menuName); 
                break;

            case StepType.OpenMenu:
                ui.OpenMenu(step.menuName);
                // Can also use jump logic here if needed
                if (!string.IsNullOrEmpty(step.jumpToAfterStep)) JumpToStep(step.jumpToAfterStep);
                else RunNextStep(); 
                break;

            case StepType.CloseUI:
                ui.CloseDialogue();
                if (!string.IsNullOrEmpty(step.jumpToAfterStep)) JumpToStep(step.jumpToAfterStep);
                else RunNextStep();
                break;

            case StepType.UpdateQuest:
                ui.UpdateQuestText(step.textContent); 
                if (!string.IsNullOrEmpty(step.jumpToAfterStep)) JumpToStep(step.jumpToAfterStep);
                else RunNextStep(); 
                break;

            case StepType.ChoiceMenu:
                ui.ShowChoices(step.choices, (selectedChoice) => 
                {
                    if (selectedChoice.rewardAmount > 0)
                    {
                        ui.playerStats.AddStat(selectedChoice.statReward, selectedChoice.rewardAmount);
                    }

                    if (!string.IsNullOrEmpty(selectedChoice.targetStepID))
                    {
                        JumpToStep(selectedChoice.targetStepID);
                    }
                    else
                    {
                        RunNextStep(); 
                    }
                });
                break;

            case StepType.Battle:
                if (step.encounterData != null)
                {
                    if (PlayerController.Instance != null)
                    {
                        PlayerController.Instance.isInBattle = true;

                        GridUnit playerGridUnit = PlayerController.Instance.GetComponent<GridUnit>();
                        if (playerGridUnit != null && BattleGrid.Instance != null)
                        {
                            Vector2Int playerSnappedGrid = BattleGrid.Instance.WorldToGrid(PlayerController.Instance.transform.position);
                            playerGridUnit.SnapToGridPosition(playerSnappedGrid);
                            BattleGrid.Instance.RegisterUnitPosition(playerGridUnit);
                        }
                    }

                    if (fieldEnemyUnit != null && BattleGrid.Instance != null)
                    {
                        Vector2Int enemySnappedGrid = BattleGrid.Instance.WorldToGrid(fieldEnemyUnit.transform.position);
                        fieldEnemyUnit.SnapToGridPosition(enemySnappedGrid);
                        BattleGrid.Instance.RegisterUnitPosition(fieldEnemyUnit);
                    }

                    battleManager.StartBattle(step.encounterData, fieldEnemyUnit, () => 
                    {
                        if (PlayerController.Instance != null)
                        {
                            PlayerController.Instance.isInBattle = false;
                        }

                        if (BattleGrid.Instance != null)
                        {
                            BattleGrid.Instance.ClearAllHighlights();
                        }

                        if (!string.IsNullOrEmpty(step.jumpToAfterStep)) JumpToStep(step.jumpToAfterStep);
                        else RunNextStep();
                    });
                }
                else
                {
                    Debug.LogWarning($"[TRIGGER: {triggerName}] Battle step reached but no EncounterData assigned!");
                    if (!string.IsNullOrEmpty(step.jumpToAfterStep)) JumpToStep(step.jumpToAfterStep);
                    else RunNextStep();
                }
                break;
        }
    }

    private void JumpToStep(string targetID)
    {
        for (int i = 0; i < sequence.Count; i++)
        {
            if (sequence[i].stepID == targetID)
            {
                currentStepIndex = i; 
                RunNextStep();        
                return;
            }
        }

        Debug.LogWarning($"[WorldTrigger: {triggerName}] Jump failed! Could not find a step matching Step ID: '{targetID}'. Exiting sequence.");
        ui.CloseDialogue();
    }
}