using UnityEngine;
using System.Collections.Generic;

public class EnemyBattleBrain : MonoBehaviour
{
    // Tracks remaining cooldown turns per skill
    private Dictionary<SkillData, int> skillCooldowns = new Dictionary<SkillData, int>();
    
    // Cached reference to this unit's grid movement component
    private GridUnit selfGridUnit;

    private void Awake()
    {
        selfGridUnit = GetComponent<GridUnit>();
    }

    // Call this when battle starts to wipe any leftover cooldown state
    public void InitializeCooldowns(EncounterData encounter)
    {
        skillCooldowns.Clear();
        if (encounter.npcSkills == null) return;

        foreach (SkillData skill in encounter.npcSkills)
            skillCooldowns[skill] = 0;

        // SAFE FIX: Directly assign the AP values to avoid running player-only initialization logic!
        if (selfGridUnit != null && encounter.npcStats != null)
        {
            selfGridUnit.maxAP = encounter.npcStats.actionPoints;
            selfGridUnit.currentAP = selfGridUnit.maxAP;
        }
    }

    // Call this at the start of every enemy turn to tick down all cooldowns
    public void TickCooldowns()
    {
        List<SkillData> keys = new List<SkillData>(skillCooldowns.Keys);
        foreach (SkillData skill in keys)
        {
            if (skillCooldowns[skill] > 0)
                skillCooldowns[skill]--;
        }

        // SAFE FIX: Directly refresh the enemy's AP pool using their maxAP variable
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
        // 1. Locate the Player GridUnit on the battlefield
        GridUnit playerGridUnit = null;
        foreach (var unit in Object.FindObjectsByType<GridUnit>(FindObjectsSortMode.None))
        {
            if (unit.isPlayer)
            {
                playerGridUnit = unit;
                break;
            }
        }

        // 2. Use your exact priority rules to choose the best intent/skill
        SkillData chosenSkill = PickSkill(encounter, combatData, enemyCurrentMH, enemyMaxMH, playerCurrentMH, playerMaxMH);
        
        bool isFallback = (chosenSkill == null);
        bool needsRangeCheck = isFallback || (chosenSkill != null && chosenSkill.type == SkillType.Attack);
        int attackRange = 2; // Default range for standard attacks (can change to fit your balance)

        // 3. SPATIAL GRID CHECK: Only process distance rules if the action is targeted at the player
        if (playerGridUnit != null && selfGridUnit != null && BattleGrid.Instance != null && needsRangeCheck)
        {
            int distanceToPlayer = BattleGrid.Instance.GetManhattanDistance(selfGridUnit.gridPosition, playerGridUnit.gridPosition);

            // If the target is too far away, spend available AP to move closer!
            if (distanceToPlayer > attackRange && selfGridUnit.currentAP > 0)
            {
                MoveCloserToTarget(playerGridUnit.gridPosition);
                // Recalculate true distance after physical token slide animation starts
                distanceToPlayer = BattleGrid.Instance.GetManhattanDistance(selfGridUnit.gridPosition, playerGridUnit.gridPosition);
            }

            // If the enemy exhausted its AP and STILL cannot reach the player, it forfeits the attack to reposition
            if (distanceToPlayer > attackRange)
            {
                return new EnemyActionResult
                {
                    skillUsed = null,
                    value = 0,
                    hit = false,
                    logMessage = $"<b>{encounter.encounterName}</b> spends AP to reposition across the grid, closing in on your position!"
                };
            }
        }

        // 4. EXECUTE COMBAT (In range or self-targeted skill)
        if (isFallback)
            return FallbackAction(encounter, combatData);

        PutOnCooldown(chosenSkill);
        return BuildActionResult(chosenSkill, encounter, combatData, playerAdaptability);
    }

    /// <summary>
    /// TACTICAL PATHFINDING MATH: Chooses the optimal walkable grid tile that gets closest to the player coordinates.
    /// </summary>
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
        // 1. UPDATE THE VARIABLE IMMEDIATELY
        selfGridUnit.gridPosition = bestTile; 
        
        // 2. MOVE THE TOKEN
        selfGridUnit.MoveToGridPosition(bestTile, apCost);
        
        // 3. REGISTER IN DICTIONARY
        BattleGrid.Instance.RegisterUnitPosition(selfGridUnit);
        
        Debug.Log($"[AI] Enemy moved to {bestTile}. Variable updated.");
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

        // Priority 1: Low health → try to find an available heal skill
        if (enemyHealthRatio < combatData.enemyLowHealthThreshold)
        {
            bool notFullHealth = enemyCurrentMH < enemyMaxMH;
            SkillData healSkill = encounter.npcSkills.Find(s => s.type == SkillType.Heal && !IsOnCooldown(s));
            if (healSkill != null && notFullHealth) return healSkill;
        }

        // Priority 2: Player is low → find an available attack skill
        if (playerHealthRatio < combatData.enemyAggressiveThreshold)
        {
            SkillData attackSkill = encounter.npcSkills.Find(s => s.type == SkillType.Attack && !IsOnCooldown(s));
            if (attackSkill != null) return attackSkill;
        }

        // Priority 3: Pick a random skill that is not on cooldown
        List<SkillData> availableSkills = encounter.npcSkills.FindAll(s => !IsOnCooldown(s));
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

    private EnemyActionResult FallbackAction(EncounterData encounter, CombatData combatData)
    {
        int damage = encounter.enemyBaseDamage + encounter.npcStats.communication;
        return new EnemyActionResult
        {
            skillUsed  = null,
            value      = damage,
            hit        = true,
            logMessage = $"<b>{encounter.encounterName}</b> attacks!"
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