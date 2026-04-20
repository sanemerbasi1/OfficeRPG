using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewPlayerStats", menuName = "RPG/Player Stats")]
public class PlayerStats : ScriptableObject
{
    [Header("Character Info")]
    public string playerName;

    [Header("Base Stats")]
    public int communication;
    public int criticalThinking;
    public int adaptability;
    public int emotionalIntelligence;
    public int sustainability;
    public int leadership;

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
        //playerSkills.Clear();
    }
}