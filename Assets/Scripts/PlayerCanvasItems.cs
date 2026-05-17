using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class PlayerCanvasItems : MonoBehaviour
{
    public Canvas healthCanvas;

   [Header("Player Visuals")]
    public TextMeshProUGUI playerNameText;
    public Slider playerMHBar;
    public TextMeshProUGUI playerMHValueText; 
    public TextMeshProUGUI playerShieldText; 
    public Slider playerShieldBar;
}
