using UnityEngine;
using TMPro;

public class GameActions : MonoBehaviour
{
    [Header("Data References")]
    public PlayerStats playerStats; // Drag your PlayerStats SO here
    public TMP_InputField nameInputField; // Drag the Input Field from your NameUI here

    [Header("UI References")]
    public GameObject dialoguePanel;
    public GameObject nameInputPanel;
    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI dialogueText;
    public bool nameInputActivated = false;

    // --- NEW: THE COMBINED FUNCTION ---
    public void ConfirmNameAndClose()
    {
        // 1. Save the name from the Input Field to the ScriptableObject
        if (nameInputField != null && !string.IsNullOrWhiteSpace(nameInputField.text))
        {
            playerStats.playerName = nameInputField.text.Trim();
            UnityEngine.Debug.Log($"<color=green>SUCCESS:</color> Name '{playerStats.playerName}' saved to PlayerStats.");
            
            // 2. Now call the close logic
            CloseDialogue();
        }
        else
        {
            UnityEngine.Debug.LogWarning("Cannot confirm: Name input is empty!");
            // Optional: You could shake the UI or show a "Name Required" message here
        }
    }

    public void ShowDialogue(string message)
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (dialogueText != null) dialogueText.text = message;

        TogglePlayerMovement(false);
        UnityEngine.Debug.Log("<color=orange>Dialogue Opened:</color> " + message);
    }

    public void ShowNameInputPanel(string message)
    {
        if (nameInputActivated == false)
        {
        
        if (nameInputPanel != null) nameInputPanel.SetActive(true);
        if (playerNameText != null) playerNameText.text = message;

        TogglePlayerMovement(false);
        dialoguePanel.SetActive(false);
        UnityEngine.Debug.Log("<color=green>Player Name Input Shown:</color> " + message);

        nameInputActivated = true;
    }
    if (nameInputActivated == true) return;
    }

    public void CloseDialogue()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (nameInputPanel != null) nameInputPanel.SetActive(false);

        TogglePlayerMovement(true);
        UnityEngine.Debug.Log("Dialogue Closed and Player Unfrozen.");
    }

    private void TogglePlayerMovement(bool canMove)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var controller = player.GetComponent<PlayerController>();
            if (controller != null) controller.enabled = canMove;
            
            // Reset velocity so they don't "slide" while talking
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }
    }

}