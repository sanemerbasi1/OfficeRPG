using UnityEngine;
using TMPro;

public class GameActions : MonoBehaviour
{
    [Header("UI References")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;

    public void ShowDialogue(string message)
    {
        if (dialoguePanel != null) 
            dialoguePanel.SetActive(true);

        if (dialogueText != null)
            dialogueText.text = message;

        // --- NEW: Disable Player Movement ---
        TogglePlayerMovement(false);

        UnityEngine.Debug.Log("<color=orange>Dialogue Opened:</color> " + message);
    }

    public void CloseDialogue()
    {
        if (dialoguePanel != null) 
            dialoguePanel.SetActive(false);

        // --- NEW: Enable Player Movement ---
        TogglePlayerMovement(true);
            
        UnityEngine.Debug.Log("Dialogue Closed");
    }

    // Helper method to find the player and toggle their script
    private void TogglePlayerMovement(bool canMove)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // Get the PlayerController script we wrote earlier
            var controller = player.GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.enabled = canMove;
            }
        }
    }
}