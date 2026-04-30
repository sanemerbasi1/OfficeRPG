using UnityEngine;
using System;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance;

    public BattleState currentState;

    [Header("Data References")]
    [SerializeField] private CombatData combatData;
    [SerializeField] private PlayerStats playerPermanentStats;

    [Header("Systems")]
    [SerializeField] private EnemyBattleBrain enemyBrain;

    [Header("UI & Scene References")]
    public BattleUI battleUI;
    public GameObject battleCanvas;

    [Header("Read-Only Live Stats (Inspector Debug)")]
    public int playerCurrentMH, playerMaxMH, playerArmor, playerShield;
    public int enemyCurrentMH, enemyMaxMH, enemyArmor, enemyShield;

    private EncounterData currentEncounter;
    private Action onBattleComplete;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void StartBattle(EncounterData data, Action onComplete)
    {
        currentEncounter = data;
        onBattleComplete = onComplete;
        currentState = BattleState.START;

        if (battleCanvas != null) battleCanvas.SetActive(true);

        SetupBattle();
    }

    private void SetupBattle()
    {
        enemyBrain.InitializeCooldowns(currentEncounter);
        // 1. Initial Narrative Log
        battleUI.UpdateLog(currentEncounter.introText);

        // 2. Initialize Player Stats
        int totalSustain = GetTotalStatValue(playerPermanentStats, StatType.Sustainability);
        playerMaxMH = CombatLogic.CalculateMaxMentalHealth(totalSustain, combatData);
        playerCurrentMH = playerMaxMH;
        playerArmor = GetTotalStatValue(playerPermanentStats, StatType.EmotionalIntelligence);
        playerShield = 0;

        // 3. Initialize Enemy Stats
        enemyMaxMH = CombatLogic.CalculateMaxMentalHealth(currentEncounter.npcStats.sustainability, combatData);
        enemyCurrentMH = enemyMaxMH;
        enemyArmor = currentEncounter.npcStats.emotionalIntelligence;
        enemyShield = 0;

        // 4. Update UI Visuals
        if (battleUI != null)
        {
            battleUI.SetupBattleUI(playerPermanentStats.playerName, currentEncounter.encounterName, currentEncounter.enemySprite);
            battleUI.GenerateSkillButtons(playerPermanentStats.playerSkills);
            UpdateUI();
        }

        // 5. Initiative
        int pAdapt = GetTotalStatValue(playerPermanentStats, StatType.Adaptability);
        if (pAdapt >= currentEncounter.npcStats.adaptability)
            StartPlayerTurn();
        else
            StartEnemyTurn();
    }

    public void UpdateUI()
    {
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
        battleUI.TickAllCooldowns();
        battleUI.ToggleActionButtons(true);
        battleUI.UpdateTurnDisplay("YOUR TURN", Color.green);
    }

    public void ExecutePlayerAction(SkillData skill)
    {
        if (currentState != BattleState.PLAYER_TURN) return;
        battleUI.ToggleActionButtons(false);
        battleUI.NotifySkillUsed(skill);

        int val1 = GetTotalStatValue(playerPermanentStats, skill.primaryStat);
        int val2 = GetTotalStatValue(playerPermanentStats, skill.secondaryStat);
        int finalValue = CombatLogic.CalculateSkillValue(skill.baseDamageValue, val1, skill.primaryWeight, val2, skill.secondaryWeight, combatData);

        switch (skill.type)
        {
            case SkillType.Attack:
                HandlePlayerAttack(skill, finalValue);
                break;

            case SkillType.Defend:
                playerShield += finalValue;
                battleUI.UpdateLog($"{playerPermanentStats.playerName} uses {skill.skillName}! (+{finalValue} Shield)");
                break;

            case SkillType.Heal:
                playerCurrentMH = Mathf.Min(playerCurrentMH + finalValue, playerMaxMH);
                battleUI.UpdateLog($"{playerPermanentStats.playerName} uses {skill.skillName} to recover focus! (+{finalValue} MH)");
                break;
        }

        UpdateUI();
        CheckWinCondition();
    }

    private void HandlePlayerAttack(SkillData skill, int damage)
    {
        int pAdapt = GetTotalStatValue(playerPermanentStats, StatType.Adaptability);
        bool hit = CombatLogic.CheckIfHit(pAdapt, currentEncounter.npcStats.adaptability, combatData);

        if (hit)
        {
            battleUI.UpdateLog($"{playerPermanentStats.playerName} used {skill.skillName}!");
            CombatLogic.ProcessDamage(damage, false, combatData, ref enemyCurrentMH, ref enemyArmor, ref enemyShield);
        }
        else
        {
            battleUI.UpdateLog("The action was ignored...");
        }
    }

    private void StartEnemyTurn()
{
    currentState = BattleState.ENEMY_TURN;
    enemyBrain.TickCooldowns();
    battleUI.ToggleActionButtons(false);
    battleUI.UpdateTurnDisplay("ENEMY TURN", Color.red);
    battleUI.UpdateLog($"{currentEncounter.encounterName} is thinking...");
    Invoke("ExecuteEnemyAction", combatData.enemyActionDelay);
}

    private void ExecuteEnemyAction()
{
    int playerAdapt = GetTotalStatValue(playerPermanentStats, StatType.Adaptability); 

    EnemyActionResult action = enemyBrain.DecideAction(
        currentEncounter, combatData,
        enemyCurrentMH, enemyMaxMH,
        playerCurrentMH, playerMaxMH,
        playerAdapt 
    );

    battleUI.UpdateLog(action.logMessage);

    if (action.hit)
    {
        switch (action.skillUsed != null ? action.skillUsed.type : SkillType.Attack)
        {
            case SkillType.Attack:
                CombatLogic.ProcessDamage(action.value, false, combatData, ref playerCurrentMH, ref playerArmor, ref playerShield);
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
        else if (playerCurrentMH <= 0) EndBattle(BattleState.LOST);
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
            // TODO: Trigger Game Over UI here
        }
    }

    private void FinishBattle()
    {
        if (battleCanvas != null) battleCanvas.SetActive(false);
        onBattleComplete?.Invoke();
    }

    public int GetTotalStatValue(PlayerStats stats, StatType type)
    {
        int total = GetBaseStat(stats, type);
        if (stats.slot1 != null) total += GetTraitBonus(stats.slot1, type);
        if (stats.slot2 != null) total += GetTraitBonus(stats.slot2, type);
        return total;
    }

    private int GetBaseStat(PlayerStats stats, StatType type)
    {
        return type switch
        {
            StatType.Communication         => stats.communication,
            StatType.CriticalThinking      => stats.criticalThinking,
            StatType.Adaptability          => stats.adaptability,
            StatType.EmotionalIntelligence => stats.emotionalIntelligence,
            StatType.Sustainability        => stats.sustainability,
            StatType.Leadership            => stats.leadership,
            _ => 0
        };
    }

    private int GetTraitBonus(TraitData trait, StatType type)
    {
        return type switch
        {
            StatType.Communication         => trait.CommunicationBonus,
            StatType.CriticalThinking      => trait.CriticalThinkingBonus,
            StatType.Adaptability          => trait.AdaptabilityBonus,
            StatType.EmotionalIntelligence => trait.EmotionalIntelligenceBonus,
            StatType.Sustainability        => trait.SustainabilityBonus,
            StatType.Leadership            => trait.LeadershipBonus,
            _ => 0
        };
    }
}