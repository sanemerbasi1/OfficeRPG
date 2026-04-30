using UnityEngine;

public static class CombatLogic
{
    // --- HIT CHANCE LOGIC ---
    // GDD: Hit Chance = 80% + (Attacker Adaptability − Target Adaptability) × 5%
    // Min 60%, Max 95%
    public static bool CheckIfHit(int attackerAdapt, int targetAdapt, CombatData data)
    {
        float differenceMod = (attackerAdapt - targetAdapt) * data.hitChancePerAdaptability;
        float finalHitChance = Mathf.Clamp(data.baseHitChance + differenceMod, data.minHitChance, data.maxHitChance);
        return Random.value <= finalHitChance;
    }

    // --- SKILL VALUE CALCULATION ---
    // GDD: Skill Power = Base Value + (PrimaryStat × PrimaryWeight) + (SecondaryStat × SecondaryWeight)
    // baseValue     → skill.baseValue from SkillData ScriptableObject
    // val1 / val2   → player stats fetched by BattleManager via GetTotalStatValue
    public static int CalculateSkillValue(int baseValue, int val1, float primaryWeight, int val2, float secondaryWeight, CombatData data)
    {
        float scaledBonus = (val1 * primaryWeight) + (val2 * secondaryWeight);
        return CalculateRawDamage(baseValue, Mathf.RoundToInt(scaledBonus), data);
    }

    // --- DAMAGE CALCULATION ---
    // GDD: Damage = Base Damage + Stat Scaling + Trait Modifier (Simplified for now)
    // GDD: Minimum Damage = 1
    // baseDamage       → skill.baseValue from SkillData ScriptableObject
    // scalingStatValue → calculated scaled bonus from CalculateSkillValue
    // traitMod         → will come from TraitSystem, default 0 if no trait applies
    public static int CalculateRawDamage(int baseDamage, int scalingStatValue, CombatData data, int traitMod = 0)
    {
        int total = baseDamage + scalingStatValue + traitMod;
        return Mathf.Max(data.minDamage, total);
    }

    // --- MENTAL HEALTH FORMULA ---
    // GDD: Mental Health = Sustainability × 3
    public static int CalculateMaxMentalHealth(int sustainability, CombatData data)
    {
        return sustainability * data.mentalHealthPerSustainability;
    }

    // --- DAMAGE APPLICATION LOGIC (The Layers) ---
    // GDD Order: Shield → Mental Armor → Mental Health
    public static void ProcessDamage(int incomingDamage, bool isTrueDamage, CombatData data,
                                     ref int currentHealth, ref int currentArmor, ref int currentShield)
    {
        if (isTrueDamage)
        {
            // GDD: True damage ignores Shield and Armor
            currentHealth -= incomingDamage;
        }
        else
        {
            int remainingDamage = incomingDamage;

            // 1. Shield Layer
            if (currentShield > 0)
            {
                int shieldDamage = Mathf.Min(currentShield, remainingDamage);
                currentShield -= shieldDamage;
                remainingDamage -= shieldDamage;
            }

            // 2. Armor Layer (Mental Armor = Emotional Intelligence)
            if (remainingDamage > 0)
            {
                int damageAfterArmor = Mathf.Max(data.minDamageAfterArmor, remainingDamage - currentArmor);
                currentHealth -= damageAfterArmor;
            }
        }

        currentHealth = Mathf.Max(0, currentHealth);
    }
}