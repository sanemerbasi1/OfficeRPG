using UnityEngine;
using System.Collections;
using System;

public enum TimeOfDay { Day, Afternoon, Night }

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

    [Header("Animation")]
    public RectTransform playerTransform;
    public RectTransform enemyTransform;
    [SerializeField] private float attackMoveSpeed = 8f;
    [SerializeField] private float attackMoveDistance = 1.5f;

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

    public void StartBattle(EncounterData data, Action onComplete)
{
    CancelInvoke(); 
    Instance = this;
    currentEncounter = data;
    onBattleComplete = onComplete;
    currentState = BattleState.START;

    // Safety Check: Make sure we actually have a BattleUI script linked
    if (battleUI == null)
    {
        Debug.LogError("[BATTLE MANAGER ERROR] The 'BattleUI' slot is empty on the BattleManager script component!");
        return;
    }

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
    
    // --- UPDATED SMART FALLBACK LOGIC ---
    if (DayManager.Instance != null)
    {
        // Perfect scenario: Use global time
        battleUI.SetTimeOfDay(DayManager.Instance.currentTime);
    }
    else
    {
        // Fallback scenario: DayManager is disconnected/missing, use local time so the UI still works!
        Debug.LogWarning("[BATTLE MANAGER] DayManager.Instance is NULL. Automatically falling back to local BattleManager time.");
        battleUI.SetTimeOfDay(TimeOfDay.Day);
    }
    
    SetupBattle();
}

    private void SetupBattle()
    {
        enemyBrain.InitializeCooldowns(currentEncounter);
        
        // 1. Initial Narrative Log
        battleUI.UpdateLog(currentEncounter.introText);

        // 2. Initialize Player Stats via ScriptableObject
        int totalSustain = playerPermanentStats.GetTotalStatValue(StatType.Sustainability);
        playerPermanentStats.maxMH = CombatLogic.CalculateMaxMentalHealth(totalSustain, combatData);
        playerArmor = playerPermanentStats.GetTotalStatValue(StatType.EmotionalIntelligence);  

        // If it's a fresh game run or player recovered from 0, set up defaults
        if (playerPermanentStats.currentMH <= 0)
        {
            playerPermanentStats.currentMH = playerPermanentStats.maxMH;
            playerPermanentStats.currentShield = 0;
        }

        // 3. Initialize Enemy Stats
        enemyMaxMH = CombatLogic.CalculateMaxMentalHealth(currentEncounter.npcStats.sustainability, combatData);
        enemyCurrentMH = enemyMaxMH;
        enemyArmor = currentEncounter.npcStats.emotionalIntelligence;
        enemyShield = 0;

        if (battleUI != null)
        {
            battleUI.SetupBattleUI(playerPermanentStats.playerName, currentEncounter.encounterName, currentEncounter.enemyPortrait);
            UpdateUI();
        }

        // 5. Initiative
        int pAdapt = playerPermanentStats.GetTotalStatValue(StatType.Adaptability);
        bool playerFirst = pAdapt >= currentEncounter.npcStats.adaptability;
        battleUI.InitializeTurnDisplay(playerPermanentStats.portrait, currentEncounter.enemyPortrait, playerFirst);
        
        isFirstTurn = true;
        if (playerFirst) StartPlayerTurn();
        else             StartEnemyTurn();
    }

    public void UpdateUI()
    {
        // Synchronize values to inspector fields for real-time tracking
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
    }

    public void ExecutePlayerAction(SkillData skill)
    {
        Debug.Log($"[BATTLE DIAGNOSTIC] Skill Clicked: {skill.skillName} | Current State: {currentState}");
        if (currentState != BattleState.PLAYER_TURN) return;
        battleUI.ToggleActionButtons(false);
        battleUI.NotifySkillUsed(skill);

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
        
        PlaySkillAnimation(true);
    }

    private void StartEnemyTurn()
    {
        currentState = BattleState.ENEMY_TURN;
        enemyBrain.TickCooldowns();
        battleUI.ToggleActionButtons(false);
        battleUI.UpdateTurnDisplay("ENEMY TURN", Color.red);
        battleUI.AdvanceTurnDisplay(); 
        battleUI.UpdateLog($"<b>{currentEncounter.encounterName}</b> is thinking...");
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
                    PlaySkillAnimation(false);
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
            if (currentState == BattleState.PLAYER_TURN) StartEnemyTurn();
            else StartPlayerTurn();
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
        // 1. Pack away the combat scene assets
        if (battleCanvas != null) battleCanvas.SetActive(false);

        // 2. Hand control off to the DayManager to update time, clocks, and day-cycles
        if (DayManager.Instance != null)
        {
            DayManager.Instance.ProcessPostBattleState();
        }

        // 3. Execute core post-battle logic triggers
        onBattleComplete?.Invoke();
    }

    private void PlaySkillAnimation(bool isPlayer)
    {
        if (isPlayer)
            StartCoroutine(MoveAndReturn(playerTransform, attackMoveDistance));   
        else
            StartCoroutine(MoveAndReturn(enemyTransform, -attackMoveDistance));   
    }

    private IEnumerator MoveAndReturn(RectTransform mover, float xOffset)
    {
        Vector2 originalPos = mover.anchoredPosition;
        Vector2 attackPos = originalPos + new Vector2(xOffset, 0f);

        while (Vector2.Distance(mover.anchoredPosition, attackPos) > 0.5f)
        {
            mover.anchoredPosition = Vector2.MoveTowards(mover.anchoredPosition, attackPos, attackMoveSpeed * Time.deltaTime);
            yield return null;
        }

        while (Vector2.Distance(mover.anchoredPosition, originalPos) > 0.5f)
        {
            mover.anchoredPosition = Vector2.MoveTowards(mover.anchoredPosition, originalPos, attackMoveSpeed * Time.deltaTime);
            yield return null;
        }

        mover.anchoredPosition = originalPos;
    }

    public void ForceEndBattle()
    {
        if (battleCanvas != null) battleCanvas.SetActive(false);
        currentState = BattleState.START;
    }
}