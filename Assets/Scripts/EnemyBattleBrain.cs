using UnityEngine;
using System.Collections.Generic;

public class EnemyBattleBrain : MonoBehaviour
{
    private Dictionary<SkillData, int> skillCooldowns = new Dictionary<SkillData, int>();
    private GridUnit selfGridUnit;

    private void Awake()
    {
        // Fallback in case it is attached directly to the prefab
        selfGridUnit = GetComponent<GridUnit>();
    }

    // Explicitly bind the active enemy token from the BattleManager
    public void SetUnitReference(GridUnit unit)
    {
        selfGridUnit = unit;
    }

    public void InitializeCooldowns(EncounterData encounter)
    {
        skillCooldowns.Clear();
        if (encounter.npcSkills == null) return;

        foreach (SkillData skill in encounter.npcSkills)
            skillCooldowns[skill] = 0;

        // Bulletproof AP Initialization
        if (selfGridUnit != null)
        {
            // Try to pull from stats; if it's missing or 0, check the inspector, or default to 3
            if (encounter.npcStats != null && encounter.npcStats.actionPoints > 0)
            {
                selfGridUnit.maxAP = encounter.npcStats.actionPoints;
            }
            else if (selfGridUnit.maxAP <= 0)
            {
                selfGridUnit.maxAP = 3; // Safe default value if everything else is unassigned
            }

            selfGridUnit.currentAP = selfGridUnit.maxAP;
        }
    }

    public void TickCooldowns()
    {
        List<SkillData> keys = new List<SkillData>(skillCooldowns.Keys);
        foreach (SkillData skill in keys)
        {
            if (skillCooldowns[skill] > 0)
                skillCooldowns[skill]--;
        }

        if (selfGridUnit != null)
        {
            selfGridUnit.currentAP = selfGridUnit.maxAP;
        }
    }

    private bool IsOnCooldown(SkillData skill)
    {
        return skillCooldowns.ContainsKey(skill) && skillCooldowns[skill] > 0;
    }

    private void PutOnCooldown(SkillData skill)
    {
        if (skillCooldowns.ContainsKey(skill))
            skillCooldowns[skill] = skill.cooldownTurns;
    }

public EnemyActionResult DecideAction(
        EncounterData encounter, CombatData combatData,
        int enemyCurrentMH, int enemyMaxMH,
        int playerCurrentMH, int playerMaxMH,
        int playerAdaptability)
    {
        // If the enemy has no AP left at the start of this evaluation, force turn end immediately
        if (selfGridUnit == null || selfGridUnit.currentAP <= 0)
        {
            return new EnemyActionResult { isTurnEnd = true, logMessage = "" };
        }

        GridUnit playerGridUnit = null;
        foreach (var unit in Object.FindObjectsByType<GridUnit>(FindObjectsSortMode.None))
        {
            if (unit.isPlayer)
            {
                playerGridUnit = unit;
                break;
            }
        }

        SkillData chosenSkill = PickSkill(encounter, combatData, enemyCurrentMH, enemyMaxMH, playerCurrentMH, playerMaxMH);
        
        bool isFallback = (chosenSkill == null);
        bool needsRangeCheck = isFallback || (chosenSkill != null && chosenSkill.type == SkillType.Attack);
        int attackRange = isFallback ? 1 : chosenSkill.attackRange; 

        // SPATIAL GRID CHECK
        if (playerGridUnit != null && selfGridUnit != null && BattleGrid.Instance != null && needsRangeCheck)
        {
            int distanceToPlayer = BattleGrid.Instance.GetManhattanDistance(selfGridUnit.gridPosition, playerGridUnit.gridPosition);

            if (distanceToPlayer > attackRange)
            {
                if (selfGridUnit.currentAP > 0)
                {
                    MoveCloserToTarget(playerGridUnit.gridPosition);
                    int newDistance = BattleGrid.Instance.GetManhattanDistance(selfGridUnit.gridPosition, playerGridUnit.gridPosition);

                    if (newDistance > attackRange && selfGridUnit.currentAP <= 0)
                    {
                        return new EnemyActionResult
                        {
                            skillUsed = null,
                            value = 0,
                            hit = false,
                            isTurnEnd = true,
                            logMessage = $"<b>{encounter.encounterName}</b> spent its remaining AP trying to reposition!"
                        };
                    }

                    // Return clean movement action (AP subtraction happens inside MoveCloserToTarget)
                    return new EnemyActionResult
                    {
                        skillUsed = null,
                        value = 0,
                        hit = false,
                        isTurnEnd = (selfGridUnit.currentAP <= 0),
                        logMessage = $"<b>{encounter.encounterName}</b> advances across the grid."
                    };
                }
                else
                {
                    return new EnemyActionResult { isTurnEnd = true, logMessage = "" };
                }
            }
        }

        // EXECUTE COMBAT
        if (isFallback)
        {
            if (selfGridUnit.currentAP < 1) 
                return new EnemyActionResult { isTurnEnd = true, logMessage = "" };

            // RESTORED: Spend exactly 1 AP for basic strike
            selfGridUnit.currentAP -= 1; 
            EnemyActionResult fallbackResult = FallbackAction(encounter, combatData, playerAdaptability);
            fallbackResult.isTurnEnd = (selfGridUnit.currentAP <= 0); 
            return fallbackResult;
        }

        // RESTORED: Spend exactly the skill's cost
        selfGridUnit.currentAP -= chosenSkill.actionPointCost;
        PutOnCooldown(chosenSkill);
        
        EnemyActionResult result = BuildActionResult(chosenSkill, encounter, combatData, playerAdaptability);
        result.isTurnEnd = (selfGridUnit.currentAP <= 0); 
        return result;
    }

