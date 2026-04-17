using UnityEngine;

[CreateAssetMenu(fileName = "NewSkill", menuName = "RPG/SkillData")]
public class SkillData : ScriptableObject
{
    [Header("Basic Info")]
    public string skillName;
    public Sprite skillIcon;

    [Header("Combat Stats")]
    public int baseDamage = 2; 
    public int cooldownTurns;
    
    [Header("Primary Scaling")]
    public StatType primaryStat;
    [Range(0f, 1f)] public float primaryWeight = 1.0f; // 1.0 = 100% of the stat

    [Header("Secondary Scaling (Optional)")]
    public StatType secondaryStat;
    [Range(0f, 1f)] public float secondaryWeight = 0.0f; // 0.0 = Disabled/0%

    [Header("Description")]
    [TextArea(3, 10)]
    public string description;
    
    public string failureEffect; 
}

public enum StatType { Communication, CriticalThinking, Adaptability, EmotionalIntelligence, Sustainability, Leadership }