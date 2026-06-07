using UnityEngine;

public class DayManager : MonoBehaviour
{
    public static DayManager Instance { get; private set; }

    [Header("Dependencies")]
    [SerializeField] private PlayerStats playerPermanentStats;
    [SerializeField] private UIManager mainUI;   
    [SerializeField] private BattleUI battleUI; 

    [Header("Timeline Tracking")]
    public TimeOfDay currentTime = TimeOfDay.Day;
    private bool dayChangeQueued = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        RefreshAllHUDs(); 
    }

    public void RefreshAllHUDs()
    {
        // 1. Tell UIManager to pull the latest values directly from PlayerStats
        if (mainUI != null)
        {
            mainUI.UpdateTimeAndDayDisplay(currentTime);
        }

        // 2. BattleUI keeps its original direct setup
        if (battleUI != null)
        {
            battleUI.SetTimeOfDay(currentTime, playerPermanentStats.currentDay); 
        }
    }

    public void ProcessPostBattleState()
    {
        dayChangeQueued = (currentTime == TimeOfDay.Night);

        currentTime = currentTime switch
        {
            TimeOfDay.Day       => TimeOfDay.Afternoon,
            TimeOfDay.Afternoon => TimeOfDay.Night,
            TimeOfDay.Night     => TimeOfDay.Day,
            _ => TimeOfDay.Day
        };

        RefreshAllHUDs(); 

        if (mainUI != null) mainUI.dialoguePanel.SetActive(true); 

        if (dayChangeQueued && (mainUI == null || !mainUI.dialoguePanel.activeSelf))
        {
            CompleteDayChange();
        }
    }

    public void CompleteDayChange()
    {
        if (!dayChangeQueued) return;
        dayChangeQueued = false; 

        // SAVING DATA DIRECTLY TO SCRIPTABLE OBJECT
        if (playerPermanentStats != null)
        {
            playerPermanentStats.currentDay++;
        }

        RefreshAllHUDs(); 

        ApplyOvernightEffects();

        if (mainUI != null && playerPermanentStats != null)
        {
            mainUI.ShowNewDayTransition(playerPermanentStats.currentDay, TeleportPlayerToStart);
        }
    }

    private void ApplyOvernightEffects()
    {
        Debug.Log($"[DayManager] Resetting stats for Day {playerPermanentStats.currentDay}.");
    }

    private void TeleportPlayerToStart()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null) player.transform.position = Vector3.zero; 
    }
}