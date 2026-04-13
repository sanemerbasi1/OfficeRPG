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


}