using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class NameDisplay : MonoBehaviour
{
    public PlayerStats stats;
    private TextMeshProUGUI textElement;

    void Start()
    {
        textElement = GetComponent<TextMeshProUGUI>();
        UpdateDisplayName();
    }

    public void UpdateDisplayName()
    {
        textElement.text = stats.playerName;
    }
}