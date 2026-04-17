using UnityEngine;

public static class CombatLogic
{
    // --- HIT CHANCE LOGIC ---
    // GDD: Hit Chance = 80% + (Attacker Adaptability − Target Adaptability) × 5%
    // Min 60%, Max 95%
    public static bool CheckIfHit(int attackerAdapt, int targetAdapt)
    {
        float baseHit = 0.80f;
        float differenceMod = (attackerAdapt - targetAdapt) * 0.05f;
        float finalHitChance = Mathf.Clamp(baseHit + differenceMod, 0.60f, 0.95f);

        // Roll a random value between 0 and 1
        return Random.value <= finalHitChance;
    }

    // --- DAMAGE CALCULATION ---
    // GDD: Damage = Base Damage + Stat Scaling + Trait Modifier (Simplified for now)
    // GDD: Minimum Damage = 1
    public static int CalculateRawDamage(int baseDamage, int scalingStatValue, int traitMod = 0)
    {
        int total = baseDamage + scalingStatValue + traitMod;
        return Mathf.Max(1, total); // Ensures damage never goes below 1
    }

    // --- MENTAL HEALTH FORMULA ---
    // GDD: Mental Health = Sustainability × 3
    public static int CalculateMaxMentalHealth(int sustainability)
    {
        return sustainability * 3;
    }

    // --- DAMAGE APPLICATION LOGIC (The Layers) ---
    // GDD Order: Shield → Mental Armor → Mental Health
    // Returns the remaining Health, Armor, and Shield after a hit
    public static void ProcessDamage(int incomingDamage, bool isTrueDamage, 
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
            // Note: Per GDD, Armor reduces damage. 
            // Usually, this means Armor subtracts from the remaining hit.
            if (remainingDamage > 0)
            {
                // Simple reduction logic: Damage = Damage - Armor
                // Ensuring even with high armor, at least 1 damage is dealt if Shield is gone
                int damageAfterArmor = Mathf.Max(1, remainingDamage - currentArmor);
                currentHealth -= damageAfterArmor;
            }
        }

        // Clamp health so it doesn't go negative in the UI
        currentHealth = Mathf.Max(0, currentHealth);
    }
}