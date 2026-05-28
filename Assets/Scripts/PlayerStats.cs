using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewPlayerStats", menuName = "RPG/Player Stats")]
public class PlayerStats : ScriptableObject
{
    [Header("Timeline")]
    public int currentDay = 1;

    [Header("Character Info")]
    public string playerName;
    public Sprite portrait;

    [Header("Base Stats")]
    public int communication;
    public int criticalThinking;
    public int adaptability;
    public int emotionalIntelligence;
    public int sustainability;
    public int leadership;

    [Header("Live Combat Stats")]
    public int currentMH;
    public int maxMH;
    public int currentShield;

    [Header("Equipment Slots")]
    public TraitData slot1;
    public TraitData slot2;

    [Header("Ability Book")]
    public List<SkillData> playerSkills = new List<SkillData>();

    public void ResetToDefaults()
    {
        playerName = "Employee";
        communication = 1;
        criticalThinking = 1;
        adaptability = 1;
        emotionalIntelligence = 1;
        sustainability = 1;
        leadership = 1;
        slot1 = null; 
        slot2 = null;
        currentDay = 1;
        
        currentMH = 0; 
        maxMH = 0;
        currentShield = 0;
    }

    public int GetTotalStatValue(StatType type)
    {
        int total = GetBaseStat(type);
        if (slot1 != null) total += GetTraitBonus(slot1, type);
        if (slot2 != null) total += GetTraitBonus(slot2, type);
        return total;
    }

    private int GetBaseStat(StatType type)
    {
        return type switch
        {
            StatType.Communication         => communication,
            StatType.CriticalThinking      => criticalThinking,
            StatType.Adaptability          => adaptability,
            StatType.EmotionalIntelligence => emotionalIntelligence,
            StatType.Sustainability        => sustainability,
            StatType.Leadership            => leadership,
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