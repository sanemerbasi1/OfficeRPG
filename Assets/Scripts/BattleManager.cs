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
    public GameObject mainCanvas;

    [Header("Grid Units")]
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
        if (currentState == BattleState.PLAYER_TURN)
        {
            HandlePlayerGridInput();
        }
    }

    public void StartBattle(EncounterData data, GridUnit enemyUnit, Action onComplete)
    {
        CancelInvoke(); 
        Instance = this;
        currentEncounter = data;
        onBattleComplete = onComplete;
        currentState = BattleState.START;

        enemyGridUnit = enemyUnit;

        if (battleUI == null)
        {
            Debug.LogError("[BATTLE MANAGER ERROR] The 'BattleUI' slot is empty on the BattleManager script component!");
            return;
        }

        if (PlayerController.Instance != null)
        {
            playerGridUnit = PlayerController.Instance.GetComponent<GridUnit>();
        }

        if (playerPermanentStats != null)
        {
            battleUI.GenerateSkillButtons(playerPermanentStats.playerSkills);
        }
        else
        {
            Debug.LogError("[BATTLE MANAGER ERROR] 'PlayerPermanentStats' reference is missing!");
        }

        // --- CLEANUP: HIDE MAIN UI ---
        if (battleCanvas != null) battleCanvas.SetActive(true);
        if (mainCanvas != null) mainCanvas.SetActive(false); 
        if (battleUI.fullLogPanel != null) battleUI.fullLogPanel.SetActive(false);

        battleUI.ClearLog();
        
        // --- TIME & DAY SETUP ---
        int currentDay = playerPermanentStats != null ? playerPermanentStats.currentDay : 1;

        if (DayManager.Instance != null)
        {
            DayManager.Instance.RefreshAllHUDs();
            battleUI.SetTimeOfDay(DayManager.Instance.currentTime, currentDay);
        }
        else
        {
            battleUI.SetTimeOfDay(TimeOfDay.Day, currentDay); 
        }
        
        SetupBattle();
    }

    private void SetupBattle()
    {
        if (enemyBrain != null)
        {
            enemyBrain.SetUnitReference(enemyGridUnit);
            enemyBrain.InitializeCooldowns(currentEncounter);
        }
        
        battleUI.UpdateLog(currentEncounter.introText);

        PositionUnitsOnGrid();

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

        validMoveTiles = BattleGrid.Instance.GetReachableTiles(playerGridUnit.gridPosition, playerGridUnit.currentAP);
        BattleGrid.Instance.HighlightMovementTiles(validMoveTiles);

        int distanceToEnemy = playerGridUnit.GetDistanceTo(enemyGridUnit);
        bool canAttackEnemy = false;

        if (playerPermanentStats != null && playerPermanentStats.playerSkills != null)
        {
            foreach (SkillData skill in playerPermanentStats.playerSkills)
            {
                if (skill.type != SkillType.Attack) continue;
                if (battleUI != null && battleUI.IsSkillOnCooldown(skill)) continue;

                int skillRange = skill.attackRange <= 0 ? 1 : skill.attackRange;
                
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
            Vector2 screenPos2D = Mouse.current.position.ReadValue();
            Vector3 mouseScreenPosition = new Vector3(screenPos2D.x, screenPos2D.y, Mathf.Abs(Camera.main.transform.position.z));
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(mouseScreenPosition);
            Vector2Int clickedTile = BattleGrid.Instance.WorldToGrid(mouseWorld);

            if (validMoveTiles.Contains(clickedTile))
            {
                int apCost = BattleGrid.Instance.GetManhattanDistance(playerGridUnit.gridPosition, clickedTile);
                
                playerGridUnit.MoveToGridPosition(clickedTile, apCost);
                BattleGrid.Instance.RegisterUnitPosition(playerGridUnit);
                BattleGrid.Instance.ClearAllHighlights();

                if (battleUI != null) battleUI.UpdateAPDisplay(playerGridUnit.currentAP, playerGridUnit.maxAP);

                CheckWinCondition();
            }
        } 
    }

    public void ExecutePlayerAction(SkillData skill)
    {
        if (currentState != BattleState.PLAYER_TURN || playerGridUnit == null) return;

        int baseActionCost = skill.actionPointCost; 
        if (playerGridUnit.currentAP < baseActionCost)
        {
            battleUI.UpdateLog("<color=orange>Not enough Action Points remaining to act!</color>");
            return;
        }

        battleUI.ToggleActionButtons(false);
        battleUI.NotifySkillUsed(skill);
        playerGridUnit.UseAP(baseActionCost);
        
        if (battleUI != null) battleUI.UpdateAPDisplay(playerGridUnit.currentAP, playerGridUnit.maxAP);
        
        BattleGrid.Instance.ClearAllHighlights();

        int val1 = playerPermanentStats.GetTotalStatValue(skill.primaryStat);
        int val2 = playerPermanentStats.GetTotalStatValue(skill.secondaryStat);
        int finalValue = CombatLogic.CalculateSkillValue(skill.baseDamageValue, val1, skill.primaryWeight, val2, skill.secondaryWeight, combatData);

        StartCoroutine(PlayerActionSequence(skill, finalValue));
    }

    private IEnumerator PlayerActionSequence(SkillData skill, int finalValue)
    {
        switch (skill.type)
        {
            case SkillType.Attack:
                yield return StartCoroutine(HandlePlayerAttackRoutine(skill, finalValue));
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

    private IEnumerator HandlePlayerAttackRoutine(SkillData skill, int damage)
    {
        if (playerGridUnit == null || enemyGridUnit == null || BattleGrid.Instance == null) yield break;

        int currentDistance = playerGridUnit.GetDistanceTo(enemyGridUnit);
        int skillRange = skill.attackRange; 

        if (currentDistance > skillRange)
        {
            battleUI.UpdateLog($"<b>{skill.skillName}</b> failed! Target is out of range. (Distance: {currentDistance}/{skillRange})");
            yield break;
        }

        int pAdapt = playerPermanentStats.GetTotalStatValue(StatType.Adaptability);
        bool hit = CombatLogic.CheckIfHit(pAdapt, currentEncounter.npcStats.adaptability, combatData);

        if (hit)
        {
            battleUI.UpdateLog($"<b>{playerPermanentStats.playerName}</b> used <b>{skill.skillName}</b>!");
            
            yield return StartCoroutine(AnimateAttackBump(playerGridUnit.transform, enemyGridUnit.transform));

            CombatLogic.ProcessDamage(damage, false, combatData, ref enemyCurrentMH, ref enemyArmor, ref enemyShield);
        }
        else
        {
            battleUI.UpdateLog($"<b>{currentEncounter.encounterName}</b> has dodged!");
            yield return StartCoroutine(AnimateAttackBump(playerGridUnit.transform, enemyGridUnit.transform));
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
        
        StartCoroutine(EnemyTurnLoopRoutine());
    }

    private IEnumerator EnemyTurnLoopRoutine()
    {
        yield return new WaitForSeconds(combatData.enemyActionDelay);

        bool parsingTurn = true;
        int safetyLoopGuard = 0; 

        while (parsingTurn && currentState == BattleState.ENEMY_TURN && safetyLoopGuard < 10)
        {
            safetyLoopGuard++;
            int playerAdapt = playerPermanentStats.GetTotalStatValue(StatType.Adaptability); 

            EnemyActionResult action = enemyBrain.DecideAction(
                currentEncounter, combatData,
                enemyCurrentMH, enemyMaxMH,
                playerPermanentStats.currentMH, playerPermanentStats.maxMH,
                playerAdapt 
            );

            if (action.isTurnEnd && string.IsNullOrEmpty(action.logMessage) && action.skillUsed == null && !action.hit)
            {
                parsingTurn = false;
                break;
            }

            if (!string.IsNullOrEmpty(action.logMessage)) 
                battleUI.UpdateLog(action.logMessage);

            if (action.hit)
            {
                SkillType actionType = action.skillUsed != null ? action.skillUsed.type : SkillType.Attack;

                if (actionType == SkillType.Attack)
                {
                    yield return StartCoroutine(AnimateAttackBump(enemyGridUnit.transform, playerGridUnit.transform));
                    CombatLogic.ProcessDamage(action.value, false, combatData, ref playerPermanentStats.currentMH, ref playerArmor, ref playerPermanentStats.currentShield);
                }
                else if (actionType == SkillType.Defend)
                {
                    enemyShield += action.value;
                }
                else if (actionType == SkillType.Heal)
                {
                    enemyCurrentMH = Mathf.Min(enemyCurrentMH + action.value, enemyMaxMH);
                }
            }
            else if (action.skillUsed != null && action.skillUsed.type == SkillType.Attack)
            {
                yield return StartCoroutine(AnimateAttackBump(enemyGridUnit.transform, playerGridUnit.transform));
            }

            UpdateUI();

            if (enemyCurrentMH <= 0 || playerPermanentStats.currentMH <= 0)
            {
                parsingTurn = false;
                break;
            }

            if (action.isTurnEnd)
            {
                parsingTurn = false;
                break;
            }

            yield return new WaitForSeconds(combatData.enemyActionDelay);
        }

        CheckWinCondition();
    }

    private IEnumerator AnimateAttackBump(Transform attacker, Transform target)
    {
        Vector3 originalPosition = attacker.position;
        Vector3 direction = (target.position - attacker.position).normalized;
        Vector3 peakBumpPosition = originalPosition + (direction * 0.5f); 

        float elapsedTime = 0f;
        float animationSpeed = 0.12f; 

        while (elapsedTime < animationSpeed)
        {
            attacker.position = Vector3.Lerp(originalPosition, peakBumpPosition, elapsedTime / animationSpeed);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        attacker.position = peakBumpPosition;

        yield return new WaitForSeconds(0.04f); 

        elapsedTime = 0f;
        while (elapsedTime < animationSpeed)
        {
            attacker.position = Vector3.Lerp(peakBumpPosition, originalPosition, elapsedTime / animationSpeed);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        attacker.position = originalPosition;
    }

    private void CheckWinCondition()
    {
        if (enemyCurrentMH <= 0) EndBattle(BattleState.WON);
        else if (playerPermanentStats.currentMH <= 0) EndBattle(BattleState.LOST);
        else
        {
            if (currentState == BattleState.PLAYER_TURN)
            {
                if (playerGridUnit.currentAP > 0)
                {
                    CalculateAndShowWalkableRange();
                    battleUI.ToggleActionButtons(true);
                }
                else
                {
                    StartEnemyTurn();
                }
            }
            else if (currentState == BattleState.ENEMY_TURN)
            {
                StartPlayerTurn();
            }
        }
    }

    private void EndBattle(BattleState result)
    {
        StopAllCoroutines(); 
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
            if (mainCanvas != null) mainCanvas.SetActive(true); // Bring UI back for movement/gameover
            MainUI.TogglePlayerMovement(true);
        }
    }

    private void FinishBattle()
    {
        if (battleCanvas != null) battleCanvas.SetActive(false);
        if (mainCanvas != null) mainCanvas.SetActive(true); // <--- RESTORE MAIN UI

        if (DayManager.Instance != null)
        {
            DayManager.Instance.ProcessPostBattleState();
        }

        onBattleComplete?.Invoke();
    }

    public void ForceEndBattle()
    {
        if (battleCanvas != null) battleCanvas.SetActive(false);
        if (mainCanvas != null) mainCanvas.SetActive(true); // <--- RESTORE MAIN UI
        currentState = BattleState.START;
    }

    private void PositionUnitsOnGrid()
    {
        if (BattleGrid.Instance == null) return;

        if (playerGridUnit != null)
        {
            Vector2Int playerSnappedTile = BattleGrid.Instance.WorldToGrid(playerGridUnit.transform.position);
            playerGridUnit.gridPosition = playerSnappedTile;
            playerGridUnit.SnapToGridPosition(playerSnappedTile); 
            BattleGrid.Instance.RegisterUnitPosition(playerGridUnit);
        }

        if (enemyGridUnit != null)
        {
            Vector2Int enemySnappedTile = BattleGrid.Instance.WorldToGrid(enemyGridUnit.transform.position);

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