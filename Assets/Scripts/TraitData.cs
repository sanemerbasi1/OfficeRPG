using UnityEngine;

[CreateAssetMenu(fileName = "NewTrait", menuName = "RPG/TraitData")]
public class TraitData : ScriptableObject
{
    [Header("Basic Info")]
    public string traitName;
    public Sprite traitIcon;

    [Header("Description")]
    [TextArea(3, 10)]
    public string description;

    [Header("Bonuses (Optional)")]
    public int CommunicationBonus;
    public int CriticalThinkingBonus;
    public int AdaptabilityBonus;
    public int EmotionalIntelligenceBonus;
    public int SustainabilityBonus;
    public int LeadershipBonus;
}