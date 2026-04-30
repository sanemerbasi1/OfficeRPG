using UnityEngine;

[CreateAssetMenu(fileName = "CombatData", menuName = "RPG/Combat Data")]
public class CombatData : ScriptableObject
{
    [Header("Hit Chance")]
    public float baseHitChance = 0.80f;
    public float hitChancePerAdaptability = 0.05f;
    public float minHitChance = 0.60f;
    public float maxHitChance = 0.95f;

    [Header("Damage")]
    public int minDamage = 1;
    public int minDamageAfterArmor = 1;

    [Header("Mental Health")]
    public int mentalHealthPerSustainability = 3;
}