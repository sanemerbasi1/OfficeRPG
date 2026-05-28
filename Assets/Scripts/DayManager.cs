using UnityEngine;

public class DayManager : MonoBehaviour
{
    public static DayManager Instance { get; private set; }

    [Header("Dependencies")]
    [SerializeField] private PlayerStats playerPermanentStats;
    [SerializeField] private UIManager mainUI; // Your MainUI script

    [Header("Timeline Tracking")]
    public TimeOfDay currentTime = TimeOfDay.Day;
    private bool dayChangeQueued = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Optional: Uncomment if this manager persists across scenes
            // DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Progresses the time of day after combat. Called by BattleManager.
    /// </summary>
    public void ProcessPostBattleState()
    {
        // 1. Check if the night is ending BEFORE advancing the enum
        dayChangeQueued = (currentTime == TimeOfDay.Night);

        currentTime = currentTime switch
        {
            TimeOfDay.Day       => TimeOfDay.Afternoon,
            TimeOfDay.Afternoon => TimeOfDay.Night,
            TimeOfDay.Night     => TimeOfDay.Day,
            _ => TimeOfDay.Day
        };

        // 2. Refresh the UI elements
        if (mainUI != null)
        {
            mainUI.dialoguePanel.SetActive(true); // Bring back dialogue for post-fight chat
        }

        // Safety Fallback: If no conversation popped up, instantly trigger the roll over
        if (dayChangeQueued && (mainUI == null || !mainUI.dialoguePanel.activeSelf))
        {
            CompleteDayChange();
        }
    }

    /// <summary>
    /// Executes the full day-end sequence. Called when post-battle dialogue finishes.
    /// </summary>
    public void CompleteDayChange()
    {
        if (!dayChangeQueued) return;
        dayChangeQueued = false; // Reset tracking flag

        if (playerPermanentStats != null)
        {
            playerPermanentStats.currentDay++;
        }

        ApplyOvernightEffects();

        if (mainUI != null)
        {
            // Transition screen plays, then teleports the player safely out of sight
            mainUI.ShowNewDayTransition(playerPermanentStats.currentDay, TeleportPlayerToStart);
        }
    }

    private void ApplyOvernightEffects()
    {
        Debug.Log($"[DayManager] Morning of Day {playerPermanentStats.currentDay}. Resetting temporary stats.");
        // This is where we will hook up your future overnight status restorations!
    }

    private void TeleportPlayerToStart()
    {
        Debug.Log("[DayManager] Relocating player to starting zone.");
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            // Reset player coordinates to your level spawn point
            player.transform.position = Vector3.zero; 
        }
    }
}