   private void MoveCloserToTarget(Vector2Int playerGridPos)
   {
        if (BattleGrid.Instance == null || selfGridUnit == null) return;

        List<Vector2Int> walkableTiles = BattleGrid.Instance.GetReachableTiles(selfGridUnit.gridPosition, selfGridUnit.currentAP);
        if (walkableTiles == null || walkableTiles.Count == 0) return;

        Vector2Int bestTile = selfGridUnit.gridPosition;
        int closestDistance = BattleGrid.Instance.GetManhattanDistance(selfGridUnit.gridPosition, playerGridPos);

        foreach (Vector2Int tile in walkableTiles)
        {
            int dist = BattleGrid.Instance.GetManhattanDistance(tile, playerGridPos);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                bestTile = tile;
            }
        }

        int apCost = BattleGrid.Instance.GetManhattanDistance(selfGridUnit.gridPosition, bestTile);
        if (apCost > 0)
        {
            selfGridUnit.gridPosition = bestTile; 
            selfGridUnit.MoveToGridPosition(bestTile, apCost); 
            
            // RESTORED: Deduct exactly the tiles traveled from the brain's pool
            selfGridUnit.currentAP -= apCost; 
            
            BattleGrid.Instance.RegisterUnitPosition(selfGridUnit);
            Debug.Log($"[AI] Enemy moved to {bestTile}. AP Left: {selfGridUnit.currentAP}");
        }
   }

    private SkillData PickSkill(
        EncounterData encounter, CombatData combatData,
        int enemyCurrentMH, int enemyMaxMH,
        int playerCurrentMH, int playerMaxMH)
    {
        if (encounter.npcSkills == null || encounter.npcSkills.Count == 0)
            return null;

        float enemyHealthRatio = (float)enemyCurrentMH / enemyMaxMH;
        float playerHealthRatio = (float)playerCurrentMH / playerMaxMH;

        // Added AP checks so AI only picks what it can afford
        if (enemyHealthRatio < combatData.enemyLowHealthThreshold)
        {
            bool notFullHealth = enemyCurrentMH < enemyMaxMH;
            SkillData healSkill = encounter.npcSkills.Find(s => s.type == SkillType.Heal && !IsOnCooldown(s) && s.actionPointCost <= selfGridUnit.currentAP);
            if (healSkill != null && notFullHealth) return healSkill;
        }

        if (playerHealthRatio < combatData.enemyAggressiveThreshold)
        {
            SkillData attackSkill = encounter.npcSkills.Find(s => s.type == SkillType.Attack && !IsOnCooldown(s) && s.actionPointCost <= selfGridUnit.currentAP);
            if (attackSkill != null) return attackSkill;
        }

        // Get all available options that fit current AP budget
        List<SkillData> availableSkills = encounter.npcSkills.FindAll(s => !IsOnCooldown(s) && s.actionPointCost <= selfGridUnit.currentAP);
        
        if (enemyCurrentMH >= enemyMaxMH)
        {
            availableSkills.RemoveAll(s => s.type == SkillType.Heal);
        }

        if (availableSkills.Count > 0)
            return availableSkills[Random.Range(0, availableSkills.Count)];

        return null;
    }

    private EnemyActionResult BuildActionResult(SkillData skill, EncounterData encounter, CombatData combatData, int playerAdaptability)
    {
        int val1 = GetEnemyStat(encounter, skill.primaryStat);
        int val2 = GetEnemyStat(encounter, skill.secondaryStat);
        int value = CombatLogic.CalculateSkillValue(skill.baseDamageValue, val1, skill.primaryWeight, val2, skill.secondaryWeight, combatData);

        bool hit = true;
        if (skill.type == SkillType.Attack)
            hit = CombatLogic.CheckIfHit(encounter.npcStats.adaptability, playerAdaptability, combatData); 

        string log = skill.type switch
        {
            SkillType.Attack => hit
                ? $"<b>{encounter.encounterName}</b> uses <b>{skill.skillName}</b>!"
                : $"<b>{encounter.encounterName}</b> tries <b>{skill.skillName}</b> but misses!",
            SkillType.Defend => $"<b>{encounter.encounterName}</b> braces with <b>{skill.skillName}</b>! (+{value} Shield)",
            SkillType.Heal   => $"<b>{encounter.encounterName}</b> uses <b>{skill.skillName}</b> to recover! (+{value} MH)",
            _                => $"<b>{encounter.encounterName}</b> missed!..."
        };

        return new EnemyActionResult
        {
            skillUsed  = skill,
            value      = value,
            hit        = hit,
            logMessage = log
        };
    }

    private EnemyActionResult FallbackAction(EncounterData encounter, CombatData combatData, int playerAdaptability)
    {
        int damage = encounter.enemyBaseDamage + encounter.npcStats.communication;
        bool hit = CombatLogic.CheckIfHit(encounter.npcStats.adaptability, playerAdaptability, combatData);

        string log = hit 
            ? $"<b>{encounter.encounterName}</b> attacks!" 
            : $"<b>{encounter.encounterName}</b> tries to attack but misses!";

        return new EnemyActionResult
        {
            skillUsed  = null,
            value      = hit ? damage : 0,
            hit        = hit,
            logMessage = log
        };
    }

    private int GetEnemyStat(EncounterData encounter, StatType type)
    {
        return type switch
        {
            StatType.Communication         => encounter.npcStats.communication,
            StatType.CriticalThinking      => encounter.npcStats.criticalThinking,
            StatType.Adaptability          => encounter.npcStats.adaptability,
            StatType.EmotionalIntelligence => encounter.npcStats.emotionalIntelligence,
            StatType.Sustainability        => encounter.npcStats.sustainability,
            StatType.Leadership            => encounter.npcStats.leadership,
            _ => 0
        };
    }
}