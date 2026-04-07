using UnityEngine;
using UnityEngine.Events; // Needed for the UnityEvent system

public class WorldTrigger : MonoBehaviour
{
    [Header("Settings")]
    public string triggerName = "New Trigger";
    public bool triggerOnlyOnce = false;
    
    [Header("Events")]
    public UnityEvent onTriggerEnter;

    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the object entering has the "Player" tag
        if (other.CompareTag("Player"))
        {
            if (triggerOnlyOnce && hasTriggered) return;

            Debug.Log($"<color=green>Trigger Activated:</color> {triggerName}");
            
            // This runs whatever function you've plugged into the Inspector
            onTriggerEnter.Invoke();
            
            hasTriggered = true;
        }
    }
}