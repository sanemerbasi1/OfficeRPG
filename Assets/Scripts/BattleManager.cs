using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.InputSystem;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance;
    public BattleState currentState;
    
    private bool isFirstTurn;

    [Header("Data References")]
    [SerializeField] private CombatData combatData;
    [SerializeField] private PlayerStats playerPermanentStats;
    [SerializeField] private UIManager MainUI;

    [Header("Systems")]
    [SerializeField] private EnemyBattleBrain enemyBrain;

    [Header("UI & Scene References")]
    public BattleUI battleUI;
    public GameObject battleCanvas;
    public GameObject gameOverUI;

    [Header("Grid Units (Assigned Dynamically)")]
    private GridUnit playerGridUnit;
    private GridUnit enemyGridUnit;
    private List<Vector2Int> validMoveTiles = new List<Vector2Int>();

    [Header("Read-Only Live Stats (Inspector Debug)")]
    public int playerCurrentMH;
    public int playerMaxMH;
    public int playerArmor;
    public int playerShield;
    public int enemyCurrentMH, enemyMaxMH, enemyArmor, enemyShield;

    private EncounterData currentEncounter;
    private Action onBattleComplete;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        // Every frame during the player's tactical turn, watch for tile selection clicks
        if (currentState == BattleState.PLAYER_TURN)
        {
            HandlePlayerGridInput();
        }
    }

    private bool isEnemyLocked = false; // Add this field
    public void StartBattle(EncounterData data, GridUnit enemyUnit, Action onComplete)
    {
        CancelInvoke(); 
        Instance = this;
        currentEncounter = data;
        enemyGridUnit = enemyUnit;
        onBattleComplete = onComplete;
        currentState = BattleState.START;

        if (battleUI == null)
        {
            Debug.LogError("[BATTLE MANAGER ERROR] The 'BattleUI' slot is empty on the BattleManager script component!");
            return;
        }

        // Dynamically find the physical actors on the field layout
        LocateGridUnits();
        isEnemyLocked = true;

        if (playerPermanentStats != null)
        {
            battleUI.GenerateSkillButtons(playerPermanentStats.playerSkills);
        }
        else
        {
            Debug.LogError("[BATTLE MANAGER ERROR] 'PlayerPermanentStats' scriptable object reference is missing!");
        }

        if (battleCanvas != null) battleCanvas.SetActive(true);
        if (MainUI != null) MainUI.dialoguePanel.SetActive(false);
        if (battleUI.fullLogPanel != null) battleUI.fullLogPanel.SetActive(false);

        battleUI.ClearLog();
        
        if (DayManager.Instance != null)
        {
            battleUI.SetTimeOfDay(DayManager.Instance.currentTime);
        }
        else
        {
            Debug.LogWarning("[BATTLE MANAGER] DayManager.Instance is NULL. Automatically falling back to local BattleManager time.");
            battleUI.SetTimeOfDay(TimeOfDay.Day);
        }
        
        SetupBattle();
    }

    private void LocateGridUnits()
{
    if (isEnemyLocked) return;
    // Clear references
    playerGridUnit = null;
    enemyGridUnit = null;

    // Use a robust search for all GridUnits
    GridUnit[] allUnits = UnityEngine.Object.FindObjectsByType<GridUnit>(FindObjectsSortMode.None);
    
    foreach (GridUnit unit in allUnits)
    {
        // IMPORTANT: Ensure the unit is part of the battle and not a destroyed ghost
        if (unit == null || !unit.gameObject.activeInHierarchy) continue;

        if (unit.isPlayer)
        {
            playerGridUnit = unit;
        }
        else
        {
            // Specifically look for the enemy that is actually initialized
            enemyGridUnit = unit;
            Debug.Log($"[BATTLE MANAGER] Found active enemy at: {enemyGridUnit.gridPosition}");
        }
    }
}

    private void SetupBattle()
    {
        enemyBrain.InitializeCooldowns(currentEncounter);
        battleUI.UpdateLog(currentEncounter.introText);

        PositionUnitsOnGrid();

        // Calculate and set up focus pools
        int totalSustain = playerPermanentStats.GetTotalStatValue(StatType.Sustainability);
        playerPermanentStats.maxMH = CombatLogic.CalculateMaxMentalHealth(totalSustain, combatData);
        playerArmor = playerPermanentStats.GetTotalStatValue(StatType.EmotionalIntelligence);  

        if (playerPermanentStats.currentMH <= 0)
        {
            playerPermanentStats.currentMH = playerPermanentStats.maxMH;
            playerPermanentStats.currentShield = 0;
        }

        enemyMaxMH = CombatLogic.CalculateMaxMentalHealth(currentEncounter.npcStats.sustainability, combatData);
        enemyCurrentMH = enemyMaxMH;
        enemyArmor = currentEncounter.npcStats.emotionalIntelligence;
        enemyShield = 0;

        // Pass scriptable object profiles into the physical grid token actors to drive dynamic stats
        if (playerGridUnit != null) playerGridUnit.Initialize(playerPermanentStats);
        if (enemyGridUnit != null)  enemyGridUnit.Initialize(currentEncounter.npcStats);

        if (battleUI != null)
        {
            battleUI.SetupBattleUI(playerPermanentStats.playerName, currentEncounter.encounterName, currentEncounter.enemyPortrait);
            UpdateUI();
        }

        int pAdapt = playerPermanentStats.GetTotalStatValue(StatType.Adaptability);
        bool playerFirst = pAdapt >= currentEncounter.npcStats.adaptability;
        battleUI.InitializeTurnDisplay(playerPermanentStats.portrait, currentEncounter.enemyPortrait, playerFirst);
        
        isFirstTurn = true;
        if (playerFirst) StartPlayerTurn();
        else             StartEnemyTurn();
    }

    public void UpdateUI()
    {
        if (playerPermanentStats != null)
        {
            playerCurrentMH = playerPermanentStats.currentMH;
            playerMaxMH = playerPermanentStats.maxMH;
            playerShield = playerPermanentStats.currentShield;
        }

        if (battleUI != null)
        {
            battleUI.UpdateStats(
                playerCurrentMH, playerMaxMH, playerShield, playerArmor,
                enemyCurrentMH, enemyMaxMH, enemyShield, enemyArmor
            );

            // Keep AP layout in sync when UI refresh calls run
            if (playerGridUnit != null)
            {
                battleUI.UpdateAPDisplay(playerGridUnit.currentAP, playerGridUnit.maxAP);
            }
        }
    }

    private void StartPlayerTurn()
    {
        currentState = BattleState.PLAYER_TURN;
        if (!isFirstTurn) battleUI.TickAllCooldowns();
        isFirstTurn = false;
        
        battleUI.ToggleActionButtons(true);
        battleUI.UpdateTurnDisplay("YOUR TURN", Color.green);
        battleUI.AdvanceTurnDisplay(); 

        // Reset your action point budget and refresh tile calculations
        if (playerGridUnit != null)
        {
            playerGridUnit.ResetAP();
            
            if (battleUI != null) battleUI.UpdateAPDisplay(playerGridUnit.currentAP, playerGridUnit.maxAP);
            
            CalculateAndShowWalkableRange();
        }
    }

   private void CalculateAndShowWalkableRange()
{
    if (playerGridUnit == null || enemyGridUnit == null || BattleGrid.Instance == null) return;

    BattleGrid.Instance.ClearAllHighlights();

    // 1. MOVEMENT: Blue Tiles
    validMoveTiles = BattleGrid.Instance.GetReachableTiles(playerGridUnit.gridPosition, playerGridUnit.currentAP);
    BattleGrid.Instance.HighlightMovementTiles(validMoveTiles);

    // 2. ATTACK: Red Tiles
    int distanceToEnemy = playerGridUnit.GetDistanceTo(enemyGridUnit);
    bool canAttackEnemy = false;

    if (playerPermanentStats != null && playerPermanentStats.playerSkills != null)
    {
        foreach (SkillData skill in playerPermanentStats.playerSkills)
        {
            if (skill.type != SkillType.Attack) continue;
            if (battleUI != null && battleUI.IsSkillOnCooldown(skill)) continue;

            // FIX: Ensure range defaults to at least 1 tile if config field reads 0
            int skillRange = skill.attackRange;
            if (skillRange <= 0) skillRange = 1; 
            
            if (distanceToEnemy <= skillRange)
            {
                canAttackEnemy = true;
                break; 
            }
        }
    }

    if (canAttackEnemy)
    {
        List<Vector2Int> enemyTileList = new List<Vector2Int> { enemyGridUnit.gridPosition };
        BattleGrid.Instance.HighlightFightTiles(enemyTileList);
    }
}

  private void HandlePlayerGridInput()
{
    if (Mouse.current == null) return;

    if (Mouse.current.leftButton.wasPressedThisFrame) 
    {
        Debug.Log("[Grid Click] Mouse click detected!"); 

        // 1. Read the 2D screen position (X, Y)
        Vector2 screenPos2D = Mouse.current.position.ReadValue();
        
        // 2. Convert it to a Vector3 and set Z to the absolute distance to your gameplay plane
        Vector3 mouseScreenPosition = new Vector3(screenPos2D.x, screenPos2D.y, Mathf.Abs(Camera.main.transform.position.z));

        // 3. Now translate it safely
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(mouseScreenPosition);
        Vector2Int clickedTile = BattleGrid.Instance.WorldToGrid(mouseWorld);

        Debug.Log($"[Grid Click] Screen Pos: {screenPos2D} | World Pos: {mouseWorld} | Calculated Tile: {clickedTile}");
        
        string validTilesList = string.Join(", ", validMoveTiles);
        Debug.Log($"[Grid Click] Current Valid Tiles: [{validTilesList}]");

        if (validMoveTiles.Contains(clickedTile))
        {
            Debug.Log("[Grid Click] Success! Clicked a valid tile. Moving player...");
            int apCost = BattleGrid.Instance.GetManhattanDistance(playerGridUnit.gridPosition, clickedTile);
            
            playerGridUnit.MoveToGridPosition(clickedTile, apCost);
            BattleGrid.Instance.RegisterUnitPosition(playerGridUnit);
            BattleGrid.Instance.ClearAllHighlights();

            if (battleUI != null) battleUI.UpdateAPDisplay(playerGridUnit.currentAP, playerGridUnit.maxAP);

            // UPDATED: Hand control over to your win/turn manager evaluation sequence
            CheckWinCondition();
        }
        else
        {
            Debug.LogWarning("[Grid Click] Click registered, but this tile is NOT in your validMoveTiles list!");
        }
    } 
}

    public void ExecutePlayerAction(SkillData skill)
    {
        if (currentState != BattleState.PLAYER_TURN || playerGridUnit == null) return;

        // --- GRID COMBAT AP ACTION CHECK ---
        int baseActionCost = 1; 
        if (playerGridUnit.currentAP < baseActionCost)
        {
            battleUI.UpdateLog("<color=orange>Not enough Action Points remaining to act!</color>");
            return;
        }

        battleUI.ToggleActionButtons(false);
        battleUI.NotifySkillUsed(skill);
        playerGridUnit.UseAP(baseActionCost); // Subtract the tactical cost
        
        // Update AP UI text right after spending point on an action card
        if (battleUI != null) battleUI.UpdateAPDisplay(playerGridUnit.currentAP, playerGridUnit.maxAP);
        
        BattleGrid.Instance.ClearAllHighlights();

        int val1 = playerPermanentStats.GetTotalStatValue(skill.primaryStat);
        int val2 = playerPermanentStats.GetTotalStatValue(skill.secondaryStat);
        int finalValue = CombatLogic.CalculateSkillValue(skill.baseDamageValue, val1, skill.primaryWeight, val2, skill.secondaryWeight, combatData);

        switch (skill.type)
        {
            case SkillType.Attack:
                HandlePlayerAttack(skill, finalValue);
                break;

            case SkillType.Defend:
                playerPermanentStats.currentShield += finalValue;
                battleUI.UpdateLog($"<b>{playerPermanentStats.playerName}</b> uses <b>{skill.skillName}</b>! (+{finalValue} Shield)");
                break;

            case SkillType.Heal:
                playerPermanentStats.currentMH = Mathf.Min(playerPermanentStats.currentMH + finalValue, playerPermanentStats.maxMH);
                battleUI.UpdateLog($"<b>{playerPermanentStats.playerName}</b> uses <b>{skill.skillName}</b> to recover focus! (+{finalValue} MH)");
                break;
        }

        UpdateUI();
        CheckWinCondition();
    }

    private void HandlePlayerAttack(SkillData skill, int damage)
{
    if (playerGridUnit == null || enemyGridUnit == null || BattleGrid.Instance == null) return;

    // FIX: Pull directly from the SkillData asset config rather than a hardcoded 2
    int currentDistance = playerGridUnit.GetDistanceTo(enemyGridUnit);
    int skillRange = skill.attackRange; 

    if (currentDistance > skillRange)
    {
        battleUI.UpdateLog($"<b>{skill.skillName}</b> failed! Target is out of range. (Distance: {currentDistance}/{skillRange})");
        return;
    }

    int pAdapt = playerPermanentStats.GetTotalStatValue(StatType.Adaptability);
    bool hit = CombatLogic.CheckIfHit(pAdapt, currentEncounter.npcStats.adaptability, combatData);

    if (hit)
    {
        battleUI.UpdateLog($"<b>{playerPermanentStats.playerName}</b> used <b>{skill.skillName}</b>!");
        CombatLogic.ProcessDamage(damage, false, combatData, ref enemyCurrentMH, ref enemyArmor, ref enemyShield);
    }
    else
    {
        battleUI.UpdateLog($"<b>{currentEncounter.encounterName}</b> has dodged!");
    }
}

    public void EndTurnButtonPressed()
    {
        if (currentState == BattleState.PLAYER_TURN)
        {
            BattleGrid.Instance.ClearAllHighlights();
            StartEnemyTurn();
        }
    }

    private void StartEnemyTurn()
    {
        currentState = BattleState.ENEMY_TURN;
        enemyBrain.TickCooldowns();
        battleUI.ToggleActionButtons(false);
        battleUI.UpdateTurnDisplay("ENEMY TURN", Color.red);
        battleUI.AdvanceTurnDisplay(); 
        battleUI.UpdateLog($"<b>{currentEncounter.encounterName}</b> is thinking...");
        
        if (enemyGridUnit != null) enemyGridUnit.ResetAP();
        
        Invoke("ExecuteEnemyAction", combatData.enemyActionDelay);
    }

    private void ExecuteEnemyAction()
    {
        int playerAdapt = playerPermanentStats.GetTotalStatValue(StatType.Adaptability); 

        EnemyActionResult action = enemyBrain.DecideAction(
            currentEncounter, combatData,
            enemyCurrentMH, enemyMaxMH,
            playerPermanentStats.currentMH, playerPermanentStats.maxMH,
            playerAdapt 
        );

        battleUI.UpdateLog(action.logMessage);

        if (action.hit)
        {
            switch (action.skillUsed != null ? action.skillUsed.type : SkillType.Attack)
            {
                case SkillType.Attack:
                    CombatLogic.ProcessDamage(action.value, false, combatData, ref playerPermanentStats.currentMH, ref playerArmor, ref playerPermanentStats.currentShield);
                    break;

                case SkillType.Defend:
                    enemyShield += action.value;
                    break;

                case SkillType.Heal:
                    enemyCurrentMH = Mathf.Min(enemyCurrentMH + action.value, enemyMaxMH);
                    break;
            }
        }

        UpdateUI();
        CheckWinCondition();
    }

    private void CheckWinCondition()
    {
        if (enemyCurrentMH <= 0) EndBattle(BattleState.WON);
        else if (playerPermanentStats.currentMH <= 0) EndBattle(BattleState.LOST);
        else
        {
            // If the player still has AP available, don't force turn progression automatically
            if (currentState == BattleState.PLAYER_TURN && playerGridUnit.currentAP > 0)
            {
                CalculateAndShowWalkableRange();
                battleUI.ToggleActionButtons(true);
            }
            else
            {
                if (currentState == BattleState.PLAYER_TURN) StartEnemyTurn();
                else StartPlayerTurn();
            }
        }
    }

    private void EndBattle(BattleState result)
    {
        currentState = result;
        if (result == BattleState.WON)
        {
            battleUI.UpdateLog("Success! You handled the situation.");
            Invoke("FinishBattle", 2f);
        }
        else
        {
            battleUI.UpdateLog("You've reached your burnout limit...");
            if (gameOverUI != null) gameOverUI.SetActive(true);
            MainUI.TogglePlayerMovement(true);
        }
    }

    private void FinishBattle()
    {
        if (battleCanvas != null) battleCanvas.SetActive(false);

        if (DayManager.Instance != null)
        {
            DayManager.Instance.ProcessPostBattleState();
        }

        onBattleComplete?.Invoke();
    }

    public void ForceEndBattle()
    {
        if (battleCanvas != null) battleCanvas.SetActive(false);
        currentState = BattleState.START;
    }

    private void PositionUnitsOnGrid()
    {
        if (BattleGrid.Instance == null)
        {
            Debug.LogError("[BATTLE MANAGER] Cannot position units because BattleGrid.Instance is missing!");
            return;
        }

        // --- FIXED: THE GRID IS STATIC, DO NOT MOVE BattleGrid.Instance.transform.position ---

        // 1. Handle the player's dynamic positioning based on the existing grid
        if (playerGridUnit != null)
        {
            // Read exactly where the player is currently standing in world space relative to the scene's static grid
            Vector2Int playerSnappedTile = BattleGrid.Instance.WorldToGrid(playerGridUnit.transform.position);
            
            playerGridUnit.gridPosition = playerSnappedTile;
            playerGridUnit.SnapToGridPosition(playerSnappedTile); 
            BattleGrid.Instance.RegisterUnitPosition(playerGridUnit);
        }

        // 2. Handle the enemy's dynamic positioning based on the existing grid
        if (enemyGridUnit != null)
        {
            // Convert the enemy's current free-roam position to the closest cell coordinate
            Vector2Int enemySnappedTile = BattleGrid.Instance.WorldToGrid(enemyGridUnit.transform.position);

            // Safety check: Prevent them from pinning down on the exact same tile if they overlapped in free-roam
            if (playerGridUnit != null && enemySnappedTile == playerGridUnit.gridPosition)
            {
                enemySnappedTile += Vector2Int.right; 
            }

            enemyGridUnit.gridPosition = enemySnappedTile;
            enemyGridUnit.SnapToGridPosition(enemySnappedTile);
            BattleGrid.Instance.RegisterUnitPosition(enemyGridUnit);
        }
    }
}