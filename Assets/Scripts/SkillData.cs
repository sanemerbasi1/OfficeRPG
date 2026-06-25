using UnityEngine;

[CreateAssetMenu(fileName = "NewSkill", menuName = "RPG/Skill")]
public class SkillData : ScriptableObject
{
    public string skillName;
    public Sprite skillIcon;
    public SkillType type; 
    public AudioClip skillSFX;
    
    [Header("Values")]
    public int baseDamageValue; 
    public int actionPointCost = 1;
    public int attackRange = 1;
    
    [Header("Scaling")]
    public StatType primaryStat;
    public float primaryWeight = 1.0f;
    public StatType secondaryStat;
    public float secondaryWeight = 0.5f;

    [Header("CooldownTurn")]
    public int cooldownTurns = 0;

    [Header("Visuals")]
    [Tooltip("Check this if you want this specific skill to use the MC's Special Attack animation!")]
    public bool useSpecialAnimation = false; 
}