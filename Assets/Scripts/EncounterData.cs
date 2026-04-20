using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewEncounter", menuName = "RPG/EncounterData")]
public class EncounterData : ScriptableObject 
{
    [Header("Visuals & Identity")]
    public string encounterName; 
    public Sprite enemySprite;  
    public EncounterType type;

    [Header("NPC Stats & Skills")]
    public PlayerStats npcStats; 
    public List<SkillData> npcSkills;

    [Header("Narrative Context")]
    [TextArea(3, 10)]
    public string introText; 
}