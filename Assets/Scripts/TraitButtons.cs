using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Outline))] // This ensures the button HAS an outline
public class TraitButton : MonoBehaviour
{
    public TraitData traitData;
    public TextMeshProUGUI nameText;
    public Image iconDisplay;

    private Outline outlineEffect;
    private UIManager uiManager;
    private CharCreationManager charMaster;
    private bool isSelected = false;

    [Header("Outline Settings")]
    public Color selectedColor = Color.green;
    public Color defaultColor = new Color(0, 0, 0, 0.5f); // Semi-transparent black

    void Awake()
    {
        uiManager = Object.FindFirstObjectByType<UIManager>();
        charMaster = Object.FindFirstObjectByType<CharCreationManager>();
        outlineEffect = GetComponent<Outline>();

        // Set default state
        outlineEffect.effectColor = defaultColor;
        outlineEffect.enabled = true; // Keep it on, or set to false if you want it hidden

        if (traitData != null)
        {
            if (nameText != null) nameText.text = traitData.traitName;
            if (iconDisplay != null) iconDisplay.sprite = traitData.traitIcon;
        }
    }

    public void OnTraitButtonClick()
    {
        if (isSelected) Deselect();
        else if (charMaster.traitPointsRemaining > 0) Select();
    }

    private void Select()
    {
        isSelected = true;
        outlineEffect.effectColor = selectedColor;
        outlineEffect.effectDistance = new Vector2(1, -1); // Make it thicker when selected
        
        charMaster.SpendTraitPoint();
        uiManager.AddTraitToStats(traitData);
    }

    private void Deselect()
    {
        isSelected = false;
        outlineEffect.effectColor = defaultColor;
        outlineEffect.effectDistance = new Vector2(1, -1); // Back to thin
        
        charMaster.RefundTraitPoint();
        uiManager.RemoveTraitFromStats(traitData);
    }
}