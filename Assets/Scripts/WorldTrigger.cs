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
        public string stepID = "";
        public string jumpToAfterStep = "";

        [Header("Step Settings")]
        public StepType type;
        public string speakerName;
        [TextArea(2, 5)] public string textContent; 
        public string menuName; 
        public EncounterData encounterData; 
        
        [Header("Dialogue Animation")]
        public Animator npcAnimator;
        
        [Header("For Toggle UI Elements Only")]
        [Tooltip("Drag all the UI GameObjects you want to affect into this list.")]
        public List<GameObject> targetUIElements = new List<GameObject>();
        
        [Tooltip("True = Turn On. False = Turn Off.")]
        public bool targetUIState = true; 
        
        [Tooltip("Check this if you want the UI to revert back after a few seconds.")]
        public bool isTemporary = false;
        
        [Tooltip("How many seconds before reverting?")]
        public float activeDuration = 3f;

        [Tooltip("Should the sequence WAIT for the timer to finish before moving to the next step?")]
        public bool waitToFinish = true;

        [Header("For Choice Menus Only")]
        public List<DialogueChoice> choices = new List<DialogueChoice>();

        // --- NEW: Audio settings for this specific step ---
        [Header("Battle Audio (Optional)")]
        public AudioClip battleStartSound;
        public AudioClip battleLoopMusic;
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

    // --- NEW: Audio Sources to actually play the clips ---

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
                if (step.npcAnimator != null)
                {
                    step.npcAnimator.SetBool("IsTalking", true);
                }

                ui.ShowDialogue(step.textContent, step.speakerName, () => 
                {
                    if (step.npcAnimator != null)
                    {
                        step.npcAnimator.SetBool("IsTalking", false);
                    }

                    if (!string.IsNullOrEmpty(step.jumpToAfterStep)) JumpToStep(step.jumpToAfterStep);
                    else RunNextStep();
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

            case StepType.ToggleUIElement:
                if (ui != null)
                {
                    ui.CloseDialogue();
                }

                if (step.targetUIElements.Count > 0)
                {
                    if (step.isTemporary)
                    {
                        StartCoroutine(HandleTemporaryUIGroup(step));
                    }
                    else
                    {
                        foreach (GameObject uiElement in step.targetUIElements)
                        {
                            if (uiElement != null) uiElement.SetActive(step.targetUIState);
                        }

                        if (!string.IsNullOrEmpty(step.jumpToAfterStep)) JumpToStep(step.jumpToAfterStep);
                        else RunNextStep();
                    }
                }
                else
                {
                    Debug.LogWarning($"[WorldTrigger: {triggerName}] ToggleUIElement step has an empty UI list!");
                    RunNextStep(); 
                }
                break;

            case StepType.Battle:
    if (step.encounterData != null)
    {
        // 1. Setup Grid positions
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

        // 2. Audio: Switch to Battle Theme using the MusicController singleton
        if (MusicController.Instance != null)
        {
            MusicController.Instance.PauseBGM();
            if (step.battleStartSound != null)
{
    MusicController.Instance.PlaySFX(step.battleStartSound);
}
            MusicController.Instance.PlayBattleMusic(step.battleLoopMusic);
        }

        // 3. Start the Battle
        battleManager.StartBattle(step.encounterData, fieldEnemyUnit, () => 
        {
            // --- POST-BATTLE LOGIC ---
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.isInBattle = false;
            }

            if (BattleGrid.Instance != null)
            {
                BattleGrid.Instance.ClearAllHighlights();
            }

            // 4. Cleanup: Stop Battle Theme and Resume BGM
            if (MusicController.Instance != null)
            {
                MusicController.Instance.StopBattleMusic();
                MusicController.Instance.ResumeBGM();
            }

            // Continue the sequence
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

    private System.Collections.IEnumerator HandleTemporaryUIGroup(TriggerStep step)
    {
        foreach (GameObject uiElement in step.targetUIElements)
        {
            if (uiElement != null) uiElement.SetActive(step.targetUIState);
        }
        
        if (!step.waitToFinish)
        {
            if (!string.IsNullOrEmpty(step.jumpToAfterStep)) JumpToStep(step.jumpToAfterStep);
            else RunNextStep();
        }

        yield return new WaitForSeconds(step.activeDuration);
        
        foreach (GameObject uiElement in step.targetUIElements)
        {
            if (uiElement != null) uiElement.SetActive(!step.targetUIState); 
        }

        if (step.waitToFinish)
        {
            if (!string.IsNullOrEmpty(step.jumpToAfterStep)) JumpToStep(step.jumpToAfterStep);
            else RunNextStep();
        }
    }
}