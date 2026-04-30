using UnityEngine;
using System.Collections.Generic;

public class EnemyBattleBrain : MonoBehaviour
{
    // Tracks remaining cooldown turns per skill
    private Dictionary<SkillData, int> skillCooldowns = new Dictionary<SkillData, int>();

    // Call this when battle starts to wipe any leftover cooldown state
    public void InitializeCooldowns(EncounterData encounter)
    {
        skillCooldowns.Clear();
        if (encounter.npcSkills == null) return;

        foreach (SkillData skill in encounter.npcSkills)
            skillCooldowns[skill] = 0;
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
        SkillData chosenSkill = PickSkill(encounter, combatData, enemyCurrentMH, enemyMaxMH, playerCurrentMH, playerMaxMH);

        if (chosenSkill == null)
            return FallbackAction(encounter, combatData);

        PutOnCooldown(chosenSkill);
        return BuildActionResult(chosenSkill, encounter, combatData, playerAdaptability);
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

        // All skills on cooldown → fallback
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
            ? $"{encounter.encounterName} uses {skill.skillName}!"
            : $"{encounter.encounterName} tries {skill.skillName} but misses!",
        SkillType.Defend => $"{encounter.encounterName} braces with {skill.skillName}! (+{value} Shield)",
        SkillType.Heal   => $"{encounter.encounterName} uses {skill.skillName} to recover! (+{value} MH)",
        _                => $"{encounter.encounterName} does something unexpected..."
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
            logMessage = $"{encounter.encounterName} attacks!"
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