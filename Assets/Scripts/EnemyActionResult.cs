public class EnemyActionResult
{
    public SkillData skillUsed;
    public int value;
    public bool hit;
    public string logMessage;
    
    // FIX: Add this line so the BattleManager knows when the enemy's turn is over
    public bool isTurnEnd; 
}