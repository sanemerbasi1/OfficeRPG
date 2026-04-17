using UnityEngine;
using System;
using System.Collections.Generic;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance;

    public enum BattleState { START, PLAYER_TURN, ENEMY_TURN, WON, LOST }
    public BattleState currentState;

    [Header("Data References")]
    [SerializeField] private PlayerStats playerPermanentStats; 

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

    // Called by WorldTrigger
    public void StartBattle(EncounterData data, Action onComplete)
    {
        currentEncounter = data;
        onBattleComplete = onComplete;
        currentState = BattleState.START;

        SetupBattle();
    }

    private void SetupBattle()
    {
        // 1. Initialize Player
        playerMaxMH = CombatLogic.CalculateMaxMentalHealth(playerPermanentStats.sustainability);
        playerCurrentMH = playerMaxMH;
        playerArmor = playerPermanentStats.emotionalIntelligence;
        playerShield = 0;

        // 2. Initialize Enemy
        enemyMaxMH = CombatLogic.CalculateMaxMentalHealth(currentEncounter.npcStats.sustainability);
        enemyCurrentMH = enemyMaxMH;
        enemyArmor = currentEncounter.npcStats.emotionalIntelligence;
        enemyShield = 0;

        Debug.Log($"Battle Started: {currentEncounter.encounterName} ({currentEncounter.type})");
        
        // 3. Initiative Check
        if (playerPermanentStats.adaptability >= currentEncounter.npcStats.adaptability)
            StartPlayerTurn();
        else
            StartEnemyTurn();
    }

    private void StartPlayerTurn()
    {
        currentState = BattleState.PLAYER_TURN;
        // UI.EnableActionButtons(true); // Placeholder for your UI script
    }

    // This is the core logic for your UI Buttons
    public void ExecutePlayerAction(SkillData skill)
    {
        if (currentState != BattleState.PLAYER_TURN) return;

        // 1. Hit Check
        bool hit = CombatLogic.CheckIfHit(playerPermanentStats.adaptability, currentEncounter.npcStats.adaptability);

        if (hit)
        {
            // 2. Multi-Attribute Scaling Logic
            int val1 = GetStatValue(playerPermanentStats, skill.primaryStat);
            int val2 = GetStatValue(playerPermanentStats, skill.secondaryStat);

            // Calculation: (Stat1 * Weight1) + (Stat2 * Weight2)
            float scaledBonus = (val1 * skill.primaryWeight) + (val2 * skill.secondaryWeight);
            
            int finalDamage = CombatLogic.CalculateRawDamage(skill.baseDamage, Mathf.RoundToInt(scaledBonus));

            // 3. Process Damage using our Static CombatLogic
            CombatLogic.ProcessDamage(finalDamage, false, ref enemyCurrentMH, ref enemyArmor, ref enemyShield);
            Debug.Log($"Used {skill.skillName}! Enemy MH now: {enemyCurrentMH}");
        }
        else
        {
            Debug.Log($"{skill.skillName} Missed/Avoided!");
        }

        CheckWinCondition();
    }

    private void StartEnemyTurn()
    {
        currentState = BattleState.ENEMY_TURN;
        Invoke("ExecuteEnemyAction", 1.2f); // Short delay for "thinking"
    }

    private void ExecuteEnemyAction()
    {
        // Simple NPC logic: Uses their best stat (Communication) for a basic attack
        int damage = 2 + currentEncounter.npcStats.communication;
        CombatLogic.ProcessDamage(damage, false, ref playerCurrentMH, ref playerArmor, ref playerShield);
        
        Debug.Log($"NPC attacked! Your MH: {playerCurrentMH}");
        CheckWinCondition();
    }

    private void CheckWinCondition()
    {
        if (enemyCurrentMH <= 0)
        {
            EndBattle(BattleState.WON);
        }
        else if (playerCurrentMH <= 0)
        {
            EndBattle(BattleState.LOST);
        }
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
            Debug.Log("Victory!");
            // Resume WorldTrigger Sequence
            onBattleComplete?.Invoke(); 
        }
        else
        {
            Debug.Log("Defeat... Burnout achieved.");
            // You could add a "Game Over" screen call here
        }
    }

    // Helper to grab the correct integer from the PlayerStats SO
    private int GetStatValue(PlayerStats stats, StatType type)
    {
        return type switch
        {
            StatType.Communication => stats.communication,
            StatType.CriticalThinking => stats.criticalThinking,
            StatType.Adaptability => stats.adaptability,
            StatType.EmotionalIntelligence => stats.emotionalIntelligence,
            StatType.Sustainability => stats.sustainability,
            StatType.Leadership => stats.leadership,
            _ => 0
        };
    }
}