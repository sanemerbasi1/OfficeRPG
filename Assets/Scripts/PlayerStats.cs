using UnityEngine;

[CreateAssetMenu(fileName = "NewPlayerStats", menuName = "RPG/Player Stats")]
public class PlayerStats : ScriptableObject
{
    [Header("CharacterStats")]
    public int communication;
    public int criticalThinking;
    public int adaptability;
    public int emotionalIntelligence;
    public int sustainability;
    public int leadership;

    public void ResetToDefaults()
    {
        communication = 1;
        criticalThinking = 1;
        adaptability = 1;
        emotionalIntelligence = 1;
        sustainability = 1;
        leadership = 1;
    }
}