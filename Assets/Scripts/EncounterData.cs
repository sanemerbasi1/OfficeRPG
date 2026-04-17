using System.Collections.Generic;
using UnityEngine;
public enum EncounterType { Standard, Elite, Boss, Tutorial, Narrative }

[CreateAssetMenu(fileName = "NewEncounter", menuName = "RPG/EncounterData")]
public class EncounterData : ScriptableObject 
{
    public string encounterName;
    public EncounterType type;
    public PlayerStats npcStats; // Using the same stat SO for NPCs
    public List<SkillData> npcSkills;
    [TextArea] public string introText; // The "Context Phase" from your GDD
}