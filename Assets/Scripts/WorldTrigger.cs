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
                ui.ShowDialogue(step.textContent, step.speakerName, () => RunNextStep());
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
                RunNextStep(); 
                break;

            case StepType.CloseUI:
                ui.CloseDialogue();
                RunNextStep();
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

                        RunNextStep();
                    });
                }
                else
                {
                    Debug.LogWarning($"[TRIGGER: {triggerName}] Battle step reached but no EncounterData assigned!");
                    RunNextStep();
                }
                break;
        }
    }
}