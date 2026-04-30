using UnityEngine;

[CreateAssetMenu(fileName = "NewSkill", menuName = "RPG/Skill")]
public class SkillData : ScriptableObject
{
    public string skillName;
    public Sprite skillIcon;
    public SkillType type; 
    
    
    [Header("Values")]
    public int baseDamageValue; 
    
    [Header("Scaling")]
    public StatType primaryStat;
    public float primaryWeight = 1.0f;
    public StatType secondaryStat;
    public float secondaryWeight = 0.5f;

    [Header("CooldownTurn")]

    public int cooldownTurns = 0;
}